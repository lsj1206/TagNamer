using System.Collections.Generic;
using TagNamer.Models;
using TagNamer.ViewModels;

namespace TagNamer.Services;

/// <summary>
/// 이름 변경 규칙을 정의하고 실행하는 서비스 인터페이스입니다.
/// </summary>
public interface IRenameService
{
    /// <summary>
    /// 규칙을 적용하여 모든 아이템의 미리보기 이름(NewName)을 업데이트합니다.
    /// </summary>
    void UpdatePreview(IEnumerable<FileItem> items, string ruleFormat, TagManagerViewModel tagManager, bool showExtension);

    /// <summary>
    /// 미리보기 상태의 이름을 실제 파일 시스템에 적용합니다.
    /// </summary>
    List<FileItem> ApplyRename(IEnumerable<FileItem> items, bool showExtension);

    /// <summary>
    /// 변경된 이름을 이전 상태로 되돌립니다.
    /// </summary>
    void UndoRename(IEnumerable<FileItem> items, bool showExtension);
}
