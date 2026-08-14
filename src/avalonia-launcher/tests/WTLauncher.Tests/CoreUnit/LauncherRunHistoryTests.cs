using System.Collections.Immutable;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherRunHistoryTests
{
    [Fact]
    public void Append_KeepsNewestWithinMaxEntries()
    {
        var history = ImmutableList<LauncherRunHistoryEntry>.Empty;
        for (var index = 0; index < 5; index++)
        {
            history = LauncherRunHistory.Append(
                history,
                LauncherRunHistoryEntry.Create(
                    "one",
                    "One",
                    $"2026-08-14T00:00:{index:D2}Z",
                    LauncherRunSource.Individual,
                    LauncherRunStatus.Requested),
                maxEntries: 3);
        }

        Assert.Equal(3, history.Count);
        Assert.Equal("2026-08-14T00:00:02Z", history[0].TimestampUtc);
        Assert.Equal("2026-08-14T00:00:04Z", history[^1].TimestampUtc);
    }

    [Fact]
    public void Sanitize_DropsInvalidAndNormalizesValid()
    {
        var valid = LauncherRunHistoryEntry.Create(
            "one",
            "One",
            "2026-08-14T00:00:00Z",
            LauncherRunSource.Selection,
            LauncherRunStatus.Failed);
        var invalidStatus = valid with { Status = "completed" };
        var missingTimestamp = valid with { TimestampUtc = "" };

        var result = LauncherRunHistory.Sanitize([valid, invalidStatus, missingTimestamp]);

        var entry = Assert.Single(result);
        Assert.Equal("one", entry.EntryId);
        Assert.Equal("One", entry.EntryName);
    }

    [Fact]
    public void Create_RejectsEmptyIdOrInvalidTimestamp()
    {
        Assert.Throws<ArgumentException>(() => LauncherRunHistoryEntry.Create(
            "",
            "One",
            "2026-08-14T00:00:00Z",
            LauncherRunSource.Individual,
            LauncherRunStatus.Requested));
        Assert.Throws<ArgumentException>(() => LauncherRunHistoryEntry.Create(
            "one",
            "One",
            "not-a-timestamp",
            LauncherRunSource.Individual,
            LauncherRunStatus.Requested));
    }
}
