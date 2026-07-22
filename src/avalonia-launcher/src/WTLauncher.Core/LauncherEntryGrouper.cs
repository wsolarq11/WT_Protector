using System.Collections.Immutable;

namespace WTLauncher.Core;

/// <summary>
/// 纯函数: 按 groupKey(none/command/directory) 把 entries 分组。
/// ponytail: 与 CommandUsageAnalyzer / DirectoryUsageAnalyzer 同模式 — OrdinalIgnoreCase 去重,引用次数倒序。
/// </summary>
public static class LauncherEntryGrouper
{
    public static IReadOnlyList<CardGroup> Group(IEnumerable<LaunchEntry> entries, string groupKey)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var entryList = entries as IReadOnlyList<LaunchEntry> ?? entries.ToList();
        var normalized = LauncherCardsGroupKeys.Normalize(groupKey);

        if (normalized == LauncherCardsGroupKeys.None)
        {
            return new[]
            {
                new CardGroup(
                    Key: string.Empty,
                    Count: entryList.Count,
                    Entries: entryList.ToImmutableList()),
            }.ToImmutableList();
        }

        Func<LaunchEntry, string> keySelector = normalized == LauncherCardsGroupKeys.Command
            ? e => e.Command
            : e => e.Directory.Value;

        return entryList
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CardGroup(
                Key: g.Key,
                Count: g.Count(),
                Entries: g.ToImmutableList()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToImmutableList();
    }
}
