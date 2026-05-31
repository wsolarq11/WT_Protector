using Avalonia.Input;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowDragEntryTransferTests
{
    [Fact]
    public void EntryTransferStoresAndReadsEntryIdThroughOneContract()
    {
        var transfer = new DataTransfer();

        MainWindowDragEntryTransfer.SetEntryId(transfer, " one ");

        Assert.Equal("one", MainWindowDragEntryTransfer.TryGetEntryId(transfer));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EntryTransferRejectsBlankEntryIds(string entryId)
    {
        var transfer = new DataTransfer();

        MainWindowDragEntryTransfer.SetEntryId(transfer, entryId);

        Assert.Null(MainWindowDragEntryTransfer.TryGetEntryId(transfer));
    }
}
