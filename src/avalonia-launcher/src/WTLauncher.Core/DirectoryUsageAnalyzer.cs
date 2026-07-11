using System.Collections.Immutable;

namespace WTLauncher.Core;

/// <summary>
/// 一行去重后的 directory + 引用计数 + 引用 entry 名列表。
/// ponytail: 不可变 record, 与 CommandRow 对称。
/// </summary>
public sealed record DirectoryRow(
    string Directory,
    int ReferenceCount,
    IReadOnlyList<string> ReferencingEntryNames);

/// <summary>
/// 纯函数分析器: 从 _entries 派生 directory 去重列表 + 引用计数。
/// 用 OrdinalIgnoreCase 去重 (Windows 路径 case-insensitive)。
/// </summary>
public static class DirectoryUsageAnalyzer
{
    public static IReadOnlyList<DirectoryRow> Analyze(IEnumerable<LaunchEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries
            .GroupBy(e => e.Directory, StringComparer.OrdinalIgnoreCase)
            .Select(g => new DirectoryRow(
                Directory: g.Key,
                ReferenceCount: g.Count(),
                ReferencingEntryNames: g.Select(e => e.Name).ToImmutableList()))
            .OrderByDescending(r => r.ReferenceCount)
            .ThenBy(r => r.Directory, StringComparer.Ordinal)
            .ToImmutableList();
    }

    public static bool CanDeleteDirectory(string directory, IEnumerable<LaunchEntry> entries) =>
        !entries.Any(e => string.Equals(e.Directory, directory, StringComparison.OrdinalIgnoreCase));
}
