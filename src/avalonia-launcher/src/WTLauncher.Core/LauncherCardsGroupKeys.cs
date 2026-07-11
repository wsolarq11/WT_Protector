namespace WTLauncher.Core;

/// <summary>
/// Cards 视图分组维度键。schema 以 string 形式持久化到 settings.cardsGroupKey。
/// ponytail: 与 LauncherEntryViewModes/LauncherEntryVisibility 同模式 — string IDs + 默认值。
/// </summary>
public static class LauncherCardsGroupKeys
{
    public const string None = "none";
    public const string Command = "command";
    public const string Directory = "directory";

    public static readonly IReadOnlyList<string> All = new[] { None, Command, Directory };

    /// <summary>未知值回退到 None(forward compat: 未来加 "tag" 等新模式时旧版本不报错)。</summary>
    public static string Normalize(string? value) =>
        All.Contains(value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            ? (value ?? string.Empty).ToLowerInvariant()
            : None;
}

/// <summary>
/// Cards 分组 ComboBox 项: display label + 持久化 value。
/// ponytail: 简单 record,用于 ComboBox ItemsSource 绑定。
/// </summary>
public sealed record LauncherCardsGroupOption(string Display, string Value);

/// <summary>
/// 一个分组: 组标识(command/directory 值)+ 引用计数 + 组内 entries。
/// ponytail: 不可变 record, Core 层无 UI 依赖。
/// </summary>
public sealed record CardGroup(
    string Key,
    int Count,
    IReadOnlyList<LaunchEntry> Entries);
