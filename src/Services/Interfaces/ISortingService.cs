using System.Collections.Generic;
using TagNamer.Models;
using static TagNamer.ViewModels.MainViewModel; // For SortOption

namespace TagNamer.Services;

public interface ISortingService
{
    IEnumerable<FileItem> Sort(IEnumerable<FileItem> items, SortOption option, bool isAscending);
}
