using System;
using System.IO;
using System.Collections.Generic;
using TagNamer.Models;

namespace TagNamer.Services;

/// <summary>
/// 파일 시스템 조작을 담당하는 서비스입니다.
/// </summary>
public class FileService : IFileService
{
    private readonly ISnackbarService _snackbarService;

    public FileService(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService;
    }

    /// <summary>
    /// 지정된 경로를 분석하여 파일 또는 폴더 아이템을 생성합니다.
    /// </summary>
    public FileItem? CreateFileItem(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                // 파일 정보 읽기
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
                // 폴더 정보 읽기
                var dirInfo = new DirectoryInfo(path);
                return new FileItem
                {
                    OriginalName = dirInfo.Name,
                    NewName = dirInfo.Name,
                    Path = path,
                    // 폴더의 경우 DirectoryName 파싱 시 마지막 구분자를 제거해야 상위 폴더가 정밀하게 잡힙니다.
                    DirectoryName = Path.GetDirectoryName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? string.Empty,
                    // 폴더 크기는 성능을 위해 사용하지 않음.
                    Size = 0,
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
            _snackbarService.Show($"파일(폴더) 가져오기 오류: {ex.Message}", SnackbarType.Error);
            return null;
        }
    }

    /// <summary>
    /// 폴더 내의 모든 파일을 재귀적으로 검색하여 경로 목록을 반환합니다.
    /// </summary>
    public IEnumerable<string> GetFilesInFolder(string folderPath)
    {
        var files = new List<string>();
        var stack = new Stack<string>(); // 재귀 대신 스택으로 명시적 구현
        stack.Push(folderPath);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();
            try
            {
                var dirInfo = new DirectoryInfo(currentDir);

                // 현재 폴더 내의 모든 파일 추가
                foreach (var file in dirInfo.GetFiles())
                {
                    files.Add(file.FullName);
                }

                // 하위 폴더들을 스택에 쌓아 다음 루프에서 탐색
                foreach (var dir in dirInfo.GetDirectories())
                {
                    stack.Push(dir.FullName);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _snackbarService.Show($"{currentDir}의 접근 권한이 없습니다", SnackbarType.Warning);
            }
            catch (Exception ex)
            {
                _snackbarService.Show($"폴더 스캔 중 오류: {ex.Message}", SnackbarType.Error);
            }
        }
        return files;
    }

    /// <summary>
    /// 파일 또는 폴더의 이름을 실제로 변경합니다.
    /// </summary>
    public bool RenameFile(string sourcePath, string destPath)
    {
        try
        {
            // 파일 이름 변경 시도
            if (File.Exists(sourcePath))
            {
                // 동일한 이름의 파일이 이미 존재하는지 체크 (대소문자 무시)
                if (File.Exists(destPath) && !string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    _snackbarService.Show("동일한 이름의 파일이 존재합니다.", SnackbarType.Error);
                    return false;
                }

                File.Move(sourcePath, destPath);
                return true;
            }
            // 폴더 이름 변경 시도
            else if (Directory.Exists(sourcePath))
            {
                if (Directory.Exists(destPath) && !string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    _snackbarService.Show("동일한 이름의 폴더가 존재합니다.", SnackbarType.Error);
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
