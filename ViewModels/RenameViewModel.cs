using System.Linq;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using TagNamer.Models;
using System;

namespace TagNamer.ViewModels;

public partial class RenameViewModel : ObservableObject
{
    [ObservableProperty]
    private string ruleFormat = "[Origin]";

    public TagManagerViewModel TagManager { get; }
    public string ResolvedRuleFormat => RuleFormat;

    /// <summary>
    /// 규칙에 태그를 추가합니다. IsUnique 태그는 중복을 자동으로 제거합니다.
    /// </summary>
    public void AddTagToRule(string tagCode, bool isForward)
    {
        if (string.IsNullOrEmpty(tagCode)) return;

        string current = RuleFormat;

        // IsUnique 태그인지 확인
        var tag = TagManager.CreatedTags.FirstOrDefault(t =>
            t.TagName.Equals(tagCode, StringComparison.OrdinalIgnoreCase));

        if (tag?.IsUnique == true)
        {
            // 같은 이름의 기존 태그가 있으면 제거
            int existingIndex = current.IndexOf(tagCode, StringComparison.OrdinalIgnoreCase);
            if (existingIndex != -1)
            {
                current = current.Remove(existingIndex, tagCode.Length);
            }
        }

        RuleFormat = isForward ? tagCode + current : current + tagCode;
    }

    /// <summary>
    /// RuleFormat 변경 시 IsUnique 태그의 중복 및 ExclusiveGroup 태그의 상호 배타성을 처리합니다.
    /// 새로 추가된 태그를 우선 보존합니다.
    /// </summary>
    partial void OnRuleFormatChanged(string? oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue)) return;

        string cleaned = newValue;
        bool hasChanges = false;

        // 1. IsUnique 태그 중복 제거
        var uniqueTags = TagManager.CreatedTags.Where(t => t.IsUnique).ToList();

        foreach (var tag in uniqueTags)
        {
            string tagName = tag.TagName;
            int firstIdx = cleaned.IndexOf(tagName, StringComparison.OrdinalIgnoreCase);
            if (firstIdx == -1) continue;

            int secondIdx = cleaned.IndexOf(tagName, firstIdx + tagName.Length, StringComparison.OrdinalIgnoreCase);
            if (secondIdx != -1)
            {
                // 중복 발견 시 신규 태그만 남기기 위해 전체 매치 위치를 리스트로 확보
                var matchIndices = new List<int>();
                int currentPos = 0;
                while ((currentPos = cleaned.IndexOf(tagName, currentPos, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    matchIndices.Add(currentPos);
                    currentPos += tagName.Length;
                }

                int preserveIndex = matchIndices.Count - 1;

                if (!string.IsNullOrEmpty(oldValue))
                {
                    int changePoint = 0;
                    int minLen = Math.Min(oldValue.Length, newValue.Length);
                    while (changePoint < minLen && oldValue[changePoint] == newValue[changePoint]) changePoint++;

                    int minDistance = int.MaxValue;
                    for (int i = 0; i < matchIndices.Count; i++)
                    {
                        int distance = Math.Abs(matchIndices[i] - changePoint);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            preserveIndex = i;
                        }
                    }
                }

                // 뒤에서부터 삭제하여 인덱스 꼬임 방지
                for (int i = matchIndices.Count - 1; i >= 0; i--)
                {
                    if (i != preserveIndex)
                    {
                        cleaned = cleaned.Remove(matchIndices[i], tagName.Length);
                    }
                }
                hasChanges = true;
            }
        }

        // 2. ExclusiveGroup 태그 상호 배타성 처리
        var groupedTags = TagManager.CreatedTags
            .Where(t => !string.IsNullOrEmpty(t.ExclusiveGroup))
            .SelectMany(t => (t.ExclusiveGroup?.Split(',') ?? Array.Empty<string>())
                .Select(g => new { Tag = t, Group = g.Trim() }))
            .GroupBy(x => x.Group, x => x.Tag)
            .ToList();

        foreach (var group in groupedTags)
        {
            var tagsInGroup = group.ToList();
            if (tagsInGroup.Count <= 1) continue;

            var presentTags = tagsInGroup
                .Where(t => cleaned.IndexOf(t.TagName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (presentTags.Count > 1)
            {
                TagItem? preserveTag = null;
                if (!string.IsNullOrEmpty(oldValue))
                {
                    preserveTag = presentTags.FirstOrDefault(t =>
                        oldValue.IndexOf(t.TagName, StringComparison.OrdinalIgnoreCase) < 0);
                }

                if (preserveTag == null)
                {
                    preserveTag = presentTags
                        .OrderByDescending(t => cleaned.LastIndexOf(t.TagName, StringComparison.OrdinalIgnoreCase))
                        .First();
                }

                foreach (var tag in presentTags)
                {
                    if (tag != preserveTag)
                    {
                        // Regex.Replace 대신 String.Replace (OrdinalIgnoreCase) 사용
                        // 단, 한 번만 지워야 함 (다중 태그는 위 1단계에서 처리됨)
                        int idx = cleaned.IndexOf(tag.TagName, StringComparison.OrdinalIgnoreCase);
                        if (idx != -1)
                        {
                            cleaned = cleaned.Remove(idx, tag.TagName.Length);
                            hasChanges = true;
                        }
                    }
                }
            }
        }

        if (hasChanges)
        {
            RuleFormat = cleaned;
        }
    }

    public RenameViewModel(TagManagerViewModel tagManager)
    {
        TagManager = tagManager;
    }
}
