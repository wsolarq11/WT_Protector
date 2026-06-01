using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSingleLaunchReceiptTextTests
{
    [Fact]
    public void RequestedReceiptIncludesNameAndCommandLine()
    {
        using var temp = new TempDirectory();
        var plan = LaunchArgumentBuilder.Build(
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = temp.Path,
                Command = "codex --version",
            },
            validateDirectory: true,
            requireConfirmation: false);

        var receipt = LauncherSingleLaunchReceiptText.Requested(" one ", plan);

        Assert.Contains("启动回执", receipt);
        Assert.Contains("已请求启动: one", receipt);
        Assert.Contains(plan.CommandLine, receipt);
    }

    [Fact]
    public void RequestedReceiptRejectsEmptyName()
    {
        using var temp = new TempDirectory();
        var plan = Plan(temp.Path, "pwsh");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherSingleLaunchReceiptText.Requested(" ", plan));

        Assert.Equal("entryName", exception.ParamName);
    }

    [Fact]
    public void FailedReceiptWithoutPlanIncludesFailureOnly()
    {
        var receipt = LauncherSingleLaunchReceiptText.Failed(
            " one ",
            new InvalidOperationException("directory does not exist"));

        Assert.Contains("启动回执", receipt);
        Assert.Contains("启动失败: one", receipt);
        Assert.Contains("错误: directory does not exist", receipt);
        Assert.DoesNotContain(LaunchArgumentBuilder.TerminalExecutable, receipt);
    }

    [Fact]
    public void FailedReceiptWithoutEntryNameDoesNotInventUnnamedEntry()
    {
        var receipt = LauncherSingleLaunchReceiptText.Failed(new InvalidOperationException("bad entry"));

        Assert.Contains("启动回执", receipt);
        Assert.Contains("启动失败: bad entry", receipt);
        Assert.DoesNotContain("(unnamed)", receipt);
        Assert.DoesNotContain(LaunchArgumentBuilder.TerminalExecutable, receipt);
    }

    [Fact]
    public void CanceledReceiptIncludesNameAndCommandLine()
    {
        using var temp = new TempDirectory();
        var plan = Plan(temp.Path, "pwsh");

        var receipt = LauncherSingleLaunchReceiptText.Canceled("one", plan);

        Assert.Contains("启动回执", receipt);
        Assert.Contains("已取消: one", receipt);
        Assert.Contains(plan.CommandLine, receipt);
    }

    [Fact]
    public void FailedReceiptWithPlanIncludesCommandLineAndFailure()
    {
        using var temp = new TempDirectory();
        var plan = Plan(temp.Path, "pwsh");

        var receipt = LauncherSingleLaunchReceiptText.Failed(
            "one",
            plan,
            new InvalidOperationException("blocked"));

        Assert.Contains("启动失败: one", receipt);
        Assert.Contains(plan.CommandLine, receipt);
        Assert.Contains("错误: blocked", receipt);
    }

    [Fact]
    public void ConfirmationFailedReceiptIncludesCommandLineAndFailure()
    {
        using var temp = new TempDirectory();
        var plan = Plan(temp.Path, "pwsh");

        var receipt = LauncherSingleLaunchReceiptText.ConfirmationFailed(
            "one",
            plan,
            new InvalidOperationException("dialog unavailable"));

        Assert.Contains("启动回执", receipt);
        Assert.Contains("启动确认失败: one", receipt);
        Assert.Contains(plan.CommandLine, receipt);
        Assert.Contains("错误: dialog unavailable", receipt);
    }

    private static LaunchPlan Plan(string directory, string command) =>
        LaunchArgumentBuilder.Build(
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = directory,
                Command = command,
            },
            validateDirectory: true,
            requireConfirmation: false);
}
