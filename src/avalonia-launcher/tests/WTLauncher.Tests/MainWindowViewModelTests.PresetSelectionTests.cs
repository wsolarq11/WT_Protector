using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void ApplyPresetCommandReplacesAndPersistsPortableSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: true);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));

        var viewModel = CreateViewModel(config, settings);

        Assert.Equal("daily", Assert.Single(viewModel.Presets).Id);
        Assert.Equal(1, viewModel.PresetCount);
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
        Assert.Equal("应用启动组", viewModel.ApplyPresetButtonText);
        Assert.Contains("缺 1", viewModel.PresetSelectionStatusText);
        Assert.Contains("额外 1", viewModel.PresetSelectionStatusText);

        viewModel.ApplyPresetCommand.Execute(null);

        Assert.Equal(1, viewModel.SelectedCount);
        Assert.True(viewModel.ApplyPresetCommand.CanExecute(null));
        Assert.Equal("已匹配", viewModel.ApplyPresetButtonText);
        Assert.Contains("已匹配启动组", viewModel.PresetSelectionStatusText);
        Assert.Contains("1/1", viewModel.PresetSelectionStatusText);
        var entries = LoadEntriesById(config);
        Assert.True(entries.Single(entry => entry.Id == "one").Selected);
        Assert.False(entries.Single(entry => entry.Id == "two").Selected);
    }

    [Fact]
    public void ApplyPresetRollsBackFileAndStatusWhenConfigBackupFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: true);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));

        var viewModel = CreateViewModel(config, settings);
        // Force File.Move to fail by replacing the config file with a directory.
        File.Delete(config);
        Directory.CreateDirectory(config);

        viewModel.ApplyPresetCommand.Execute(null);

        Assert.Equal(1, viewModel.SelectedCount);
        Assert.False(viewModel.Entries.Single(entry => entry.Id == "one").IsSelected);
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "two").IsSelected);
        Assert.Contains("存储保存失败", viewModel.StatusText);
        Assert.DoesNotContain("已应用", viewModel.StatusText);
    }

    [Fact]
    public void ApplyPresetDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.ApplyPresetCommand.Execute(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
        Assert.Equal(["one"], LoadEntriesById(config).Where(entry => entry.Selected).Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void PresetLoadingPrefersConfiguredDefaultPreset()
    {
        using var defaultTemp = new TempDirectory();
        var (defaultConfig, defaultSettings, defaultPresets) = CreatePortableFixture(
            defaultTemp,
            oneSelected: false,
            twoSelected: false);
        SavePresetFile(
            defaultPresets,
            "team",
            Preset("daily", "Daily", "one"),
            Preset("team", "Team", "two"));

        var defaultViewModel = CreateViewModel(defaultConfig, defaultSettings);

        Assert.Equal(["daily", "team"], defaultViewModel.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("team", defaultViewModel.SelectedPreset?.Id);
    }

    [Fact]
    public void BrokenPresetWarnsAndDoesNotRewriteSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: true);
        SavePresetFile(presets, "broken", Preset("broken", "Broken", "one", "missing-entry"));

        var viewModel = CreateViewModel(config, settings);

        Assert.Equal("检查启动组", viewModel.ApplyPresetButtonText);
        Assert.Contains("引用了 1 个不存在的启动项", viewModel.PresetSelectionStatusText);

        viewModel.ApplyPresetCommand.Execute(null);

        Assert.Contains("missing-entry", viewModel.StatusText);
        Assert.False(File.Exists(config + ".bak"));
        var entries = LoadEntriesById(config);
        Assert.False(entries.Single(entry => entry.Id == "one").Selected);
        Assert.True(entries.Single(entry => entry.Id == "two").Selected);
    }
}
