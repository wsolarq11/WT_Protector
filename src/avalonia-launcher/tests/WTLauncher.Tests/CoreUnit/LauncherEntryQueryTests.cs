using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherEntryQueryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NormalizeSortModeIdRejectsMissingModeIds(string? sortModeId)
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryQuery.NormalizeSortModeId(sortModeId));

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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            " codeX ",
            LauncherEntryQuery.CustomSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                null!,
                LauncherEntryQuery.CustomSortModeId,
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

        // ponytail: use empty filter so the null-field entry passes filtering and
        // is validated in the sort-phase ThrowIfInvalidProjection instead of
        // depending on RequireNonEmpty inside MatchesFilter (which no longer normalizes).
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.AlphabeticalSortModeId,
                customOrder,
                static entry => entry));

        Assert.Equal("Entry name is required.", exception.Message);
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.CustomSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.AlphabeticalSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.CustomSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.CustomSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.CustomSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.CustomSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.AlphabeticalSortModeId,
                [],
                static entry => entry));

        Assert.Contains("不能为空", exception.Message);
        Assert.Equal("Id", exception.ParamName);
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.AlphabeticalSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.UpdatedSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.UpdatedSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.UpdatedSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.RecentSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.RecentSortModeId,
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
            LauncherEntryQuery.FilterAndSort(
                entries,
                string.Empty,
                LauncherEntryQuery.RecentSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.RecentSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            "pwsh D:",
            LauncherEntryQuery.CustomSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            "pwsh foo",
            LauncherEntryQuery.CustomSortModeId,
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

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            "pwsh bar",
            LauncherEntryQuery.CustomSortModeId,
            customOrder,
            static entry => entry);

        Assert.Empty(visible);
    }

    private static LaunchEntry Entry(
        string id,
        string name,
        string directory = "C:\\default",
        string command = "",
        string? updatedAtUtc = "",
        string? launchedAtUtc = "") =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(id) ? EntryId.Empty : id,
            Name = string.IsNullOrWhiteSpace(name) ? EntryName.Empty : name,
            Directory = string.IsNullOrWhiteSpace(directory) ? WTLauncher.Core.DirectoryPath.Empty : (WTLauncher.Core.DirectoryPath)directory,
            Command = command,
            UpdatedAtUtc = updatedAtUtc!,
            LaunchedAtUtc = launchedAtUtc!,
        };
}

