using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public partial class TagManagerViewModel : ObservableObject
{
    public IRelayCommand CreateTagCommand { get; }
    public IRelayCommand<TagItem> DeleteTagCommand { get; }

    public ObservableCollection<TagItem> CreatedTags { get; } = new();
    // 태그 타입
    public string[] TagTypes { get; } = { "[Number]", "[AtoZ]", "[Name.trim]", "[Today]", "[Time.now]" };
    // 태그 생성 카운트 관리
    private readonly Dictionary<string, int> _tagCounts = new();

    // 날짜/시간 드롭다운 항목
    public string[] DatePartTypes { get; } = { "-", "YY", "YYYY", "MM", "DD" };
    public string[] TimePartTypes { get; } = { "-", "HH", "MM", "SS" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TagTypeDescription))]
    private string selectedTagType;

    public string TagTypeDescription => SelectedTagType switch
    {
        "[Number]" => "시작 값에서부터 순차적으로 증가하는 수를 입력합니다.",
        "[AtoZ]" => "시작 알파벳에서 A-Z 순서로 알파벳을 입력합니다.",
        "[Name.trim]" => "파일명에서 설정한 범위를 자르거나 남깁니다.",
        "[Today]" => "형식에 맞춰 오늘 날짜를 입력합니다.\n년: YYYY/YY, 월: MM, 일: DD",
        "[Time.now]" => "형식에 맞춰 현재 시간을 입력합니다.\n시: HH, 분: MM, 초: SS",
        _ => ""
    };

    [ObservableProperty]
    private string optionStartValue = "";

    [ObservableProperty]
    private string optionDigits = "";

    [ObservableProperty]
    private string optionLowerCount = "";

    // Name.trim 옵션
    [ObservableProperty] private string optionSplitStart = "1";
    [ObservableProperty] private string optionSplitEnd = "1";

    // Toggle Button용 속성 (True/False 대신 문자열로 바인딩하거나, 뷰에서 변환기를 사용할 수 있음.
    // 여기서는 뷰모델에서 bool로 관리하고 커맨드로 토글하는 방식을 사용하거나,
    // 단순하게 문자열 상태로 관리하여 뷰에 바인딩합니다.)
    [ObservableProperty] private bool optionSplitFromBack = false; // false: 앞에서부터, true: 뒤에서부터
    [ObservableProperty] private bool optionSplitKeep = false;     // false: 선택 삭제, true: 선택 남기기

    // 날짜 옵션
    [ObservableProperty] private string optionDatePart1 = "YY";
    [ObservableProperty] private string optionDatePart2 = "MM";
    [ObservableProperty] private string optionDatePart3 = "DD";
    [ObservableProperty] private string optionDateSep1 = "";
    [ObservableProperty] private string optionDateSep2 = "";

    // 시간 옵션
    [ObservableProperty] private string optionTimePart1 = "HH";
    [ObservableProperty] private string optionTimePart2 = "MM";
    [ObservableProperty] private string optionTimePart3 = "SS";
    [ObservableProperty] private string optionTimeSep1 = "";
    [ObservableProperty] private string optionTimeSep2 = "";

    /// <summary>
    /// 정수형 옵션 값을 검증하고 범위 내로 조정합니다.
    /// </summary>
    private string ValidateIntOption(string value, int min, int max, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (int.TryParse(value, out int result))
        {
            if (result < min) result = min;
            if (result > max) result = max;
            return result.ToString();
        }
        return defaultValue;
    }

    /// <summary>
    /// Long형 옵션 값을 검증합니다.
    /// </summary>
    private string ValidateLongOption(string value, long min, long max, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (long.TryParse(value, out long result))
        {
            if (result < min) result = min;
            if (result > max) result = max;
            return result.ToString();
        }
        return defaultValue;
    }

    // 시작 값 옵션 예외 처리
    partial void OnOptionStartValueChanged(string value)
    {
        if (SelectedTagType == "[Number]")
        {
            OptionStartValue = ValidateLongOption(value, 0, long.MaxValue, "0");
        }
        else if (SelectedTagType == "[AtoZ]")
        {
            if (string.IsNullOrWhiteSpace(value) || !value.All(char.IsLetter))
            {
                OptionStartValue = "A";
                return;
            }
            OptionStartValue = value.ToUpper();
        }
    }

    // 자리 수 옵션 예외 처리
    partial void OnOptionDigitsChanged(string value) => OptionDigits = ValidateIntOption(value, 1, 100, "1");

    // 소문자 수 옵션 예외 처리
    partial void OnOptionLowerCountChanged(string value) => OptionLowerCount = ValidateIntOption(value, 0, 100, "0");

    // 분할 옵션 예외 처리
    partial void OnOptionSplitStartChanged(string value) => OptionSplitStart = ValidateIntOption(value, 1, 255, "1");
    partial void OnOptionSplitEndChanged(string value) => OptionSplitEnd = ValidateIntOption(value, 1, 255, "1");

    public TagManagerViewModel()
    {
        CreateTagCommand = new RelayCommand(CreateTag);
        DeleteTagCommand = new RelayCommand<TagItem>(DeleteTag);

        // 초기 기본 태그 추가
        AddStandardTags();
        SelectedTagType = TagTypes.FirstOrDefault() ?? "[Number]";
    }

    private void AddStandardTags()
    {
        CreatedTags.Add(new TagItem
        {
            TagName = "[Origin]",
            Type = TagType.NameOrigin,
            ToolTip = "파일이 추가될 당시의 이름을 입력합니다.",
            IsStandard = true
        });

        CreatedTags.Add(new TagItem
        {
            TagName = "[OnlyNumber]",
            Type = TagType.OnlyNumber,
            ToolTip = "파일명에서 숫자만 남기고 제거합니다.\n[OnlyLetter], [ToUpper], [ToLower]와 함께 사용할 수 없습니다.",
            IsStandard = true,
            IsUnique = true,
            ExclusiveGroup = "FilterGroup,CaseConversion"
        });

        CreatedTags.Add(new TagItem
        {
            TagName = "[OnlyLetter]",
            Type = TagType.OnlyLetter,
            ToolTip = "파일명에서 숫자와 특수문자를 제거합니다.\n[OnlyNumber]와 함께 사용할 수 없습니다.",
            IsStandard = true,
            IsUnique = true,
            ExclusiveGroup = "FilterGroup"
        });

        CreatedTags.Add(new TagItem
        {
            TagName = "[ToUpper]",
            Type = TagType.ToUpper,
            ToolTip = "변경될 파일명의 모든 알파벳을 대문자로 변경합니다.\n[OnlyNumber], [ToLower]와 함께 사용할 수 없습니다.",
            IsStandard = true,
            IsUnique = true,
            ExclusiveGroup = "CaseConversion"
        });

        CreatedTags.Add(new TagItem
        {
            TagName = "[ToLower]",
            Type = TagType.ToLower,
            ToolTip = "변경될 파일명의 모든 알파벳을 소문자로 변경합니다.\n[OnlyNumber], [ToUpper]와 함께 사용할 수 없습니다.",
            IsStandard = true,
            IsUnique = true,
            ExclusiveGroup = "CaseConversion"
        });
    }

    private void CreateTag()
    {
        if (string.IsNullOrEmpty(SelectedTagType)) return;

        // TagName Counter
        if (!_tagCounts.ContainsKey(SelectedTagType))
            _tagCounts[SelectedTagType] = 0;

        _tagCounts[SelectedTagType]++;
        int currentCount = _tagCounts[SelectedTagType];

        TagItem? newItem = null;

        switch (SelectedTagType)
        {
            case "[Number]":
                {
                    // 생성 전 최종 검역
                    OnOptionStartValueChanged(OptionStartValue);
                    OnOptionDigitsChanged(OptionDigits);

                     newItem = new TagItem
                    {
                        TagName = $"[Number{currentCount}]",
                        Type = TagType.Number,
                        Params = new NumberTagParams
                        {
                            StartValue = long.Parse(OptionStartValue),
                            Digits = int.Parse(OptionDigits)
                        },
                        ToolTip = $"시작 값 : {OptionStartValue}\n자리 수 : {OptionDigits}"
                    };
                    newItem.Options.Add($"시작 값 : {OptionStartValue}");
                    newItem.Options.Add($"자리 수 : {OptionDigits}");
                }
                break;
            case "[AtoZ]":
                {
                    // 생성 전 최종 검역
                    OnOptionStartValueChanged(OptionStartValue);
                    OnOptionDigitsChanged(OptionDigits);
                    OnOptionLowerCountChanged(OptionLowerCount);

                    string startValue = OptionStartValue.ToUpper();
                    int digits = int.Parse(OptionDigits);

                    // 시작 값의 길이가 자리 수 옵션보다 짧을 경우, 뒤에 'A'를 채워서 길이를 맞춥니다.
                    // 예: Start="B", Digits=3 -> "BAA"
                    if (startValue.Length < digits)
                    {
                        startValue = startValue.PadRight(digits, 'A');
                    }

                    newItem = new TagItem
                    {
                        TagName = $"[AtoZ{currentCount}]",
                        Type = TagType.AtoZ,
                        Params = new AtoZTagParams
                        {
                            StartValue = startValue,
                            Digits = digits,
                            LowerCount = int.Parse(OptionLowerCount)
                        },
                        ToolTip = $"시작 값 : {OptionStartValue}\n자리 수 : {OptionDigits}\n소문자 수 : {OptionLowerCount}"
                    };
                    newItem.Options.Add($"시작 값 : {OptionStartValue}");
                    newItem.Options.Add($"자리 수 : {OptionDigits}");
                    newItem.Options.Add($"소문자 수 : {OptionLowerCount}");
                }
                break;
            case "[Today]":
                newItem = new TagItem
                {
                    TagName = $"[Today{currentCount}]",
                    Type = TagType.Today,
                    Params = new DateTimeTagParams
                    {
                        Part1 = OptionDatePart1,
                        Part2 = OptionDatePart2,
                        Part3 = OptionDatePart3,
                        Sep1 = OptionDateSep1,
                        Sep2 = OptionDateSep2
                    },
                    ToolTip = GetDateTimeToolTip(OptionDatePart1, OptionDateSep1, OptionDatePart2, OptionDateSep2, OptionDatePart3)
                };
                newItem.Options.Add($"형식 : {newItem.ToolTip}");
                break;
            case "[Time.now]":
                newItem = new TagItem
                {
                    TagName = $"[Time.now{currentCount}]",
                    Type = TagType.TimeNow,
                    Params = new DateTimeTagParams
                    {
                        Part1 = OptionTimePart1,
                        Part2 = OptionTimePart2,
                        Part3 = OptionTimePart3,
                        Sep1 = OptionTimeSep1,
                        Sep2 = OptionTimeSep2
                    },
                    ToolTip = GetDateTimeToolTip(OptionTimePart1, OptionTimeSep1, OptionTimePart2, OptionTimeSep2, OptionTimePart3)
                };
                newItem.Options.Add($"형식 : {newItem.ToolTip}");
                break;
            case "[Name.trim]":
                {
                    // 생성 전 검증
                    OnOptionSplitStartChanged(OptionSplitStart);
                    OnOptionSplitEndChanged(OptionSplitEnd);

                    int start = int.Parse(OptionSplitStart);
                    int end = int.Parse(OptionSplitEnd);

                    // 시작이 끝보다 크면 스왑
                    if (start > end)
                    {
                        (start, end) = (end, start);
                        OptionSplitStart = start.ToString();
                        OptionSplitEnd = end.ToString();
                    }

                    newItem = new TagItem
                    {
                        TagName = $"[Name.trim{currentCount}]",
                        Type = TagType.OriginSplit,
                        Params = new OriginSplitTagParams
                        {
                            IsFromBack = OptionSplitFromBack,
                            IsKeep = OptionSplitKeep,
                            StartCount = start,
                            EndCount = end
                        },
                        ToolTip = $"방향 : {(OptionSplitFromBack ? "뒤에서부터" : "앞에서부터")}\n범위 : {start} ~ {end}\n동작 : {(OptionSplitKeep ? "남기기" : "삭제")}"
                    };
                    newItem.Options.Add($"방향 : {(OptionSplitFromBack ? "뒤에서부터" : "앞에서부터")}");
                    newItem.Options.Add($"범위 : {start} ~ {end}");
                    newItem.Options.Add($"동작 : {(OptionSplitKeep ? "남기기" : "삭제")}");
                }
                break;
        }

        if (newItem != null)
        {
            CreatedTags.Add(newItem);
        }
    }

    // 날짜/시간 옵션 툴팁
    private string GetDateTimeToolTip(string p1, string s1, string p2, string s2, string p3)
    {
        var parts = new List<string>();
        if (p1 != "-") parts.Add(p1);
        parts.Add(s1);
        if (p2 != "-") parts.Add(p2);
        parts.Add(s2);
        if (p3 != "-") parts.Add(p3);

        return string.Join("", parts);
    }

    // 태그 삭제
    private void DeleteTag(TagItem? item)
    {
        if (item != null && CreatedTags.Contains(item))
        {
            CreatedTags.Remove(item);
        }
    }
}
