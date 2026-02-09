using System.Windows.Media;

namespace TagNamer.Services;

/// <summary>
/// 파일 및 폴더의 시스템 아이콘을 추출하는 서비스 인터페이스입니다.
/// </summary>
public interface IIconService
{
    /// <summary>
    /// 지정된 경로의 파일 또는 폴더 아이콘을 가져옵니다.
    /// </summary>
    /// <param name="path">파일 또는 폴더 경로</param>
    /// <param name="isFolder">폴더 여부</param>
    /// <returns>아이콘 이미지 소스</returns>
    ImageSource? GetIcon(string path, bool isFolder);
}
