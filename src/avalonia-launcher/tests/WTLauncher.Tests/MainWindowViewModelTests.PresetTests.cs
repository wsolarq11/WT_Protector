using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
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
}
