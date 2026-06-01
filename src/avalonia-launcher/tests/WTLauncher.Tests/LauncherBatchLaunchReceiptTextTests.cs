using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherBatchLaunchReceiptTextTests
{
    [Fact]
    public void ReceiptMarksStartedFailedAndSkippedPlans()
    {
        using var temp = new TempDirectory();
        var attempts = 0;
        var workflow = new LauncherLaunchWorkflow(_ =>
        {
            attempts++;
            if (attempts == 2)
                throw new InvalidOperationException("blocked");
        });
        var preflight = workflow.PreflightBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
                Entry("three", temp.Path),
            ],
            requireConfirmation: false);

        var report = workflow.StartBatch(preflight);
        var receipt = LauncherBatchLaunchReceiptText.Format(preflight, report);

        Assert.Contains("启动回执", receipt);
        Assert.Contains("已请求启动 1 项，失败 1 项，未启动 1 项", receipt);
        Assert.Contains("• 已请求: one", receipt);
        Assert.Contains("• 启动失败: two", receipt);
        Assert.Contains("• 未启动: three", receipt);
        Assert.Contains("two: blocked", receipt);
        Assert.Equal(3, Count(receipt, LaunchArgumentBuilder.TerminalExecutable));
    }

    [Fact]
    public void ReceiptReportsPreflightFailuresWithoutStartedPlans()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var preflight = workflow.PreflightBatch(
            [
                Entry("valid", temp.Path),
                Entry("missing", Path.Combine(temp.Path, "missing")),
            ],
            requireConfirmation: false);

        var report = workflow.StartBatch(preflight);
        var receipt = LauncherBatchLaunchReceiptText.Format(preflight, report);

        Assert.Contains("已请求启动 0 项，失败 1 项，未启动 1 项", receipt);
        Assert.Contains("• 未启动: valid", receipt);
        Assert.Contains("missing:", receipt);
        Assert.DoesNotContain("should not start", receipt);
    }

    [Fact]
    public void CanceledReceiptMarksEveryPlanAsNotStarted()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var preflight = workflow.PreflightBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
            ],
            requireConfirmation: true);

        var receipt = LauncherBatchLaunchReceiptText.Canceled(preflight.Items);

        Assert.Contains("启动回执", receipt);
        Assert.Contains("已取消: 已请求启动 0 项，未启动 2 项", receipt);
        Assert.Contains("• 未启动: one", receipt);
        Assert.Contains("• 未启动: two", receipt);
        Assert.Equal(2, Count(receipt, LaunchArgumentBuilder.TerminalExecutable));
        Assert.DoesNotContain("should not start", receipt);
    }

    [Fact]
    public void ConfirmationFailedReceiptMarksEveryPlanAsNotStarted()
    {
        using var temp = new TempDirectory();
        var workflow = new LauncherLaunchWorkflow(_ => throw new InvalidOperationException("should not start"));
        var preflight = workflow.PreflightBatch(
            [
                Entry("one", temp.Path),
                Entry("two", temp.Path),
            ],
            requireConfirmation: true);

        var receipt = LauncherBatchLaunchReceiptText.ConfirmationFailed(
            preflight.Items,
            new InvalidOperationException("dialog unavailable"));

        Assert.Contains("启动回执", receipt);
        Assert.Contains("启动确认失败: dialog unavailable", receipt);
        Assert.Contains("已请求启动 0 项，未启动 2 项", receipt);
        Assert.Contains("• 未启动: one", receipt);
        Assert.Contains("• 未启动: two", receipt);
        Assert.Equal(2, Count(receipt, LaunchArgumentBuilder.TerminalExecutable));
        Assert.DoesNotContain("should not start", receipt);
    }

    [Fact]
    public void FailedReceiptShowsBatchFailureWithoutStalePlans()
    {
        var receipt = LauncherBatchLaunchReceiptText.Failed(new InvalidOperationException("broken"));

        Assert.Contains("启动回执", receipt);
        Assert.Contains("批量启动失败: broken", receipt);
        Assert.DoesNotContain(LaunchArgumentBuilder.TerminalExecutable, receipt);
    }

    private static LaunchEntry Entry(string id, string directory) =>
        new()
        {
            Id = id,
            Name = id,
            Directory = directory,
            Command = "pwsh",
        };

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
