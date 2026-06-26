using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed record LauncherEntryDeleteTarget
{
    public LauncherEntryDeleteTarget(LaunchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        EntryId = LauncherEntryIds.RequireEntryId(entry.Id, nameof(entry.Id));
        EntryName = RequireEntryName(entry.Name);
    }

    public string EntryId { get; }

    public string EntryName { get; }

    private static string RequireEntryName(string? entryName)
    {
        var normalized = LauncherEntryText.Normalize(entryName);
        if (normalized.Length == 0)
            throw new InvalidOperationException("Launcher entry delete target name is required.");

        return normalized;
    }
}
