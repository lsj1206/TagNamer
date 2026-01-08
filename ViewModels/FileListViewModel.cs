using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TagNamer.Models;
using TagNamer.Services;

namespace TagNamer.ViewModels;

public class FileListViewModel
{
    public ObservableCollection<FileItem> Items { get; } = new();
    private readonly HashSet<string> _pathSet = new(StringComparer.OrdinalIgnoreCase);
    private int _nextAddIndex = 1;

    public void Clear()
    {
        Items.Clear();
        _pathSet.Clear();
        _nextAddIndex = 1;
    }

    public bool AddItem(FileItem item)
    {
        // 중복 체크 (HashSet 사용으로 O(1) 처리)
        if (_pathSet.Contains(item.Path))
        {
            return false;
        }

        item.AddIndex = _nextAddIndex++;
        Items.Add(item);
        _pathSet.Add(item.Path);
        return true;
    }

    /// <summary>
    /// 대량의 아이템을 한꺼번에 추가합니다.
    /// WPF ObservableCollection은 AddRange가 없으므로 순회하며 추가하되,
    /// 대량 추가 시의 중복 체크 성능을 보장합니다.
    /// </summary>
    public int AddRange(IEnumerable<FileItem> newItems)
    {
        int addedCount = 0;
        foreach (var item in newItems)
        {
            if (!_pathSet.Contains(item.Path))
            {
                item.AddIndex = _nextAddIndex++;
                Items.Add(item);
                _pathSet.Add(item.Path);
                addedCount++;
            }
        }
        return addedCount;
    }

    public void UpdateNextAddIndex(int nextIndex)
    {
        _nextAddIndex = nextIndex;
    }

    /// <summary>
    /// 목록을 정렬합니다.
    /// </summary>
    public void Sorting(ISortingService sortingService, MainViewModel.SortOption option, bool ascending)
    {
        if (Items.Count == 0) return;

        var sortedItems = sortingService.Sort(Items, option, ascending).ToList();

        // UI 업데이트 최소화를 위해 컬렉션을 직접 수정하지 않고
        // 가능한 경우에만 최적화하거나, 대규모일 시 Reset 통지가 필요할 수 있음.
        // 여기서는 기존 방식을 유지하되 Items.Clear() 후 재삽입 시 HashSet도 관리.
        Items.Clear();
        _pathSet.Clear();
        foreach (var item in sortedItems)
        {
            Items.Add(item);
            _pathSet.Add(item.Path);
        }
    }

    /// <summary>
    /// 여러 아이템을 목록에서 제거합니다.
    /// </summary>
    /// <returns>삭제된 아이템 개수</returns>
    public int RemoveItems(IEnumerable<FileItem> itemsToRemove)
    {
        if (itemsToRemove == null) return 0;

        var itemList = itemsToRemove.ToList();
        foreach (var item in itemList)
        {
            if (Items.Remove(item))
            {
                _pathSet.Remove(item.Path);
            }
        }

        return itemList.Count;
    }

    /// <summary>
    /// 현재 순서대로 번호를 재정렬합니다.
    /// </summary>
    public void ReorderIndex()
    {
        int index = 1;
        foreach (var item in Items)
        {
            item.AddIndex = index++;
        }
        _nextAddIndex = index;
    }

    public void MoveItems(System.Collections.Generic.List<FileItem> itemsToMove, int targetIndex, bool isBottom)
    {
        if (itemsToMove == null || itemsToMove.Count == 0) return;

        // 1. 드롭 기준이 되는 타켓 아이템을 미리 확보
        FileItem? targetItem = (targetIndex >= 0 && targetIndex < Items.Count) ? Items[targetIndex] : null;

        // 2. 이동할 아이템들을 리스트에서 제거
        var actualMoving = itemsToMove.Where(i => Items.Contains(i)).ToList();
        foreach (var item in actualMoving)
        {
            Items.Remove(item);
        }

        // 3. 삽입 위치 결정 (제거된 리스트 기준)
        int insertIndex;
        if (targetItem != null && Items.Contains(targetItem))
        {
            insertIndex = Items.IndexOf(targetItem);
            if (isBottom) insertIndex++;
        }
        else
        {
            // 타겟 아이템이 없거나 이동 대상에 포함되어 제거된 경우,
            // 원래 요청된 targetIndex를 현재 리스트 범위 내로 보정
            insertIndex = Math.Min(targetIndex, Items.Count);
        }

        // 4. 새 위치에 삽입
        for (int i = 0; i < actualMoving.Count; i++)
        {
            Items.Insert(Math.Min(insertIndex + i, Items.Count), actualMoving[i]);
        }
    }
}
