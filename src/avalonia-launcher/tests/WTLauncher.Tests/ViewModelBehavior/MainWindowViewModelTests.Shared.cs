using System.Collections.Immutable;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

public sealed partial class MainWindowViewModelTests
{
    private static LaunchEntry[] NumberedEntries(int count) =>
        Enumerable.Range(1, count)
            .Select(index => Entry($"entry-{index}", $"Entry {index}"))
            .ToArray();

    private static (string Config, string Settings, string Presets) CreateSingleEntryFixture(TempDirectory temp) =>
        CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex"));

    private static (string Config, string Settings, string Presets) CreatePortableFixture(
        TempDirectory temp,
        bool oneSelected,
        bool twoSelected) =>
        CreatePortableFixture(
            temp,
            Entry("one", "one", command: "codex", selected: oneSelected),
            Entry("two", "two", command: "opencode", selected: twoSelected));

    private static LaunchEntry Entry(
        string id,
        string? name = null,
        string? directory = null,
        string command = "pwsh",
        bool selected = false,
        string updatedAtUtc = "") =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(id) ? EntryId.Empty : id,
            Name = string.IsNullOrWhiteSpace(name ?? id) ? EntryName.Empty : name ?? id,
            Directory = string.IsNullOrWhiteSpace(directory) && string.IsNullOrWhiteSpace(id) ? WTLauncher.Core.DirectoryPath.Empty : (WTLauncher.Core.DirectoryPath)(directory ?? $"D:\\{id}"),
            Command = command,
            Selected = selected,
            UpdatedAtUtc = updatedAtUtc,
        };

    private static (string Config, string Settings, string Presets) CreatePortableFixture(
        TempDirectory temp,
        params LaunchEntry[] entries)
    {
        var configDir = Path.Combine(temp.Path, LauncherStore.ConfigDirectoryName);
        Directory.CreateDirectory(configDir);
        var storePath = Path.Combine(configDir, LauncherStore.StoreFileName);
        var storeFile = new LauncherStoreFile
        {
            Entries = entries.ToImmutableList(),
            Settings = new LauncherSettings(),
            Presets =
            [
                new LauncherPreset
                {
                    Id = "default",
                    Name = "Default",
                    EntryIds = entries.Select(e => e.Id.Value).ToImmutableList(),
                },
            ],
            DefaultPresetId = "default",
        };
        LauncherStore.Save(storeFile, storePath, backup: false);
        return (storePath, storePath, storePath);
    }

    private static void AssertNoStoreTempFiles(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        var fileName = Path.GetFileName(path);
        // ponytail: after save failure, temp files are intentionally kept for
        // crash recovery. CleanupStaleTempFiles handles them at next startup.
        // Call it first to simulate that recovery path, then assert clean.
        LauncherJson.CleanupStaleTempFiles(directory);
        Assert.Empty(Directory.EnumerateFiles(directory, $"{fileName}.tmp-*"));
    }

    private static void SavePresetFile(string path, string defaultPresetId, params LauncherPreset[] presets)
    {
        var storeFile = LauncherStore.Load(path);
        storeFile = storeFile with
        {
            Presets = presets.ToImmutableList(),
            DefaultPresetId = defaultPresetId,
        };
        LauncherStore.Save(storeFile, path, backup: false);
    }

    private static LauncherPreset Preset(string id, string name, params string[] entryIds) =>
        Preset(id, name, locked: false, entryIds);

    private static LauncherPreset Preset(string id, string name, bool locked, params string[] entryIds) =>
        new()
        {
            Id = id,
            Name = name,
            EntryIds = [.. entryIds],
            Locked = locked,
        };

    private static MainWindowViewModel CreateViewModel(
        string config,
        string settings,
        LauncherSettings? launcherSettings = null,
        Action<MainWindowViewModel>? configure = null,
        Func<DateTimeOffset>? utcNow = null,
        LauncherLaunchWorkflow? launchWorkflow = null,
        StubDialogService? dialogService = null)
    {
        // Both config and settings point to the same unified store file now.
        var storeFile = LauncherStore.Load(config);
        var dialogs = dialogService ?? new StubDialogService();
        var effectiveStoreFile = launcherSettings is not null ? storeFile with { Settings = launcherSettings } : storeFile;
        var appStore = new StoreGrain(config, effectiveStoreFile);
        var viewModel = new MainWindowViewModel(
            config,
            appStore,
            appStore.Select(s => s),
            launchWorkflow ?? CreateLaunchWorkflow(),
            dialogs,
            new EntryService(dialogs),
            new PresetService(dialogs),
            utcNow ?? SystemUtcNow);
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private static List<LaunchEntry> LoadEntriesById(string config) =>
        LauncherStore.Load(config).Entries.OrderBy(entry => entry.Id.Value).ToList();

    private static LauncherLaunchWorkflow CreateLaunchWorkflow() => new(_ => { });

    private static DateTimeOffset SystemUtcNow() => DateTimeOffset.UtcNow;
}

