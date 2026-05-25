using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherPersistenceResultWorkflowTests
{
    [Fact]
    public void ApplyReturnsTrueAndLeavesStatusUnchangedWhenSaved()
    {
        string? status = null;

        var succeeded = LauncherPersistenceResultWorkflow.Apply(
            LauncherPersistenceStatusResult.Saved(),
            value => status = value);

        Assert.True(succeeded);
        Assert.Null(status);
    }

    [Fact]
    public void ApplyReturnsFalseAndUpdatesFailureStatus()
    {
        string? status = null;

        var succeeded = LauncherPersistenceResultWorkflow.Apply(
            LauncherPersistenceStatusResult.Failed("配置保存失败"),
            value => status = value);

        Assert.False(succeeded);
        Assert.Equal("配置保存失败", status);
    }
}
