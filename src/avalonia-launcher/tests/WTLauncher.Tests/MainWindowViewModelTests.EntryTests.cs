using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task AddEntryRollsBackInMemoryStateWhenConfigSaveFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var launcherSettings = new LauncherSettings
        {
            SortMode = LauncherEntryVisibility.CustomSortModeId,
            CustomEntryOrder = ["one", "two"],
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            vm => vm.RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(
                NewThreeEntryDraft()));

        File.Delete(config);
        Directory.CreateDirectory(config);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Equal(["one", "two"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["one", "two"], launcherSettings.CustomEntryOrder);
        Assert.Equal(2, viewModel.TotalCount);
        Assert.Equal(0, viewModel.SelectedCount);
        Assert.Contains("配置保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task AddEntryRollsBackConfigFileWhenSettingsSaveFailsAfterConfigSave()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(
                NewThreeEntryDraft()));

        File.Delete(settings);
        Directory.CreateDirectory(settings);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Equal(["one", "two"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Contains("设置保存失败", viewModel.StatusText);
        var savedEntries = LoadEntriesById(config);
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "three");
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task EditEntryRollsBackConfigFileWhenPresetSaveFailsAfterConfigSave()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(
                RenamedOneEntryDraft()));
        File.Delete(presets);
        Directory.CreateDirectory(presets);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.Equal(["one", "two"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Contains("启动组保存失败", viewModel.StatusText);
        var savedEntries = LoadEntriesById(config);
        Assert.Contains(savedEntries, entry => entry.Id == "one");
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one-renamed");
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task AddEntryCancellationLeavesFilesAndOrderUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            vm => vm.RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(null));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherConfigStore.Load(config).Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal(LauncherEntryStatus.AddCanceled, viewModel.StatusText);
    }

    [Fact]
    public async Task EntryDialogFailuresLeaveFilesAndReferencesUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            vm =>
            {
                vm.RequestEntryDraftAsync = _ => throw new InvalidOperationException("dialog unavailable");
                vm.ConfirmDeleteEntryAsync = _ => throw new InvalidOperationException("dialog unavailable");
            });

        StartDefaultDragPreview(viewModel, launcherSettings);
        await viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Equal("新增按钮对话框失败: dialog unavailable", viewModel.StatusText);
        AssertNoEntryMutation(config, presets, launcherSettings, viewModel);

        StartDefaultDragPreview(viewModel, launcherSettings);
        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.Equal("编辑 One 对话框失败: dialog unavailable", viewModel.StatusText);
        AssertNoEntryMutation(config, presets, launcherSettings, viewModel);

        StartDefaultDragPreview(viewModel, launcherSettings);
        await viewModel.Entries.Single(entry => entry.Id == "one").DeleteCommand.ExecuteAsync(null);

        Assert.Equal("删除确认失败: One — dialog unavailable", viewModel.StatusText);
        AssertNoEntryMutation(config, presets, launcherSettings, viewModel);
    }

    [Fact]
    public async Task AddEntryDuringDragPreviewDoesNotCommitPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            vm => vm.RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(
                NewFourEntryDraft()));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        var expectedOrder = DefaultDragOrder().Concat(["four"]).ToArray();
        Assert.Equal(expectedOrder, launcherSettings.CustomEntryOrder);
        Assert.Equal(expectedOrder, LauncherSettingsStore.Load(settings).CustomEntryOrder);
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public async Task EditEntryCancellationLeavesFilesAndReferencesUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            vm => vm.RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(null));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherConfigStore.Load(config).Select(entry => entry.Id).ToArray());
        Assert.Equal(["one"], LauncherPresetStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal(LauncherEntryStatus.EditCanceled("One"), viewModel.StatusText);
    }

    [Fact]
    public async Task EditEntryDialogReceivesSnapshotEntry()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        LaunchEntry? dialogEntry = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.RequestEntryDraftAsync = entry =>
            {
                dialogEntry = entry;
                entry!.Id = "broken";
                entry.Name = "";
                return Task.FromResult<EntryDraft?>(null);
            });

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.NotNull(dialogEntry);
        Assert.Equal("broken", dialogEntry!.Id);
        Assert.Equal("", dialogEntry.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Id);
        Assert.Equal("One", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherConfigStore.Load(config).Select(entry => entry.Id).ToArray());
        Assert.Equal(["one"], LauncherPresetStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.EditCanceled("One"), viewModel.StatusText);
    }

    [Fact]
    public async Task EditEntryUsesSnapshotEntryWhenDialogReturnsDraft()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            configure: vm => vm.RequestEntryDraftAsync = entry =>
            {
                entry!.Id = "broken";
                entry.Name = "";
                return Task.FromResult<EntryDraft?>(RenamedOneEntryDraft());
            });

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.Equal(["one-renamed", "two", "three"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["one-renamed"], LauncherPresetStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(["one-renamed", "two", "three"], launcherSettings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.Updated("One Renamed"), viewModel.StatusText);
    }

    [Fact]
    public async Task EntryCommandsPersistPortableConfigAndReferences()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));

        var requestCount = 0;
        var timestamps = new Queue<DateTimeOffset>(
        [
            DateTimeOffset.Parse("2026-05-30T01:02:03Z"),
            DateTimeOffset.Parse("2026-05-30T04:05:06Z"),
        ]);
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings
            {
                SortMode = LauncherEntryVisibility.CustomSortModeId,
                CustomEntryOrder = ["one", "two"],
            },
            vm =>
            {
                vm.RequestEntryDraftAsync = _ => Task.FromResult<EntryDraft?>(
                    requestCount++ == 0
                        ? NewThreeEntryDraft()
                        : RenamedOneEntryDraft());
                vm.ConfirmDeleteEntryAsync = _ => Task.FromResult(true);
            },
            utcNow: timestamps.Dequeue);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        var savedEntries = LoadEntriesById(config);
        var added = savedEntries.Single(entry => entry.Id == "three");
        Assert.True(added.Selected);
        Assert.Equal("2026-05-30T01:02:03.0000000+00:00", added.UpdatedAtUtc);
        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(["one", "two", "three"], savedSettings.CustomEntryOrder);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        savedEntries = LoadEntriesById(config);
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one");
        var edited = savedEntries.Single(entry => entry.Id == "one-renamed");
        Assert.Equal("One Renamed", edited.Name);
        Assert.Equal("D:\\renamed", edited.Directory);
        Assert.Equal("opencode", edited.Command);
        Assert.False(edited.Selected);
        Assert.Equal("2026-05-30T04:05:06.0000000+00:00", edited.UpdatedAtUtc);

        var savedPresetFile = LauncherPresetStore.Load(presets);
        Assert.Equal(["one-renamed"], savedPresetFile.Presets.Single(preset => preset.Id == "daily").EntryIds);
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(["one-renamed", "two", "three"], savedSettings.CustomEntryOrder);

        await viewModel.Entries.Single(entry => entry.Id == "one-renamed").DeleteCommand.ExecuteAsync(null);

        savedEntries = LoadEntriesById(config);
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one-renamed");
        savedPresetFile = LauncherPresetStore.Load(presets);
        Assert.Empty(savedPresetFile.Presets.Single(preset => preset.Id == "daily").EntryIds);
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(["two", "three"], savedSettings.CustomEntryOrder);
    }

    [Fact]
    public async Task DeleteEntryCancellationLeavesFilesAndReferencesUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            vm => vm.ConfirmDeleteEntryAsync = _ => Task.FromResult(false));

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.Entries.Single(entry => entry.Id == "one").DeleteCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherConfigStore.Load(config).Select(entry => entry.Id).ToArray());
        Assert.Equal(["one"], LauncherPresetStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal(LauncherEntryStatus.DeleteCanceled("One"), viewModel.StatusText);
    }

    [Fact]
    public async Task DeleteEntryConfirmationCancellationIgnoresMutatedEntrySnapshot()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        LaunchEntry? externalSnapshot = null;
        LauncherEntryDeleteTarget? target = null;
        viewModel.ConfirmDeleteEntryAsync = candidate =>
        {
            target = candidate;
            externalSnapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
            externalSnapshot.Id = "broken";
            externalSnapshot.Name = "";
            return Task.FromResult(false);
        };

        await viewModel.Entries.Single(entry => entry.Id == "one").DeleteCommand.ExecuteAsync(null);

        Assert.NotNull(target);
        Assert.Equal("one", target!.EntryId);
        Assert.Equal("One", target.EntryName);
        Assert.Equal("broken", externalSnapshot!.Id);
        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal("One", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Equal(DefaultDragOrder(), LauncherConfigStore.Load(config).Select(entry => entry.Id).ToArray());
        Assert.Equal(["one"], LauncherPresetStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.DeleteCanceled("One"), viewModel.StatusText);
    }

    [Fact]
    public async Task DeleteEntryUsesConfirmationSnapshotForReferencesAndStatus()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        LaunchEntry? externalSnapshot = null;
        LauncherEntryDeleteTarget? target = null;
        viewModel.ConfirmDeleteEntryAsync = candidate =>
        {
            target = candidate;
            externalSnapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
            externalSnapshot.Id = "broken";
            externalSnapshot.Name = "";
            return Task.FromResult(true);
        };

        await viewModel.Entries.Single(entry => entry.Id == "one").DeleteCommand.ExecuteAsync(null);

        Assert.NotNull(target);
        Assert.Equal("one", target!.EntryId);
        Assert.Equal("One", target.EntryName);
        Assert.Equal("broken", externalSnapshot!.Id);
        Assert.Equal(["two", "three"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["two", "three"], LauncherConfigStore.Load(config).Select(entry => entry.Id).ToArray());
        Assert.Empty(LauncherPresetStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(["two", "three"], launcherSettings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.Deleted("One"), viewModel.StatusText);
    }

    private static void AssertNoEntryMutation(
        string config,
        string presets,
        LauncherSettings launcherSettings,
        MainWindowViewModel viewModel)
    {
        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherConfigStore.Load(config).Select(entry => entry.Id).ToArray());
        Assert.Equal(["one"], LauncherPresetStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void GenerateUniqueCloneIdReturnsSourceCopyWhenNoConflict()
    {
        var result = MainWindowViewModel.GenerateUniqueCloneId(
            "myentry",
            ["one", "two", "three"]);

        Assert.Equal("myentry-copy", result);
    }

    [Fact]
    public void GenerateUniqueCloneIdAppendsSuffixWhenCopyExists()
    {
        var result = MainWindowViewModel.GenerateUniqueCloneId(
            "myentry",
            ["one", "myentry-copy", "two"]);

        Assert.Equal("myentry-copy-2", result);
    }

    [Fact]
    public void GenerateUniqueCloneIdSkipsIntermediateCollisions()
    {
        var result = MainWindowViewModel.GenerateUniqueCloneId(
            "myentry",
            ["one", "myentry-copy", "myentry-copy-2", "myentry-copy-3"]);

        Assert.Equal("myentry-copy-4", result);
    }

    [Fact]
    public void GenerateUniqueCloneIdOnlyDetectsExactOrdinalCollision()
    {
        var result = MainWindowViewModel.GenerateUniqueCloneId(
            "myentry",
            [" myentry-copy "]);

        Assert.Equal("myentry-copy", result);
    }

    private static EntryDraft NewThreeEntryDraft() =>
        new("three", "Three", "D:\\three", "pwsh", Selected: true);

    private static EntryDraft NewFourEntryDraft() =>
        new("four", "Four", "D:\\four", "pwsh", Selected: false);

    private static EntryDraft RenamedOneEntryDraft() =>
        new("one-renamed", "One Renamed", "D:\\renamed", "opencode", Selected: false);
}
