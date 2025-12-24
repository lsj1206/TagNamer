using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using TagNamer.Services;

namespace TagNamer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private bool _isUpdatingSortDirection;

    public SnackbarViewModel Snackbar { get; }
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

    // 현재 규칙 표시 (RenameViewModel과 동기화)
    public string CurrentRuleDisplay => _renameViewModel.RuleFormat;

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

    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly ISnackbarService _snackbarService;
    private readonly IFileService _fileService;
    private readonly IRenameService _renameService;
    private readonly ISortingService _sortingService;

    private readonly RenameViewModel _renameViewModel;

    public MainViewModel(
        IWindowService windowService,
        IDialogService dialogService,
        ISnackbarService snackbarService,
        IFileService fileService,
        IRenameService renameService,
        ISortingService sortingService,
        SnackbarViewModel snackbarViewModel,
        RenameViewModel renameViewModel)
    {
        _windowService = windowService;
        _dialogService = dialogService;
        _snackbarService = snackbarService;
        _fileService = fileService;
        _renameService = renameService;
        _sortingService = sortingService;
        Snackbar = snackbarViewModel;
        _renameViewModel = renameViewModel;

        // RenameViewModel의 RuleFormat 변경 시 UI 알림
        _renameViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(RenameViewModel.RuleFormat))
            {
                OnPropertyChanged(nameof(CurrentRuleDisplay));
                UpdatePreview();
            }
        };

        selectedSortOption = SortOptions.First();

        AddFilesCommand = new RelayCommand(AddFiles);
        AddFolderCommand = new RelayCommand(AddFolder);
        DeleteFilesCommand = new RelayCommand<System.Collections.IList>(DeleteFiles);
        ListClearCommand = new AsyncRelayCommand(ListClearAsync);
        OpenRuleSettingsCommand = new RelayCommand(OpenRenameWindow);
        ApplyChangesCommand = new RelayCommand(ApplyChanges);
        UndoChangesCommand = new RelayCommand(UndoChanges);
        ApplyExtensionCommand = new RelayCommand(ApplyExtension);
        ReorderNumberCommand = new RelayCommand(ReorderNumber);
    }

    private void OpenRenameWindow()
    {
        // View 타입(RenameWindow)을 직접 명시하긴 하지만,
        // 이는 Generic 제약을 만족하기 위함이며 생성 및 바인딩 로직은 서비스가 담당합니다.
        _windowService.ShowDialog<TagNamer.Views.RenameWindow>(_renameViewModel);
    }

    // 목록의 번호를 현재 순서에 맞게 다시 매기는 로직입니다.
    private async void ReorderNumber()
    {
        try
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
                    item.AddIndex = index++;
                }
                // 다음 추가될 번호 업데이트
                FileList.UpdateNextAddIndex(index);
                _snackbarService.Show("번호 재정렬이 완료되었습니다.", Services.SnackbarType.Success);
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Show($"번호 재정렬 중 오류: {ex.Message}", Services.SnackbarType.Error);
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
        try
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
                UpdatePreview();
                _snackbarService.Show($"{dialog.FileNames.Count()}개의 파일이 추가되었습니다.", Services.SnackbarType.Success);
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Show($"파일 추가 중 오류가 발생했습니다: {ex.Message}", Services.SnackbarType.Error);
        }
    }

    // 폴더 추가
    // 폴더 추가 버튼 클릭 시 실행되는 로직입니다.
    private async void AddFolder()
    {
        try
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = true,
                Title = "폴더 추가"
            };

            // 폴더 선택 창을 띄웁니다.
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folderNames = dialog.FileNames.ToList();
                if (folderNames.Count == 0) return;

                // 선택된 모든 폴더에 대해 처리 방식을 한 번만 묻습니다.
                // 첫 번째 폴더 이름을 대표로 보여주고, 전체 개수를 전달합니다.
                var option = await _dialogService.ShowFolderAddOptionAsync(
                    Path.GetFileName(folderNames[0]),
                    folderNames.Count);

                // 취소한 경우 중단합니다.
                if (option == FolderAddOption.Cancel) return;

                foreach (var folder in folderNames)
                {
                    if (option == FolderAddOption.Files)
                    {
                        // '폴더 내 파일 추가' 선택 시: 내부 파일들을 재귀적으로 가져와 추가합니다.
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
                    else if (option == FolderAddOption.Folder)
                    {
                        // '폴더 추가' 선택 시: 폴더 자체를 목록 아이템으로 추가합니다.
                        var item = _fileService.CreateFileItem(folder);
                        if (item != null)
                        {
                            item.UpdateDisplay(ShowExtension);
                            FileList.AddItem(item);
                        }
                    }
                }

                // 목록 정렬 및 프리뷰 업데이트를 수행합니다.
                SortFiles();
                UpdatePreview();
                _snackbarService.Show("폴더 추가가 완료되었습니다.", Services.SnackbarType.Success);
            }
        }
        catch (Exception ex)
        {
            // 예상치 못한 오류 발생 시 사용자에게 알립니다.
            _snackbarService.Show($"폴더 추가 중 오류: {ex.Message}", Services.SnackbarType.Error);
        }
    }

    // 파일 삭제
    private async void DeleteFiles(System.Collections.IList? items)
    {
        try
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
            _snackbarService.Show($"{count}개의 파일이 목록에서 삭제되었습니다.", Services.SnackbarType.Warning);
        }
        catch (Exception ex)
        {
            _snackbarService.Show($"파일 목록 삭제 중 오류: {ex.Message}", Services.SnackbarType.Error);
        }
    }

    // 목록 삭제
    private async Task ListClearAsync()
    {
        if (FileList.Items.Count == 0) return;
        var result = await _dialogService.ShowConfirmationAsync("파일 목록을 전부 삭제하시겠습니까?", "목록 삭제");
        if (result)
        {
            FileList.Clear();
            _snackbarService.Show("파일 목록이 전부 삭제되었습니다.", Services.SnackbarType.Warning);
        }
    }

    // 목록 정렬
    // 설정된 정렬 기준에 따라 파일을 정렬합니다.
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

    // 이름 변경 규칙을 실제 파일/폴더에 적용하는 로직입니다.
    private async void ApplyChanges()
    {
        try
        {
            if (FileList.Items.Count == 0) return;

            // 변경된 내용이 있는지 확인 (OriginalName/Extension과 NewName/Extension 비교)
            bool hasChanges = FileList.Items.Any(i => i.IsChanged);
            if (!hasChanges)
            {
                _snackbarService.Show("변경할 내용이 없습니다.", Services.SnackbarType.Info);
                return;
            }

            // 실제 이름 변경 작업을 수행하고 실패한 항목 리스트를 받습니다.
            var failedItems = _renameService.ApplyRename(FileList.Items, ShowExtension);

            // 실패한 항목이 있으면 사용자에게 알림을 띄웁니다.
            if (failedItems.Count > 0)
            {
                string message = $"{failedItems.Count}개의 이름 변경에 실패했습니다.";
                _snackbarService.Show(message, Services.SnackbarType.Warning, 5000);
            }
            else
            {
                _snackbarService.Show("이름이 변경되었습니다.", Services.SnackbarType.Success);
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Show($"이름 변경 적용 중 오류: {ex.Message}", Services.SnackbarType.Error);
        }
    }

    // 변경된 이름을 이전 상태로 되돌리는 로직입니다.
    private void UndoChanges()
    {
        if (FileList.Items.Count == 0) return;

        // 되돌릴 수 있는 항목(PreviousPath가 있는 항목)이 있는지 확인
        bool canUndo = FileList.Items.Any(i => !string.IsNullOrEmpty(i.PreviousPath));
        if (!canUndo)
        {
            _snackbarService.Show("이름 변경 기록이 없습니다.", Services.SnackbarType.Info);
            return;
        }

        try
        {
            _renameService.UndoRename(FileList.Items, ShowExtension);
            _snackbarService.Show("이름을 되돌렸습니다.", Services.SnackbarType.Warning);
        }
        catch (Exception ex)
        {
            _snackbarService.Show($"되돌리기 중 오류: {ex.Message}", Services.SnackbarType.Error);
        }
    }

    // 현재 설정된 규칙에 따라 변경될 이름의 미리보기를 업데이트합니다.
    private void UpdatePreview()
    {
        if (FileList.Items.Count == 0) return;
        _renameService.UpdatePreview(FileList.Items, _renameViewModel.ResolvedRuleFormat, _renameViewModel.TagManager, ShowExtension);
    }

    // 외부에서 파일이나 폴더를 드래그 앤 드롭했을 때 실행되는 로직입니다.
    public async void AddDroppedItems(string[] paths)
    {
        try
        {
            if (paths == null || paths.Length == 0) return;

            // 드롭된 경로들을 파일과 폴더로 분리합니다.
            var files = paths.Where(System.IO.File.Exists).ToList();
            var folders = paths.Where(System.IO.Directory.Exists).ToList();

            // 파일들은 즉시 추가합니다.
            foreach (var filePath in files)
            {
                var item = _fileService.CreateFileItem(filePath);
                if (item != null)
                {
                    item.UpdateDisplay(ShowExtension);
                    FileList.AddItem(item);
                }
            }

            // 폴더가 포함된 경우 처리 방식을 한 번만 묻습니다.
            if (folders.Count > 0)
            {
                var option = await _dialogService.ShowFolderAddOptionAsync(
                    Path.GetFileName(folders[0]),
                    folders.Count);

                if (option != FolderAddOption.Cancel)
                {
                    foreach (var folderPath in folders)
                    {
                        if (option == FolderAddOption.Files)
                        {
                            // 폴더 내부의 모든 파일들을 재귀적으로 찾아 추가합니다.
                            var filePaths = _fileService.GetFilesInFolder(folderPath);
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
                        else if (option == FolderAddOption.Folder)
                        {
                            // 폴더 자체를 목록에 추가합니다.
                            var item = _fileService.CreateFileItem(folderPath);
                            if (item != null)
                            {
                                item.UpdateDisplay(ShowExtension);
                                FileList.AddItem(item);
                            }
                        }
                    }
                }
            }

            // 모든 항목 추가 후 정렬 및 프리뷰를 업데이트합니다.
            SortFiles();
            UpdatePreview();
            _snackbarService.Show("목록이 추가되었습니다.", Services.SnackbarType.Success);
        }
        catch (Exception ex)
        {
            // 드롭 처리 중 오류 발생 시 알림을 표시합니다.
            _snackbarService.Show($"목록 추가 중 오류: {ex.Message}", Services.SnackbarType.Error);
        }
    }
}
