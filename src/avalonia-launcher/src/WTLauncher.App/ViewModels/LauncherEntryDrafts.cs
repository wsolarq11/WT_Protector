using WTLauncher.Core;
namespace WTLauncher.App.ViewModels;

public sealed record EntryDraft(
    string Id,
    string Name,
    string Directory,
    string Command,
    bool Selected);
public static class LauncherEntryDrafts
{
    // Must match LauncherEntryIds.WindowsReservedNames (case-insensitive per OrdinalIgnoreCase).
    private static readonly HashSet<string> s_reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    // Normalizes each field at the ViewModel boundary rather than relying on callers —
    // dialog inputs may contain unicode variants and whitespace before the caller validates.
    public static EntryDraft NormalizeDraft(EntryDraft draft)
    {
        return new EntryDraft(
            LauncherEntryIds.Normalize(draft.Id),
            LauncherEntryText.Normalize(draft.Name),
            LauncherEntryText.Normalize(draft.Directory),
            LauncherEntryText.Normalize(draft.Command),
            draft.Selected);
    }

    public static bool TryNormalizeRequiredDraft(
        EntryDraft draft,
        out EntryDraft normalized,
        out string? requiredFieldError)
    {
        normalized = NormalizeDraft(draft);
        requiredFieldError = GetRequiredFieldError(normalized);
        return requiredFieldError is null;
    }

    public static LaunchEntry CreateEntry(EntryDraft draft, LaunchEntry? previousEntry)
    {
        var normalized = NormalizeDraft(draft);
        return new LaunchEntry
        {
            Id = normalized.Id,
            Name = normalized.Name,
            Directory = normalized.Directory,
            Command = normalized.Command,
            Selected = normalized.Selected,
            UpdatedAtUtc = previousEntry?.UpdatedAtUtc ?? string.Empty,
        };
    }

    public static string? ValidateEntry(
        LaunchEntry entry,
        string? originalId,
        IEnumerable<LaunchEntry> existingEntries)
    {
        var entryId = LauncherEntryIds.Normalize(entry.Id);
        var requiredFieldError = GetRequiredFieldError(
            entryId,
            entry.Name,
            entry.Directory,
            entry.Command);
        if (requiredFieldError is not null)
            return requiredFieldError;

        // Per-item call (dialog submit, not in a loop), so O(n) scan over
        // existingEntries is acceptable. Typical entry count < 100.
        if (existingEntries.Any(existing =>
                !LauncherEntryIds.SameNormalized(existing.Id, originalId)
                && LauncherEntryIds.SameNormalized(existing.Id, entryId)))
            return LauncherEntryDraftStatus.DuplicateId(entryId);

        return null;
    }

    public static string? GetRequiredFieldError(EntryDraft draft)
    {
        return GetRequiredFieldError(
            draft.Id,
            draft.Name,
            draft.Directory,
            draft.Command);
    }

    private static string? GetRequiredFieldError(
        string? id,
        string? name,
        string? directory,
        string? command)
    {
        if (!LauncherEntryIds.IsUsable(id))
            return LauncherEntryDraftStatus.IdRequired;
        // Reject Windows reserved names and path separators / parent-directory tokens
        // at dialog-validation time rather than at save time. Without this guard, the
        // save-time RequireEntryId rejects the ID and silently rolls back the entry
        // via the snapshot mechanism, leaving no user-facing explanation.
        // Use OrdinalIgnoreCase to match LauncherEntryIds.RequireEntryId behavior
        // (which uses WindowsReservedNames with StringComparer.OrdinalIgnoreCase).
        // ponytail: local HashSet instead of pattern matching — 22 entries, negligible.
        var normalizedId = LauncherEntryIds.Normalize(id);
        if (s_reservedNames.Contains(normalizedId))
            return LauncherEntryDraftStatus.IdInvalid;
        if (normalizedId.Contains('\\') || normalizedId.Contains('/') || normalizedId.Contains(".."))
            return LauncherEntryDraftStatus.IdInvalid;
        if (string.IsNullOrWhiteSpace(name))
            return LauncherEntryDraftStatus.NameRequired;
        if (string.IsNullOrWhiteSpace(directory))
            return LauncherEntryDraftStatus.DirectoryRequired;
        if (string.IsNullOrWhiteSpace(command))
            return LauncherEntryDraftStatus.CommandRequired;

        return null;
    }
}

public static class LauncherEntryDraftStatus
{
    public static string IdRequired => "按钮 ID 不能为空";

    public static string IdInvalid => "按钮 ID 不能使用 Windows 保留名、路径分隔符或 ..";

    public static string NameRequired => "按钮名称不能为空";

    public static string DirectoryRequired => "按钮目录不能为空";

    public static string CommandRequired => "按钮命令不能为空";

    public static string DuplicateId(string entryId) =>
        $"按钮 ID 已存在: {entryId}";
}
