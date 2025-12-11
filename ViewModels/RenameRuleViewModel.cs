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
    private string ruleFormat = "[origin]"; // 기본값

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
    public ObservableCollection<string> TagTypes { get; } = new() { "[num]", "[alpha]", "[today]", "[now]" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TagTypeDescription))]
    private string selectedTagType;

    public string TagTypeDescription => SelectedTagType switch
    {
        "[num]" => "순차적인 번호를 입력합니다.",
        "[alpha]" => "알파벳 순서를 입력합니다.",
        "[today]" => "오늘 날짜를 입력합니다. " + "대소문자 구분없이 년:YYYY/YY 월:MM 일:DD",
        "[now]" => "현재 시간을 입력합니다. " + "대소문자 구분없이 시:HH 분:MM 초:SS",
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
    private string ruleExplanation = "태그를 끌어당겨 원하는 위치에 규칙을 추가할 수 있습니다.";

    // UI 표시용 속성들 (Visibility 제어 등은 View의 DataTrigger로 처리 예정이지만, 필요시 VM에서 불리언 속성 제공 가능)

    private int _numCount = 0;
    private int _alphaCount = 0;
    private int _dateCount = 0;
    private int _timeCount = 0;

    public IRelayCommand CreateTagCommand { get; }

    public RenameRuleViewModel()
    {
        CreateTagCommand = new RelayCommand(CreateTag);

        // 초기 기본 태그 추가
        AddFixedTags();
        SelectedTagType = TagTypes.FirstOrDefault() ?? "[num]";
    }

    private void AddFixedTags()
    {
        CreatedTags.Add(new TagItem
        {
            DisplayName = "[origin]",
            Code = "[origin]",
            ToolTip = "파일이 목록에 추가될 당시의 이름"
        });

        CreatedTags.Add(new TagItem
        {
            DisplayName = "[prevName]",
            Code = "[prevName]",
            ToolTip = "마지막으로 변경된 이름 (처음엔 원본 이름)"
        });
    }

    private void CreateTag()
    {
        if (string.IsNullOrEmpty(SelectedTagType)) return;

        TagItem? newItem = null;

        switch (SelectedTagType)
        {
            case "[num]":
                _numCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[num{_numCount}]",
                    Code = $"[num:{OptionDigits}:{OptionStartValue}]",
                    ToolTip = $"자리 수 : {OptionDigits}, 시작 값 : {OptionStartValue}"
                };
                break;
            case "[alpha]":
                _alphaCount++;
                var caseStr = OptionLowerCase ? "소문자" : "대문자";
                newItem = new TagItem
                {
                    DisplayName = $"[alpha{_alphaCount}]",
                    Code = $"[alpha:{OptionDigits}:{OptionStartValue}:{(OptionLowerCase ? "lower" : "upper")}]",
                    ToolTip = $"자리 수 : {OptionDigits}, 시작 값 : {OptionStartValue}, {caseStr}"
                };
                break;
            case "[today]":
                _dateCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[today{_dateCount}]",
                    Code = $"[today:{OptionDateFormat}]",
                    ToolTip = $"형식 : {OptionDateFormat} (예: yyyyMMdd)"
                };
                break;
            case "[now]":
                _timeCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[now{_timeCount}]",
                    Code = $"[now:{OptionDateFormat}]",
                    ToolTip = $"형식: {OptionDateFormat} (예: HHmmss)"
                };
                break;
        }

        if (newItem != null)
        {
            CreatedTags.Add(newItem);
        }
    }
}
