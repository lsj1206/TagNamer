using System;
using System.IO;
using System.Collections.Generic;
using TagNamer.Models;

namespace TagNamer.Services;

public class FileService : IFileService
{
    private readonly ISnackbarService _snackbarService;

    public FileService(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
    }

    public FileItem? CreateFileItem(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                return new FileItem
                {
                    OriginalName = fileInfo.Name,
                    NewName = fileInfo.Name,
                    Path = path,
                    DirectoryName = Path.GetDirectoryName(path) ?? string.Empty,
                    Size = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime,
                    IsFolder = false,
                    AddIndex = null
                };
            }
            else if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                return new FileItem
                {
                    OriginalName = dirInfo.Name,
                    NewName = dirInfo.Name,
                    Path = path,
                    DirectoryName = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? string.Empty,
                    Size = 0, // 폴더 크기는 0으로 처리 (계산 비용 문제)
                    CreatedDate = dirInfo.CreationTime,
                    ModifiedDate = dirInfo.LastWriteTime,
                    IsFolder = true,
                    AddIndex = null
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _snackbarService.Show($"파일 정보를 읽는 중 오류가 발생했습니다: {ex.Message}", SnackbarType.Error);
            return null;
        }
    }

    public IEnumerable<string> GetFilesInFolder(string folderPath)
    {
        var files = new List<string>();
        var stack = new Stack<string>();
        stack.Push(folderPath);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();
            try
            {
                var dirInfo = new DirectoryInfo(currentDir);

                foreach (var file in dirInfo.GetFiles())
                {
                    files.Add(file.FullName);
                }

                foreach (var dir in dirInfo.GetDirectories())
                {
                    stack.Push(dir.FullName);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _snackbarService.Show($"폴더 접근 권한이 없습니다: {currentDir}", SnackbarType.Warning);
            }
            catch (Exception ex)
            {
                _snackbarService.Show($"폴더 스캔 중 오류 발생: {ex.Message}", SnackbarType.Error);
            }
        }
        return files;
    }

    public bool RenameFile(string sourcePath, string destPath)
    {
        try
        {
            // 파일인 경우
            if (File.Exists(sourcePath))
            {
                if (File.Exists(destPath) && !string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    _snackbarService.Show("대상 경로에 이미 파일이 존재합니다.", SnackbarType.Warning);
                    return false;
                }

                File.Move(sourcePath, destPath);
                return true;
            }
            // 폴더인 경우
            else if (Directory.Exists(sourcePath))
            {
                if (Directory.Exists(destPath) && !string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    _snackbarService.Show("대상 경로에 이미 폴더가 존재합니다.", SnackbarType.Warning);
                    return false;
                }

                Directory.Move(sourcePath, destPath);
                return true;
            }

            _snackbarService.Show("소스 파일을 찾을 수 없습니다.", SnackbarType.Error);
            return false;
        }
        catch (Exception ex)
        {
            _snackbarService.Show($"이름 변경 실패: {ex.Message}", SnackbarType.Error);
            return false;
        }
    }
}
