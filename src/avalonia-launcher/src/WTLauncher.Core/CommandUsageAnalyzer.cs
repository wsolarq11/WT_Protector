using System.Collections.Immutable;

namespace WTLauncher.Core;

/// <summary>
/// 一行去重后的 command + 引用计数 + 引用 entry 名列表。
/// ponytail: 不可变 record, 复制即用, UI 直接绑定。
/// </summary>
public sealed record CommandRow(
    string Command,
    int ReferenceCount,
    IReadOnlyList<string> ReferencingEntryNames);

/// <summary>
/// 一行 commandTemplates 字典项 + 引用计数。
/// ReferenceCount = entry.Command == "$key" 的 entry 数 (OrdinalIgnoreCase)。
/// </summary>
public sealed record TemplateRow(
    string Key,
    string ExpandedValue,
    int ReferenceCount);

/// <summary>
/// 纯函数分析器: 从 _entries + _settings.CommandTemplates 派生去重列表 + 引用计数。
/// 无副作用, 无状态, 可直接单元测试。
/// </summary>
public static class CommandUsageAnalyzer
{
    /// <summary>
    /// 按 command 字段去重列出 entries, 含引用次数 + 引用 entry 名。
    /// 用 OrdinalIgnoreCase 去重 (与 LauncherSettings.CommandTemplates 的 comparer 一致)。
    /// 默认按引用次数倒序, 相同引用数按 command ordinal 排序。
    /// </summary>
    public static IReadOnlyList<CommandRow> Analyze(IEnumerable<LaunchEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var groups = entries
            .GroupBy(e => e.Command, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CommandRow(
                Command: g.Key,
                ReferenceCount: g.Count(),
                ReferencingEntryNames: g.Select(e => e.Name).ToImmutableList()))
            .OrderByDescending(r => r.ReferenceCount)
            .ThenBy(r => r.Command, StringComparer.Ordinal)
            .ToImmutableList();

        return groups;
    }

    /// <summary>
    /// 列出 commandTemplates 字典 + 每个 key 的引用计数。
    /// 引用计数 = entries 中 command == "$key" 的 entry 数 (OrdinalIgnoreCase)。
    /// 按引用次数倒序, 相同引用数按 key ordinal 排序。
    /// </summary>
    public static IReadOnlyList<TemplateRow> AnalyzeTemplates(
        IImmutableDictionary<string, string> templates,
        IEnumerable<LaunchEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(entries);

        var entryList = entries as IReadOnlyList<LaunchEntry> ?? entries.ToList();
        var rows = templates
            .Select(kv =>
            {
                var key = "$" + kv.Key;
                var refCount = entryList.Count(e => string.Equals(e.Command, key, StringComparison.OrdinalIgnoreCase));
                return new TemplateRow(kv.Key, kv.Value, refCount);
            })
            .OrderByDescending(r => r.ReferenceCount)
            .ThenBy(r => r.Key, StringComparer.Ordinal)
            .ToImmutableList();

        return rows;
    }

    /// <summary>
    /// 检查删除某 command 是否安全 (引用 = 0)。引用 > 0 时调用方应阻止删除。
    /// </summary>
    public static bool CanDeleteCommand(string command, IEnumerable<LaunchEntry> entries) =>
        !entries.Any(e => string.Equals(e.Command, command, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 检查删除某模板 key 是否安全 (无 entry 引用 $key)。引用 > 0 时调用方应阻止删除。
    /// </summary>
    public static bool CanDeleteTemplate(string key, IEnumerable<LaunchEntry> entries)
    {
        var refToken = "$" + key;
        return !entries.Any(e => string.Equals(e.Command, refToken, StringComparison.OrdinalIgnoreCase));
    }
}
