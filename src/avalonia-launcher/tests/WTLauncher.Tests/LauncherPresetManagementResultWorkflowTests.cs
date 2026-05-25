using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed class LauncherPresetManagementResultWorkflowTests
{
    [Fact]
    public void ApplyReloadsAndUpdatesStatus()
    {
        var reloadCalls = 0;
        string? nextPresetId = null;
        string? status = null;

        LauncherPresetManagementResultWorkflow.Apply(
            LauncherPresetManagementResult.Reload("team", "saved"),
            presetId =>
            {
                reloadCalls++;
                nextPresetId = presetId;
            },
            value => status = value);

        Assert.Equal(1, reloadCalls);
        Assert.Equal("team", nextPresetId);
        Assert.Equal("saved", status);
    }

    [Fact]
    public void ApplySkipsReloadWhenNotRequested()
    {
        var reloadCalls = 0;
        string? status = null;

        LauncherPresetManagementResultWorkflow.Apply(
            LauncherPresetManagementResult.Status("missing"),
            _ => reloadCalls++,
            value => status = value);

        Assert.Equal(0, reloadCalls);
        Assert.Equal("missing", status);
    }
}
