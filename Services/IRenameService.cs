using System.Collections.Generic;
using TagNamer.Models;
using TagNamer.ViewModels;

namespace TagNamer.Services;

public interface IRenameService
{
    void UpdatePreview(IEnumerable<FileItem> items, string ruleFormat, TagManagerViewModel tagManager);
    void ApplyRename(IEnumerable<FileItem> items);
    void UndoRename(IEnumerable<FileItem> items);
}
