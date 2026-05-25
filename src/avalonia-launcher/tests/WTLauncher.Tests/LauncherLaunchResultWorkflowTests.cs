using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherLaunchResultWorkflowTests
{
    [Fact]
    public void ApplyUpdatesDryRunAndStatusWhenPresent()
    {
        string? dryRun = null;
        string? status = null;

        LauncherLaunchResultWorkflow.Apply(
            "pwsh --version",
            "已启动: one",
            value => status = value,
            value => dryRun = value);

        Assert.Equal("pwsh --version", dryRun);
        Assert.Equal("已启动: one", status);
    }

    [Fact]
    public void ApplySkipsDryRunUpdateWhenMissing()
    {
        string? dryRun = "keep";
        string? status = null;

        LauncherLaunchResultWorkflow.Apply(
            null,
            "已取消: one",
            value => status = value,
            value => dryRun = value);

        Assert.Equal("keep", dryRun);
        Assert.Equal("已取消: one", status);
    }
}
