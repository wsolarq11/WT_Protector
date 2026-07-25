using System.Text.Json.Serialization;

namespace WTLauncher.Core;

/// <summary>Immutable data carrier. All properties are init-only — every state
/// change produces a new instance via <c>with { }</c>, making mutations explicit,
/// traceable, and impossible to miss. No invisible shared-mutation bugs.</summary>
public sealed record LaunchEntry
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public EntryId Id { get; init; } = EntryId.Empty;

    [JsonPropertyName("name")]
    [JsonRequired]
    public EntryName Name { get; init; } = EntryName.Empty;

    [JsonPropertyName("directory")]
    [JsonRequired]
    [JsonConverter(typeof(DirectoryPathJsonConverter))]
    public DirectoryPath Directory { get; init; } = DirectoryPath.Empty;

    [JsonPropertyName("command")]
    [JsonRequired]
    public string Command { get; init; } = "";

    [JsonPropertyName("selected")]
    public bool Selected { get; init; }

    /// <summary>
    /// ISO 8601 UTC timestamp of the last update to this entry.
    /// An empty string is a valid value per the data model design decision (ADR in data-types.md)
    /// and is treated as <see cref="DateTimeOffset.MinValue"/> during date-based sorting.
    /// </summary>
    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; init; } = "";

    /// <summary>
    /// ISO 8601 UTC timestamp of the last launch of this entry.
    /// An empty string is a valid value per the data model design decision (ADR in data-types.md)
    /// and is treated as <see cref="DateTimeOffset.MinValue"/> during date-based sorting.
    /// </summary>
    [JsonPropertyName("launchedAtUtc")]
    public string LaunchedAtUtc { get; init; } = "";

    /// <summary>
    /// 累积启动次数。frequent sort 模式按 frecency(count, recency) 评分排序。
    /// Optional — 旧版本 JSON 无此字段时反序列化为 0（前向兼容）。
    /// </summary>
    [JsonPropertyName("launchCount")]
    public int LaunchCount { get; init; }
}
