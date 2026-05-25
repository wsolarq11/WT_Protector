using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherSettingsResultWorkflowTests
{
    [Fact]
    public void ApplyRunsSuccessCallbackWhenSaved()
    {
        string? status = null;
        var successCalls = 0;

        var succeeded = LauncherSettingsResultWorkflow.Apply(
            LauncherSettingsMutationResult.Saved(),
            value => status = value,
            () => successCalls++);

        Assert.True(succeeded);
        Assert.Null(status);
        Assert.Equal(1, successCalls);
    }

    [Fact]
    public void ApplyUpdatesFailureStatusAndSkipsSuccessCallback()
    {
        string? status = null;
        var successCalls = 0;

        var succeeded = LauncherSettingsResultWorkflow.Apply(
            LauncherSettingsMutationResult.Failed("设置保存失败"),
            value => status = value,
            () => successCalls++);

        Assert.False(succeeded);
        Assert.Equal("设置保存失败", status);
        Assert.Equal(0, successCalls);
    }
}
