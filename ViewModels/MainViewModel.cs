using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using TagNamer.Services;

namespace TagNamer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private bool _isUpdatingSortDirection;

    public FileListViewModel FileList { get; } = new();

    public enum SortType
    {
        AddIndex,
        NameNumber,
        NamePath,
        PathNumber,
        PathName,
        Size,
        CreatedDate,
        ModifiedDate
    }

    public class SortOption
    {
        public string Display { get; set; } = string.Empty;
        public SortType Type { get; set; }
    }

    public ObservableCollection<SortOption> SortOptions { get; } =
    [
        new() { Display = "번호", Type = SortType.AddIndex },
        new() { Display = "이름-번호", Type = SortType.NameNumber },
        new() { Display = "이름-경로", Type = SortType.NamePath },
        new() { Display = "경로-번호", Type = SortType.PathNumber },
        new() { Display = "경로-이름", Type = SortType.PathName },
        new() { Display = "크기", Type = SortType.Size },
        new() { Display = "생성일", Type = SortType.CreatedDate },
        new() { Display = "수정일", Type = SortType.ModifiedDate }
    ];

    [ObservableProperty]
    private string currentRuleDisplay = "규칙: [number] - [name]";

    [ObservableProperty]
    private string appVersion = "v1.0.0";

    [ObservableProperty]
    private SortOption selectedSortOption;

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
    public IRelayCommand<System.Collections.IList> DeleteFilesCommand { get; }
    public IRelayCommand ListClearCommand { get; }
    public IRelayCommand OpenRuleSettingsCommand { get; }
    public IRelayCommand ApplyChangesCommand { get; }
    public IRelayCommand UndoChangesCommand { get; }
    public IRelayCommand ApplyExtensionCommand { get; }

    private readonly IDialogService _dialogService;

    public MainViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;

        selectedSortOption = SortOptions.First();

        AddFilesCommand = new RelayCommand(AddFiles);
        AddFolderCommand = new RelayCommand(AddFolder);
        DeleteFilesCommand = new RelayCommand<System.Collections.IList>(DeleteFiles);
        ListClearCommand = new AsyncRelayCommand(ListClearAsync);
        OpenRuleSettingsCommand = new RelayCommand(() => { });
        ApplyChangesCommand = new RelayCommand(() => { });
        UndoChangesCommand = new RelayCommand(() => { });
        ApplyExtensionCommand = new RelayCommand(ApplyExtension);
    }

    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        SortFiles();
    }

    partial void OnSortAscendingChanged(bool value)
    {
        if (_isUpdatingSortDirection || !value) return;

        _isUpdatingSortDirection = true;
        SortDescending = false;
        _isUpdatingSortDirection = false;

        SortFiles();
    }

    partial void OnSortDescendingChanged(bool value)
    {
        if (_isUpdatingSortDirection || !value) return;

        _isUpdatingSortDirection = true;
        SortAscending = false;
        _isUpdatingSortDirection = false;

        SortFiles();
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
            SortFiles();
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
            SortFiles();
        }
    }

    // 파일 삭제 로직
    private async void DeleteFiles(System.Collections.IList? items)
    {
        if (items == null || items.Count == 0) return;

        // 삭제할 항목 리스트 복사 (순회 중 컬렉션 변경 방지)
        var itemsToDelete = items.Cast<TagNamer.Models.FileItem>().ToList();
        int count = itemsToDelete.Count;

        if (ConfirmDeletion)
        {
            string message = count == 1
                ? "선택된 파일을 목록에서 삭제하시겠습니까?"
                : $"{count}개의 선택된 파일을 목록에서 삭제하시겠습니까?";

            var result = await _dialogService.ShowConfirmationAsync(message, "삭제 확인");
            if (!result) return;
        }

        foreach (var item in itemsToDelete)
        {
            FileList.Items.Remove(item);
        }
    }

    // 목록 삭제 로직
    private async Task ListClearAsync()
    {
        var result = await _dialogService.ShowConfirmationAsync("파일 목록을 전부 삭제하시겠습니까?", "목록 삭제");
        if (result)
        {
            FileList.Clear();
        }
    }

    private void SortFiles()
    {
        if (FileList.Items.Count == 0) return;

        var items = FileList.Items.ToList();
        IOrderedEnumerable<TagNamer.Models.FileItem> sortedItems;
        var comparer = new NaturalStringComparer();

        // 1차 정렬
        switch (SelectedSortOption.Type)
        {
            case SortType.NameNumber:
            case SortType.NamePath:
                sortedItems = SortAscending
                    ? items.OrderBy(x => x.OriginalName, comparer)
                    : items.OrderByDescending(x => x.OriginalName, comparer);
                break;
            case SortType.PathNumber:
            case SortType.PathName:
                sortedItems = SortAscending
                    ? items.OrderBy(x => x.DirectoryName, comparer)
                    : items.OrderByDescending(x => x.DirectoryName, comparer);
                break;
            case SortType.Size:
                sortedItems = SortAscending
                    ? items.OrderBy(x => x.Size)
                    : items.OrderByDescending(x => x.Size);
                break;
            case SortType.CreatedDate:
                sortedItems = SortAscending
                    ? items.OrderBy(x => x.CreatedDate)
                    : items.OrderByDescending(x => x.CreatedDate);
                break;
            case SortType.ModifiedDate:
                sortedItems = SortAscending
                    ? items.OrderBy(x => x.ModifiedDate)
                    : items.OrderByDescending(x => x.ModifiedDate);
                break;
            case SortType.AddIndex:
            default:
                sortedItems = SortAscending
                    ? items.OrderBy(x => x.AddIndex)
                    : items.OrderByDescending(x => x.AddIndex);
                break;
        }

        // 2차 정렬 (동점자 처리)
        switch (SelectedSortOption.Type)
        {
            case SortType.NameNumber:
            case SortType.PathNumber:
            case SortType.Size:
            case SortType.CreatedDate:
            case SortType.ModifiedDate:
                // 중복 시 번호(AddIndex) 순
                sortedItems = SortAscending
                    ? sortedItems.ThenBy(x => x.AddIndex)
                    : sortedItems.ThenByDescending(x => x.AddIndex);
                break;
            case SortType.NamePath:
                // 중복 시 경로(Path) 순
                sortedItems = SortAscending
                    ? sortedItems.ThenBy(x => x.Path, comparer)
                    : sortedItems.ThenByDescending(x => x.Path, comparer);
                break;
            case SortType.PathName:
                // 중복 시 이름(OriginalName) 순
                sortedItems = SortAscending
                    ? sortedItems.ThenBy(x => x.OriginalName, comparer)
                    : sortedItems.ThenByDescending(x => x.OriginalName, comparer);
                break;
        }

        // 정렬된 리스트 반영
        FileList.Items.Clear();
        foreach (var item in sortedItems)
        {
            FileList.Items.Add(item);
        }
    }
}
