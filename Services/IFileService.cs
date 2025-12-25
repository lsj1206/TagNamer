using System.Collections.Generic;
using TagNamer.Models;

namespace TagNamer.Services;

/// <summary>
/// 파일 시스템의 실제 조작을 담당하는 서비스 인터페이스입니다.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 지정된 경로를 바탕으로 FileItem 객체를 생성합니다.
    /// </summary>
    FileItem? CreateFileItem(string path);

    /// <summary>
    /// 지정된 폴더 내의 모든 파일 경로를 재귀적으로 가져옵니다.
    /// </summary>
    IEnumerable<string> GetFilesInFolder(string folderPath);

    /// <summary>
    /// 파일 또는 폴더의 이름을 변경(이동)합니다.
    /// </summary>
    /// <param name="sourcePath">현재 경로</param>
    /// <param name="destPath">변경할 새 경로</param>
    void RenameFile(string sourcePath, string destPath);
}
