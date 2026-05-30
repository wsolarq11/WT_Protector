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
    public void SyncWithEntriesNormalizesIdsFromSettingsAndKnownEntries()
    {
        List<string> order = [" two ", "retired", " two ", "", " one "];

        LauncherEntryOrder.SyncWithEntries(order, [" one ", "two", "three", " three "]);

        Assert.Equal(["two", "one", "three"], order);
    }

    [Fact]
    public void SyncWithEntriesNormalizesIdsBeforeFiltering()
    {
        List<string> order = [" two ", "retired", "two", " ", " one "];

        LauncherEntryOrder.SyncWithEntries(order, [" one ", "two", " three ", "three"]);

        Assert.Equal(["two", "one", "three"], order);
    }

    [Fact]
    public void MoveRelativeMovesDraggedEntryBeforeTarget()
    {
        List<string> order = ["one", "two", "three"];

        LauncherEntryOrder.MoveRelative(order, "three", "one", insertAfter: false);

        Assert.Equal(["three", "one", "two"], order);
    }

    [Fact]
    public void MoveRelativeNormalizesDraggedAndTargetIds()
    {
        List<string> order = [" one ", " two ", "three"];

        LauncherEntryOrder.MoveRelative(order, " three ", " one ", insertAfter: false);

        Assert.Equal(["three", "one", "two"], order);
    }

    [Fact]
    public void MoveRelativeNormalizesIdsBeforeMoving()
    {
        List<string> order = [" one ", "two", "three"];

        LauncherEntryOrder.MoveRelative(order, " one ", " three ", insertAfter: true);

        Assert.Equal(["two", "three", "one"], order);
    }

    [Fact]
    public void MoveRelativeIgnoresUnknownDraggedEntry()
    {
        List<string> order = ["one", "two", "three"];

        LauncherEntryOrder.MoveRelative(order, "missing", "one", insertAfter: false);

        Assert.Equal(["one", "two", "three"], order);
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
    public void BuildIndexNormalizesIdsBeforeIndexing()
    {
        var index = LauncherEntryOrder.BuildIndex([" two ", "one", "two"]);

        Assert.Equal(0, index["two"]);
        Assert.Equal(1, index["one"]);
        Assert.DoesNotContain(" two ", index.Keys);
    }

    [Fact]
    public void BuildIndexUsesFirstOccurrenceForDuplicateIds()
    {
        var index = LauncherEntryOrder.BuildIndex(["two", "one", "two"]);

        Assert.Equal(0, index["two"]);
        Assert.Equal(1, index["one"]);
    }

    [Fact]
    public void BuildIndexNormalizesKeysBeforeIndexing()
    {
        var index = LauncherEntryOrder.BuildIndex([" two ", "one", " two "]);

        Assert.Equal(0, index["two"]);
        Assert.Equal(1, index["one"]);
    }
}
