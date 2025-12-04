using System.Linq;
using System.Collections.Generic;
using TagNamer.Models;
using static TagNamer.ViewModels.MainViewModel;

namespace TagNamer.Services;

public class SortingService : ISortingService
{
    public IEnumerable<FileItem> Sort(IEnumerable<FileItem> items, SortOption option, bool isAscending)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return itemList;

        IOrderedEnumerable<FileItem> sortedItems;
        var comparer = new WindowsNaturalComparer();

        // 1차 정렬
        switch (option.Type)
        {
            case SortType.NameNumber:
            case SortType.NamePath:
                sortedItems = isAscending
                    ? itemList.OrderBy(x => x.OriginalName, comparer)
                    : itemList.OrderByDescending(x => x.OriginalName, comparer);
                break;
            case SortType.PathNumber:
            case SortType.PathName:
                sortedItems = isAscending
                    ? itemList.OrderBy(x => x.DirectoryName, comparer)
                    : itemList.OrderByDescending(x => x.DirectoryName, comparer);
                break;
            case SortType.Size:
                sortedItems = isAscending
                    ? itemList.OrderBy(x => x.Size)
                    : itemList.OrderByDescending(x => x.Size);
                break;
            case SortType.CreatedDate:
                sortedItems = isAscending
                    ? itemList.OrderBy(x => x.CreatedDate)
                    : itemList.OrderByDescending(x => x.CreatedDate);
                break;
            case SortType.ModifiedDate:
                sortedItems = isAscending
                    ? itemList.OrderBy(x => x.ModifiedDate)
                    : itemList.OrderByDescending(x => x.ModifiedDate);
                break;
            case SortType.AddIndex:
            default:
                sortedItems = isAscending
                    ? itemList.OrderBy(x => x.AddIndex)
                    : itemList.OrderByDescending(x => x.AddIndex);
                break;
        }

        // 2차 정렬
        switch (option.Type)
        {
            case SortType.NameNumber:
            case SortType.PathNumber:
            case SortType.Size:
            case SortType.CreatedDate:
            case SortType.ModifiedDate:
                // 중복 시 번호(AddIndex) 순
                sortedItems = isAscending
                    ? sortedItems.ThenBy(x => x.AddIndex)
                    : sortedItems.ThenByDescending(x => x.AddIndex);
                break;
            case SortType.NamePath:
                // 중복 시 경로(Path) 순
                sortedItems = isAscending
                    ? sortedItems.ThenBy(x => x.Path, comparer)
                    : sortedItems.ThenByDescending(x => x.Path, comparer);
                break;
            case SortType.PathName:
                // 중복 시 이름(OriginalName) 순
                sortedItems = isAscending
                    ? sortedItems.ThenBy(x => x.OriginalName, comparer)
                    : sortedItems.ThenByDescending(x => x.OriginalName, comparer);
                break;
        }

        return sortedItems;
    }
}
