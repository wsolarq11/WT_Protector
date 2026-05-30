using Avalonia.Input;

namespace WTLauncher.App.Views;

public static class MainWindowDragEntryTransfer
{
    public static void SetEntryId(DataTransfer dataTransfer, string entryId)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        dataTransfer.Add(DataTransferItem.CreateText(entryId));
    }

    public static bool ContainsEntryId(IDataTransfer dataTransfer)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        return dataTransfer.Contains(DataFormat.Text);
    }

    public static string? TryGetEntryId(IDataTransfer dataTransfer)
    {
        ArgumentNullException.ThrowIfNull(dataTransfer);

        var entryId = dataTransfer.TryGetText();
        return string.IsNullOrWhiteSpace(entryId) ? null : entryId;
    }
}
