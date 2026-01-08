using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public partial class RenameViewModel : ObservableObject
{
    [ObservableProperty]
    private string ruleFormat = "[Name.origin]"; // 기본값

    public TagManagerViewModel TagManager { get; }

    // RenameService에서 TagItem list를 순회하며 직접 매칭하고 치환
    public string ResolvedRuleFormat => RuleFormat;

    partial void OnRuleFormatChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        // [ToUpper]와 [ToLower] 태그 정규식 검색
        var matches = System.Text.RegularExpressions.Regex.Matches(value, @"\[(ToUpper|ToLower)\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (matches.Count > 1)
        {
            // 마지막에 추가된 태그만 남기고 나머지는 모두 제거 (가장 최신 의도를 반영)
            string cleaned = value;
            // 뒤에서부터 지워야 인덱스가 변하지 않음
            for (int i = matches.Count - 2; i >= 0; i--)
            {
                cleaned = cleaned.Remove(matches[i].Index, matches[i].Length);
            }

            // 값이 바뀌었을 때만 다시 설정하여 무한 루프 방지
            if (cleaned != value)
            {
                RuleFormat = cleaned;
            }
        }
    }

    /// 규칙에 태그 추가
    /// 'isForward' 앞에 추가할지 여부 (true: 맨 앞, false: 맨 뒤)
    public void AddTagToRule(string tagCode, bool isForward)
    {
        if (string.IsNullOrEmpty(tagCode)) return;

        if (isForward)
        {
            RuleFormat = tagCode + RuleFormat;
        }
        else
        {
            RuleFormat = RuleFormat + tagCode;
        }
    }

    [ObservableProperty]
    private string ruleGuildTooltip = "태그를 끌어당겨 원하는 위치에 추가할 수 있습니다." + "\n" +
                                      "태그를 우클릭 하면 컨텍스트 메뉴가 나타납니다.";

    public RenameViewModel(TagManagerViewModel tagManager)
    {
        TagManager = tagManager;
    }
}
