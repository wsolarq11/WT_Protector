using Avalonia.Input;

namespace WTLauncher.App.Views;

public static class MainWindowDragTextTransfer
{
    public static DataTransfer CreateEntryIdTransfer(string entryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);

        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateText(entryId));
        return transfer;
    }

    public static bool CanAcceptEntryId(IDataTransfer dataTransfer)
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
