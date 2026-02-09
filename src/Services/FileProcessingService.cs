using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagNamer.Models;
using TagNamer.Services;

namespace TagNamer.Services;

/// <summary>
/// 파일 처리 로직 전담 서비스 클래스입니다.
/// </summary>
public class FileProcessingService : IFileProcessingService
{
    private readonly IFileService _fileService;
    private readonly IIconService _iconService;

    public FileProcessingService(IFileService fileService, IIconService iconService)
    {
        _fileService = fileService;
        _iconService = iconService;
    }

    public async Task<FileProcessingResult> ProcessPathsAsync(
        IEnumerable<string> paths,
        bool showExtension,
        int maxItemCount,
        int currentCount,
        bool expandFolders = true,
        IProgress<FileProcessingProgress>? progress = null)
    {
        var result = new FileProcessingResult();
        var rawPaths = paths.ToList();
        result.TotalPathCount = rawPaths.Count;

        // 1. 파일과 폴더 경로 분리 및 폴더 내 파일 추출
        var files = rawPaths.Where(File.Exists).ToList();
        var folders = rawPaths.Where(Directory.Exists).ToList();
        var finalPaths = new List<string>(files);

        if (folders.Count > 0 && expandFolders)
        {
            await Task.Run(() =>
            {
                foreach (var folderPath in folders)
                {
                    finalPaths.AddRange(_fileService.GetFilesInFolder(folderPath));
                }
            });
        }
        else if (folders.Count > 0 && !expandFolders)
        {
            // 폴더 자체를 추가하는 경우 finalPaths에 폴더 경로들이 이미 포함되어 있어야 함
            // 위에서 var finalPaths = new List<string>(files); 로 시작했으므로 folders를 추가해줌
            finalPaths.AddRange(folders);
        }

        int addCount = finalPaths.Count;

        // 2. 정책 검사: 최대 수량 초과 여부
        if (currentCount + addCount > maxItemCount)
        {
            result.IgnoredCount = addCount;
            return result; // 아이템 생성 없이 결과 반환
        }

        if (addCount == 0) return result;

        // 3. FileItem 객체 생성 (병렬 처리 지원 가능하나 안정성을 위해 Task.Run 내부 처리)
        var newItems = await Task.Run(() =>
        {
            var items = new List<FileItem>(addCount);
            for (int i = 0; i < addCount; i++)
            {
                var item = _fileService.CreateFileItem(finalPaths[i]);
                if (item != null)
                {
                    item.Icon = _iconService.GetIcon(item.Path, item.IsFolder);
                    item.UpdateDisplay(showExtension);
                    items.Add(item);
                }

                // 진행률 업데이트 (100개 단위 또는 마지막)
                if (progress != null && (i % 100 == 0 || i == addCount - 1))
                {
                    progress.Report(new FileProcessingProgress
                    {
                        CurrentIndex = i + 1,
                        TotalCount = addCount
                    });
                }
            }
            return items;
        });

        result.NewItems = newItems;
        return result;
    }
}
