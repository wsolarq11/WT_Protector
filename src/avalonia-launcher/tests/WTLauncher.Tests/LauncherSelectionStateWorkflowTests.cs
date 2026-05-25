using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherSelectionStateWorkflowTests
{
    [Fact]
    public void RefreshAppliesSelectionStateAndNotifiesCommands()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);

        LauncherPresetSelectionState? applied = null;
        var notifyCount = 0;

        var result = LauncherSelectionStateWorkflow.Refresh(
            selectedPreset: null,
            presetCount: 0,
            owner.Entries,
            state => applied = state,
            () => notifyCount++);

        Assert.Equal(1, notifyCount);
        var appliedState = applied ?? throw new InvalidOperationException("Selection state was not applied.");
        Assert.Same(appliedState, result);
        Assert.Equal(1, appliedState.SelectedCount);
        Assert.Equal("应用启动组", appliedState.ApplyPresetButtonText);
    }

    private static MainWindowViewModel CreateOwner(string rootPath)
    {
        var configPath = Path.Combine(rootPath, "config.json");
        LauncherConfigStore.Save(
            [
                Entry("one", selected: true),
                Entry("two", selected: false),
            ],
            configPath);

        return new MainWindowViewModel(
            configPath,
            Path.Combine(rootPath, "settings.json"),
            new LauncherSettings(),
            new LauncherProcess());
    }

    private static LaunchEntry Entry(string id, bool selected)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
            Selected = selected,
        };
    }
}
