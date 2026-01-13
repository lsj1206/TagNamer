using System.Linq;
using System.Text.RegularExpressions;
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
            var matches = Regex.Matches(cleaned, Regex.Escape(tag.TagName), RegexOptions.IgnoreCase);

            if (matches.Count > 1)
            {
                int preserveIndex = matches.Count - 1; // 기본값: 마지막 것

                // oldValue가 있으면 변화 지점 찾기
                if (!string.IsNullOrEmpty(oldValue))
                {
                    // 문자열 앞에서부터 비교하여 첫 변화 지점 찾기
                    int changePoint = 0;
                    int minLen = Math.Min(oldValue.Length, newValue.Length);

                    while (changePoint < minLen && oldValue[changePoint] == newValue[changePoint])
                    {
                        changePoint++;
                    }

                    // 변화 지점과 가장 가까운 매치가 새로 추가된 것
                    int minDistance = int.MaxValue;
                    for (int i = 0; i < matches.Count; i++)
                    {
                        int distance = Math.Abs(matches[i].Index - changePoint);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            preserveIndex = i;
                        }
                    }
                }

                // preserveIndex를 제외한 나머지 제거 (뒤에서부터)
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    if (i != preserveIndex)
                    {
                        cleaned = cleaned.Remove(matches[i].Index, matches[i].Length);
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
            if (tagsInGroup.Count <= 1) continue; // 그룹에 태그가 1개 이하면 스킵

            // 이 그룹의 태그 중 현재 문자열에 존재하는 것들 찾기
            var presentTags = tagsInGroup
                .Where(t => cleaned.IndexOf(t.TagName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (presentTags.Count > 1)
            {
                // 여러 개 있으면 가장 최근에 추가된 것만 남기고 제거
                // oldValue와 비교하여 새로 추가된 것 찾기
                TagItem? preserveTag = null;

                if (!string.IsNullOrEmpty(oldValue))
                {
                    // oldValue에 없던 태그 찾기 (새로 추가된 것)
                    preserveTag = presentTags.FirstOrDefault(t =>
                        oldValue.IndexOf(t.TagName, StringComparison.OrdinalIgnoreCase) < 0);
                }

                // 못 찾았으면 위치상 마지막 것 보존
                if (preserveTag == null)
                {
                    preserveTag = presentTags
                        .OrderByDescending(t => cleaned.LastIndexOf(t.TagName, StringComparison.OrdinalIgnoreCase))
                        .First();
                }

                // preserveTag를 제외한 나머지 제거
                foreach (var tag in presentTags)
                {
                    if (tag != preserveTag)
                    {
                        cleaned = Regex.Replace(cleaned, Regex.Escape(tag.TagName), "", RegexOptions.IgnoreCase);
                        hasChanges = true;
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
