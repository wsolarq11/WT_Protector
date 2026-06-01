using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void MissingPresetFileFailsBeforeViewModelStateIsBuilt()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        File.Delete(presets);

        var exception = Assert.Throws<FileNotFoundException>(() => CreateViewModel(config, settings));

        Assert.Equal("Launcher presets not found.", exception.Message);
        Assert.Equal(presets, exception.FileName);
    }

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

        Directory.CreateDirectory(presets + ".bak");

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.PresetCount);
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Single(viewModel.Presets, preset => preset.Id == "daily");
        Assert.DoesNotContain(viewModel.Presets, preset => preset.Id == "project-pair");
        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["daily"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Contains("启动组保存失败", viewModel.StatusText);
        Assert.DoesNotContain("已保存启动组", viewModel.StatusText);
        AssertNoStoreTempFiles(presets);
    }

    [Fact]
    public async Task LaunchGroupSaveRollbackRejectsMissingSelectedPresetInsteadOfSelectingFirst()
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
            configure: vm => vm.RequestNewPresetAsync = _ => Task.FromResult<string?>("Project Pair"));
        viewModel.SelectedPreset = new LauncherPresetViewModel(Preset("ghost", "Ghost", "one"));
        Directory.CreateDirectory(presets + ".bak");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null));

        Assert.Equal("Launcher selected preset id does not match any preset: ghost.", exception.Message);
        Assert.DoesNotContain(viewModel.Presets, preset => preset.Id == "project-pair");
    }

    [Fact]
    public void LaunchGroupUpdateRollsBackInMemoryStateWhenPersistFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var viewModel = CreateViewModel(config, settings);

        viewModel.Entries.Single(entry => entry.Id == "two").IsSelected = true;
        Directory.CreateDirectory(presets + ".bak");

        viewModel.UpdateSelectedPresetCommand.Execute(null);

        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        var preset = Assert.Single(viewModel.Presets, preset => preset.Id == "daily");
        Assert.Equal(["one"], preset.EntryIds);
        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["one"], presetFile.Presets.Single(preset => preset.Id == "daily").EntryIds);
        Assert.Contains("启动组保存失败", viewModel.StatusText);
        Assert.DoesNotContain("已更新启动组", viewModel.StatusText);
        AssertNoStoreTempFiles(presets);
    }

    [Fact]
    public async Task LaunchGroupDeleteRollsBackInMemoryStateWhenPersistFails()
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
            configure: vm => vm.ConfirmDeletePresetAsync = _ => Task.FromResult(true));

        Directory.CreateDirectory(presets + ".bak");

        await viewModel.DeleteSelectedPresetCommand.ExecuteAsync(null);

        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Equal(["daily", "secondary"], viewModel.Presets.Select(preset => preset.Id).ToArray());
        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal("daily", presetFile.DefaultPresetId);
        Assert.Equal(["daily", "secondary"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Contains("启动组保存失败", viewModel.StatusText);
        Assert.DoesNotContain("已删除启动组", viewModel.StatusText);
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
        LauncherPresetSaveTarget? target = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.RequestNewPresetAsync = candidate =>
            {
                target = candidate;
                return Task.FromResult<string?>(null);
            });

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.NotNull(target);
        Assert.Equal(["one"], target!.EntryIds);
        Assert.Equal(1, target.EntryCount);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal("已取消: 保存启动组", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupSaveUsesReadOnlyDialogTargetSnapshot()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: true),
            Entry("two", "Two", command: "opencode"));
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestNewPresetAsync = target =>
            {
                var mutable = Assert.IsAssignableFrom<IList<string>>(target.EntryIds);
                Assert.Throws<NotSupportedException>(() => mutable.Clear());
                return Task.FromResult<string?>("Snapshot");
            });

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        var savedPreset = LauncherPresetStore.Load(presets).Presets.Single(preset => preset.Id == "snapshot");
        Assert.Equal(["one"], savedPreset.EntryIds);
        Assert.Equal("已保存启动组: Snapshot（1 项）", viewModel.StatusText);
    }

    [Fact]
    public async Task LaunchGroupSaveDialogFailureLeavesPresetFileUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestNewPresetAsync = _ =>
                throw new InvalidOperationException("dialog unavailable"));

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        var presetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["daily"], presetFile.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("daily", viewModel.SelectedPreset?.Id);
        Assert.Equal("保存启动组对话框失败: dialog unavailable", viewModel.StatusText);
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
