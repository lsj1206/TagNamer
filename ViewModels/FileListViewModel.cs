using System;
using System.Linq;
using System.Collections.ObjectModel;
using TagNamer.Models;

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

    public bool AddItem(FileItem item)
    {
        // 중복 체크 (이미 목록에 있는 경로는 추가하지 않음)
        if (Items.Any(i => i.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        item.AddIndex = _nextAddIndex++;
        Items.Add(item);
        return true;
    }

    public void UpdateNextAddIndex(int nextIndex)
    {
        _nextAddIndex = nextIndex;
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
