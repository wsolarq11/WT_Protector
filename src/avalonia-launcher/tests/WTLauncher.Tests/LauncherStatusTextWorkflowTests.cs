using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherStatusTextWorkflowTests
{
    [Fact]
    public void ApplyUpdatesStatusWhenPresent()
    {
        string? status = null;

        var updated = LauncherStatusTextWorkflow.Apply("已保存", value => status = value);

        Assert.True(updated);
        Assert.Equal("已保存", status);
    }

    [Fact]
    public void ApplySkipsStatusWhenMissing()
    {
        string? status = "keep";

        var updated = LauncherStatusTextWorkflow.Apply(null, value => status = value);

        Assert.False(updated);
        Assert.Equal("keep", status);
    }
}
