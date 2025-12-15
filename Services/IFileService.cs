using System.Collections.Generic;
using TagNamer.Models;

namespace TagNamer.Services;

public interface IFileService
{
    FileItem? CreateFileItem(string path);
    IEnumerable<string> GetFilesInFolder(string folderPath);
    bool RenameFile(string sourcePath, string destPath);
}
