using System.Collections.Generic;
using TagNamer.Models;
using TagNamer.ViewModels;

namespace TagNamer.Services;

public interface IRenameService
{
    void UpdatePreview(IEnumerable<FileItem> items, string ruleFormat, TagManagerViewModel tagManager, bool showExtension);
    void ApplyRename(IEnumerable<FileItem> items, bool showExtension);
    void UndoRename(IEnumerable<FileItem> items, bool showExtension);
}
