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
    private bool isIndividualEditMode = false;

    public IRelayCommand AddFilesCommand { get; }
    public IRelayCommand AddFolderCommand { get; }
    public IRelayCommand<System.Collections.IList> DeleteFilesCommand { get; }
    public IRelayCommand ListClearCommand { get; }
    public IRelayCommand OpenRuleSettingsCommand { get; }
    public IRelayCommand ApplyChangesCommand { get; }
    public IRelayCommand UndoChangesCommand { get; }
    public IRelayCommand ExtensionCommand { get; }
    public IRelayCommand ReorderNumberCommand { get; }

    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly ISnackbarService _snackbarService;
    private readonly IFileService _fileService;
    private readonly IRenameService _renameService;
    private readonly ISortingService _sortingService;

    private readonly RenameViewModel _renameViewModel;
    private readonly ExtensionViewModel _extensionViewModel;

    public MainViewModel(
        IWindowService windowService,
        IDialogService dialogService,
        ISnackbarService snackbarService,
        IFileService fileService,
        IRenameService renameService,
        ISortingService sortingService,
        SnackbarViewModel snackbarViewModel,
        RenameViewModel renameViewModel,
        ExtensionViewModel extensionViewModel)
    {
        _windowService = windowService;
        _dialogService = dialogService;
        _snackbarService = snackbarService;
        _fileService = fileService;
        _renameService = renameService;
        _sortingService = sortingService;
        Snackbar = snackbarViewModel;
        _renameViewModel = renameViewModel;
        _extensionViewModel = extensionViewModel;

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

        AddFilesCommand = new RelayCommand(() => AddFiles(false));
        AddFolderCommand = new RelayCommand(() => AddFiles(true));
        DeleteFilesCommand = new RelayCommand<System.Collections.IList>(DeleteFiles);
        ListClearCommand = new AsyncRelayCommand(ListClearAsync);
        OpenRuleSettingsCommand = new RelayCommand(OpenRenameWindow);
        ApplyChangesCommand = new RelayCommand(ApplyChanges);
        UndoChangesCommand = new RelayCommand(UndoChanges);
        ExtensionCommand = new RelayCommand(Extension);
        ReorderNumberCommand = new RelayCommand(ReorderNumber);
    }

    private void OpenRenameWindow()
    {
        // View 타입(RenameWindow)을 직접 명시하긴 하지만,
        // 이는 Generic 제약을 만족하기 위함이며 생성 및 바인딩 로직은 서비스가 담당합니다.
        _windowService.ShowDialog<TagNamer.Views.RenameWindow>(_renameViewModel);
    }

    // 현재 설정된 규칙에 따라 변경될 이름의 미리보기를 업데이트합니다.
    private void UpdatePreview()
    {
        if (FileList.Items.Count == 0) return;
        _renameService.UpdatePreview(FileList.Items, _renameViewModel.ResolvedRuleFormat, _renameViewModel.TagManager, ShowExtension);
    }

    // 파일/폴더 추가 (다이얼로그 진입점)
    private async void AddFiles(bool isFolder)
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = isFolder,
            Multiselect = true,
            Title = isFolder ? "폴더 추가" : "파일 추가"
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            await ProcessFiles(dialog.FileNames);
        }
    }

    // 드래그 앤 드롭 진입점
    public async void DropFiles(string[] paths)
    {
        await ProcessFiles(paths);
    }

    /// <summary>
    /// 파일 추가 프로세스 전체를 관장합니다.
    /// </summary>
    private async Task ProcessFiles(IEnumerable<string> paths)
    {
        var finalPaths = await FileScanner(paths);
        AddFilesToList(finalPaths);
    }

    /// <summary>
    /// 입력받은 경로들을 스캔하여 폴더 옵션에 따라 최종 파일/폴더 리스트를 반환합니다.
    /// </summary>
    private async Task<List<string>> FileScanner(IEnumerable<string> paths)
    {
        if (paths == null) return new List<string>();
        var rawPaths = paths.ToList();
        if (rawPaths.Count == 0) return new List<string>();

        var files = rawPaths.Where(File.Exists).ToList();
        var folders = rawPaths.Where(Directory.Exists).ToList();
        var finalPaths = new List<string>(files);

        if (folders.Count > 0)
        {
            var option = await _dialogService.ShowFolderAddOptionAsync(Path.GetFileName(folders[0]), folders.Count);
            if (option != FolderAddOption.Cancel)
            {
                foreach (var folderPath in folders)
                {
                    if (option == FolderAddOption.Files)
                        finalPaths.AddRange(_fileService.GetFilesInFolder(folderPath));
                    else if (option == FolderAddOption.Folder)
                        finalPaths.Add(folderPath);
                }
            }
            else if (files.Count == 0)
            {
                return new List<string>();
            }
        }

        return finalPaths;
    }

    /// <summary>
    /// 준비된 경로들을 기반으로 아이템을 생성하고 목록에 추가합니다.
    /// </summary>
    private void AddFilesToList(IEnumerable<string> paths)
    {
        var pathList = paths.ToList();
        int totalCount = pathList.Count;
        if (totalCount == 0) return;

        int successCount = 0;
        foreach (var path in pathList)
        {
            var item = _fileService.CreateFileItem(path);
            if (item != null)
            {
                item.UpdateDisplay(ShowExtension);
                if (FileList.AddItem(item))
                {
                    successCount++;
                }
            }
        }

        SortFiles();
        UpdatePreview();
        // 스낵바 알림
        if (successCount == 0 && totalCount > 0)
        {
            _snackbarService.Show("목록에 이미 존재합니다.", Services.SnackbarType.Error);
        }
        else if (successCount < totalCount)
        {
            _snackbarService.Show($"{totalCount}개중 {successCount}개를 추가합니다.", Services.SnackbarType.Warning);
        }
        else if (successCount > 0)
        {
            _snackbarService.Show($"{successCount}개를 추가합니다.", Services.SnackbarType.Success);
        }
    }

    // 목록의 번호를 현재 순서에 맞게 다시 매기는 로직입니다.
    private async void ReorderNumber()
    {
        if (FileList.Items.Count == 0) return;

        var result = await _dialogService.ShowConfirmationAsync(
                "번호를 현재 순서대로 정렬하시겠습니까?\n기존 번호는 초기화됩니다.",
                "번호 재정렬");

        if (result)
        {
            FileList.ReorderIndex();
            _snackbarService.Show("번호를 재정렬합니다.", Services.SnackbarType.Success);
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
        UpdateSortDirection(isAscending: true);
    }

    // 내림차순
    partial void OnSortDescendingChanged(bool value)
    {
        if (_isUpdatingSortDirection || !value) return;
        UpdateSortDirection(isAscending: false);
    }

    private void UpdateSortDirection(bool isAscending)
    {
        _isUpdatingSortDirection = true;
        if (isAscending)
            SortDescending = false;
        else
            SortAscending = false;
        _isUpdatingSortDirection = false;
        SortFiles();
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

        FileList.RemoveItems(itemsToDelete);
        _snackbarService.Show($"{count}개를 목록에서 제거합니다.", Services.SnackbarType.Warning);
    }

    // 목록 삭제
    private async Task ListClearAsync()
    {
        if (FileList.Items.Count == 0) return;
        var result = await _dialogService.ShowConfirmationAsync("파일 목록을 전부 삭제하시겠습니까?", "목록 삭제");
        if (result)
        {
            FileList.Clear();
            _snackbarService.Show("목록을 전부 제거합니다.", Services.SnackbarType.Success);
        }
    }

    // 목록 정렬
    private void SortFiles()
    {
        FileList.Sorting(_sortingService, SelectedSortOption, SortAscending);
    }

    // 이름 변경 규칙을 실제 파일/폴더에 적용하는 로직입니다.
    private async void ApplyChanges()
    {
        if (FileList.Items.Count == 0) return;

        // 변경된 내용이 있는지 확인 (OriginalName/Extension과 NewName/Extension 비교)
        bool hasChanges = FileList.Items.Any(i => i.IsChanged);
        if (!hasChanges)
        {
            _snackbarService.Show("변경된 규칙이 없습니다.", Services.SnackbarType.Info);
            return;
        }

        // 실제 이름 변경 작업을 수행합니다. (서비스 내부에서 결과 보고)
        _renameService.ApplyRename(FileList.Items, ShowExtension);
    }

    // 변경된 이름을 이전 상태로 되돌리는 로직입니다.
    private void UndoChanges()
    {
        if (FileList.Items.Count == 0) return;

        // 되돌릴 수 있는 항목(PreviousPath가 있는 항목)이 있는지 확인
        bool canUndo = FileList.Items.Any(i => !string.IsNullOrEmpty(i.PreviousPath));
        if (!canUndo)
        {
            _snackbarService.Show("변경된 기록이 없습니다.", Services.SnackbarType.Info);
            return;
        }
        _renameService.UndoRename(FileList.Items, ShowExtension);
    }

    private void Extension()
    {
        // ExtensionViewModel은 Transient이므로 매번 새로 받아오거나, Factory 패턴을 써야 하지만
        // 여기서는 간단하게 DI로 주입받은 인스턴스(하나)를 재사용하거나,
        // MainViewModel 생성자에서 Transient로 하나만 받아와서 재사용하는 방식을 씁니다.
        // 다만 Transient인데 주입받으면 그 인스턴스는 계속 유지됩니다.
        // 상태 초기화(Initialize)를 하므로 재사용해도 문제없습니다.

        // 1. 상태 초기화
        _extensionViewModel.Initialize(FileList.Items);

        // 2. 다이얼로그 표시
        var result = _windowService.ShowDialog<TagNamer.Views.ExtensionWindow>(_extensionViewModel);

        // 3. 결과 처리 (변경 사항이 있으면 미리보기 갱신)
        if (result == true)
        {
            UpdatePreview();
            _snackbarService.Show("확장자 변경 규칙이 적용되었습니다.", SnackbarType.Success);
        }
    }
}
