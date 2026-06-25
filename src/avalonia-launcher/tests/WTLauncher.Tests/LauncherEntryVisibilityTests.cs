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

        Assert.Equal("Launcher sort mode is required.", exception.Message);
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
    public void FilterAndSort_ReturnsReadOnlyList()
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

        // The return type is IReadOnlyList<T> — callers cannot mutate without casting.
        Assert.IsAssignableFrom<IReadOnlyList<LaunchEntry>>(visible);
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

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.AlphabeticalSortModeId,
                [],
                static entry => entry));

        Assert.Equal("Entry id is required. (Parameter 'Id')", exception.Message);
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

    [Fact]
    public void FilterAndSort_SortsByLaunchedAtDescendingThenNameThenId()
    {
        var entries = new[]
        {
            Entry("two", "Alpha", launchedAtUtc: "2026-05-28T12:00:00Z"),
            Entry("one", "Alpha", launchedAtUtc: "2026-05-28T12:00:00Z"),
            Entry("three", "Beta", launchedAtUtc: "2026-05-27T12:00:00Z"),
            Entry("zero", "Zero", launchedAtUtc: ""),
        };
        var customOrder = new List<string> { "zero", "three", "two", "one" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryVisibility.RecentSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal(["one", "two", "three", "zero"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_RecentSortRejectsNullLaunchedAt()
    {
        var entries = new[]
        {
            Entry("one", "One", launchedAtUtc: "2026-05-28T12:00:00Z"),
            Entry("two", "Two", launchedAtUtc: null!),
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.RecentSortModeId,
                [],
                static entry => entry));

        Assert.Equal("Launcher entry launchedAtUtc is required for projection.", exception.Message);
    }

    [Fact]
    public void FilterAndSort_RecentSortRejectsInvalidTimestamp()
    {
        var entries = new[]
        {
            Entry("one", "One", launchedAtUtc: "not-a-date"),
            Entry("two", "Two", launchedAtUtc: "2026-05-28T12:00:00Z"),
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryVisibility.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryVisibility.RecentSortModeId,
                [],
                static entry => entry));

        Assert.Equal("Invalid launcher entry launchedAtUtc: not-a-date.", exception.Message);
    }

    [Fact]
    public void FilterAndSort_RecentSortTreatsEmptyLaunchedAtAsMinValue()
    {
        var entries = new[]
        {
            Entry("old", "Old", launchedAtUtc: "2025-01-01T00:00:00Z"),
            Entry("newest", "Newest", launchedAtUtc: "2026-06-01T00:00:00Z"),
            Entry("never", "Never", launchedAtUtc: ""),
        };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryVisibility.RecentSortModeId,
            [],
            static entry => entry);

        Assert.Equal(["newest", "old", "never"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_FiltersByMultipleTokensWithAndSemantics()
    {
        var entries = new[]
        {
            Entry("one", "One", directory: "D:\\pwsh-projects", command: "codex"),
            Entry("two", "Two", directory: "D:\\other", command: "pwsh"),
            Entry("three", "Three", directory: "D:\\pwsh-tools", command: "pwsh"),
        };
        var customOrder = new List<string> { "one", "two", "three" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            "pwsh D:",
            LauncherEntryVisibility.CustomSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal(["one", "two", "three"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_MultiTokenRequiresAllTokensToMatch()
    {
        var entries = new[]
        {
            Entry("one", "One", directory: "D:\\foo", command: "pwsh"),
            Entry("two", "Two", directory: "D:\\bar", command: "codex"),
            Entry("three", "Three", directory: "D:\\foo", command: "codex"),
        };
        var customOrder = new List<string> { "one", "two", "three" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            "pwsh foo",
            LauncherEntryVisibility.CustomSortModeId,
            customOrder,
            static entry => entry);

        Assert.Equal(["one"], visible.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_MultiTokenReturnsEmptyWhenNoEntryMatchesAll()
    {
        var entries = new[]
        {
            Entry("one", "One", directory: "D:\\foo", command: "pwsh"),
            Entry("two", "Two", directory: "D:\\bar", command: "codex"),
        };
        var customOrder = new List<string> { "one", "two" };

        var visible = LauncherEntryVisibility.FilterAndSort(
            entries,
            "pwsh bar",
            LauncherEntryVisibility.CustomSortModeId,
            customOrder,
            static entry => entry);

        Assert.Empty(visible);
    }

    private static LaunchEntry Entry(
        string id,
        string name,
        string directory = "",
        string command = "",
        string? updatedAtUtc = "",
        string? launchedAtUtc = "") =>
        new()
        {
            Id = id,
            Name = name,
            Directory = directory,
            Command = command,
            UpdatedAtUtc = updatedAtUtc!,
            LaunchedAtUtc = launchedAtUtc!,
        };
}
