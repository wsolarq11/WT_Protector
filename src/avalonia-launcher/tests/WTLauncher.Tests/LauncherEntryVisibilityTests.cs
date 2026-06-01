using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryVisibilityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NormalizeSortModeIdRejectsMissingModeIds(string? sortModeId)
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.NormalizeSortModeId(sortModeId));

        Assert.Equal("Unknown launcher sort mode: .", exception.Message);
    }

    [Fact]
    public void FilterAndSort_FiltersAcrossNameDirectoryAndCommand()
    {
        var entries = new[]
        {
            Entry("one", "One", directory: "D:\\one", command: "pwsh"),
            Entry("two", "Two", directory: "D:\\two", command: "codex"),
            Entry("three", "Three", directory: "D:\\CODEX", command: "pwsh"),
        };
        var customOrder = new List<string> { "one", "two", "three" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            " codeX ",
            LauncherEntryVisibility.CustomSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal(["two", "three"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_RejectsNullFilterTextInsteadOfShowingEverything()
    {
        var entries = new[]
        {
            Entry("one", "One"),
        };

        var exception = Assert.Throws<ArgumentNullException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                null!,
                LauncherEntryVisibility.CustomSortModeId,
                ["one"],
                static entry => entry));

        Assert.Equal("filterText", exception.ParamName);
    }

    [Fact]
    public void FilterAndSort_RejectsNullTextFieldsBeforeProjection()
    {
        var entries = new[]
        {
            Entry("one", name: null!, directory: null!, command: null!),
            Entry("two", "Two", directory: "D:\\two", command: "codex"),
        };
        var customOrder = new List<string> { "one", "two" };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                "codex",
                LauncherEntryVisibility.AlphabeticalSortModeId,
                customOrder,
                static entry => entry));

        Assert.Equal("Launcher entry name is required for projection.", exception.Message);
    }

    [Fact]
    public void FilterAndSort_CustomSortUsesExistingOrderWithoutMutatingIt()
    {
        var entries = new[]
        {
            Entry("one", "One"),
            Entry("two", "Two"),
            Entry("three", "Three"),
        };
        var customOrder = new List<string> { "two", "one", "three" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryVisibility.CustomSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal(["two", "one", "three"], visible.Select(entry => entry.Id).ToArray());
        Assert.Equal(["two", "one", "three"], customOrder);
    }

    [Fact]
    public void FilterAndSort_ReturnsReadOnlyProjectionSnapshot()
    {
        var entries = new[]
        {
            Entry("one", "One"),
        };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryVisibility.AlphabeticalSortModeId,
            [],
            static entry => entry);

        var list = Assert.IsAssignableFrom<IList<LaunchEntry>>(visible);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(Entry("two", "Two")));
        Assert.Throws<NotSupportedException>(() => list[0] = Entry("two", "Two"));
        Assert.Equal(["one"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_CustomSortRejectsMissingEntryIdsBeforeProjection()
    {
        var entries = new[]
        {
            Entry("one", "One"),
            Entry("two", "Two"),
            Entry("three", "Three"),
        };
        var customOrder = new List<string> { "two", "one" };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.CustomSortModeId,
                customOrder,
                static entry => entry));

        Assert.Equal("Launcher custom order is missing entry id for projection: three.", exception.Message);
        Assert.Equal(["two", "one"], customOrder);
    }

    [Fact]
    public void FilterAndSort_CustomSortRejectsUnknownOrderIdsBeforeProjection()
    {
        var entries = new[]
        {
            Entry("one", "One"),
            Entry("two", "Two"),
        };
        var customOrder = new List<string> { "two", "one", "missing" };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.CustomSortModeId,
                customOrder,
                static entry => entry));

        Assert.Equal("Launcher custom order references missing entry id for projection: missing.", exception.Message);
        Assert.Equal(["two", "one", "missing"], customOrder);
    }

    [Fact]
    public void FilterAndSort_CustomSortRejectsDuplicateOrderBeforeProjection()
    {
        var entries = new[]
        {
            Entry("one", "One"),
            Entry("two", "Two"),
        };
        var customOrder = new List<string> { "two", " two " };

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.CustomSortModeId,
                customOrder,
                static entry => entry));

        Assert.Equal("customOrder", exception.ParamName);
        Assert.Equal(["two", " two "], customOrder);
    }

    [Fact]
    public void FilterAndSort_CustomSortNormalizesEntryIdsBeforeLookup()
    {
        var entries = new[]
        {
            Entry(" one ", "Zed"),
            Entry(" two ", "Alpha"),
            Entry("three", "Middle"),
        };
        var customOrder = new List<string> { " one ", " two ", "three" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryVisibility.CustomSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal([" one ", " two ", "three"], visible.Select(entry => entry.Id).ToArray());
        Assert.Equal([" one ", " two ", "three"], customOrder);
    }

    [Fact]
    public void FilterAndSort_RejectsBlankEntryIdsBeforeProjection()
    {
        var entries = new[]
        {
            Entry("one", "One"),
            Entry(" ", "Two"),
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.AlphabeticalSortModeId,
                [],
                static entry => entry));

        Assert.Equal("Launcher entry id is required for projection.", exception.Message);
    }

    [Fact]
    public void FilterAndSort_SortsAlphabeticallyThenById()
    {
        var entries = new[]
        {
            Entry("two", "Beta"),
            Entry("three", "alpha"),
            Entry("one", "Alpha"),
        };
        var customOrder = new List<string> { "two", "three", "one" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryVisibility.AlphabeticalSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal(["one", "three", "two"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_SortsByUpdatedThenNameAndId()
    {
        var entries = new[]
        {
            Entry("two", "Alpha", updatedAtUtc: "2026-05-28T12:00:00Z"),
            Entry("one", "Alpha", updatedAtUtc: "2026-05-28T12:00:00Z"),
            Entry("three", "Beta", updatedAtUtc: "2026-05-27T12:00:00Z"),
            Entry("zero", "Zero"),
        };
        var customOrder = new List<string> { "zero", "three", "two", "one" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryVisibility.UpdatedSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal(["one", "two", "three", "zero"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_RejectsInvalidUpdatedTimestampWhenSortingByUpdated()
    {
        var entries = new[]
        {
            Entry("one", "One", updatedAtUtc: "not-a-date"),
            Entry("two", "Two", updatedAtUtc: "2026-05-28T12:00:00Z"),
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.UpdatedSortModeId,
                [],
                static entry => entry));

        Assert.Equal("Invalid launcher entry updatedAtUtc: not-a-date.", exception.Message);
    }

    [Fact]
    public void FilterAndSort_RejectsNullUpdatedTimestampWhenSortingByUpdated()
    {
        var entries = new[]
        {
            Entry("one", "One", updatedAtUtc: null!),
            Entry("two", "Two", updatedAtUtc: "2026-05-28T12:00:00Z"),
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.UpdatedSortModeId,
                [],
                static entry => entry));

        Assert.Equal("Launcher entry updatedAtUtc is required for projection.", exception.Message);
    }

    private static LaunchEntry Entry(
        string id,
        string name,
        string directory = "",
        string command = "",
        string? updatedAtUtc = "") =>
        new()
        {
            Id = id,
            Name = name,
            Directory = directory,
            Command = command,
            UpdatedAtUtc = updatedAtUtc!,
        };
}
