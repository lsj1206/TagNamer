using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagNamer.Models;

namespace TagNamer.Services;

public class FileProcessingProgress
{
    public int CurrentIndex { get; set; }
    public int TotalCount { get; set; }
    public double Percent => TotalCount > 0 ? (double)CurrentIndex / TotalCount * 100 : 0;
}

public class FileProcessingResult
{
    public List<FileItem> NewItems { get; set; } = new();
    public int TotalPathCount { get; set; }
    public int IgnoredCount { get; set; }
}

/// <summary>
/// 파일 및 폴더 경로를 입력받아 비즈니스 규칙(최대 개수 등)에 따라 처리하는 서비스 인터페이스입니다.
/// </summary>
public interface IFileProcessingService
{
    /// <summary>
    /// 입력된 경로들을 분석하여 FileItem 리스트를 반환합니다.
    /// </summary>
    /// <param name="paths">파일 또는 폴더 경로 목록</param>
    /// <param name="showExtension">확장자 표시 여부 (FileItem 초기 상태 설정용)</param>
    /// <param name="maxItemCount">최대 수용 가능 아이템 수</param>
    /// <param name="currentCount">현재 목록에 있는 아이템 수</param>
    /// <param name="progress">진행률 보고객체</param>
    Task<FileProcessingResult> ProcessPathsAsync(
        IEnumerable<string> paths,
        bool showExtension,
        int maxItemCount,
        int currentCount,
        bool expandFolders = true,
        IProgress<FileProcessingProgress>? progress = null);
}
