// 파일/폴더 정보 Model

namespace TagNamer.Models;

public class FileItem
{
    // 필수 속성
    public required string OriginalName { get; set; }
    public required string Path { get; set; }
    // 자동 계산 속성
    public string NewName { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsFolder { get; set; }
    public int? AddIndex { get; set; }  // 파일만 번호, 폴더는 null
    // 파일 날짜 정보
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    // 파일 확장자
    public string Extension => IsFolder ? string.Empty : System.IO.Path.GetExtension(OriginalName);
    // 확장자 제외된 파일명
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
}
