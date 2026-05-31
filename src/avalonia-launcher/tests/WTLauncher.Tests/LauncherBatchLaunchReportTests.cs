using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherBatchLaunchReportTests
{
    [Fact]
    public void ReportTracksStartedCount()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordStarted();
        report.RecordStarted();

        Assert.Equal(2, report.StartedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Empty(report.FailureMessages);
    }

    [Fact]
    public void ReportTracksFailureMessages()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordStarted();
        report.RecordFailure("one", new InvalidOperationException("missing one"));
        report.RecordFailure("two", new InvalidOperationException("missing two"));

        Assert.Equal(1, report.StartedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(["one: missing one", "two: missing two"], report.FailureMessages);
    }

    [Fact]
    public void ReportTracksSkippedCount()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordSkipped(2);
        report.RecordSkipped(3);

        Assert.Equal(5, report.SkippedCount);
    }

    [Fact]
    public void ReportRejectsNegativeSkippedCount()
    {
        var report = new LauncherBatchLaunchReport();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            report.RecordSkipped(-1));

        Assert.Equal("count", exception.ParamName);
    }

    [Fact]
    public void ReportNormalizesFailureEntryNames()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordFailure(" one ", new InvalidOperationException("missing one"));
        report.RecordFailure("", new InvalidOperationException("missing name"));

        Assert.Equal(["one: missing one", "(unnamed): missing name"], report.FailureMessages);
    }

    [Fact]
    public void ReportRejectsNullFailureEntryName()
    {
        var report = new LauncherBatchLaunchReport();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            report.RecordFailure(null!, new InvalidOperationException("missing name")));

        Assert.Equal("entryName", exception.ParamName);
    }
}
