using System;
using System.IO;
using System.Collections.Generic;
using TagNamer.Models;

namespace TagNamer.Services;

public class FileService : IFileService
{
    public FileItem? CreateFileItem(string path)
    {
        try
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
                AddIndex = null
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating file item: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"Access denied to: {currentDir}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing folder {currentDir}: {ex.Message}");
            }
        }
        return files;
    }

    public bool RenameFile(string sourcePath, string destPath)
    {
        try
        {
            if (!File.Exists(sourcePath)) return false;

            // 대상 파일이 이미 존재하면 실패 (덮어쓰기 방지)
            if (File.Exists(destPath) && !string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                return false;

            // 대소문자만 변경되는 경우를 위해 Move 사용
            File.Move(sourcePath, destPath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error renaming file: {ex.Message}");
            return false;
        }
    }
}
