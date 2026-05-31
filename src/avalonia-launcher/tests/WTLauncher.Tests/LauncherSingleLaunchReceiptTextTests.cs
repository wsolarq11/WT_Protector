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
    public void RequestedReceiptNormalizesEmptyName()
    {
        using var temp = new TempDirectory();
        var plan = LaunchArgumentBuilder.Build(
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = temp.Path,
                Command = "pwsh",
            },
            validateDirectory: true,
            requireConfirmation: false);

        var receipt = LauncherSingleLaunchReceiptText.Requested(" ", plan);

        Assert.Contains("已请求启动: (unnamed)", receipt);
    }
}
