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
        Assert.False(viewModel.ApplyPresetCommand.CanExecute(null));
        Assert.Equal("已匹配", viewModel.ApplyPresetButtonText);
        Assert.Contains("已匹配启动组", viewModel.PresetSelectionStatusText);
        Assert.Contains("1/1", viewModel.PresetSelectionStatusText);
        var entries = LoadEntriesById(config);
        Assert.True(entries.Single(entry => entry.Id == "one").Selected);
        Assert.False(entries.Single(entry => entry.Id == "two").Selected);
    }

    [Fact]
    public void PresetLoadingPrefersDefaultAndFallsBackToFirstPreset()
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

        using var fallbackTemp = new TempDirectory();
        var (fallbackConfig, fallbackSettings, fallbackPresets) = CreatePortableFixture(
            fallbackTemp,
            oneSelected: false,
            twoSelected: false);
        SavePresetFile(
            fallbackPresets,
            "missing",
            Preset("daily", "Daily", "one"),
            Preset("team", "Team", "two"));

        var fallbackViewModel = CreateViewModel(fallbackConfig, fallbackSettings);

        Assert.Equal("daily", fallbackViewModel.SelectedPreset?.Id);
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
