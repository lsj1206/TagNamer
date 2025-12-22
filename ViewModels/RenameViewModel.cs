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

    // 실제 파일명 변경 로직에서 사용할 변환된 규칙 문자열
    public string ResolvedRuleFormat
    {
        get
        {
            var resolved = RuleFormat;
            foreach (var tag in TagManager.CreatedTags)
            {
                if (!string.IsNullOrEmpty(tag.DisplayName) && !string.IsNullOrEmpty(tag.Code))
                {
                    resolved = resolved.Replace(tag.DisplayName, tag.Code);
                }
            }
            return resolved;
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
