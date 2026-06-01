using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherBatchLaunchReportTests
{
    [Fact]
    public void ReportTracksRequestedCount()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordRequested();
        report.RecordRequested();

        Assert.Equal(2, report.RequestedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Empty(report.FailureMessages);
    }

    [Fact]
    public void ReportUsesRequestedLaunchLanguage()
    {
        Assert.NotNull(typeof(LauncherBatchLaunchReport).GetProperty(nameof(LauncherBatchLaunchReport.RequestedCount)));
        Assert.NotNull(typeof(LauncherBatchLaunchReport).GetMethod(nameof(LauncherBatchLaunchReport.RecordRequested)));
        Assert.Null(typeof(LauncherBatchLaunchReport).GetProperty("StartedCount"));
        Assert.Null(typeof(LauncherBatchLaunchReport).GetMethod("RecordStarted"));
    }

    [Fact]
    public void ReportTracksFailureMessages()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordRequested();
        report.RecordFailure("one", new InvalidOperationException("missing one"));
        report.RecordFailure("two", new InvalidOperationException("missing two"));

        Assert.Equal(1, report.RequestedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(["one: missing one", "two: missing two"], report.FailureMessages);
    }

    [Fact]
    public void ReportTracksFailureWithoutInventingEntryName()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordFailure(new InvalidOperationException("entry name is empty"));

        Assert.Equal(1, report.FailedCount);
        Assert.Equal(["entry name is empty"], report.FailureMessages);
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

        Assert.Equal(["one: missing one"], report.FailureMessages);
    }

    [Fact]
    public void ReportRejectsBlankFailureEntryName()
    {
        var report = new LauncherBatchLaunchReport();

        var exception = Assert.Throws<InvalidDataException>(() =>
            report.RecordFailure(" ", new InvalidOperationException("missing name")));

        Assert.Equal("Entry name is required for batch launch report.", exception.Message);
        Assert.Empty(report.FailureMessages);
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
