using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public static class LauncherEntryDrafts
{
    public static LaunchEntry CreateEntry(EntryDraft draft, LaunchEntry? previousEntry)
    {
        return new LaunchEntry
        {
            Id = (draft.Id ?? string.Empty).Trim(),
            Name = (draft.Name ?? string.Empty).Trim(),
            Directory = (draft.Directory ?? string.Empty).Trim(),
            Command = (draft.Command ?? string.Empty).Trim(),
            Confirm = false,
            Selected = draft.Selected,
            UpdatedAtUtc = previousEntry?.UpdatedAtUtc ?? string.Empty,
        };
    }

    public static string? ValidateEntry(
        LaunchEntry entry,
        string? originalId,
        IEnumerable<LaunchEntry> existingEntries)
    {
        if (string.IsNullOrWhiteSpace(entry.Id))
            return "按钮 ID 不能为空";
        if (string.IsNullOrWhiteSpace(entry.Name))
            return "按钮名称不能为空";
        if (string.IsNullOrWhiteSpace(entry.Directory))
            return "按钮目录不能为空";
        if (string.IsNullOrWhiteSpace(entry.Command))
            return "按钮命令不能为空";
        if (existingEntries.Any(existing =>
                !string.Equals(existing.Id, originalId, StringComparison.Ordinal)
                && string.Equals(existing.Id, entry.Id, StringComparison.Ordinal)))
            return $"按钮 ID 已存在: {entry.Id}";

        return null;
    }
}
