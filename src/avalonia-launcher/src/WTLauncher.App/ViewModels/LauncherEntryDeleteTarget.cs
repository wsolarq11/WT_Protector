using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherEntryDeleteTarget
{
    public LauncherEntryDeleteTarget(LaunchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EntryId = RequireEntryId(entry.Id);
        EntryName = RequireEntryName(entry.Name);
    }

    public string EntryId { get; }

    public string EntryName { get; }

    private static string RequireEntryId(string? entryId)
    {
        var normalized = LauncherEntryIds.Normalize(entryId);
        if (!LauncherEntryIds.IsUsable(normalized))
            throw new InvalidOperationException("Launcher entry delete target id is required.");

        return normalized;
    }

    private static string RequireEntryName(string? entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launcher entry delete target name is required.");

        return normalized;
    }
}
