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
    private bool showExtension = false;

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
    public IRelayCommand ReorderNumberCommand { get; }

    private readonly IDialogService _dialogService;
    private readonly ISortingService _sortingService;
    private readonly IFileService _fileService;

    public MainViewModel(IDialogService dialogService, ISortingService sortingService, IFileService fileService)
    {
        _dialogService = dialogService;
        _sortingService = sortingService;
        _fileService = fileService;

        selectedSortOption = SortOptions.First();

        AddFilesCommand = new RelayCommand(AddFiles);
        AddFolderCommand = new RelayCommand(AddFolder);
        DeleteFilesCommand = new RelayCommand<System.Collections.IList>(DeleteFiles);
        ListClearCommand = new AsyncRelayCommand(ListClearAsync);
        OpenRuleSettingsCommand = new RelayCommand(() => { });
        ApplyChangesCommand = new RelayCommand(() => { });
        UndoChangesCommand = new RelayCommand(() => { });
        ApplyExtensionCommand = new RelayCommand(ApplyExtension);
        ReorderNumberCommand = new RelayCommand(ReorderNumber);
    }

    private async void ReorderNumber()
    {
        if (FileList.Items.Count == 0) return;

        var result = await _dialogService.ShowConfirmationAsync(
            "번호를 현재 목록 순서대로 정렬하시겠습니까?\n현재 부여된 번호는 초기화됩니다.",
            "번호 재정렬");

        if (result)
        {
            int index = 1;
            foreach (var item in FileList.Items)
            {
                // 폴더가 아닌 경우에만 번호 부여
                if (!item.IsFolder)
                {
                    item.AddIndex = index++;
                }
            }
            // 다음 추가될 번호 업데이트
            FileList.UpdateNextAddIndex(index);
        }
    }

    // 확장자 표시 ON/OFF
    partial void OnShowExtensionChanged(bool value)
    {
        foreach (var item in FileList.Items)
        {
            item.UpdateDisplay(value);
        }
    }

    // 정렬 기준 선택
    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        SortFiles();
    }

    // 오름차순
    partial void OnSortAscendingChanged(bool value)
    {
        if (_isUpdatingSortDirection || !value) return;

        _isUpdatingSortDirection = true;
        SortDescending = false;
        _isUpdatingSortDirection = false;

        SortFiles();
    }

    // 내림차순
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

    // 파일 추가
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
                var item = _fileService.CreateFileItem(file);
                if (item != null)
                {
                    item.UpdateDisplay(ShowExtension);
                    FileList.AddItem(item);
                }
            }
            SortFiles();
        }
    }

    // 폴더 추가
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
                var files = _fileService.GetFilesInFolder(folder);
                foreach (var file in files)
                {
                    var item = _fileService.CreateFileItem(file);
                    if (item != null)
                    {
                        item.UpdateDisplay(ShowExtension);
                        FileList.AddItem(item);
                    }
                }
            }
            SortFiles();
        }
    }

    // 파일 삭제
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

    // 목록 삭제
    private async Task ListClearAsync()
    {
        var result = await _dialogService.ShowConfirmationAsync("파일 목록을 전부 삭제하시겠습니까?", "목록 삭제");
        if (result)
        {
            FileList.Clear();
        }
    }

    // 목록 정렬
    private void SortFiles()
    {
        if (FileList.Items.Count == 0) return;

        var sortedItems = _sortingService.Sort(FileList.Items, SelectedSortOption, SortAscending);

        // 정렬된 리스트 반영
        FileList.Items.Clear();
        foreach (var item in sortedItems)
        {
            FileList.Items.Add(item);
        }
    }

    // 드래그 앤 드롭으로 전달된 파일 및 폴더를 처리하는 메서드
    public void AddDroppedItems(string[] paths)
    {
        if (paths == null || paths.Length == 0) return;

        foreach (var path in paths)
        {
            if (System.IO.File.Exists(path))
            {
                // 파일인 경우
                var item = _fileService.CreateFileItem(path);
                if (item != null)
                {
                    item.UpdateDisplay(ShowExtension);
                    FileList.AddItem(item);
                }
            }
            else if (System.IO.Directory.Exists(path))
            {
                // 폴더인 경우
                var filePaths = _fileService.GetFilesInFolder(path);
                foreach (var filePath in filePaths)
                {
                    var item = _fileService.CreateFileItem(filePath);
                    if (item != null)
                    {
                        item.UpdateDisplay(ShowExtension);
                        FileList.AddItem(item);
                    }
                }
            }
        }
        SortFiles();
    }
}
