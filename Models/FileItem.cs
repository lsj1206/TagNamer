// 파일/폴더 정보 Model

using CommunityToolkit.Mvvm.ComponentModel;

namespace TagNamer.Models;

public partial class FileItem : ObservableObject
{
    // 필수 속성
    public required string OriginalName { get; set; }
    public required string DirectoryName { get; set; }
    public required string Path { get; set; }

    // 표시용 속성
    [ObservableProperty]
    private string displayOriginalName = string.Empty;

    [ObservableProperty]
    private string displayNewName = string.Empty;

    // 자동 계산 속성
    public string NewName { get; set; } = string.Empty;

    // Undo를 위한 이전 경로
    public string PreviousPath { get; set; } = string.Empty;

    // 원본 이름과 새 이름이 다른지 여부를 반환합니다.
    public bool IsChanged => !string.IsNullOrEmpty(NewName) && OriginalName != NewName;

    public long Size { get; set; }
    public bool IsFolder { get; set; }

    [ObservableProperty]
    private int? addIndex;  // 파일만 번호, 폴더는 null
    // 파일 날짜 정보
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    // 파일 확장자
    public string Extension => IsFolder ? string.Empty : System.IO.Path.GetExtension(OriginalName);

    // 확장자를 제외한 파일명 (중복 호출 방지용)
    public string NameWithoutExtension => IsFolder ? OriginalName : System.IO.Path.GetFileNameWithoutExtension(OriginalName);

    // 파일 크기를 읽기 쉬운 형태로 변환
    public string UnitSize => FormatFileSize(Size);
    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return string.Empty;

        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.#} {sizes[order]}";
    }

    public void UpdateDisplay(bool showExtension)
    {
        if (IsFolder)
        {
            DisplayOriginalName = OriginalName;
            DisplayNewName = NewName;
            return;
        }

        if (showExtension)
        {
            DisplayOriginalName = OriginalName;
            DisplayNewName = NewName;
        }
        else
        {
            DisplayOriginalName = NameWithoutExtension;
            DisplayNewName = string.IsNullOrEmpty(NewName)
                ? string.Empty
                : System.IO.Path.GetFileNameWithoutExtension(NewName);
        }
    }
}
