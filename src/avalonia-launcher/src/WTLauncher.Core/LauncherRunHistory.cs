using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json.Serialization;

namespace WTLauncher.Core;

public enum LauncherRunSource
{
    Individual,
    Selection,
    Preset,
    Group,
}

public enum LauncherRunStatus
{
    Requested,
    Failed,
    Skipped,
    Canceled,
}

/// <summary>
/// One repository-owned launch observation. This is not a terminal health
/// report: Requested only means the process start request reached the launcher
/// boundary.
/// </summary>
public sealed record LauncherRunHistoryEntry
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; init; } = "";

    [JsonPropertyName("entryId")]
    [JsonRequired]
    public string EntryId { get; init; } = "";

    [JsonPropertyName("entryName")]
    [JsonRequired]
    public string EntryName { get; init; } = "";

    [JsonPropertyName("timestampUtc")]
    [JsonRequired]
    public string TimestampUtc { get; init; } = "";

    [JsonPropertyName("source")]
    [JsonRequired]
    public string Source { get; init; } = "";

    [JsonPropertyName("status")]
    [JsonRequired]
    public string Status { get; init; } = "";

    public static LauncherRunHistoryEntry Create(
        string entryId,
        string entryName,
        string timestampUtc,
        LauncherRunSource source,
        LauncherRunStatus status)
    {
        var normalizedId = WTLauncher.Core.EntryId.Normalize(entryId);
        if (normalizedId.Length == 0)
            throw new ArgumentException("Run history entry id is required.", nameof(entryId));
        var normalizedName = WTLauncher.Core.EntryName.Normalize(entryName);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Run history entry name is required.", nameof(entryName));
        if (!DateTimeOffset.TryParse(
                timestampUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out _))
            throw new ArgumentException("Run history timestamp must be an ISO-8601 UTC value.", nameof(timestampUtc));

        return new LauncherRunHistoryEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            EntryId = normalizedId,
            EntryName = normalizedName,
            TimestampUtc = timestampUtc,
            Source = source.ToString(),
            Status = status.ToString(),
        };
    }
}

public static class LauncherRunHistory
{
    public const int DefaultMaxEntries = 100;

    public static ImmutableList<LauncherRunHistoryEntry> Append(
        ImmutableList<LauncherRunHistoryEntry> history,
        LauncherRunHistoryEntry entry,
        int maxEntries = DefaultMaxEntries)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(entry);
        if (maxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Max entries must be positive.");

        return Sanitize(history.Add(entry), maxEntries);
    }

    public static ImmutableList<LauncherRunHistoryEntry> AppendRange(
        ImmutableList<LauncherRunHistoryEntry> history,
        IEnumerable<LauncherRunHistoryEntry> entries,
        int maxEntries = DefaultMaxEntries)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(entries);
        if (maxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Max entries must be positive.");

        var builder = history.ToBuilder();
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            builder.Add(entry);
        }

        return Sanitize(builder.ToImmutable(), maxEntries);
    }

    public static ImmutableList<LauncherRunHistoryEntry> Sanitize(
        IEnumerable<LauncherRunHistoryEntry>? history,
        int maxEntries = DefaultMaxEntries)
    {
        if (maxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Max entries must be positive.");

        var valid = new List<LauncherRunHistoryEntry>();
        if (history is not null)
        {
            foreach (var entry in history)
            {
                if (entry is null || !IsValid(entry))
                    continue;
                valid.Add(entry with
                {
                    EntryId = WTLauncher.Core.EntryId.Normalize(entry.EntryId),
                    EntryName = WTLauncher.Core.EntryName.Normalize(entry.EntryName),
                });
            }
        }

        if (valid.Count <= maxEntries)
            return valid.ToImmutableList();
        return valid.Skip(valid.Count - maxEntries).ToImmutableList();
    }

    private static bool IsValid(LauncherRunHistoryEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
            return false;
        if (string.IsNullOrWhiteSpace(entry.EntryId) || string.IsNullOrWhiteSpace(entry.EntryName))
            return false;
        if (!DateTimeOffset.TryParse(
                entry.TimestampUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out _))
            return false;
        if (!Enum.TryParse<LauncherRunSource>(entry.Source, ignoreCase: false, out _))
            return false;
        return Enum.TryParse<LauncherRunStatus>(entry.Status, ignoreCase: false, out _);
    }
}
