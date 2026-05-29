using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryDrafts
{
    public static EntryDraft NormalizeDraft(EntryDraft draft)
    {
        return new EntryDraft(
            (draft.Id ?? string.Empty).Trim(),
            (draft.Name ?? string.Empty).Trim(),
            (draft.Directory ?? string.Empty).Trim(),
            (draft.Command ?? string.Empty).Trim(),
            draft.Selected);
    }

    public static bool HasRequiredFields(EntryDraft draft)
    {
        return LauncherEntryIds.IsUsable(draft.Id)
            && !string.IsNullOrWhiteSpace(draft.Name)
            && !string.IsNullOrWhiteSpace(draft.Directory)
            && !string.IsNullOrWhiteSpace(draft.Command);
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
            Confirm = false,
            Selected = normalized.Selected,
            UpdatedAtUtc = previousEntry?.UpdatedAtUtc ?? string.Empty,
        };
    }

    public static string? ValidateEntry(
        LaunchEntry entry,
        string? originalId,
        IEnumerable<LaunchEntry> existingEntries)
    {
        if (!LauncherEntryIds.IsUsable(entry.Id))
            return "按钮 ID 不能为空";
        if (string.IsNullOrWhiteSpace(entry.Name))
            return "按钮名称不能为空";
        if (string.IsNullOrWhiteSpace(entry.Directory))
            return "按钮目录不能为空";
        if (string.IsNullOrWhiteSpace(entry.Command))
            return "按钮命令不能为空";
        if (existingEntries.Any(existing =>
                !LauncherEntryIds.Same(existing.Id, originalId)
                && LauncherEntryIds.Same(existing.Id, entry.Id)))
            return $"按钮 ID 已存在: {entry.Id}";

        return null;
    }
}
