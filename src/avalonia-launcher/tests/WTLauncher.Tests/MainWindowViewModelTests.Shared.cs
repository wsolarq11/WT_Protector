using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

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
            Id = id,
            Name = name ?? id,
            Directory = directory ?? $"D:\\{id}",
            Command = command,
            Selected = selected,
            UpdatedAtUtc = updatedAtUtc,
        };

    private static (string Config, string Settings, string Presets) CreatePortableFixture(
        TempDirectory temp,
        params LaunchEntry[] entries)
    {
        var configDir = Path.Combine(temp.Path, LauncherConfigStore.ConfigDirectoryName);
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, LauncherConfigStore.ConfigFileName);
        LauncherConfigStore.Save(entries, config, backup: true);
        return (
            config,
            Path.Combine(configDir, LauncherConfigStore.SettingsFileName),
            Path.Combine(configDir, LauncherPresetStore.PresetsFileName));
    }

    private static void AssertNoStoreTempFiles(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        var fileName = Path.GetFileName(path);
        Assert.Empty(Directory.EnumerateFiles(directory, $"{fileName}.tmp-*"));
    }

    private static void SavePresetFile(string path, string defaultPresetId, params LauncherPreset[] presets) =>
        LauncherPresetStore.Save(
            new LauncherPresetFile
            {
                DefaultPresetId = defaultPresetId,
                Presets = [.. presets],
            },
            path,
            backup: true);

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
        Func<DateTimeOffset>? utcNow = null)
    {
        var viewModel = new MainWindowViewModel(
            config,
            settings,
            launcherSettings ?? new LauncherSettings(),
            CreateLaunchWorkflow(),
            utcNow ?? SystemUtcNow);
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private static List<LaunchEntry> LoadEntriesById(string config) =>
        LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();

    private static LauncherLaunchWorkflow CreateLaunchWorkflow() => new(_ => { });

    private static DateTimeOffset SystemUtcNow() => DateTimeOffset.UtcNow;
}
