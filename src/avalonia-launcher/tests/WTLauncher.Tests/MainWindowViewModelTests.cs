using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void BatchSelectionCommandsPersistPortableConfig()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        var settings = Path.Combine(configDir, "settings.json");
        File.WriteAllText(config, """
        [
          {
            "id": "one",
            "name": "one",
            "directory": "D:\\one",
            "command": "codex",
            "confirm": true,
            "selected": false
          },
          {
            "id": "two",
            "name": "two",
            "directory": "D:\\two",
            "command": "opencode",
            "confirm": false,
            "selected": false
          }
        ]
        """);

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

        Assert.Equal(0, viewModel.SelectedCount);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.True(entry.Selected));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(LauncherConfigStore.Load(config), entry => Assert.False(entry.Selected));
        Assert.True(File.Exists(config + ".bak"));
    }

    [Fact]
    public void ApplyPresetCommandReplacesAndPersistsPortableSelection()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        var settings = Path.Combine(configDir, "settings.json");
        File.WriteAllText(config, """
        [
          {
            "id": "one",
            "name": "one",
            "directory": "D:\\one",
            "command": "codex",
            "confirm": true,
            "selected": false
          },
          {
            "id": "two",
            "name": "two",
            "directory": "D:\\two",
            "command": "opencode",
            "confirm": false,
            "selected": true
          }
        ]
        """);
        File.WriteAllText(Path.Combine(configDir, "presets.json"), """
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

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess());

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
        var entries = LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();
        Assert.True(entries.Single(entry => entry.Id == "one").Selected);
        Assert.False(entries.Single(entry => entry.Id == "two").Selected);
    }

    [Fact]
    public async Task LaunchGroupCommandsCreateUpdateAndDeletePortablePresets()
    {
        using var temp = new TempDirectory();
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        var settings = Path.Combine(configDir, "settings.json");
        var presets = Path.Combine(configDir, "presets.json");
        File.WriteAllText(config, """
        [
          {
            "id": "one",
            "name": "one",
            "directory": "D:\\one",
            "command": "codex",
            "confirm": true,
            "selected": true
          },
          {
            "id": "two",
            "name": "two",
            "directory": "D:\\two",
            "command": "opencode",
            "confirm": false,
            "selected": false
          }
        ]
        """);
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

        var viewModel = new MainWindowViewModel(config, settings, new LauncherSettings(), new LauncherProcess())
        {
            RequestNewPresetAsync = () => Task.FromResult<PresetDraft?>(new PresetDraft("Project Pair")),
        };

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

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wt-launcher-viewmodel-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
