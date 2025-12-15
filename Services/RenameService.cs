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

        // 카운터 초기화
        int numCounter = string.IsNullOrWhiteSpace(tagManager.OptionStartValue) ? 1 : (int.TryParse(tagManager.OptionStartValue, out int v) ? v : 1);
        int alphaCounter = 0; // 알파벳 로직은 복잡하므로 일단 단순 카운터로 시작 (A, B, ...)

        // 정규식: 태그 패턴 [Type:...] 또는 [Type] 찾기
        // 예: [Number], [AtoZ], [Name.origin] 등
        // 하지만 사용자가 입력한 포맷은 파싱된 상태가 아니라 [Name.origin]_[Number] 형태임.
        // 태그를 식별하기 위해 정규식 사용
        // 대괄호로 묶인 부분을 찾음
        var regex = new Regex(@"\[(.*?)\]");

        foreach (var item in items)
        {
            string newName = ruleFormat;

            // 1. 고정 태그 처리 (파일마다 값이 다른 것들)
            newName = newName.Replace("[Name.origin]", Path.GetFileNameWithoutExtension(item.OriginalName));
            // [Name.prev]는 Undo 시점에만 유효하거나 히스토리가 있어야 함. 현재는 원본과 동일하게 처리하거나 구현 보류
            newName = newName.Replace("[Name.prev]", Path.GetFileNameWithoutExtension(item.OriginalName));

            // 2. 순차 태그 처리 ([Number], [AtoZ])
            // 정규식으로 매칭하여 하나씩 처리해야 함. 왜냐하면 [Number]가 여러 번 나올 수도 있고, 포맷이 [Number:3:1] 처럼 들어올 수도 있음.
            // 하지만 현재 UI 구조상 TagManager가 옵션을 들고 있고, RuleFormat에는 [Number] 텍스트만 들어감.
            // TagManagerViewModel의 Replace 로직을 보면 ResolvedRuleFormat을 만드는데,
            // 여기서는 FileItem 별로 순차적인 값을 생성해야 하므로 직접 처리해야 함.

            // [Number] 처리
            if (newName.Contains("[Number]"))
            {
                // 옵션 가져오기
                int digits = 0;
                int.TryParse(tagManager.OptionDigits, out digits);

                string numStr = numCounter.ToString().PadLeft(digits, '0');
                newName = newName.Replace("[Number]", numStr);

                // 다음 파일을 위해 증가
                numCounter++;
            }

            // [AtoZ] 처리 (간략화: A, B, ... Z, AA ...)
            if (newName.Contains("[AtoZ]"))
            {
                // 알파벳 로직은 OptionStartValue가 문자일 수 있음. 복잡하므로 여기서는 단순화
                // 실제로는 TagManager의 설정을 따라야 함.
                // 일단은 구현 편의상 스킵하거나 추후 보강
            }

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
        return Regex.Replace(input, Regex.Escape(search), replacement.Replace("$","$$"), RegexOptions.IgnoreCase);
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
