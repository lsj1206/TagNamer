// 이름 변경 규칙 ViewModel
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty]
    private string ruleGuildTooltip = "태그를 끌어당겨 원하는 위치에 규칙을 추가할 수 있습니다.";

    public RenameViewModel(TagManagerViewModel tagManager)
    {
        TagManager = tagManager;
    }
}
