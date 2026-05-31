using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task LaunchGroupCommandsCreateUpdateAndDeletePortablePresets()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("blank-manual", "Manual", locked: true));

        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestNewPresetAsync = _ => Task.FromResult<string?>("Project Pair"));

        Assert.True(viewModel.SaveSelectionAsPresetCommand.CanExecute(null));

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.Equal("project-pair", viewModel.SelectedPreset?.Id);
        var presetFile = LauncherPresetStore.Load(presets);
        var created = presetFile.Presets.Single(preset => preset.Id == "project-pair");
        Assert.Equal("Project Pair", created.Name);
        Assert.Equal(["one"], created.EntryIds);
        Assert.False(created.Locked);
        Assert.False(viewModel.ApplyPresetCommand.CanExecute(null));

        viewModel.Entries.Single(entry => entry.Entry.Id == "two").IsSelected = true;

        Assert.True(viewModel.UpdateSelectedPresetCommand.CanExecute(null));

        viewModel.UpdateSelectedPresetCommand.Execute(null);

        presetFile = LauncherPresetStore.Load(presets);
        created = presetFile.Presets.Single(preset => preset.Id == "project-pair");
        Assert.Equal(["one", "two"], created.EntryIds);
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));

        viewModel.ConfirmDeletePresetAsync = preset => Task.FromResult(preset.Id == "project-pair");

        Assert.True(viewModel.DeleteSelectedPresetCommand.CanExecute(null));

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        presetFile = LauncherPresetStore.Load(presets);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "project-pair");
        Assert.Contains(presetFile.Presets, preset => preset.Id == "daily");

        viewModel.SelectedPreset = viewModel.Presets.Single(preset => preset.Id == "blank-manual");

        Assert.True(viewModel.SelectedPreset.IsLocked);
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));
        Assert.False(viewModel.DeleteSelectedPresetCommand.CanExecute(null));
        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
    }

    [Fact]
    public void ChangingSelectedPresetNotifiesPresetManagementCommands()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("blank-manual", "Manual", locked: true));

        var viewModel = CreateViewModel(config, settings);
        var applyNotifications = 0;
        var updateNotifications = 0;
        var deleteNotifications = 0;

        viewModel.ApplyPresetCommand.CanExecuteChanged += (_, _) => applyNotifications++;
        viewModel.UpdateSelectedPresetCommand.CanExecuteChanged += (_, _) => updateNotifications++;
        viewModel.DeleteSelectedPresetCommand.CanExecuteChanged += (_, _) => deleteNotifications++;

        viewModel.SelectedPreset = viewModel.Presets.Single(preset => preset.Id == "blank-manual");

        Assert.True(applyNotifications > 0);
        Assert.True(updateNotifications > 0);
        Assert.True(deleteNotifications > 0);
    }

    [Fact]
    public void LaunchGroupUpdateCommandRequiresSelectedEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));

        var viewModel = CreateViewModel(config, settings);

        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
        Assert.False(viewModel.UpdateSelectedPresetCommand.CanExecute(null));
    }

    [Fact]
    public async Task LaunchGroupDeleteCancellationLeavesPresetFileUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));
        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.ConfirmDeletePresetAsync = _ => Task.FromResult(false));

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["daily", "secondary"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Equal("已取消: 删除启动组 Daily", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupDeleteDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(
            presets,
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.ConfirmDeletePresetAsync = _ => Task.FromResult(true));

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        Assert.Equal(ReorderedDragOrder(), launcherSettings.CustomEntryOrder);

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
        var presetFile = LauncherPresetStore.Load(presets);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "daily");
        Assert.Equal("secondary", viewModel.SelectedPreset?.Id);
    }
}
