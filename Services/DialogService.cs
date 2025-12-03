using System;
using System.Windows;
using System.Threading.Tasks;
using ModernWpf.Controls;

namespace TagNamer.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string message, string title = "확인")
    {
        // 현재 활성화된 윈도우 찾기 (없으면 null)
        var window = Application.Current.MainWindow;
        if (window == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "네",
            CloseButtonText = "아니오",
            DefaultButton = ContentDialogButton.Primary,
            Owner = window
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
