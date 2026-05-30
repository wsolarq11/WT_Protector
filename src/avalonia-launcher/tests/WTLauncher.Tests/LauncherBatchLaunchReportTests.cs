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
        Assert.Equal(["one: missing one", "two: missing two"], report.FailureMessages);
    }

    [Fact]
    public void ReportNormalizesFailureEntryNames()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordFailure(" one ", new InvalidOperationException("missing one"));
        report.RecordFailure(null!, new InvalidOperationException("missing name"));

        Assert.Equal(["one: missing one", "(unnamed): missing name"], report.FailureMessages);
    }
}
