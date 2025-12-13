// 이름 변경 규칙 ViewModel
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public partial class RenameRuleViewModel : ObservableObject
{
    [ObservableProperty]
    private string ruleFormat = "[Name.origin]"; // 기본값

    // 실제 파일명 변경 로직에서 사용할 변환된 규칙 문자열
    public string ResolvedRuleFormat
    {
        get
        {
            var resolved = RuleFormat;
            foreach (var tag in CreatedTags)
            {
                if (!string.IsNullOrEmpty(tag.DisplayName) && !string.IsNullOrEmpty(tag.Code))
                {
                    resolved = resolved.Replace(tag.DisplayName, tag.Code);
                }
            }
            return resolved;
        }
    }

    public ObservableCollection<TagItem> CreatedTags { get; } = new();
    public ObservableCollection<string> TagTypes { get; } = new() { "[Number]", "[AtoZ]", "[Today]", "[Time.now]" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TagTypeDescription))]
    private string selectedTagType;

    public string TagTypeDescription => SelectedTagType switch
    {
        "[Number]" => "규칙대로 순차적으로 증가하는 수를 입력하는 태그",
        "[AtoZ]" => "규칙대로 A-Z 순서로 알파벳을 입력하는 태그",
        "[Today]" => "형식에 맞춰 오늘 날짜를 입력하는 태그\n대소문자 구분없이 년:YYYY/YY 월:MM 일:DD",
        "[Time.now]" => "형식에 맞춰 현재 시간을 입력하는 태그\n대소문자 구분없이 시:HH 분:MM 초:SS",
        _ => ""
    };

    [ObservableProperty]
    private string optionDigits = "";

    [ObservableProperty]
    private string optionStartValue = "";

    [ObservableProperty]
    private string optionDateFormat = "";

    [ObservableProperty]
    private bool optionLowerCase = false;

    [ObservableProperty]
    private string ruleGuildTooltip = "태그를 끌어당겨 원하는 위치에 규칙을 추가할 수 있습니다.";

    // UI 표시용 속성들 (Visibility 제어 등은 View의 DataTrigger로 처리 예정이지만, 필요시 VM에서 불리언 속성 제공 가능)

    private int _numTagCount = 0;
    private int _alphaTagCount = 0;
    private int _todayTagCount = 0;
    private int _nowTagCount = 0;

    public IRelayCommand CreateTagCommand { get; }

    public RenameRuleViewModel()
    {
        CreateTagCommand = new RelayCommand(CreateTag);

        // 초기 기본 태그 추가
        AddFixedTags();
        SelectedTagType = TagTypes.FirstOrDefault() ?? "[Number]";
    }

    private void AddFixedTags()
    {
        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.origin]",
            Code = "[Name.origin]",
            ToolTip = "파일이 추가될 당시의 파일명을 입력합니다."
        });

        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.prev]",
            Code = "[Name.prev]",
            ToolTip = "마지막으로 변경된 파일명을 입력합니다.\n처음엔 원본 파일명이 입력됩니다."
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
                newItem = new TagItem
                {
                    DisplayName = $"[Number{_numTagCount}]",
                    Code = $"[Number:{OptionDigits}:{OptionStartValue}]",
                    ToolTip = $"규칙대로 순차적으로 증가하는 수가 입력됩니다.\n자리 수 : {OptionDigits}, 시작 값 : {OptionStartValue}"
                };
                break;
            case "[AtoZ]":
                _alphaTagCount++;
                var caseStr = OptionLowerCase ? "소문자" : "대문자";
                newItem = new TagItem
                {
                    DisplayName = $"[AtoZ{_alphaTagCount}]",
                    Code = $"[AtoZ:{OptionDigits}:{OptionStartValue}:{(OptionLowerCase ? "lower" : "upper")}]",
                    ToolTip = $"규칙대로 A-Z 순서로 알파벳이 입력됩니다.\n자리 수 : {OptionDigits}, 시작 값 : {OptionStartValue}, {caseStr}"
                };
                break;
            case "[Today]":
                _todayTagCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[Today{_todayTagCount}]",
                    Code = $"[Today:{OptionDateFormat}]",
                    ToolTip = $"{OptionDateFormat} 형식으로 오늘 날짜가 입력됩니다."
                };
                break;
            case "[Time.now]":
                _nowTagCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[Time.now{_nowTagCount}]",
                    Code = $"[Time.now:{OptionDateFormat}]",
                    ToolTip = $"{OptionDateFormat} 형식으로 현재 시간이 입력됩니다."
                };
                break;
        }

        if (newItem != null)
        {
            CreatedTags.Add(newItem);
        }
    }
}
