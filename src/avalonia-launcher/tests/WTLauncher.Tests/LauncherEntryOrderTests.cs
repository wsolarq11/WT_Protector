using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryOrderTests
{
    [Fact]
    public void SyncWithEntriesRemovesUnknownAndDuplicateIdsThenAppendsMissingIds()
    {
        List<string> order = ["two", "retired", "two", "", "one"];

        LauncherEntryOrder.SyncWithEntries(order, ["one", "two", "three", "three"]);

        Assert.Equal(["two", "one", "three"], order);
    }

    [Fact]
    public void MoveRelativeMovesDraggedEntryBeforeTarget()
    {
        List<string> order = ["one", "two", "three"];

        LauncherEntryOrder.MoveRelative(order, "three", "one", insertAfter: false);

        Assert.Equal(["three", "one", "two"], order);
    }

    [Theory]
    [InlineData(false, "three", "two", "one", "three")]
    [InlineData(true, "three", "two", "three", "one")]
    [InlineData(false, "missing", "two", "three", "one")]
    [InlineData(true, "missing", "two", "three", "one")]
    public void MoveRelativeSupportsBeforeAfterAndMissingTargetFallback(
        bool insertAfter,
        string targetEntryId,
        string first,
        string second,
        string third)
    {
        List<string> order = ["one", "two", "three"];

        LauncherEntryOrder.MoveRelative(order, "one", targetEntryId, insertAfter);

        Assert.Equal([first, second, third], order);
    }

    [Fact]
    public void BuildIndexUsesFirstOccurrenceForDuplicateIds()
    {
        var index = LauncherEntryOrder.BuildIndex(["two", "one", "two"]);

        Assert.Equal(0, index["two"]);
        Assert.Equal(1, index["one"]);
    }
}
