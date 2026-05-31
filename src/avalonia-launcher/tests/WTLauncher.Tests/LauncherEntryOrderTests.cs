using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryOrderTests
{
    [Fact]
    public void SyncWithEntriesRemovesUnknownIdsThenAppendsMissingIds()
    {
        List<string> order = ["two", "retired", "one"];

        LauncherEntryOrder.SyncWithEntries(order, ["one", "two", "three"]);

        Assert.Equal(["two", "one", "three"], order);
    }

    [Fact]
    public void SyncWithEntriesNormalizesIdsFromSettingsAndKnownEntries()
    {
        List<string> order = [" two ", "retired", " one "];

        LauncherEntryOrder.SyncWithEntries(order, [" one ", "two", " three "]);

        Assert.Equal(["two", "one", "three"], order);
    }

    [Fact]
    public void SyncWithEntriesRejectsBlankCustomOrderBeforeMutatingOrder()
    {
        List<string> order = ["one", " ", "two"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", "two"]));

        Assert.Equal("customOrder", exception.ParamName);
        Assert.Equal(["one", " ", "two"], order);
    }

    [Fact]
    public void SyncWithEntriesRejectsDuplicateCustomOrderBeforeMutatingOrder()
    {
        List<string> order = ["one", " two ", "two"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", "two"]));

        Assert.Equal("customOrder", exception.ParamName);
        Assert.Equal(["one", " two ", "two"], order);
    }

    [Fact]
    public void SyncWithEntriesRejectsBlankKnownEntryIdsBeforeMutatingOrder()
    {
        List<string> order = ["one"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", " ", "two"]));

        Assert.Equal("entryIds", exception.ParamName);
        Assert.Equal(["one"], order);
    }

    [Fact]
    public void SyncWithEntriesRejectsDuplicateKnownEntryIdsBeforeMutatingOrder()
    {
        List<string> order = ["one"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", " two ", "two"]));

        Assert.Equal("entryIds", exception.ParamName);
        Assert.Equal(["one"], order);
    }

    [Fact]
    public void MoveRelativeMovesDraggedEntryBeforeTarget()
    {
        List<string> order = ["one", "two", "three"];

        var moved = LauncherEntryOrder.MoveRelative(order, "three", "one", insertAfter: false);

        Assert.True(moved);
        Assert.Equal(["three", "one", "two"], order);
    }

    [Fact]
    public void MoveRelativeNormalizesDraggedAndTargetIds()
    {
        List<string> order = [" one ", " two ", "three"];

        var moved = LauncherEntryOrder.MoveRelative(order, " three ", " one ", insertAfter: false);

        Assert.True(moved);
        Assert.Equal(["three", "one", "two"], order);
    }

    [Fact]
    public void MoveRelativeNormalizesIdsBeforeMoving()
    {
        List<string> order = [" one ", "two", "three"];

        var moved = LauncherEntryOrder.MoveRelative(order, " one ", " three ", insertAfter: true);

        Assert.True(moved);
        Assert.Equal(["two", "three", "one"], order);
    }

    [Fact]
    public void MoveRelativeRejectsDuplicateCustomOrderBeforeMutatingOrder()
    {
        List<string> order = [" one ", "two", " two ", "three"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, " three ", " one ", insertAfter: false));

        Assert.Equal("customOrder", exception.ParamName);
        Assert.Equal([" one ", "two", " two ", "three"], order);
    }

    [Fact]
    public void MoveRelativeRejectsBlankCustomOrderBeforeMutatingOrder()
    {
        List<string> order = ["one", " ", "two"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, "two", "one", insertAfter: false));

        Assert.Equal("customOrder", exception.ParamName);
        Assert.Equal(["one", " ", "two"], order);
    }

    [Fact]
    public void MoveRelativeRejectsUnknownDraggedEntryBeforeMutatingOrder()
    {
        List<string> order = ["one", "two", "three"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, "missing", "one", insertAfter: false));

        Assert.Equal("draggedEntryId", exception.ParamName);
        Assert.Equal(["one", "two", "three"], order);
    }

    [Theory]
    [InlineData(false, "three", "two", "one", "three")]
    [InlineData(true, "three", "two", "three", "one")]
    public void MoveRelativeSupportsBeforeAndAfterTarget(
        bool insertAfter,
        string targetEntryId,
        string first,
        string second,
        string third)
    {
        List<string> order = ["one", "two", "three"];

        var moved = LauncherEntryOrder.MoveRelative(order, "one", targetEntryId, insertAfter);

        Assert.True(moved);
        Assert.Equal([first, second, third], order);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MoveRelativeRejectsMissingTargetBeforeMutatingOrder(bool insertAfter)
    {
        List<string> order = ["one", "two", "three"];

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, "one", "missing", insertAfter));

        Assert.Equal("targetEntryId", exception.ParamName);
        Assert.Equal(["one", "two", "three"], order);
    }

    [Fact]
    public void BuildIndexNormalizesIdsBeforeIndexing()
    {
        var index = LauncherEntryOrder.BuildIndex([" two ", "one"]);

        Assert.Equal(0, index["two"]);
        Assert.Equal(1, index["one"]);
        Assert.DoesNotContain(" two ", index.Keys);
    }

    [Fact]
    public void BuildIndexRejectsDuplicateIds()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.BuildIndex(["two", "one", "two"]));

        Assert.Equal("customOrder", exception.ParamName);
    }

    [Fact]
    public void BuildIndexRejectsBlankIds()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.BuildIndex(["two", " ", "one"]));

        Assert.Equal("customOrder", exception.ParamName);
    }

    [Fact]
    public void BuildIndexNormalizesKeysBeforeIndexing()
    {
        var index = LauncherEntryOrder.BuildIndex([" two ", "one"]);

        Assert.Equal(0, index["two"]);
        Assert.Equal(1, index["one"]);
    }
}
