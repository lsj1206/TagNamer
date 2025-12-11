// 이름 변경 규칙 ViewModel
using CommunityToolkit.Mvvm.ComponentModel;

namespace TagNamer.ViewModels;

public partial class RenameRuleViewModel : ObservableObject
{
    [ObservableProperty]
    private string ruleFormat = "[number] - [name]";

    public RenameRuleViewModel()
    {
    }
}
