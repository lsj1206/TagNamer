using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace TagNamer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private bool _isUpdatingSortDirection;

    public FileListViewModel FileList { get; } = new();

    public ObservableCollection<string> SortOptions { get; } =
    [
        "기본 정렬",
        "이름",
        "크기",
        "생성일",
        "수정일"
    ];

    [ObservableProperty]
    private string currentRuleDisplay = "규칙: [number] - [name]";

    [ObservableProperty]
    private string appVersion = "v1.0.0";

    [ObservableProperty]
    private string selectedSortOption;

    [ObservableProperty]
    private bool sortAscending = true;

    [ObservableProperty]
    private bool sortDescending;

    [ObservableProperty]
    private bool confirmDeletion = true;

    [ObservableProperty]
    private bool showExtension = true;

    [ObservableProperty]
    private string extensionInput = string.Empty;

    public IRelayCommand AddFilesCommand { get; }
    public IRelayCommand AddFolderCommand { get; }
    public IRelayCommand ListClearCommand { get; }
    public IRelayCommand OpenRuleSettingsCommand { get; }
    public IRelayCommand ApplyChangesCommand { get; }
    public IRelayCommand UndoChangesCommand { get; }
    public IRelayCommand ApplyExtensionCommand { get; }

    public MainViewModel()
    {
        selectedSortOption = SortOptions.First();

        AddFilesCommand = new RelayCommand(AddFiles);
        AddFolderCommand = new RelayCommand(AddFolder);
        ListClearCommand = new RelayCommand(() => { });
        OpenRuleSettingsCommand = new RelayCommand(() => { });
        ApplyChangesCommand = new RelayCommand(() => { });
        UndoChangesCommand = new RelayCommand(() => { });
        ApplyExtensionCommand = new RelayCommand(ApplyExtension);
    }

    partial void OnSortAscendingChanged(bool value)
    {
        if (_isUpdatingSortDirection || !value) return;

        _isUpdatingSortDirection = true;
        SortDescending = false;
        _isUpdatingSortDirection = false;
    }

    partial void OnSortDescendingChanged(bool value)
    {
        if (_isUpdatingSortDirection || !value) return;

        _isUpdatingSortDirection = true;
        SortAscending = false;
        _isUpdatingSortDirection = false;
    }

    private void ApplyExtension()
    {
        if (string.IsNullOrWhiteSpace(ExtensionInput))
            return;

        // 점으로 시작하지 않으면 자동으로 추가
        var extension = ExtensionInput.Trim();
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        // 실제 확장자 변경 로직은 여기에 구현 예정
        // 현재는 입력값 정규화만 수행
        ExtensionInput = extension;
    }

    // 파일 추가 로직
    private void AddFiles()
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = false,
            Multiselect = true,
            Title = "파일 추가"
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            foreach (var file in dialog.FileNames)
            {
                FileList.AddFile(file);
            }
        }
    }

    // 폴더 추가 로직
    private void AddFolder()
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Multiselect = true,
            Title = "폴더 추가"
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            foreach (var folder in dialog.FileNames)
            {
                FileList.AddFolder(folder);
            }
        }
    }
}
