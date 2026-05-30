using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    private static LaunchEntry[] NumberedEntries(int count) =>
        Enumerable.Range(1, count)
            .Select(index => new LaunchEntry
            {
                Id = $"entry-{index}",
                Name = $"Entry {index}",
                Directory = $"D:\\entry-{index}",
                Command = "pwsh",
                Confirm = false,
            })
            .ToArray();

    private static (string Config, string Settings, string Presets) CreateSingleEntryFixture(TempDirectory temp) =>
        CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "One",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = false,
            });

    private static (string Config, string Settings, string Presets) CreatePortableFixture(
        TempDirectory temp,
        bool oneSelected,
        bool twoSelected) =>
        CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "one",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = true,
                Selected = oneSelected,
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "two",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
                Selected = twoSelected,
            });

    private static (string Config, string Settings, string Presets) CreatePortableFixture(
        TempDirectory temp,
        params LaunchEntry[] entries)
    {
        var configDir = Path.Combine(temp.Path, "config");
        Directory.CreateDirectory(configDir);
        var config = Path.Combine(configDir, "config.json");
        LauncherConfigStore.Save(entries, config);
        return (config, Path.Combine(configDir, "settings.json"), Path.Combine(configDir, "presets.json"));
    }

    private static MainWindowViewModel CreateViewModel(
        string config,
        string settings,
        LauncherSettings? launcherSettings = null,
        Action<MainWindowViewModel>? configure = null)
    {
        var viewModel = new MainWindowViewModel(
            config,
            settings,
            launcherSettings ?? new LauncherSettings(),
            CreateLaunchWorkflow());
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private static List<LaunchEntry> LoadEntriesById(string config) =>
        LauncherConfigStore.Load(config).OrderBy(entry => entry.Id).ToList();

    private static LauncherLaunchWorkflow CreateLaunchWorkflow() => new(_ => { });
}
