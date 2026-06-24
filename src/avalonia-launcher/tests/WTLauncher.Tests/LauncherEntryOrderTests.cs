using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryOrderTests
{
    [Fact]
    public void SyncWithEntriesRemovesUnknownIdsThenAppendsMissingIds()
    {
        var order = ImmutableList.Create("two", "retired", "one");

        var result = LauncherEntryOrder.SyncWithEntries(order, ["one", "two", "three"]);

        Assert.Equal(["two", "one", "three"], result);
    }

    [Fact]
    public void SyncWithEntriesNormalizesIdsFromSettingsAndKnownEntries()
    {
        var order = ImmutableList.Create(" two ", "retired", " one ");

        var result = LauncherEntryOrder.SyncWithEntries(order, [" one ", "two", " three "]);

        Assert.Equal(["two", "one", "three"], result);
    }

    [Fact]
    public void SyncWithEntriesRejectsBlankCustomOrderBeforeMutatingOrder()
    {
        var order = ImmutableList.Create("one", " ", "two");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", "two"]));

        Assert.Equal("customOrder", exception.ParamName);
    }

    [Fact]
    public void SyncWithEntriesRejectsDuplicateCustomOrderBeforeMutatingOrder()
    {
        var order = ImmutableList.Create("one", " two ", "two");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", "two"]));

        Assert.Equal("customOrder", exception.ParamName);
    }

    [Fact]
    public void SyncWithEntriesRejectsBlankKnownEntryIdsBeforeMutatingOrder()
    {
        var order = ImmutableList.Create("one");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", " ", "two"]));

        Assert.Equal("entryIds", exception.ParamName);
    }

    [Fact]
    public void SyncWithEntriesRejectsDuplicateKnownEntryIdsBeforeMutatingOrder()
    {
        var order = ImmutableList.Create("one");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.SyncWithEntries(order, ["one", " two ", "two"]));

        Assert.Equal("entryIds", exception.ParamName);
    }

    [Fact]
    public void MoveRelativeMovesDraggedEntryBeforeTarget()
    {
        var order = ImmutableList.Create("one", "two", "three");

        var (changed, result) = LauncherEntryOrder.MoveRelative(order, "three", "one", insertAfter: false);

        Assert.True(changed);
        Assert.Equal(["three", "one", "two"], result);
    }

    [Fact]
    public void MoveRelativeNormalizesDraggedAndTargetIds()
    {
        var order = ImmutableList.Create(" one ", " two ", "three");

        var (changed, result) = LauncherEntryOrder.MoveRelative(order, " three ", " one ", insertAfter: false);

        Assert.True(changed);
        Assert.Equal(["three", "one", "two"], result);
    }

    [Fact]
    public void MoveRelativeNormalizesIdsBeforeMoving()
    {
        var order = ImmutableList.Create(" one ", "two", "three");

        var (changed, result) = LauncherEntryOrder.MoveRelative(order, " one ", " three ", insertAfter: true);

        Assert.True(changed);
        Assert.Equal(["two", "three", "one"], result);
    }

    [Fact]
    public void MoveRelativeRejectsDuplicateCustomOrderBeforeMutatingOrder()
    {
        var order = ImmutableList.Create(" one ", "two", " two ", "three");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, " three ", " one ", insertAfter: false));

        Assert.Equal("customOrder", exception.ParamName);
    }

    [Fact]
    public void MoveRelativeRejectsBlankCustomOrderBeforeMutatingOrder()
    {
        var order = ImmutableList.Create("one", " ", "two");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, "two", "one", insertAfter: false));

        Assert.Equal("customOrder", exception.ParamName);
    }

    [Fact]
    public void MoveRelativeRejectsUnknownDraggedEntryBeforeMutatingOrder()
    {
        var order = ImmutableList.Create("one", "two", "three");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, "missing", "one", insertAfter: false));

        Assert.Equal("draggedEntryId", exception.ParamName);
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
        var order = ImmutableList.Create("one", "two", "three");

        var (changed, result) = LauncherEntryOrder.MoveRelative(order, "one", targetEntryId, insertAfter);

        Assert.True(changed);
        Assert.Equal([first, second, third], result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MoveRelativeRejectsMissingTargetBeforeMutatingOrder(bool insertAfter)
    {
        var order = ImmutableList.Create("one", "two", "three");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryOrder.MoveRelative(order, "one", "missing", insertAfter));

        Assert.Equal("targetEntryId", exception.ParamName);
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
    public void BuildIndexReturnsReadOnlyIndexSnapshot()
    {
        var index = LauncherEntryOrder.BuildIndex(["two", "one"]);

        var dictionary = Assert.IsAssignableFrom<IDictionary<string, int>>(index);
        Assert.True(dictionary.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => dictionary.Add("three", 2));
        Assert.Throws<NotSupportedException>(() => dictionary["two"] = 9);
        Assert.Equal(0, index["two"]);
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
