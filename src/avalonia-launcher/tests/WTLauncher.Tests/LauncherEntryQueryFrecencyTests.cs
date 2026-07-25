using WTLauncher.Core;

namespace WTLauncher.Tests;

// Frecency 排序测试（task 07-11-entry-launch-count-frecency）。
// ComputeFrecency 是 private,通过 FilterAndSort observable 行为验证：
// 构造 entry 时用 DateTimeOffset.UtcNow 反推 launchedAtUtc,触发预期时间桶。
public sealed class LauncherEntryQueryFrecencyTests
{
    private static LaunchEntry Entry(
        string id,
        string name,
        int launchCount = 0,
        string? launchedAtUtc = "") =>
        new()
        {
            Id = id,
            Name = name,
            Directory = (WTLauncher.Core.DirectoryPath)"D:\\proj",
            Command = "pwsh",
            UpdatedAtUtc = "",
            LaunchedAtUtc = launchedAtUtc ?? "",
            LaunchCount = launchCount,
        };

    private static string IsoUtc(DateTimeOffset value) => value.ToString("O");

    [Fact]
    public void FilterAndSort_FrequentMode_DoesNotThrowOnEmptyList()
    {
        var visible = LauncherEntryQuery.FilterAndSort(
            Array.Empty<LaunchEntry>(),
            string.Empty,
            LauncherEntryQuery.FrequentSortModeId,
            [],
            static entry => entry);

        Assert.Empty(visible);
    }

    [Fact]
    public void NormalizeSortModeId_AcceptsFrequentToLowercase()
    {
        Assert.Equal(
            LauncherEntryQuery.FrequentSortModeId,
            LauncherEntryQuery.NormalizeSortModeId(LauncherEntryQuery.FrequentSortModeId));
    }

    [Fact]
    public void FilterAndSort_FrequentMode_OrdersByScoreDesc()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new[]
        {
            // A=10/30min前 → bucket 1h × 4.0 = 40.0
            Entry("a", "Alpha", launchCount: 10, launchedAtUtc: IsoUtc(now.AddMinutes(-30))),
            // B=3/10min前 → bucket 1h × 4.0 = 12.0
            Entry("b", "Bravo", launchCount: 3, launchedAtUtc: IsoUtc(now.AddMinutes(-10))),
            // C=5/2h前 → bucket 24h × 2.0 = 10.0
            Entry("c", "Charlie", launchCount: 5, launchedAtUtc: IsoUtc(now.AddHours(-2))),
        };

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.FrequentSortModeId,
            [],
            static entry => entry);

        Assert.Equal(["a", "b", "c"], visible.Select(e => e.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_FrequentMode_FourBucketsDecayCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new[]
        {
            // D=20/3d前 → bucket 7d × 0.5 = 10.0
            Entry("d", "Delta", launchCount: 20, launchedAtUtc: IsoUtc(now.AddDays(-3))),
            // E=1/10d前 → bucket else × 0.25 = 0.25
            Entry("e", "Echo", launchCount: 1, launchedAtUtc: IsoUtc(now.AddDays(-10))),
            // A=10/30min前 → 40.0
            Entry("a", "Alpha", launchCount: 10, launchedAtUtc: IsoUtc(now.AddMinutes(-30))),
            // B=3/10min前 → 12.0
            Entry("b", "Bravo", launchCount: 3, launchedAtUtc: IsoUtc(now.AddMinutes(-10))),
            // C=5/2h前 → 10.0 (与 D 同分,name tiebreak: C < D)
            Entry("c", "Charlie", launchCount: 5, launchedAtUtc: IsoUtc(now.AddHours(-2))),
        };

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.FrequentSortModeId,
            [],
            static entry => entry);

        // 预期: a(40) > b(12) > c=d score 同为 10 → name tiebreak → c(Charlie) < d(Delta) → e(0.25)
        Assert.Equal(["a", "b", "c", "d", "e"], visible.Select(e => e.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_FrequentMode_ZeroCountAlwaysScore0()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new[]
        {
            // count=0 即使 launchedAt 是 1 分钟前 → score=0
            Entry("zero", "Zero", launchCount: 0, launchedAtUtc: IsoUtc(now.AddMinutes(-1))),
            // count=2/3h前 → 2 × 2.0 = 4.0
            Entry("two", "Two", launchCount: 2, launchedAtUtc: IsoUtc(now.AddHours(-3))),
        };

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.FrequentSortModeId,
            [],
            static entry => entry);

        Assert.Equal(["two", "zero"], visible.Select(e => e.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_FrequentMode_NeverLaunchedFallsInElseBucket()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new[]
        {
            // count=4 + launchedAt="" → ParseLaunchedAt 返回 MinValue → delta 巨大 → else × 0.25 = 1.0
            Entry("legacy", "Legacy", launchCount: 4, launchedAtUtc: ""),
            // count=0 + launchedAt="" → score=0
            Entry("never", "Never", launchCount: 0, launchedAtUtc: ""),
        };

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.FrequentSortModeId,
            [],
            static entry => entry);

        // legacy(1.0) > never(0.0)
        Assert.Equal(["legacy", "never"], visible.Select(e => e.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_FrequentMode_NameTiebreakWhenScoreEqual()
    {
        var now = DateTimeOffset.UtcNow;
        // 两条同 score: B=4/2h前(4×2.0=8), A=4/2h前(4×2.0=8) → name asc → A 在 B 前
        var entries = new[]
        {
            Entry("id1", "Bravo", launchCount: 4, launchedAtUtc: IsoUtc(now.AddHours(-2))),
            Entry("id2", "Alpha", launchCount: 4, launchedAtUtc: IsoUtc(now.AddHours(-2))),
        };

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.FrequentSortModeId,
            [],
            static entry => entry);

        Assert.Equal(["id2", "id1"], visible.Select(e => e.Id).ToArray());
    }

    [Fact]
    public void FilterAndSort_FrequentMode_IdTiebreakWhenScoreAndNameEqual()
    {
        var now = DateTimeOffset.UtcNow;
        // 同 score + 同 name → id asc
        var entries = new[]
        {
            Entry("zzz", "Same", launchCount: 4, launchedAtUtc: IsoUtc(now.AddHours(-2))),
            Entry("aaa", "Same", launchCount: 4, launchedAtUtc: IsoUtc(now.AddHours(-2))),
        };

        var visible = LauncherEntryQuery.FilterAndSort(
            entries,
            string.Empty,
            LauncherEntryQuery.FrequentSortModeId,
            [],
            static entry => entry);

        Assert.Equal(["aaa", "zzz"], visible.Select(e => e.Id).ToArray());
    }
}

