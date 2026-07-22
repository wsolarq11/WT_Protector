using WTLauncher.App.Features.Entries;
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
            SortMode = LauncherEntryQuery.CustomSortModeId,
            CustomEntryOrder = ["one", "two"],
        };
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(
            NewThreeEntryDraft());
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

        File.Delete(config);
        Directory.CreateDirectory(config);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Equal(["one", "two"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["one", "two"], viewModel.Settings.CustomEntryOrder);
        Assert.Equal(2, viewModel.TotalCount);
        Assert.Equal(0, viewModel.SelectedCount);
        Assert.Contains("存储保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task AddEntryInMemoryStateRestoredWhenSettingsCommitFailsAfterConfigCommit()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(
            NewThreeEntryDraft());
        var viewModel = CreateViewModel(
            config,
            settings,
            dialogService: dialogService);

        File.Delete(settings);
        Directory.CreateDirectory(settings);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Equal(["one", "two"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Contains("存储保存失败", viewModel.StatusText);
        // ponytail: unified store saves atomically — when save fails, nothing is persisted.
        var savedEntries = LoadEntriesById(config);
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "three");
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task EditEntryInMemoryStateRestoredWhenPresetCommitFailsAfterConfigCommit()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(
            RenamedOneEntryDraft());
        var viewModel = CreateViewModel(
            config,
            settings,
            dialogService: dialogService);
        File.Delete(presets);
        Directory.CreateDirectory(presets);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.Equal(["one", "two"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Contains("存储保存失败", viewModel.StatusText);
        // ponytail: unified store saves atomically — when save fails, nothing is persisted.
        // Store file was deleted for the failure test, so in-memory state is the truth.
        Assert.All(viewModel.Entries, entry => Assert.NotEqual("one-renamed", entry.Id));
        Assert.Contains(viewModel.Entries, entry => entry.Id == "one");
        AssertNoStoreTempFiles(config);
    }

    [Fact]
    public async Task AddEntryCancellationLeavesFilesAndOrderUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);
        var launcherSettings = CreateDragSettings();
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(null);
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(config).Entries.Select(entry => entry.Id.Value).ToArray());
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal(LauncherEntryStatus.AddCanceled.Text, viewModel.StatusText);
        Assert.Equal(LauncherStatusTone.Warning, viewModel.StatusTone);
    }

    [Fact]
    public async Task EntryDialogFailuresLeaveFilesAndReferencesUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => throw new InvalidOperationException("dialog unavailable");
        dialogService.OnConfirmDeleteEntry = _ => throw new InvalidOperationException("dialog unavailable");
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

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
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(
            NewFourEntryDraft());
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        var expectedOrder = DefaultDragOrder().Concat(["four"]).ToArray();
        Assert.Equal(expectedOrder, viewModel.Settings.CustomEntryOrder);
        Assert.Equal(expectedOrder, LauncherStore.Load(settings).Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public async Task EditEntryCancellationLeavesFilesAndReferencesUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(null);
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(config).Entries.Select(entry => entry.Id.Value).ToArray());
        Assert.Equal(["one"], LauncherStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal(LauncherEntryStatus.EditCanceled("One").Text, viewModel.StatusText);
    }

    [Fact]
    public async Task EditEntryDialogReceivesSnapshotEntry()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        LaunchEntry? dialogEntry = null;
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (entry, _, _) =>
        {
            dialogEntry = entry;
            entry = entry! with { Id = "broken", Name = EntryName.Empty };
            dialogEntry = entry;
            return Task.FromResult<EntryDraft?>(null);
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.NotNull(dialogEntry);
        Assert.Equal("broken", dialogEntry!.Id);
        Assert.Equal("", dialogEntry.Name);
        Assert.Equal("one", viewModel.Entries.Single(entry => entry.Id == "one").Id);
        Assert.Equal("One", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(config).Entries.Select(entry => entry.Id.Value).ToArray());
        Assert.Equal(["one"], LauncherStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.EditCanceled("One").Text, viewModel.StatusText);
    }

    [Fact]
    public async Task EditEntryUsesSnapshotEntryWhenDialogReturnsDraft()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (entry, _, _) =>
        {
            entry = entry! with { Id = "broken", Name = EntryName.Empty };
            return Task.FromResult<EntryDraft?>(RenamedOneEntryDraft());
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        Assert.Equal(["one-renamed", "two", "three"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["one-renamed"], LauncherStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(["one-renamed", "two", "three"], viewModel.Settings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.Updated("One Renamed").Text, viewModel.StatusText);
        Assert.Equal(LauncherStatusTone.Success, viewModel.StatusTone);
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
        var dialogService = new StubDialogService();
        dialogService.OnRequestEntryDraft = (_, _, _) => Task.FromResult<EntryDraft?>(
            requestCount++ == 0
                ? NewThreeEntryDraft()
                : RenamedOneEntryDraft());
        dialogService.OnConfirmDeleteEntry = _ => Task.FromResult(true);
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings
            {
                SortMode = LauncherEntryQuery.CustomSortModeId,
                CustomEntryOrder = ["one", "two"],
            },
            utcNow: timestamps.Dequeue,
            dialogService: dialogService);

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        var savedEntries = LoadEntriesById(config);
        var added = savedEntries.Single(entry => entry.Id == "three");
        Assert.True(added.Selected);
        Assert.Equal("2026-05-30T01:02:03.0000000+00:00", added.UpdatedAtUtc);
        var savedSettings = LauncherStore.Load(settings).Settings;
        Assert.Equal(["one", "two", "three"], savedSettings.CustomEntryOrder);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        savedEntries = LoadEntriesById(config);
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one");
        var edited = savedEntries.Single(entry => entry.Id == "one-renamed");
        Assert.Equal("One Renamed", edited.Name);
        Assert.Equal("D:\\renamed", edited.Directory.Value);
        Assert.Equal("opencode", edited.Command);
        Assert.False(edited.Selected);
        Assert.Equal("2026-05-30T04:05:06.0000000+00:00", edited.UpdatedAtUtc);

        var savedPresetFile = LauncherStore.Load(presets);
        Assert.Equal(["one-renamed"], savedPresetFile.Presets.Single(preset => preset.Id == "daily").EntryIds);
        savedSettings = LauncherStore.Load(settings).Settings;
        Assert.Equal(["one-renamed", "two", "three"], savedSettings.CustomEntryOrder);

        await viewModel.Entries.Single(entry => entry.Id == "one-renamed").DeleteCommand.ExecuteAsync(null);

        savedEntries = LoadEntriesById(config);
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one-renamed");
        savedPresetFile = LauncherStore.Load(presets);
        Assert.Empty(savedPresetFile.Presets.Single(preset => preset.Id == "daily").EntryIds);
        savedSettings = LauncherStore.Load(settings).Settings;
        Assert.Equal(["two", "three"], savedSettings.CustomEntryOrder);
    }

    [Fact]
    public async Task DeleteEntryCancellationLeavesFilesAndReferencesUnchanged()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        var dialogService = new StubDialogService();
        dialogService.OnConfirmDeleteEntry = _ => Task.FromResult(false);
        var viewModel = CreateViewModel(
            config,
            settings,
            launcherSettings,
            dialogService: dialogService);

        StartDefaultDragPreview(viewModel, launcherSettings);

        await viewModel.Entries.Single(entry => entry.Id == "one").DeleteCommand.ExecuteAsync(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(config).Entries.Select(entry => entry.Id.Value).ToArray());
        Assert.Equal(["one"], LauncherStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
        Assert.Equal(LauncherEntryStatus.DeleteCanceled("One").Text, viewModel.StatusText);
    }

    [Fact]
    public async Task DeleteEntryConfirmationCancellationIgnoresMutatedEntrySnapshot()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        LaunchEntry? externalSnapshot = null;
        LauncherEntryDeleteTarget? target = null;
        var dialogService = new StubDialogService();
        var viewModel = CreateViewModel(config, settings, launcherSettings, dialogService: dialogService);
        dialogService.OnConfirmDeleteEntry = candidate =>
        {
            target = candidate;
            externalSnapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
            externalSnapshot = externalSnapshot! with { Id = "broken", Name = EntryName.Empty };
            return Task.FromResult(false);
        };

        await viewModel.Entries.Single(entry => entry.Id == "one").DeleteCommand.ExecuteAsync(null);

        Assert.NotNull(target);
        Assert.Equal("one", target!.EntryId);
        Assert.Equal("One", target.EntryName);
        Assert.Equal("broken", externalSnapshot!.Id);
        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal("One", viewModel.Entries.Single(entry => entry.Id == "one").Name);
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(config).Entries.Select(entry => entry.Id.Value).ToArray());
        Assert.Equal(["one"], LauncherStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.DeleteCanceled("One").Text, viewModel.StatusText);
    }

    [Fact]
    public async Task DeleteEntryUsesConfirmationSnapshotForReferencesAndStatus()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreateDragFixture(temp);
        SavePresetFile(presets, "daily", Preset("daily", "Daily", "one"));
        var launcherSettings = CreateDragSettings();
        LaunchEntry? externalSnapshot = null;
        LauncherEntryDeleteTarget? target = null;
        var dialogService = new StubDialogService();
        var viewModel = CreateViewModel(config, settings, launcherSettings, dialogService: dialogService);
        dialogService.OnConfirmDeleteEntry = candidate =>
        {
            target = candidate;
            externalSnapshot = viewModel.Entries.Single(entry => entry.Id == "one").SnapshotEntry();
            externalSnapshot = externalSnapshot! with { Id = "broken", Name = EntryName.Empty };
            return Task.FromResult(true);
        };

        await viewModel.Entries.Single(entry => entry.Id == "one").DeleteCommand.ExecuteAsync(null);

        Assert.NotNull(target);
        Assert.Equal("one", target!.EntryId);
        Assert.Equal("One", target.EntryName);
        Assert.Equal("broken", externalSnapshot!.Id);
        Assert.Equal(["two", "three"], viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["two", "three"], LauncherStore.Load(config).Entries.Select(entry => entry.Id).ToArray());
        Assert.Empty(LauncherStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(["two", "three"], viewModel.Settings.CustomEntryOrder);
        Assert.Equal(LauncherEntryStatus.Deleted("One").Text, viewModel.StatusText);
    }

    private static void AssertNoEntryMutation(
        string config,
        string presets,
        LauncherSettings launcherSettings,
        MainWindowViewModel viewModel)
    {
        Assert.Equal(DefaultDragOrder(), viewModel.Entries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(config).Entries.Select(entry => entry.Id.Value).ToArray());
        Assert.Equal(["one"], LauncherStore.Load(presets).Presets.Single().EntryIds);
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void GenerateUniqueCloneIdReturnsSourceCopyWhenNoConflict()
    {
        var svc = new EntryService(new StubDialogService());
        var result = svc.GenerateUniqueCloneId(
            "myentry",
            ["one", "two", "three"]);

        Assert.Equal("myentry-copy", result);
    }

    [Fact]
    public void GenerateUniqueCloneIdAppendsSuffixWhenCopyExists()
    {
        var svc = new EntryService(new StubDialogService());
        var result = svc.GenerateUniqueCloneId(
            "myentry",
            ["one", "myentry-copy", "two"]);

        Assert.Equal("myentry-copy-2", result);
    }

    [Fact]
    public void GenerateUniqueCloneIdSkipsIntermediateCollisions()
    {
        var svc = new EntryService(new StubDialogService());
        var result = svc.GenerateUniqueCloneId(
            "myentry",
            ["one", "myentry-copy", "myentry-copy-2", "myentry-copy-3"]);

        Assert.Equal("myentry-copy-4", result);
    }

    [Fact]
    public void GenerateUniqueCloneIdOnlyDetectsExactOrdinalCollision()
    {
        var svc = new EntryService(new StubDialogService());
        var result = svc.GenerateUniqueCloneId(
            "myentry",
            [" myentry-copy "]);

        Assert.Equal("myentry-copy", result);
    }

    [Fact]
    public void ReplaceEntries_ResetsKeyboardFocusedEntryId()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(config, settings);
        viewModel.KeyboardFocusedEntryId = "one";
        Assert.Equal("one", viewModel.KeyboardFocusedEntryId);
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "one").IsKeyboardFocused);

        viewModel.RefreshCommand.Execute(null);

        Assert.Null(viewModel.KeyboardFocusedEntryId);
        Assert.All(viewModel.Entries, entry => Assert.False(entry.IsKeyboardFocused));
    }

    private static EntryDraft NewThreeEntryDraft() =>
        new("three", "Three", "D:\\three", "pwsh", Selected: true);

    private static EntryDraft NewFourEntryDraft() =>
        new("four", "Four", "D:\\four", "pwsh", Selected: false);

    private static EntryDraft RenamedOneEntryDraft() =>
        new("one-renamed", "One Renamed", "D:\\renamed", "opencode", Selected: false);
}
