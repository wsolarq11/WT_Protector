using Avalonia.Input;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowDragTextTransferTests
{
    [Fact]
    public void CreateEntryIdTransferStoresTextPayload()
    {
        var transfer = MainWindowDragTextTransfer.CreateEntryIdTransfer("three");

        Assert.True(MainWindowDragTextTransfer.CanAcceptEntryId(transfer));
        Assert.Equal("three", MainWindowDragTextTransfer.TryGetEntryId(transfer));
    }

    [Fact]
    public void TryGetEntryIdReturnsNullForWhitespacePayload()
    {
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateText("   "));

        Assert.True(MainWindowDragTextTransfer.CanAcceptEntryId(transfer));
        Assert.Null(MainWindowDragTextTransfer.TryGetEntryId(transfer));
    }

    [Fact]
    public void CanAcceptEntryIdReturnsFalseWithoutText()
    {
        var transfer = new DataTransfer();

        Assert.False(MainWindowDragTextTransfer.CanAcceptEntryId(transfer));
        Assert.Null(MainWindowDragTextTransfer.TryGetEntryId(transfer));
    }
}
