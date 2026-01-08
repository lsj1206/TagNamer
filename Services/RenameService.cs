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

        // 입력된 규칙(ruleFormat)에 해당 태그의 TagName이 포함되어 있는지 확인합니다.
        var activeTags = tagManager.CreatedTags
            .Where(t => ruleFormat.IndexOf(t.TagName, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        // 날짜/시간 태그 계산 결과를 캐싱하여 반복적인 DateTime 포맷팅을 방지합니다.
        var dateCache = new Dictionary<string, string>();
        DateTime now = DateTime.Now;

        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            string newName = ruleFormat;

            foreach (var tag in activeTags)
            {
                string replacement = "";
                switch (tag.Type)
                {
                    case TagType.NameOrigin:
                        replacement = item.BaseName;
                        break;
                    case TagType.Number:
                        // 숫자 태그: 시작값 + 인덱스
                        if (tag.Params is NumberTagParams numP)
                            replacement = (numP.StartValue + i).ToString().PadLeft(numP.Digits, '0');
                        break;
                    case TagType.AtoZ:
                        // 알파벳 태그: 알파벳 연산 수행
                        if (tag.Params is AtoZTagParams azP)
                        {
                            long startNum = AlphaToNum(azP.StartValue);
                            string alphaStr = NumToAlpha(startNum + i);

                            // 소문자 변환 옵션 처리
                            if (azP.LowerCount > 0)
                            {
                                if (azP.LowerCount >= alphaStr.Length) replacement = alphaStr.ToLower();
                                else replacement = alphaStr.Substring(0, alphaStr.Length - azP.LowerCount) +
                                                 alphaStr.Substring(alphaStr.Length - azP.LowerCount).ToLower();
                            }
                            else
                            {
                                replacement = alphaStr;
                            }
                        }
                        break;
                    case TagType.Today:
                    case TagType.TimeNow:
                        // 날짜/시간 태그: 캐시된 값 사용 또는 포맷팅 후 캐싱
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
                }

                // 계산된 값으로 태그 치환 (대소문자 무시)
                if (!string.IsNullOrEmpty(replacement))
                {
                     newName = ReplaceCaseInsensitive(newName, tag.TagName, replacement);
                }
            }


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
                    _fileService.RenameFile(item.Path, newPath);

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
                    _fileService.RenameFile(item.Path, item.PreviousPath);

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

        return firstError switch
        {
            FileNotFoundException => "이름 변경 실패 : 원본을 찾을 수 없습니다.",
            UnauthorizedAccessException => "이름 변경 실패 : 권한이 없습니다.",
            IOException => "이름 변경 실패 : 변경할 이름이 이미 존재합니다.",
            _ => $"{errors.Count}개의 작업이 실패했습니다."
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
}
