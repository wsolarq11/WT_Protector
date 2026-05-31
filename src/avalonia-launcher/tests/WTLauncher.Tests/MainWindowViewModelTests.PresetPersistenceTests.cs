using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task LaunchGroupSaveRollsBackInMemoryStateWhenPersistFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));

        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestNewPresetAsync = _ => Task.FromResult<string?>("Project Pair"));

        File.Delete(presets);
        Directory.CreateDirectory(presets);

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.PresetCount);
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Single(viewModel.Presets, preset => preset.Id == "daily");
        Assert.DoesNotContain(viewModel.Presets, preset => preset.Id == "project-pair");
        AssertNoStoreTempFiles(presets);
    }

    [Fact]
    public async Task LaunchGroupSavePassesSelectedEntryIdsToPresetDialog()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: true),
            Entry("two", "Two", command: "opencode"),
            Entry("three", "Three"));
        var launcherSettings = CreateDragSettings();
        IReadOnlyList<string>? dialogEntryIds = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.RequestNewPresetAsync = entryIds =>
            {
                dialogEntryIds = entryIds.ToArray();
                return Task.FromResult<string?>(null);
            });

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.Equal(["one"], dialogEntryIds);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("已取消: 保存启动组", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupSaveRejectsBlankPresetNameWithoutMutatingFile()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));

        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestNewPresetAsync = _ => Task.FromResult<string?>("   "));

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["daily"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("启动组名称不能为空", viewModel.StatusText);
    }
}
