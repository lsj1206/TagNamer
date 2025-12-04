using System;
using System.Collections.ObjectModel;
using TagNamer.Models;
using System.Linq;

namespace TagNamer.ViewModels;

public class FileListViewModel
{
    public ObservableCollection<FileItem> Items { get; } = new();
    private int _nextAddIndex = 1;

    public void Clear()
    {
        Items.Clear();
        _nextAddIndex = 1;
    }

    // 파일 경로를 받아 목록에 추가하는 메서드
    public void AddFile(string path)
    {
        try
        {
            // 중복 파일 체크
            if (Items.Any(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var fileInfo = new System.IO.FileInfo(path);
            var item = new FileItem
            {
                AddIndex = _nextAddIndex++,
                OriginalName = fileInfo.Name,
                NewName = fileInfo.Name,
                Path = path,
                DirectoryName = System.IO.Path.GetDirectoryName(path) ?? string.Empty,
                Size = fileInfo.Length,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime
            };
            Items.Add(item);
        }
        catch (Exception ex)
        {
            // 로그 또는 에러 처리
            System.Diagnostics.Debug.WriteLine($"Error adding file: {ex.Message}");
        }
    }

    // 폴더 내의 파일을 재귀적으로 추가하는 메서드 (예외 처리 강화)
    public void AddFolder(string folderPath)
    {
        var stack = new System.Collections.Generic.Stack<string>();
        stack.Push(folderPath);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();

            try
            {
                var dirInfo = new System.IO.DirectoryInfo(currentDir);

                // 현재 폴더의 파일 추가
                foreach (var file in dirInfo.GetFiles())
                {
                    AddFile(file.FullName);
                }

                // 하위 폴더를 스택에 추가
                foreach (var dir in dirInfo.GetDirectories())
                {
                    stack.Push(dir.FullName);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 권한 없는 폴더는 건너뜀
                System.Diagnostics.Debug.WriteLine($"Access denied to: {currentDir}");
            }
            catch (Exception ex)
            {
                // 기타 오류 처리
                System.Diagnostics.Debug.WriteLine($"Error processing folder {currentDir}: {ex.Message}");
            }
        }
    }
}
