using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using TagNamer.Services;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public partial class MainViewModel : ObservableObject
{

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

    public ObservableCollection<SortOption> SortOptions { get; } = new();

    // 현재 규칙 표시 (RenameViewModel과 동기화)
    public string CurrentRuleDisplay => _renameViewModel.RuleFormat;

    // 최대 파일 허용 개수
    private const int MaxItemCount = 50000;

    [ObservableProperty]
    private string selectedLanguage = "en-US";

    [ObservableProperty]
    private bool isBusy = false;

    [ObservableProperty]
    private string busyMessage = "";

    [ObservableProperty]
    private SortOption selectedSortOption;

    [ObservableProperty]
    private bool isSortAscending = true;

    [ObservableProperty]
    private bool confirmDeletion = true;

    [ObservableProperty]
    private bool showExtension = false;

    [ObservableProperty]
    private bool manualEditMode = false;

    public IRelayCommand AddFilesCommand { get; }
    public IRelayCommand AddFolderCommand { get; }
    public IRelayCommand<System.Collections.IList> DeleteFilesCommand { get; }
    public IRelayCommand ListClearCommand { get; }
    public IRelayCommand OpenRuleSettingsCommand { get; }
    public IRelayCommand ApplyChangesCommand { get; }
    public IRelayCommand UndoChangesCommand { get; }
    public IRelayCommand ReorderNumberCommand { get; }
    public IAsyncRelayCommand<FileItem> ManualEditCommand { get; }

    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly ISnackbarService _snackbarService;
    private readonly IFileService _fileService;
    private readonly IRenameService _renameService;
    private readonly ISortingService _sortingService;
    private readonly ILanguageService _languageService;

    private readonly RenameViewModel _renameViewModel;

    public MainViewModel(
        IWindowService windowService,
        IDialogService dialogService,
        ISnackbarService snackbarService,
        IFileService fileService,
        IRenameService renameService,
        ISortingService sortingService,
        ILanguageService languageService,
        SnackbarViewModel snackbarViewModel,
        RenameViewModel renameViewModel)
    {
        _windowService = windowService;
        _dialogService = dialogService;
        _snackbarService = snackbarService;
        _fileService = fileService;
        _renameService = renameService;
        _sortingService = sortingService;
        _languageService = languageService;
        Snackbar = snackbarViewModel;
        _renameViewModel = renameViewModel;

        // 시스템 언어 감지 및 설정
        selectedLanguage = _languageService.GetSystemLanguage();
        _languageService.ChangeLanguage(selectedLanguage);
        _renameViewModel.TagManager.RefreshLanguage();

        // RenameViewModel의 RuleFormat 변경 시 UI 알림
        _renameViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(RenameViewModel.RuleFormat))
            {
                OnPropertyChanged(nameof(CurrentRuleDisplay));
                UpdatePreview();
            }
        };

        // 정렬 옵션 초기화
        InitializeSortOptions();
        selectedSortOption = SortOptions.First();

        AddFilesCommand = new RelayCommand(() => AddFiles(false));
        AddFolderCommand = new RelayCommand(() => AddFiles(true));
        DeleteFilesCommand = new RelayCommand<System.Collections.IList>(DeleteFiles);
        ListClearCommand = new AsyncRelayCommand(ListClearAsync);
        OpenRuleSettingsCommand = new RelayCommand(OpenRenameWindow);
        ApplyChangesCommand = new AsyncRelayCommand(ApplyChangesAsync);
        UndoChangesCommand = new AsyncRelayCommand(UndoChanges);
        ReorderNumberCommand = new RelayCommand(ReorderNumber);
        ManualEditCommand = new AsyncRelayCommand<FileItem>(ManualEditAsync);
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
        _renameService.UpdatePreview(FileList.Items, _renameViewModel.ResolvedRuleFormat, _renameViewModel.TagManager);
    }

    // 파일/폴더 추가 (다이얼로그 진입점)
    private async void AddFiles(bool isFolder)
    {
        if (isFolder)
        {
            var dialog = new OpenFolderDialog
            {
                Multiselect = true,
                Title = _languageService.GetString("Dlg_Title_AddFolder", "폴더 추가")
            };

            if (dialog.ShowDialog() == true)
            {
                await ProcessFiles(dialog.FolderNames);
            }
        }
        else
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = _languageService.GetString("Dlg_Title_AddFile", "파일 추가")
            };

            if (dialog.ShowDialog() == true)
            {
                await ProcessFiles(dialog.FileNames);
            }
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
        IsBusy = true;

        try
        {
            var rawPaths = paths.ToList();
            var files = rawPaths.Where(File.Exists).ToList();
            var folders = rawPaths.Where(Directory.Exists).ToList();
            var finalPaths = new List<string>(files);

            // 폴더가 포함된 경우 옵션 확인
            if (folders.Count > 0)
            {
                var option = await _dialogService.ShowFolderAddOptionAsync(Path.GetFileName(folders[0]), folders.Count);
                if (option == FolderAddOption.Cancel)
                {
                    if (files.Count == 0) return;
                }
                else
                {
                    await Task.Run(() =>
                    {
                        foreach (var folderPath in folders)
                        {
                            if (option == FolderAddOption.Files)
                                finalPaths.AddRange(_fileService.GetFilesInFolder(folderPath));
                            else if (option == FolderAddOption.Folder)
                                finalPaths.Add(folderPath);
                        }
                    });
                }
            }

            // 개수 제한 정책
            int currentCount = FileList.Items.Count;
            int addCount = finalPaths.Count;

            if (currentCount + addCount > MaxItemCount)
            {
                var msg = _languageService.GetString("Msg_MaxItemExceeded", "총 파일 개수가 {0:N0}개를 초과하여 작업을 취소합니다.\n(현재: {1:N0} + 추가: {2:N0})");
                _snackbarService.Show(string.Format(msg, MaxItemCount, currentCount, addCount), Services.SnackbarType.Error);
                return;
            }

            if (addCount == 0) return;

            // 진행률 표시 시작
            _snackbarService.ShowProgress(_languageService.GetString("Msg_LoadingFile", "파일 불러오는 중..."));

            // 아이템 생성 및 추가
            var newItems = await Task.Run(() =>
            {
                var items = new List<FileItem>(addCount);
                for (int i = 0; i < addCount; i++)
                {
                    var item = _fileService.CreateFileItem(finalPaths[i]);
                    if (item != null)
                    {
                        item.UpdateDisplay(ShowExtension);
                        items.Add(item);
                    }

                    // 100개 단위 또는 마지막에 진행률 업데이트 (UI 부하 감소)
                    if (i % 100 == 0 || i == addCount - 1)
                    {
                        double percent = (double)(i + 1) / addCount * 100;
                        var progressMsg = _languageService.GetString("Msg_LoadingFileProgress", "파일 불러오는 중... ({0:0}%)");
                        _snackbarService.UpdateProgress(string.Format(progressMsg, percent));
                    }
                }
                return items;
            });

            AddFilesToList(newItems);
        }
        catch (Exception ex)
        {
            var errorMsg = _languageService.GetString("Msg_FileAddError", "파일 추가 중 오류가 발생했습니다: {0}");
            _snackbarService.Show(string.Format(errorMsg, ex.Message), Services.SnackbarType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 이미 생성된 아이템들을 목록에 추가합니다.
    /// </summary>
    private void AddFilesToList(List<FileItem> items)
    {
        int totalCount = items.Count;
        if (totalCount == 0) return;

        // 배치 처리 및 지연 제거 -> 한 번에 추가
        int successCount = FileList.AddRange(items);

        SortFiles();
        UpdatePreview();

        // 스낵바 알림
        if (successCount == 0 && totalCount > 0)
        {
            _snackbarService.Show(_languageService.GetString("Msg_AlreadyExists", "목록에 이미 존재합니다."), Services.SnackbarType.Error);
        }
        else if (successCount < totalCount)
        {
            var partialMsg = _languageService.GetString("Msg_AddFilePartial", "{0}개중 {1}개를 추가합니다.");
            _snackbarService.Show(string.Format(partialMsg, totalCount, successCount), Services.SnackbarType.Warning);
        }
        else if (successCount > 0)
        {
            var successMsg = _languageService.GetString("Msg_AddFile", "{0}개를 추가합니다.");
            _snackbarService.Show(string.Format(successMsg, successCount), Services.SnackbarType.Success);
        }
    }

    // 목록의 번호를 현재 순서에 맞게 다시 매기는 로직입니다.
    private async void ReorderNumber()
    {
        if (FileList.Items.Count == 0) return;

        var result = await _dialogService.ShowConfirmationAsync(
                _languageService.GetString("Dlg_Ask_ReorderIndex", "번호를 현재 순서대로 정렬하시겠습니까?\n기존 번호는 초기화됩니다."),
                _languageService.GetString("Dlg_Title_ReorderIndex", "번호 재정렬"));

        if (result)
        {
            FileList.ReorderIndex();
            _snackbarService.Show(_languageService.GetString("Msg_ReorderIndex", "번호를 재정렬합니다."), Services.SnackbarType.Success);
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

    // 파일 삭제
    private async void DeleteFiles(System.Collections.IList? items)
    {
        if (items == null || items.Count == 0) return;

        // 삭제할 항목 리스트 복사 (순회 중 컬렉션 변경 방지)
        var itemsToDelete = items.Cast<TagNamer.Models.FileItem>().ToList();
        int count = itemsToDelete.Count;

        if (ConfirmDeletion)
        {
            string message;
            if (count == 1)
            {
                message = _languageService.GetString("Dlg_Ask_DeleteSingle", "선택된 파일을 목록에서 삭제하시겠습니까?");
            }
            else
            {
                var multiMsg = _languageService.GetString("Dlg_Ask_DeleteMulti", "{0}개의 선택된 파일을 목록에서 삭제하시겠습니까?");
                message = string.Format(multiMsg, count);
            }

            var result = await _dialogService.ShowConfirmationAsync(message, _languageService.GetString("Dlg_Title_DeleteConfirm", "삭제 확인"));
            if (!result) return;
        }

        FileList.RemoveItems(itemsToDelete);
        var removedMsg = _languageService.GetString("Msg_RemoveFile", "{0}개를 목록에서 제거합니다.");
        _snackbarService.Show(string.Format(removedMsg, count), Services.SnackbarType.Warning);
    }

    // 목록 삭제
    private async Task ListClearAsync()
    {
        if (FileList.Items.Count == 0) return;
        var result = await _dialogService.ShowConfirmationAsync(
            _languageService.GetString("Dlg_Ask_ClearList", "파일 목록을 전부 삭제하시겠습니까?"),
            _languageService.GetString("Dlg_Title_ClearList", "목록 삭제"));
        if (result)
        {
            FileList.Clear();
            _snackbarService.Show(_languageService.GetString("Msg_ClearList", "목록을 전부 제거합니다."), Services.SnackbarType.Success);
        }
    }

    // 목록 정렬
    private void SortFiles()
    {
        FileList.Sorting(_sortingService, SelectedSortOption, IsSortAscending);
    }

    // 정렬 기준 선택
    partial void OnSelectedSortOptionChanged(SortOption value)
    {
        SortFiles();
    }

    // 정렬 방향 (토글) 변경 시
    partial void OnIsSortAscendingChanged(bool value)
    {
        SortFiles();
    }

    // 이름 변경 규칙을 실제 파일/폴더에 적용하는 로직입니다.
    private async Task ApplyChangesAsync()
    {
        if (FileList.Items.Count == 0) return;

        // 변경된 내용이 있는지 확인 (OriginalName/Extension과 NewName/Extension 비교)
        // 전체 리스트를 확인하지만 변경된 것만 실제 작업
        if (!FileList.Items.Any(i => i.IsChanged))
        {
            _snackbarService.Show(_languageService.GetString("Msg_NoRuleChanges", "변경된 규칙이 없습니다."), Services.SnackbarType.Info);
            return;
        }

        // 실제 이름 변경 작업을 수행합니다. (서비스 내부에서 결과 보고)
        IsBusy = true;
        _snackbarService.ShowProgress(_languageService.GetString("Msg_ApplyingChanges", "이름 변경 적용 중..."));

        try
        {
            var targetCount = FileList.Items.Count(i => i.IsChanged);
            var progress = new Progress<int>(current =>
            {
                double percent = (double)current / targetCount * 100;
                var applyingProgressMsg = _languageService.GetString("Msg_ApplyingProgress", "이름 변경 적용 중... ({0:0}%)");
                _snackbarService.UpdateProgress(string.Format(applyingProgressMsg, percent));
            });

            await _renameService.ApplyRenameAsync(FileList.Items, progress);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 개별 파일 수동 편집
    private async Task ManualEditAsync(FileItem? item)
    {
        if (item == null || !ManualEditMode) return;

        var newName = await _dialogService.ShowManualEditAsync(item.NewName);
        if (newName != null)
        {
            item.NewName = newName;
        }
    }

    // 변경된 이름을 이전 상태로 되돌리는 로직입니다.
    private async Task UndoChanges()
    {
        if (FileList.Items.Count == 0) return;

        // 되돌릴 수 있는 항목(PreviousPath가 있는 항목)이 있는지 확인
        var undoTargets = FileList.Items.Where(i => !string.IsNullOrEmpty(i.PreviousPath)).ToList();
        if (undoTargets.Count == 0)
        {
            _snackbarService.Show(_languageService.GetString("Msg_NoChangeHistory", "변경된 기록이 없습니다."), Services.SnackbarType.Info);
            return;
        }

        IsBusy = true;
        _snackbarService.ShowProgress(_languageService.GetString("Msg_UndoingChanges", "변경 취소 중..."));

        try
        {
            var targetCount = undoTargets.Count;
            var progress = new Progress<int>(current =>
            {
                double percent = (double)current / targetCount * 100;
                var undoProgressMsg = _languageService.GetString("Msg_UndoProgress", "변경 취소 중... ({0:0}%)");
                _snackbarService.UpdateProgress(string.Format(undoProgressMsg, percent));
            });

            await _renameService.UndoRenameAsync(FileList.Items, progress);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 정렬 옵션 초기화 (리소스에서 가져오기)
    private void InitializeSortOptions()
    {
        SortOptions.Clear();

        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_Index", "번호"), Type = SortType.AddIndex });
        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_NameIndex", "이름-번호"), Type = SortType.NameNumber });
        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_NamePath", "이름-경로"), Type = SortType.NamePath });
        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_PathIndex", "경로-번호"), Type = SortType.PathNumber });
        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_PathName", "경로-이름"), Type = SortType.PathName });
        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_Size", "크기"), Type = SortType.Size });
        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_CreatedDate", "생성일"), Type = SortType.CreatedDate });
        SortOptions.Add(new SortOption { Display = _languageService.GetString("Sort_ModifiedDate", "수정일"), Type = SortType.ModifiedDate });
    }

    // 언어 변경 시 호출
    partial void OnSelectedLanguageChanged(string value)
    {
        _languageService.ChangeLanguage(value);

        // 정렬 옵션 재로드
        var currentType = SelectedSortOption?.Type ?? SortType.AddIndex;
        InitializeSortOptions();
        SelectedSortOption = SortOptions.FirstOrDefault(o => o.Type == currentType) ?? SortOptions.First();

        // 태그 설명 및 툴팁 갱신
        _renameViewModel.TagManager.RefreshLanguage();
    }

}
