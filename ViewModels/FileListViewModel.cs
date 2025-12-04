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

    public void AddItem(FileItem item)
    {
        // 중복 체크
        if (Items.Any(i => i.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        item.AddIndex = _nextAddIndex++;
        Items.Add(item);
    }
}
