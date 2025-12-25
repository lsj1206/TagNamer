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
    private static readonly Regex _numberTagRegex = new(@"\[Number:(\d+):(\d+)\]", RegexOptions.Compiled);
    private static readonly Regex _atozTagRegex = new(@"\[AtoZ:([a-zA-Z]*):(\d+):(\d+)\]", RegexOptions.Compiled);

    private readonly ISnackbarService _snackbarService;
    private readonly IFileService _fileService;

    public RenameService(IFileService fileService, ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
        _fileService = fileService;
    }

    public void UpdatePreview(IEnumerable<FileItem> items, string ruleFormat, TagManagerViewModel tagManager, bool showExtension)
    {
        if (string.IsNullOrEmpty(ruleFormat))
        {
            foreach (var item in items)
            {
                item.NewName = item.OriginalName;
                item.UpdateDisplay(showExtension);
            }
            return;
        }

        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        string processedFormat = ruleFormat;
        DateTime now = DateTime.Now;

        // 공통 태그 사전 처리
        if (processedFormat.Contains("[Today]", StringComparison.OrdinalIgnoreCase))
        {
            string format = string.IsNullOrWhiteSpace(tagManager.OptionDateFormat) ? "yyyyMMdd" : tagManager.OptionDateFormat;
            string dateStr = now.ToString(ConvertDateFormat(format));
            processedFormat = ReplaceCaseInsensitive(processedFormat, "[Today]", dateStr);
        }

        if (processedFormat.Contains("[Time.now]", StringComparison.OrdinalIgnoreCase))
        {
            string format = string.IsNullOrWhiteSpace(tagManager.OptionDateFormat) ? "HHmmss" : tagManager.OptionDateFormat;
            string timeStr = now.ToString(ConvertDateFormat(format));
            processedFormat = ReplaceCaseInsensitive(processedFormat, "[Time.now]", timeStr);
        }

        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            string newName = processedFormat;

            string fileNameOnly = Path.GetFileNameWithoutExtension(item.OriginalName);
            newName = newName.Replace("[Name.origin]", fileNameOnly);
            newName = newName.Replace("[Name.prev]", fileNameOnly);

            newName = _numberTagRegex.Replace(newName, match =>
            {
                if (!long.TryParse(match.Groups[1].Value, out long startValue) || startValue < 0)
                    startValue = 0;
                if (!int.TryParse(match.Groups[2].Value, out int digits) || digits <= 0)
                    digits = 1;
                return (startValue + i).ToString().PadLeft(digits, '0');
            });

            newName = _atozTagRegex.Replace(newName, match =>
            {
                string startValueStr = match.Groups[1].Value.ToUpper();
                if (string.IsNullOrEmpty(startValueStr)) startValueStr = "A";
                if (!int.TryParse(match.Groups[2].Value, out int digits) || digits <= 0)
                    digits = 1;
                if (!int.TryParse(match.Groups[3].Value, out int lowerCount) || lowerCount < 0)
                    lowerCount = 0;

                if (startValueStr.Length < digits)
                    startValueStr = startValueStr.PadRight(digits, 'A');

                long startNum = AlphaToNum(startValueStr);
                string alphaStr = NumToAlpha(startNum + i);

                if (lowerCount > 0)
                {
                    if (lowerCount >= alphaStr.Length) return alphaStr.ToLower();
                    return alphaStr.Substring(0, alphaStr.Length - lowerCount) +
                           alphaStr.Substring(alphaStr.Length - lowerCount).ToLower();
                }
                return alphaStr;
            });

            if (!item.IsFolder)
                newName += Path.GetExtension(item.OriginalName);

            item.NewName = newName;
            item.UpdateDisplay(showExtension);
        }
    }

    /// <summary>
    /// 배치 이름 변경 작업을 수행하고 통합 보고합니다.
    /// </summary>
    public void ApplyRename(IEnumerable<FileItem> items, bool showExtension)
    {
        int successCount = 0;
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.NewName) || item.OriginalName == item.NewName) continue;

            try
            {
                string newPath = Path.Combine(item.DirectoryName, item.NewName);
                _fileService.RenameFile(item.Path, newPath);

                item.PreviousPath = item.Path;
                item.Path = newPath;
                item.OriginalName = item.NewName;
                item.UpdateDisplay(showExtension);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{item.OriginalName} -> {item.NewName}: {ex.Message}");
            }
        }

        ReportBatchResult("변경 적용", successCount, errors);
    }

    /// <summary>
    /// 배치 이름 복구 작업을 수행하고 통합 보고합니다.
    /// </summary>
    public void UndoRename(IEnumerable<FileItem> items, bool showExtension)
    {
        int successCount = 0;
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.PreviousPath)) continue;

            try
            {
                _fileService.RenameFile(item.Path, item.PreviousPath);

                item.Path = item.PreviousPath;
                item.OriginalName = Path.GetFileName(item.PreviousPath);
                item.PreviousPath = string.Empty;
                item.UpdateDisplay(showExtension);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{item.OriginalName} -> 원본: {ex.Message}");
            }
        }

        ReportBatchResult("변경 취소", successCount, errors);
    }

    /// <summary>
    /// 배치 작업 결과를 분석하여 단 한 번의 스낵바 알림을 보냅니다.
    /// </summary>
    private void ReportBatchResult(string actionName, int successCount, List<string> errors)
    {
        if (successCount == 0 && errors.Count == 0) return;

        if (errors.Count == 0)
        {
            if (actionName == "변경 적용")
                _snackbarService.Show($"{successCount}개의 파일 이름 변경 적용", SnackbarType.Success);
            else if (actionName == "변경 취소")
                _snackbarService.Show($"{successCount}개의 파일 이름 변경 취소합니다.", SnackbarType.Success);
            else
                _snackbarService.Show($"{successCount}개의 항목 {actionName} 완료", SnackbarType.Success);
        }
        else if (successCount > 0)
        {
            _snackbarService.Show($"{successCount}개 성공, {errors.Count}개 실패했습니다.", SnackbarType.Warning);
        }
        else
        {
            string firstError = errors[0];
            if (firstError.Contains("찾을 수 없습니다"))
                _snackbarService.Show("이름 변경 실패 : 원본을 찾을 수 없습니다.", SnackbarType.Error);
            else if (firstError.Contains("존재합니다"))
                _snackbarService.Show("이름 변경 실패 : 변경할 이름이 이미 존재합니다.", SnackbarType.Error);
            else
                _snackbarService.Show($"{errors.Count}개 항목 {actionName} 실패", SnackbarType.Error);
        }
    }

    private string ConvertDateFormat(string input) =>
        input.Replace("YYYY", "yyyy").Replace("YY", "yy").Replace("DD", "dd").Replace("AA", "tt");

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
