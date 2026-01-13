using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TagNamer.Models;
using TagNamer.ViewModels;

namespace TagNamer.Services;

/// <summary>
/// 파일 및 폴더의 이름 변경 규칙을 처리하는 서비스입니다.
/// 배치 작업 시 발생하는 예외를 수집하여 통합 보고합니다.
/// </summary>
public class RenameService : IRenameService
{
    private readonly ISnackbarService _snackbarService;
    private readonly IFileService _fileService;

    // Regex 객체를 정적 필드로 정의하여 반복 생성 방지 (성능 최적화)
    private static readonly Regex ToUpperRegex = new Regex(@"\[ToUpper\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ToLowerRegex = new Regex(@"\[ToLower\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public enum BatchActionType
    {
        ApplyRename,
        UndoRename,
    }

    public RenameService(IFileService fileService, ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
        _fileService = fileService;
    }

    public void UpdatePreview(IEnumerable<FileItem> items, string ruleFormat, TagManagerViewModel tagManager)
    {
        if (string.IsNullOrEmpty(ruleFormat))
        {
            foreach (var item in items)
            {
                item.NewName = item.BaseName;
            }
            return;
        }

        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        // 1. 변환 태그 플래그 확인 및 순수 포맷 추출
        bool isUpper = ruleFormat.IndexOf("[ToUpper]", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isLower = ruleFormat.IndexOf("[ToLower]", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isOnlyNumber = ruleFormat.IndexOf("[OnlyNumber]", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isOnlyLetter = ruleFormat.IndexOf("[OnlyLetter]", StringComparison.OrdinalIgnoreCase) >= 0;

        string pureFormat = ruleFormat;
        pureFormat = ToUpperRegex.Replace(pureFormat, "");
        pureFormat = ToLowerRegex.Replace(pureFormat, "");
        pureFormat = Regex.Replace(pureFormat, @"\[OnlyNumber\]", "", RegexOptions.IgnoreCase);
        pureFormat = Regex.Replace(pureFormat, @"\[OnlyLetter\]", "", RegexOptions.IgnoreCase);

        // 2. 다른 태그 없이 변환 태그만 있는 경우 [Name.origin]을 기본으로 사용
        if (string.IsNullOrWhiteSpace(pureFormat) && (isUpper || isLower || isOnlyNumber || isOnlyLetter))
        {
            pureFormat = "[Origin]";
        }

        // 3. 치환에 사용될 활성 태그 필터링 (순수 포맷 기준)
        var activeTags = tagManager.CreatedTags
            .Where(t => pureFormat.IndexOf(t.TagName, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        // 날짜/시간 태그 계산 결과를 캐싱하여 반복적인 DateTime 포맷팅을 방지합니다.
        var dateCache = new Dictionary<string, string>();
        DateTime now = DateTime.Now;

        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            string newName = pureFormat;

            foreach (var tag in activeTags)
            {
                string replacement = "";
                switch (tag.Type)
                {
                    case TagType.NameOrigin:
                        replacement = item.BaseName;
                        break;
                    case TagType.Number:
                        if (tag.Params is NumberTagParams numP)
                            replacement = (numP.StartValue + i).ToString().PadLeft(numP.Digits, '0');
                        break;
                    case TagType.AtoZ:
                        if (tag.Params is AtoZTagParams azP)
                        {
                            long startNum = AlphaToNum(azP.StartValue);
                            string alphaStr = NumToAlpha(startNum + i);
                            if (azP.LowerCount > 0)
                            {
                                if (azP.LowerCount >= alphaStr.Length) replacement = alphaStr.ToLower();
                                else replacement = alphaStr.Substring(0, alphaStr.Length - azP.LowerCount) +
                                                 alphaStr.Substring(alphaStr.Length - azP.LowerCount).ToLower();
                            }
                            else replacement = alphaStr;
                        }
                        break;
                    case TagType.Today:
                    case TagType.TimeNow:
                        if (tag.Params is DateTimeTagParams dtP)
                        {
                            if (!dateCache.TryGetValue(tag.TagName, out string? cachedDate))
                            {
                                cachedDate = FormatDateTime(dtP, now, tag.Type);
                                dateCache[tag.TagName] = cachedDate;
                            }
                            replacement = cachedDate;
                        }
                        break;
                    case TagType.OriginSplit:
                        if (tag.Params is OriginSplitTagParams splitP)
                        {
                            string origin = item.BaseName;
                            int length = origin.Length;
                            int start = Math.Max(1, splitP.StartCount);
                            int end = Math.Max(1, splitP.EndCount);
                            if (start > end) (start, end) = (end, start);

                            int startIndex, endIndex;
                            if (splitP.IsFromBack)
                            {
                                endIndex = length - start;
                                startIndex = length - end;
                            }
                            else
                            {
                                startIndex = start - 1;
                                endIndex = end - 1;
                            }

                            if (startIndex < 0) startIndex = 0;
                            if (endIndex >= length) endIndex = length - 1;

                            if (startIndex > endIndex || startIndex >= length || endIndex < 0)
                            {
                                // 선택된 범위가 유효하지 않으면:
                                // 삭제 모드 -> 아무것도 삭제 안 함 (원본 그대로)
                                // 남기기 모드 -> 아무것도 안 남김 (빈 문자열)
                                replacement = splitP.IsKeep ? "" : origin;
                            }
                            else
                            {
                                if (splitP.IsKeep)
                                {
                                    int len = endIndex - startIndex + 1;
                                    replacement = origin.Substring(startIndex, len);
                                }
                                else
                                {
                                    replacement = origin.Remove(startIndex, endIndex - startIndex + 1);
                                }
                            }
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(replacement))
                {
                     newName = ReplaceCaseInsensitive(newName, tag.TagName, replacement);
                }
            }

            // 4. 변환 파이프라인 (Filtering -> Case 순서)
            if (isOnlyNumber) newName = new string(newName.Where(char.IsDigit).ToArray());
            else if (isOnlyLetter) newName = new string(newName.Where(char.IsLetter).ToArray());

            if (isUpper) newName = newName.ToUpper();
            else if (isLower) newName = newName.ToLower();

            item.NewName = newName;
        }
    }

    /// <summary>
    /// 이름 변경 작업을 수행합니다.
    /// </summary>
    public async Task ApplyRenameAsync(IEnumerable<FileItem> items, IProgress<int>? progress = null)
    {
        var targetItems = items.Where(i => i.IsChanged).ToList();
        if (targetItems.Count == 0) return;

        int totalCount = targetItems.Count;
        int successCount = 0;
        var errors = new List<Exception>();

        await Task.Run(() =>
        {
            for (int i = 0; i < totalCount; i++)
            {
                var item = targetItems[i];
                try
                {
                    string newFullName = item.NewName + item.BaseExtension;
                    string newPath = Path.Combine(item.Directory, newFullName);

                    SafeRename(item.Path, newPath);
                    item.PreviousPath = item.Path;
                    item.Path = newPath;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }

                // 10개 단위로 진행률 보고 (UI 부하 감소)
                if (progress != null && (i % 10 == 0 || i == totalCount - 1))
                {
                    progress.Report(i + 1);
                }
            }
        });

        // 결과 스낵바는 UI 스레드에서 안전하게 호출
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ShowRenameSnackbar(BatchActionType.ApplyRename, successCount, errors);
        });
    }

    /// <summary>
    /// 이름 되돌리기 작업을 수행합니다.
    /// </summary>
    public async Task UndoRenameAsync(IEnumerable<FileItem> items, IProgress<int>? progress = null)
    {
        var targetItems = items.Where(i => !string.IsNullOrEmpty(i.PreviousPath)).ToList();
        if (targetItems.Count == 0) return;

        int totalCount = targetItems.Count;
        int successCount = 0;
        var errors = new List<Exception>();

        await Task.Run(() =>
        {
            for (int i = 0; i < totalCount; i++)
            {
                var item = targetItems[i];
                try
                {
                    SafeRename(item.Path, item.PreviousPath);
                    item.Path = item.PreviousPath;
                    item.PreviousPath = string.Empty;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }

                if (progress != null && (i % 10 == 0 || i == totalCount - 1))
                {
                    progress.Report(i + 1);
                }
            }
        });

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ShowRenameSnackbar(BatchActionType.UndoRename, successCount, errors);
        });
    }

    /// <summary>
    /// 이름 변경 작업 결과를 분석하여 단 한 번의 스낵바 알림을 표시합니다.
    /// </summary>
    private void ShowRenameSnackbar(BatchActionType actionType, int successCount, IReadOnlyList<Exception> errors)
    {
        // 아무 일도 안 일어난 경우
        if (successCount == 0 && errors.Count == 0)
            return;
        // 전부 성공
        if (errors.Count == 0)
        {
            _snackbarService.Show(GetSuccessMessage(actionType, successCount), SnackbarType.Success);
            return;
        }
        // 일부 성공
        if (successCount > 0)
        {
            _snackbarService.Show($"{successCount}개 성공, {errors.Count}개 실패했습니다.", SnackbarType.Warning);
            return;
        }
        // 전부 실패
        _snackbarService.Show(GetFailureMessage(actionType, errors), SnackbarType.Error);
    }

    private static string GetSuccessMessage(BatchActionType actionType, int successCount)
    {
        return actionType switch
        {
            BatchActionType.ApplyRename => $"{successCount}개의 이름을 변경합니다.",
            BatchActionType.UndoRename => $"{successCount}개의 이름 변경을 취소합니다.",
            _ => $"{successCount}개의 작업이 완료되었습니다."
        };
    }

    private static string GetFailureMessage(BatchActionType actionType, IReadOnlyList<Exception> errors)
    {
        Exception firstError = errors[0];

        // 파일명/경로가 너무 긴 경우 (HResult: 0x800700CE, 0x8007007B 등)
        uint hr = (uint)firstError.HResult;
        if (firstError is PathTooLongException || hr == 0x800700CE || hr == 0x8007007B)
        {
            return "실패 : 파일명 또는 경로가 너무 깁니다 (최대 255자).";
        }

        return firstError switch
        {
            FileNotFoundException => "실패 : 원본을 찾을 수 없습니다.",
            UnauthorizedAccessException => "실패 : 권한이 없습니다.",
            IOException => "실패 : 이미 존재하거나 사용할 수 없는 이름입니다.",
            _ => $"실패 : {errors.Count}개 작업 도중 오류가 발생했습니다."
        };
    }

    private string FormatDateTime(DateTimeTagParams p, DateTime now, TagType type)
    {
        var result = new System.Text.StringBuilder();

        result.Append(GetPartValue(p.Part1, now, type));
        result.Append(p.Sep1);

        result.Append(GetPartValue(p.Part2, now, type));
        result.Append(p.Sep2);

        result.Append(GetPartValue(p.Part3, now, type));

        return result.ToString();
    }

    private string GetPartValue(string part, DateTime now, TagType type)
    {
        return part switch
        {
            "YY" => now.ToString("yy"),
            "YYYY" => now.ToString("yyyy"),
            "MM" => type == TagType.TimeNow ? now.ToString("mm") : now.ToString("MM"),
            "DD" => now.ToString("dd"),
            "HH" => now.ToString("HH"),
            "SS" => now.ToString("ss"),
            "-" => string.Empty,
            _ => string.Empty
        };
    }

    private string ReplaceCaseInsensitive(string input, string search, string replacement) =>
        Regex.Replace(input, Regex.Escape(search), replacement.Replace("$", "$$"), RegexOptions.IgnoreCase);

    private long AlphaToNum(string column)
    {
        long result = 0;
        foreach (char c in column) { result *= 26; result += c - 'A' + 1; }
        return result;
    }

    private string NumToAlpha(long number)
    {
        string column = "";
        while (number > 0)
        {
            long modulo = (number - 1) % 26;
            column = Convert.ToChar('A' + modulo) + column;
            number = (number - 1) / 26;
        }
        return column;
    }

    /// <summary>
    /// 파일명을 변경합니다. 윈도우의 대소문자 무시 특성을 고려하여 2단계 변경을 지원합니다.
    /// </summary>
    private void SafeRename(string sourcePath, string destPath)
    {
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath)) return;
        if (sourcePath == destPath) return;

        // 대소문자만 다른 경우 (Case-only rename)
        bool isCaseOnlyChange = string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(sourcePath, destPath, StringComparison.Ordinal);

        if (isCaseOnlyChange)
        {
            string tempPath = sourcePath + ".tmp_" + Guid.NewGuid().ToString("N");
            _fileService.RenameFile(sourcePath, tempPath);
            _fileService.RenameFile(tempPath, destPath);
        }
        else
        {
            _fileService.RenameFile(sourcePath, destPath);
        }
    }
}
