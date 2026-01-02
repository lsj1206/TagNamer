using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public partial class ExtensionViewModel : ObservableObject
{
    private IList<FileItem> _targetItems = new List<FileItem>();

    [ObservableProperty]
    private string extensionInput = string.Empty;

    public IRelayCommand ApplyCommand { get; }
    public IRelayCommand CancelCommand { get; }

    // 창 닫기 요청을 위한 Action (View에서 바인딩)
    public System.Action<bool>? RequestClose { get; set; }

    public ExtensionViewModel()
    {
        ApplyCommand = new RelayCommand(Apply);
        CancelCommand = new RelayCommand(Cancel);
    }

    public void Initialize(IList<FileItem> items)
    {
        _targetItems = items ?? new List<FileItem>();
        ExtensionInput = string.Empty;
    }

    private void Apply()
    {
        if (string.IsNullOrWhiteSpace(ExtensionInput))
        {
            // 입력이 없으면 아무것도 안 하고 닫음 (취소와 비슷)
            RequestClose?.Invoke(false);
            return;
        }

        string newExt = ExtensionInput.Trim();
        if (!newExt.StartsWith('.'))
        {
            newExt = "." + newExt;
        }

        foreach (var item in _targetItems)
        {
            item.NewExtension = newExt;
        }

        RequestClose?.Invoke(true);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
