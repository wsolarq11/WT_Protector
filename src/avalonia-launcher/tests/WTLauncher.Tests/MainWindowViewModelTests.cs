using Avalonia;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void BatchSelectionCommandsPersistPortableConfig()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(config, settings);

        Assert.Equal(0, viewModel.SelectedCount);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.True(entry.Selected));
        Assert.All(LauncherConfigStore.Load(config + ".bak"), entry => Assert.False(entry.Selected));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.False(entry.Selected));
        Assert.All(LauncherConfigStore.Load(config + ".bak"), entry => Assert.True(entry.Selected));
    }

    [Fact]
    public void SelectVisibleReportsWhenVisibleEntriesAreAlreadySelected()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: true, twoSelected: true);

        var viewModel = CreateViewModel(config, settings);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal("可见项已全部勾选", viewModel.StatusText);
        Assert.Equal(2, viewModel.SelectedCount);
        Assert.All(viewModel.Entries, entry => Assert.True(entry.IsSelected));
    }

    [Fact]
    public void SelectionChangesRollBackWhenConfigSaveFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(config, settings);
        File.Delete(config);
        Directory.CreateDirectory(config);

        viewModel.Entries.Single(entry => entry.Id == "one").IsSelected = true;

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(viewModel.Entries, entry => Assert.False(entry.IsSelected));
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(config)!, "config.json.tmp-*"));
    }

    [Fact]
    public void SelectionRollbackUsesTypedSnapshot()
    {
        Assert.Null(typeof(MainWindowViewModel).GetMethod("CaptureSelectionSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
        Assert.Null(typeof(MainWindowViewModel).GetMethod("RestoreSelection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
        Assert.Contains(
            typeof(LauncherSelectionMutationSnapshot).GetMethods(),
            method => method.Name == nameof(LauncherSelectionMutationSnapshot.Capture));
    }


    [Fact]
    public void ConstructorAppliesSavedFilterWithoutPersistingSettings()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { LastFilterText = "CODEX" });

        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Entry.Id);
        Assert.Equal("CODEX", viewModel.FilterText);
        Assert.False(File.Exists(settings));
    }

    [Fact]
    public void FilterTextPersistsPortableSettingsAndFiltersVisibleEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);

        Assert.Equal(["one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.FilterText = "CODEX";

        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Entry.Id);
        Assert.Equal("CODEX", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "D:\\TWO";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("two", visible.Entry.Id);
        Assert.Equal("D:\\TWO", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "ONE";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Entry.Id);
        Assert.Equal("ONE", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "   ";

        Assert.Equal(["one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal("   ", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "missing";

        Assert.Empty(viewModel.VisibleEntries);
        Assert.Equal(0, viewModel.VisibleCount);
        Assert.Equal("missing", LauncherSettingsStore.Load(settings).LastFilterText);
    }


    [Fact]
    public void ApplyPresetCommandReplacesAndPersistsPortableSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: true);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "description": "demo preset",
              "entryIds": ["one"]
            }
          ]
        }
        """);

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
        File.WriteAllText(defaultPresets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "team",
          "presets": [
            { "id": "daily", "name": "Daily", "entryIds": ["one"] },
            { "id": "team", "name": "Team", "entryIds": ["two"] }
          ]
        }
        """);

        var defaultViewModel = CreateViewModel(defaultConfig, defaultSettings);

        Assert.Equal(["daily", "team"], defaultViewModel.Presets.Select(preset => preset.Id).ToArray());
        Assert.Equal("team", defaultViewModel.SelectedPreset?.Id);

        using var fallbackTemp = new TempDirectory();
        var (fallbackConfig, fallbackSettings, fallbackPresets) = CreatePortableFixture(
            fallbackTemp,
            oneSelected: false,
            twoSelected: false);
        File.WriteAllText(fallbackPresets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "missing",
          "presets": [
            { "id": "daily", "name": "Daily", "entryIds": ["one"] },
            { "id": "team", "name": "Team", "entryIds": ["two"] }
          ]
        }
        """);

        var fallbackViewModel = CreateViewModel(fallbackConfig, fallbackSettings);

        Assert.Equal("daily", fallbackViewModel.SelectedPreset?.Id);
    }

    [Fact]
    public async Task LaunchGroupSaveRollsBackInMemoryStateWhenPersistFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "description": "demo preset",
              "entryIds": ["one"]
            }
          ]
        }
        """);

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
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(presets)!, "presets.json.tmp-*"));
    }

    [Fact]
    public async Task LaunchGroupSavePassesSelectedEntryIdsToPresetDialog()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        IReadOnlyList<string>? dialogEntryIds = null;
        var viewModel = CreateViewModel(
            config,
            settings,
            configure: vm => vm.RequestNewPresetAsync = entryIds =>
            {
                dialogEntryIds = entryIds.ToArray();
                return Task.FromResult<string?>(null);
            });

        await viewModel.SaveSelectionAsPresetCommand.ExecuteAsync(null);

        Assert.Equal(["one"], dialogEntryIds);
        Assert.Equal("已取消: 保存启动组", viewModel.StatusText);
    }

    [Fact]
    public void BrokenPresetWarnsAndDoesNotRewriteSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: true);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "broken",
          "presets": [
            {
              "id": "broken",
              "name": "Broken",
              "description": "references a retired config id",
              "entryIds": ["one", "missing-entry"]
            }
          ]
        }
        """);

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

    [Fact]
    public async Task LaunchGroupCommandsCreateUpdateAndDeletePortablePresets()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: true, twoSelected: false);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "description": "demo preset",
              "entryIds": ["one"]
            },
            {
              "id": "blank-manual",
              "name": "Manual",
              "description": "locked empty preset",
              "entryIds": [],
              "locked": true
            }
          ]
        }
        """);

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
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "description": "demo preset",
              "entryIds": ["one"]
            },
            {
              "id": "blank-manual",
              "name": "Manual",
              "description": "locked empty preset",
              "entryIds": [],
              "locked": true
            }
          ]
        }
        """);

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
    public void SortModesAndCustomDragOrderPersistPortableSettings()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "Zulu",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = true,
                UpdatedAtUtc = "2026-05-17T00:00:00Z",
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "Alpha",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
                UpdatedAtUtc = "2026-05-18T00:00:00Z",
            },
            new LaunchEntry
            {
                Id = "three",
                Name = "Mike",
                Directory = "D:\\three",
                Command = "pwsh",
                Confirm = false,
                UpdatedAtUtc = "",
            });

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom", CustomEntryOrder = ["three", "one", "two"] });

        Assert.Equal(["custom", "updated", "alphabetical"], viewModel.SortModes.Select(mode => mode.Id).ToArray());
        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.BeginDragReorder("two");
        Assert.True(viewModel.PreviewMoveEntry("two", "three", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("custom", savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "alphabetical");

        Assert.Equal(["two", "three", "one"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("alphabetical", savedSettings.SortMode);
        Assert.Equal(["two", "three", "one"], savedSettings.CustomEntryOrder);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "updated");

        Assert.Equal(["two", "one", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal("updated", savedSettings.SortMode);
    }

}
