using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public partial class TagManagerViewModel : ObservableObject
{
    public ObservableCollection<TagItem> CreatedTags { get; } = new();
    public ObservableCollection<string> TagTypes { get; } = new() { "[Number]", "[AtoZ]", "[Today]", "[Time.now]" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TagTypeDescription))]
    private string selectedTagType;

    public string TagTypeDescription => SelectedTagType switch
    {
        "[Number]" => "규칙대로 순차적으로 증가하는 수를 입력하는 태그",
        "[AtoZ]" => "규칙대로 A-Z 순서로 알파벳을 입력하는 태그\n 시작 값에 숫자를 입력할 경우 Excel 스타일로 변환해서 적용합니다.",
        "[Today]" => "형식에 맞춰 오늘 날짜를 입력하는 태그\n대소문자 구분없이 년:YYYY/YY 월:MM 일:DD",
        "[Time.now]" => "형식에 맞춰 현재 시간을 입력하는 태그\n대소문자 구분없이 시:HH 분:MM 초:SS",
        _ => ""
    };

    [ObservableProperty]
    private string optionDigits = "";

    partial void OnOptionDigitsChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        var numericValue = new string(value.Where(char.IsDigit).ToArray());

        if (int.TryParse(numericValue, out int result))
        {
            if (result > 255) result = 255;
            if (result < 0) result = 0;
            if (OptionDigits != result.ToString()) // 재진입 방지
                OptionDigits = result.ToString();
        }
        else
        {
             OptionDigits = "";
        }
    }

    [ObservableProperty]
    private string optionStartValue = "";

    partial void OnOptionStartValueChanged(string value)
    {
         if (SelectedTagType == "[Number]")
         {
             if (string.IsNullOrEmpty(value)) return;
             var numericValue = new string(value.Where(char.IsDigit).ToArray());
             if(value != numericValue)
             {
                 OptionStartValue = numericValue;
                 return;
             }

             // Number일 경우 시작값은 0 이상
             if (long.TryParse(numericValue, out long result))
             {
                 if (result < 0) OptionStartValue = "0";
             }
         }
         // AtoZ일 경우 문자가능
    }

    [ObservableProperty]
    private string optionDateFormat = "";

    [ObservableProperty]
    private string optionLowerCount = "";

    partial void OnOptionLowerCountChanged(string value)
    {
         if (string.IsNullOrEmpty(value)) return;

        var numericValue = new string(value.Where(char.IsDigit).ToArray());

        if (int.TryParse(numericValue, out int result))
        {
            if (result > 255) result = 255;
            if (result < 0) result = 0;
            OptionLowerCount = result.ToString();
        }
        else
        {
             OptionLowerCount = "0";
        }
    }

    private int _numTagCount = 0;
    private int _atozTagCount = 0;
    private int _todayTagCount = 0;
    private int _nowTagCount = 0;

    public IRelayCommand CreateTagCommand { get; }
    public IRelayCommand<TagItem> DeleteTagCommand { get; }

    public TagManagerViewModel()
    {
        CreateTagCommand = new RelayCommand(CreateTag);
        DeleteTagCommand = new RelayCommand<TagItem>(DeleteTag);

        // 초기 기본 태그 추가
        AddFixedTags();
        SelectedTagType = TagTypes.FirstOrDefault() ?? "[Number]";
    }

    private void DeleteTag(TagItem? item)
    {
        if (item != null && CreatedTags.Contains(item))
        {
            CreatedTags.Remove(item);
        }
    }

    private void AddFixedTags()
    {
        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.origin]",
            Code = "[Name.origin]",
            ToolTip = "파일이 추가될 당시의 파일명을 입력합니다.",
            IsFixed = true
        });

        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.prev]",
            Code = "[Name.prev]",
            ToolTip = "마지막으로 변경된 파일명을 입력합니다.\n처음엔 원본 파일명이 입력됩니다.",
            IsFixed = true
        });
    }

    private void CreateTag()
    {
        if (string.IsNullOrEmpty(SelectedTagType)) return;

        TagItem? newItem = null;

        switch (SelectedTagType)
        {
            case "[Number]":
                _numTagCount++;
                {
                    // 자리수 계산
                    int.TryParse(OptionDigits, out int d);
                    int realDigits = d <= 0 ? 1 : d;
                    string displayDigits = !string.IsNullOrEmpty(OptionDigits)
                        ? (OptionDigits != realDigits.ToString() ? $"{realDigits} ({OptionDigits})" : $"{realDigits}")
                        : $"{realDigits} (비었음)";

                    // 시작값 계산
                    long.TryParse(OptionStartValue, out long s);
                    long realStart = s < 0 ? 0 : s; // 0은 허용
                    string displayStart = !string.IsNullOrEmpty(OptionStartValue)
                        ? (OptionStartValue != realStart.ToString() ? $"{realStart} ({OptionStartValue})" : $"{realStart}")
                        : $"{realStart} (비었음)";
                    if(string.IsNullOrEmpty(OptionStartValue)) { realStart = 1; displayStart = $"{realStart} (비었음)"; }

                    // TagManager는 항상 값을 채워서 보내도록 수정해야 함. Code 생성 시.
                    string codeDigits = realDigits.ToString();
                    string codeStart = string.IsNullOrEmpty(OptionStartValue) ? "1" : realStart.ToString();

                    // 툴팁용은 입력값 위주로 보여주되 보정치 병기
                    if (string.IsNullOrEmpty(OptionDigits)) displayDigits = $"{realDigits} (비었음)";
                    if (string.IsNullOrEmpty(OptionStartValue)) displayStart = $"{realStart} (비었음)";

                     newItem = new TagItem
                    {
                        DisplayName = $"[Number{_numTagCount}]",
                        Code = $"[Number:{codeStart}:{codeDigits}]",
                        ToolTip = $"시작 값 : {displayStart}\n자리 수 : {displayDigits}"
                    };
                    newItem.Options.Add($"시작 값 : {displayStart}");
                    newItem.Options.Add($"자리 수 : {displayDigits}");
                }
                break;
            case "[AtoZ]":
                _atozTagCount++;
                {
                    int.TryParse(OptionDigits, out int d);
                    int realDigits = d <= 0 ? 1 : d;
                    string displayDigits = !string.IsNullOrEmpty(OptionDigits)
                        ? (OptionDigits != realDigits.ToString() ? $"{realDigits}({OptionDigits})" : $"{realDigits}")
                        : $"{realDigits} (비었음)";

                    // AtoZ 시작값 처리 (숫자/알파벳만 유지)
                    // - 모든 비문자/비숫자 제거 (한글/중문/기호 등 제외)
                    // - 숫자 부분은 Excel 컬럼 스타일로 변환 (1->A, 27->AA)
                    // - 알파벳 부분은 그대로 이어 붙임
                    // - 둘 다 없으면 A 로 대체
                    string rawStart = OptionStartValue ?? string.Empty;
                    // 한글/중문 등 비-ASCII 알파벳은 모두 제거하고, ASCII 영문/숫자만 사용
                    string filtered = new string(rawStart.Where(IsAsciiLetterOrDigit).ToArray());

                    string digitsPart = new string(filtered.Where(char.IsDigit).ToArray());
                    string lettersPart = new string(filtered.Where(char.IsLetter).ToArray());

                    string inputFiltered = filtered; // 입력에서 유효문자만 추출한 값 (표시용)
                    string digitsConverted = string.Empty;
                    if (!string.IsNullOrEmpty(digitsPart))
                    {
                        long numericStart = 1;
                        long.TryParse(digitsPart, out numericStart);
                        if (numericStart < 1) numericStart = 1;
                        digitsConverted = NumberToExcelColumn(numericStart);
                    }

                    string combined = $"{digitsConverted}{lettersPart}";

                    string realStart;
                    string displayStart;
                    if (string.IsNullOrEmpty(combined))
                    {
                        realStart = "A";
                        displayStart = "A (비었음)";
                    }
                    else
                    {
                        realStart = combined;
                        // 실제값(입력값) 형식으로 표시
                        displayStart = !string.IsNullOrEmpty(inputFiltered)
                            ? $"{realStart} ({inputFiltered})"
                            : $"{realStart} (비었음)";
                    }

                    // LowerCount
                    int.TryParse(OptionLowerCount, out int l);
                    int realLower = l < 0 ? 0 : l;
                    string displayLower = !string.IsNullOrEmpty(OptionLowerCount)
                        ? (OptionLowerCount != realLower.ToString() ? $"{realLower}({OptionLowerCount})" : $"{realLower}")
                        : $"{realLower} (비었음)";

                    string codeDigits = realDigits.ToString();
                    string codeLower = realLower.ToString();

                    newItem = new TagItem
                    {
                        DisplayName = $"[AtoZ{_atozTagCount}]",
                        Code = $"[AtoZ:{realStart}:{codeDigits}:{codeLower}]",
                        ToolTip = $"시작 값 : {displayStart}\n자리 수 : {displayDigits}\n소문자 수 : {displayLower}"
                    };
                    newItem.Options.Add($"시작 값 : {displayStart}");
                    newItem.Options.Add($"자리 수 : {displayDigits}");
                    newItem.Options.Add($"소문자 수 : {displayLower}");
                }
                break;
            case "[Today]":
                _todayTagCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[Today{_todayTagCount}]",
                    Code = $"[Today:{OptionDateFormat}]",
                    ToolTip = $"{OptionDateFormat}"
                };
                newItem.Options.Add($"형식 : {OptionDateFormat}");
                break;
            case "[Time.now]":
                _nowTagCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[Time.now{_nowTagCount}]",
                    Code = $"[Time.now:{OptionDateFormat}]",
                    ToolTip = $"{OptionDateFormat}"
                };
                newItem.Options.Add($"형식 : {OptionDateFormat}");
                break;
        }

        if (newItem != null)
        {
            CreatedTags.Add(newItem);
        }
    }

    private static bool IsAllDigits(string value)
    {
        return value.All(char.IsDigit);
    }

    private static bool IsAsciiLetterOrDigit(char c)
    {
        return (c >= '0' && c <= '9')
            || (c >= 'A' && c <= 'Z')
            || (c >= 'a' && c <= 'z');
    }

    // Excel 스타일 숫자 -> 알파벳 변환 (1 -> A, 26 -> Z, 27 -> AA ...)
    private static string NumberToExcelColumn(long number)
    {
        if (number < 1) number = 1;
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
