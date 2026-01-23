using System;
using System.Windows;
using System.Threading.Tasks;
using ModernWpf.Controls;

namespace TagNamer.Services;

/// <summary>
/// 사용자 인터페이스와 상호작용하는 다이얼로그 서비스입니다.
/// </summary>
public class DialogService : IDialogService
{
    private readonly ILanguageService _languageService;

    public DialogService(ILanguageService languageService)
    {
        _languageService = languageService;
    }

    /// <summary>
    /// 예/아니오 선택이 필요한 확인 창을 표시합니다.
    /// </summary>
    public async Task<bool> ShowConfirmationAsync(string message, string title = "")
    {
        // 현재 활성화된 메인 윈도우를 찾아 다이얼로그의 부모로 설정합니다.
        var window = Application.Current.MainWindow;
        if (window == null) return false;

        var dialog = new ContentDialog
        {
            Title = string.IsNullOrEmpty(title) ? _languageService.GetString("Dlg_Confirm", "확인") : title,
            Content = message,
            PrimaryButtonText = _languageService.GetString("Dlg_Yes", "네"),
            CloseButtonText = _languageService.GetString("Dlg_No", "아니오"),
            DefaultButton = ContentDialogButton.Primary,
            Owner = window
        };

        var result = await dialog.ShowAsync();
        // '네' 버튼을 눌렀을 때만 true를 반환합니다.
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// 폴더 추가 방식(내부 파일 또는 폴더 자체)을 선택하는 창을 표시합니다.
    /// </summary>
    public async Task<FolderAddOption> ShowFolderAddOptionAsync(string firstFolderName, int count = 1)
    {
        var window = Application.Current.MainWindow;
        if (window == null) return FolderAddOption.Cancel;

        // 폴더 개수에 따라 메시지를 다르게 표시합니다.
        string content;
        if (count > 1)
        {
            var multiTemplate = _languageService.GetString("Dlg_Ask_FolderOptionMulti", "'{0}'폴더 외 {1}개의 폴더를 어떻게 추가하시겠습니까?");
            content = string.Format(multiTemplate, firstFolderName, count - 1);
        }
        else
        {
            var singleTemplate = _languageService.GetString("Dlg_Ask_FolderOptionSingle", "'{0}' 폴더를 어떻게 추가하시겠습니까?");
            content = string.Format(singleTemplate, firstFolderName);
        }

        var dialog = new ContentDialog
        {
            Title = _languageService.GetString("Dlg_Title_FolderOption", "폴더 추가 옵션"),
            Content = content,
            PrimaryButtonText = _languageService.GetString("Dlg_Btn_AddFileInFolder", "폴더 내 파일 추가"),
            SecondaryButtonText = _languageService.GetString("Dlg_Btn_AddFolder", "폴더 추가"),
            CloseButtonText = _languageService.GetString("Dlg_Cancel", "취소"),
            DefaultButton = ContentDialogButton.Primary,
            Owner = window
        };

        var result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => FolderAddOption.Files,     // 폴더 내부의 파일들만 목록에 추가
            ContentDialogResult.Secondary => FolderAddOption.Folder, // 폴더 자체(이름 변경 목적)를 목록에 추가
            _ => FolderAddOption.Cancel                              // 작업 취소
        };
    }

    /// <summary>
    /// 수동으로 파일 이름을 입력받는 대화 상자를 표시합니다.
    /// </summary>
    public async Task<string?> ShowManualEditAsync(string currentName)
    {
        var window = Application.Current.MainWindow;
        if (window == null) return null;

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = currentName,
            Margin = new Thickness(0, 10, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var dialog = new ContentDialog
        {
            Title = _languageService.GetString("Dlg_Title_ManualRename", "개별 이름 변경"),
            Content = new System.Windows.Controls.StackPanel
            {
                Children =
                {
                    new System.Windows.Controls.TextBlock { Text = _languageService.GetString("Dlg_Ask_ManualRename", "변경할 파일명을 입력하세요:") },
                    textBox
                }
            },
            PrimaryButtonText = _languageService.GetString("Dlg_Confirm", "확인"),
            CloseButtonText = _languageService.GetString("Dlg_Cancel", "취소"),
            DefaultButton = ContentDialogButton.Primary,
            Owner = window
        };

        // 다이얼로그가 뜨자마자 텍스트박스에 포커스를 주고 전체 선택합니다.
        dialog.Opened += (s, e) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text : null;
    }
}
