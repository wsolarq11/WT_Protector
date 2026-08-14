using WTLauncher.App.ViewModels;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class LauncherBatchLaunchReportTests
{
    [Fact]
    public void ReportPreservesRequestedCount()
    {
        var report = new LauncherBatchLaunchReport(requestedCount: 2);

        Assert.Equal(2, report.RequestedCount);
        Assert.Equal(0, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Empty(report.FailureMessages);
    }

    [Fact]
    public void ReportPreservesFailureMessages()
    {
        var report = new LauncherBatchLaunchReport(
            requestedCount: 1,
            failureMessages:
            [
                "one: missing one",
                "two: missing two",
            ]);

        Assert.Equal(1, report.RequestedCount);
        Assert.Equal(2, report.FailedCount);
        Assert.Equal(0, report.SkippedCount);
        Assert.Equal(["one: missing one", "two: missing two"], report.FailureMessages);
    }

    [Fact]
    public void ReportFailureMessagesDoNotExposeMutableStorage()
    {
        var source = new List<string> { "one: missing one" };
        var report = new LauncherBatchLaunchReport(failureMessages: source);
        source.Add("fake success");

        Assert.IsNotType<List<string>>(report.FailureMessages);
        var messages = Assert.IsAssignableFrom<IList<string>>(report.FailureMessages);
        Assert.True(messages.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => messages.Add("fake success"));
        Assert.Equal(["one: missing one"], report.FailureMessages);
        Assert.Equal(1, report.FailedCount);
    }

    [Fact]
    public void ReportPreservesSkippedCount()
    {
        var report = new LauncherBatchLaunchReport(skippedCount: 5);

        Assert.Equal(5, report.SkippedCount);
    }

    [Fact]
    public void ReportRejectsNegativeRequestedCount()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LauncherBatchLaunchReport(requestedCount: -1));

        Assert.Equal("requestedCount", exception.ParamName);
    }

    [Fact]
    public void ReportRejectsNegativeSkippedCount()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LauncherBatchLaunchReport(skippedCount: -1));

        Assert.Equal("skippedCount", exception.ParamName);
    }

    [Fact]
    public void ReportBuilderPreservesFailureEntryIdentity()
    {
        var builder = new LauncherBatchLaunchReportBuilder();
        builder.RecordFailure("one", "One", new InvalidOperationException("blocked"));

        var report = builder.ToReport();

        Assert.Equal(["one"], report.FailureEntryIds);
        Assert.Equal(["One"], report.FailureEntryNames);
    }

    [Fact]
    public void ReportRejectsNullFailureMessages()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            new LauncherBatchLaunchReport(failureMessages: ["ok", null!]));

        Assert.Equal("Launcher batch failure message is null.", exception.Message);
    }
}
