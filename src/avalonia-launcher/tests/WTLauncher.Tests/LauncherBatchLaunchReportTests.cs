using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherBatchLaunchReportTests
{
    [Fact]
    public void ToStatusTextReportsStartedCountWhenNoFailures()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordStarted();
        report.RecordStarted();

        Assert.Equal("已启动 2 个勾选项", report.ToStatusText());
    }

    [Fact]
    public void ToStatusTextLimitsFailurePreview()
    {
        var report = new LauncherBatchLaunchReport();

        report.RecordStarted();
        report.RecordFailure("one", new InvalidOperationException("missing one"));
        report.RecordFailure("two", new InvalidOperationException("missing two"));
        report.RecordFailure("three", new InvalidOperationException("missing three"));
        report.RecordFailure("four", new InvalidOperationException("missing four"));

        Assert.Equal(
            "已启动 1 个勾选项，4 个失败：one: missing one；two: missing two；three: missing three；…另有 1 项",
            report.ToStatusText());
    }
}
