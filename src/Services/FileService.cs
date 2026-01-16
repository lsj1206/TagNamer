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
    private static readonly string[] SizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    public FileService()
    {
    }

    /// <summary>
    /// 지정된 경로를 분석하여 파일 또는 폴더 아이템을 생성합니다.
    /// </summary>
    public FileItem? CreateFileItem(string path)
    {
        if (File.Exists(path))
        {
            var fileInfo = new FileInfo(path);
            return new FileItem
            {
                Path = path,
                Size = fileInfo.Length,
                DisplaySize = FormatFileSize(fileInfo.Length),
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
                Path = path,
                Size = 0, // 폴더 크기는 성능을 위해 사용하지 않음.
                CreatedDate = dirInfo.CreationTime,
                ModifiedDate = dirInfo.LastWriteTime,
                IsFolder = true,
                AddIndex = null
            };
        }
        return null;
    }

    /// <summary>
    /// 폴더 내의 모든 파일을 재귀적으로 검색하여 경로 목록을 반환합니다.
    /// </summary>
    public IEnumerable<string> GetFilesInFolder(string folderPath)
    {
        var files = new List<string>();
        var stack = new Stack<string>();
        stack.Push(folderPath);

        while (stack.Count > 0)
        {
            var currentDir = stack.Pop();
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
        return files;
    }

    /// <summary>
    /// 파일 또는 폴더의 이름을 변경합니다.
    /// </summary>
    public void RenameFile(string sourcePath, string destPath)
    {
        // 대소문자 무시하고 같은 경로라면 생략
        if (string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
            return;

        // 존재 여부 확인 및 이동
        if (File.Exists(sourcePath))
        {
            if (File.Exists(destPath))
                throw new IOException("변경할 이름의 파일이 존재합니다.");

            File.Move(sourcePath, destPath);
        }
        else if (Directory.Exists(sourcePath))
        {
            if (Directory.Exists(destPath))
                throw new IOException("변경할 이름의 폴더가 존재합니다.");

            Directory.Move(sourcePath, destPath);
        }
        else
        {
            throw new FileNotFoundException("원본 파일 또는 폴더를 찾을 수 없습니다.");
        }
    }
    /// <summary>
    /// 파일 크기를 읽기 쉬운 형태로 변환합니다.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        int i = (int)Math.Log(bytes, 1024);
        i = Math.Min(i, SizeSuffixes.Length - 1);
        double readable = bytes / Math.Pow(1024, i);
        return $"{readable:0.#} {SizeSuffixes[i]}";
    }
}
