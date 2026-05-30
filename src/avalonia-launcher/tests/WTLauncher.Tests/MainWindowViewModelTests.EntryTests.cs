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
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(config)!, "config.json.tmp-*"));
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
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(config)!, "config.json.tmp-*"));
    }

    [Fact]
    public async Task EditEntryRollsBackConfigFileWhenPresetSaveFailsAfterConfigSave()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "entryIds": ["one"]
            }
          ]
        }
        """);
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
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(config)!, "config.json.tmp-*"));
    }

    [Fact]
    public async Task EntryCommandsPersistPortableConfigAndReferences()
    {
        using var temp = new TempDirectory();
        var (config, settings, presets) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        File.WriteAllText(presets, """
        {
          "schemaVersion": 1,
          "defaultPresetId": "daily",
          "presets": [
            {
              "id": "daily",
              "name": "Daily",
              "entryIds": ["one"]
            }
          ]
        }
        """);

        var requestCount = 0;
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
            });

        await viewModel.AddEntryCommand.ExecuteAsync(null);

        var savedEntries = LoadEntriesById(config);
        var added = savedEntries.Single(entry => entry.Id == "three");
        Assert.True(added.Selected);
        Assert.False(string.IsNullOrWhiteSpace(added.UpdatedAtUtc));
        var savedSettings = LauncherSettingsStore.Load(settings);
        Assert.Equal(["one", "two", "three"], savedSettings.CustomEntryOrder);

        await viewModel.Entries.Single(entry => entry.Id == "one").EditCommand.ExecuteAsync(null);

        savedEntries = LoadEntriesById(config);
        Assert.DoesNotContain(savedEntries, entry => entry.Id == "one");
        var edited = savedEntries.Single(entry => entry.Id == "one-renamed");
        Assert.Equal("One Renamed", edited.Name);
        Assert.Equal("D:\\renamed", edited.Directory);
        Assert.Equal("opencode", edited.Command);
        Assert.False(edited.Confirm);
        Assert.False(edited.Selected);
        Assert.False(string.IsNullOrWhiteSpace(edited.UpdatedAtUtc));

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

    private static EntryDraft NewThreeEntryDraft() =>
        new("three", "Three", "D:\\three", "pwsh", Selected: true);

    private static EntryDraft RenamedOneEntryDraft() =>
        new("one-renamed", "One Renamed", "D:\\renamed", "opencode", Selected: false);
}
