using System.Linq;
using System.Text.RegularExpressions;
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

        // [ToUpper]와 [ToLower] 태그가 동시에 있거나 여러 개이면 마지막 것만 남김
        var matches = Regex.Matches(value, @"\[(ToUpper|ToLower)\]", RegexOptions.IgnoreCase);

        if (matches.Count > 1)
        {
            string cleaned = value;
            // 뒤에서부터 지워야 인덱스가 꼬이지 않음 (마지막 매치 제외)
            for (int i = matches.Count - 2; i >= 0; i--)
            {
                cleaned = cleaned.Remove(matches[i].Index, matches[i].Length);
            }

            if (cleaned != value)
            {
                RuleFormat = cleaned;
            }
        }
    }

    /// <summary>
    /// 규칙 문자열에서 대소문자 변환 태그를 모두 제거합니다.
    /// </summary>
    public string RemoveCaseTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        string result = Regex.Replace(input, @"\[ToUpper\]", "", RegexOptions.IgnoreCase);
        return Regex.Replace(result, @"\[ToLower\]", "", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 규칙에 태그 추가
    /// </summary>
    public void AddTagToRule(string tagCode, bool isForward)
    {
        if (string.IsNullOrEmpty(tagCode)) return;

        string current = RuleFormat;

        // 대소문자 태그인 경우 기존 태그 제거
        if (tagCode.Equals("[ToUpper]", System.StringComparison.OrdinalIgnoreCase) ||
            tagCode.Equals("[ToLower]", System.StringComparison.OrdinalIgnoreCase))
        {
            current = RemoveCaseTags(current);
        }

        RuleFormat = isForward ? tagCode + current : current + tagCode;
    }

    [ObservableProperty]
    private string ruleGuildTooltip = "태그를 끌어당겨 원하는 위치에 추가할 수 있습니다." + "\n" +
                                      "태그를 우클릭 하면 컨텍스트 메뉴가 나타납니다.";

    public RenameViewModel(TagManagerViewModel tagManager)
    {
        TagManager = tagManager;
    }
}
