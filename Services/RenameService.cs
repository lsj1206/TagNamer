using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TagNamer.Models;
using TagNamer.ViewModels;

namespace TagNamer.Services;

public class RenameService : IRenameService
{
    private static readonly Regex _numberTagRegex = new(@"\[Number:(\d+):(\d+)\]", RegexOptions.Compiled);
    private static readonly Regex _atozTagRegex = new(@"\[AtoZ:([a-zA-Z]*):(\d+):(\d+)\]", RegexOptions.Compiled);

    private readonly IFileService _fileService;

    public RenameService(IFileService fileService)
    {
        _fileService = fileService;
    }

    public void UpdatePreview(IEnumerable<FileItem> items, string ruleFormat, TagManagerViewModel tagManager, bool showExtension)
    {
        // 규칙이 없으면 원본 이름 그대로
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

        for (int i = 0; i < itemList.Count; i++)
        {
            var item = itemList[i];
            string newName = ruleFormat;

            // 고정 태그 처리 (파일마다 값이 다른 것들)
            newName = newName.Replace("[Name.origin]", Path.GetFileNameWithoutExtension(item.OriginalName));
            newName = newName.Replace("[Name.prev]", Path.GetFileNameWithoutExtension(item.OriginalName));

            // [Number:시작 값:자리 수]
            newName = _numberTagRegex.Replace(newName, match =>
            {
                // Group[1] = 시작 값, Group[2] = 자리 수
                if (!long.TryParse(match.Groups[1].Value, out long startValue) || startValue < 0)
                    startValue = 0;

                if (!int.TryParse(match.Groups[2].Value, out int digits) || digits <= 0)
                    digits = 1;

                long currentValue = startValue + i;
                return currentValue.ToString().PadLeft(digits, '0');
            });

            // [AtoZ:시작 값:자리 수:소문자 수]
            newName = _atozTagRegex.Replace(newName, match =>
            {
                // Group[1] = 시작 값, Group[2] = 자리 수, Group[3] = 소문자 수
                string startValueStr = match.Groups[1].Value.ToUpper();
                if (string.IsNullOrEmpty(startValueStr)) startValueStr = "A";

                if (!int.TryParse(match.Groups[2].Value, out int digits) || digits <= 0)
                    digits = 1;

                if (!int.TryParse(match.Groups[3].Value, out int lowerCount) || lowerCount < 0)
                    lowerCount = 0;

                // 자리수 보정
                if (startValueStr.Length < digits)
                {
                    startValueStr = startValueStr.PadRight(digits, 'A');
                }

                long startNum = AlphaToNum(startValueStr);
                long currentNum = startNum + i;
                string alphaStr = NumToAlpha(currentNum);

                // 소문자 처리
                if (lowerCount > 0)
                {
                    if (lowerCount >= alphaStr.Length)
                        return alphaStr.ToLower();

                    string upperPart = alphaStr.Substring(0, alphaStr.Length - lowerCount);
                    string lowerPart = alphaStr.Substring(alphaStr.Length - lowerCount).ToLower();
                    return upperPart + lowerPart;
                }

                return alphaStr;
            });

            // 3. 시간/날짜 태그 처리 ([Today], [Time.now])
            // 이것들은 파일마다 변하지 않고 현재 시간 기준임
            if (newName.Contains("[Today]"))
            {
                string format = string.IsNullOrWhiteSpace(tagManager.OptionDateFormat) ? "yyyyMMdd" : tagManager.OptionDateFormat;
                // C# 날짜 포맷과 사용자 입력 포맷 매칭 필요 (YY->yy, DD->dd 등)
                // 대소문자 무시하고 치환하는게 좋음
                string dateStr = DateTime.Now.ToString(ConvertDateFormat(format));
                newName = newName.Replace("[Today]", dateStr);
            }

            if (newName.Contains("[Time.now]"))
            {
                string format = string.IsNullOrWhiteSpace(tagManager.OptionDateFormat) ? "HHmmss" : tagManager.OptionDateFormat;
                string timeStr = DateTime.Now.ToString(ConvertDateFormat(format));
                // 대소문자 주의: TagViewModel에서 [Time.now]로 정의됨
                newName = ReplaceCaseInsensitive(newName, "[Time.now]", timeStr);
            }


            // 확장자 처리
            if (!item.IsFolder)
            {
                newName += Path.GetExtension(item.OriginalName);
            }

            item.NewName = newName;
            item.UpdateDisplay(showExtension); // 확장자 표시 여부는 일단 true로 가정하거나 파라미터로 받아야 함.
                                               // 하지만 여기서는 MainViewModel의 ShowExtension 값을 모르므로,
                                               // FileItem에 ShowExtension 상태를 저장하거나,
                                               // 일단은 NewName 변경 시 UpdateDisplay를 호출.
        }
    }

    private string ConvertDateFormat(string input)
    {
        // 사용자가 YYYY, DD 등을 입력할 수 있으므로 C# 표준 포맷(yyyy, dd)으로 변환
        // 예시: YYYY -> yyyy, DD -> dd
        // 이는 매우 단순화된 로직임. 실제로는 정확한 파싱 필요.
        return input.Replace("YYYY", "yyyy").Replace("YY", "yy")
                    .Replace("DD", "dd")
                    .Replace("AA", "tt"); // 오전/오후
    }

    private string ReplaceCaseInsensitive(string input, string search, string replacement)
    {
        return Regex.Replace(input, Regex.Escape(search), replacement.Replace("$", "$$"), RegexOptions.IgnoreCase);
    }

    private long AlphaToNum(string column)
    {
        long result = 0;
        foreach (char c in column)
        {
            result *= 26;
            result += c - 'A' + 1;
        }
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

    public void ApplyRename(IEnumerable<FileItem> items, bool showExtension)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.NewName) || item.OriginalName == item.NewName) continue;

            string dir = item.DirectoryName;
            string newPath = Path.Combine(dir, item.NewName);

            if (_fileService.RenameFile(item.Path, newPath))
            {
                // 성공 시 상태 업데이트
                item.PreviousPath = item.Path; // 되돌리기를 위해 현재 경로 저장
                item.Path = newPath;
                item.OriginalName = item.NewName; // 이름 변경 완료 처리
                item.UpdateDisplay(showExtension);
                // NewName 초기화? 아니면 유지? -> 유지하는 편이 나음
            }
        }
    }

    public void UndoRename(IEnumerable<FileItem> items, bool showExtension)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.PreviousPath)) continue;

            if (_fileService.RenameFile(item.Path, item.PreviousPath))
            {
                // 성공 시
                item.Path = item.PreviousPath;
                item.Path = item.PreviousPath;
                item.OriginalName = Path.GetFileName(item.PreviousPath);
                item.PreviousPath = string.Empty; // Undo 완료
                item.UpdateDisplay(showExtension);
            }
        }
    }
}
