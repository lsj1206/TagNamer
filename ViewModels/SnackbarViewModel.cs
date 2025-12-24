using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TagNamer.ViewModels;

public partial class SnackbarViewModel : ObservableObject
{
    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private Services.SnackbarType type = Services.SnackbarType.Info;

    // 애니메이션 트리거용 프로퍼티 (View에서 바인딩)
    [ObservableProperty]
    private bool isAnimating;
}
