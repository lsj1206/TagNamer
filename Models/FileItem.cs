// 파일/폴더 정보 Model

using CommunityToolkit.Mvvm.ComponentModel;

namespace TagNamer.Models;

public partial class FileItem : ObservableObject
{
    // 파일명이 포함된 전체 경로
    private string _path = default!;
    public required string Path
    {
        get => _path;
        set
        {
            if (SetProperty(ref _path, value))
            {
                ParsePathInfo();
            }
        }
    }

    // 기본 데이터
    public string Directory { get; private set; } = string.Empty;
    public string BaseName { get; private set; } = string.Empty;
    public string BaseExtension { get; private set; } = string.Empty;
    public long Size { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // 변경 데이터
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChanged))]
    private string newName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChanged))]
    private string newExtension = string.Empty;

    // 표시용 번호/이름/확장자/크기
    [ObservableProperty]
    private int? addIndex;
    [ObservableProperty]
    private string displayBaseName = string.Empty;
    [ObservableProperty]
    private string displayNewName = string.Empty;
    public string DisplaySize { get; set; } = string.Empty;

    // 파일/폴더 구분
    public bool IsFolder { get; set; }

    // Undo를 위한 변경전 정보
    public string PreviousPath { get; set; } = string.Empty;

    // 변경 여부: 이름이나 확장자 중 하나라도 바뀌면 변경된 것임
    public bool IsChanged =>
        (!string.IsNullOrEmpty(NewName) && BaseName != NewName) ||
        (!string.IsNullOrEmpty(NewExtension) && BaseExtension != NewExtension);

    // NewName이나 NewExtension이 바뀔 때마다 표시용 이름도 갱신
    // (UpdateDisplay 호출 시 필요한 showExtension 상태를 모르므로
    // 기본적으로 마지막 상태를 기억하거나, setter에서 명시적으로 호출 필요)
    private bool _lastShowExtension = false;
    partial void OnNewNameChanged(string value) => UpdateDisplay(_lastShowExtension);
    partial void OnNewExtensionChanged(string value) => UpdateDisplay(_lastShowExtension);

    private void ParsePathInfo()
    {
        if (IsFolder)
        {
            // 폴더일 경우: Directory는 상위 폴더 경로, BaseName은 폴더명
            Directory = System.IO.Path.GetDirectoryName(_path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)) ?? string.Empty;
            BaseName = System.IO.Path.GetFileName(_path); // 폴더명
            BaseExtension = string.Empty;
        }
        else
        {
            Directory = System.IO.Path.GetDirectoryName(_path) ?? string.Empty;
            BaseName = System.IO.Path.GetFileNameWithoutExtension(_path);
            BaseExtension = System.IO.Path.GetExtension(_path);
        }

        // 초기화: 변경될 값들은 원본 값으로 시작
        NewName = BaseName;
        NewExtension = BaseExtension;

        // 정보가 바뀌었으므로 표시 이름도 갱신
        UpdateDisplay(_lastShowExtension);
    }

    // 확장자 표시 여부에 따라 표시 이름 변경
    public void UpdateDisplay(bool showExtension)
    {
        _lastShowExtension = showExtension;
        if (IsFolder)
        {
            DisplayBaseName = BaseName;
            DisplayNewName = NewName;
            return;
        }

        if (showExtension)
        {
            DisplayBaseName = BaseName + BaseExtension;
            DisplayNewName = NewName + NewExtension;
        }
        else
        {
            DisplayBaseName = BaseName;
            DisplayNewName = NewName;
        }
    }
}
