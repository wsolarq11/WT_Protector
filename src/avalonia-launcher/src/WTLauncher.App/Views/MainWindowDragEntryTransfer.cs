using Avalonia.Input;
using WTLauncher.Core;

namespace WTLauncher.App.Views;

public static class MainWindowDragEntryTransfer
{
    public static void SetEntryId(DataTransfer dataTransfer, string entryId)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        dataTransfer.Add(DataTransferItem.CreateText(entryId));
    }

    public static string? TryGetEntryId(IDataTransfer dataTransfer)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        var entryId = LauncherEntryIds.Normalize(dataTransfer.TryGetText());
        return LauncherEntryIds.IsUsable(entryId) ? entryId : null;
    }
}
