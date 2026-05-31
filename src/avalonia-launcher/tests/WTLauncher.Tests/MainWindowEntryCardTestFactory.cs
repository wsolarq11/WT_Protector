using Avalonia.Controls;
using WTLauncher.App.ViewModels;
using WTLauncher.App.Views;
using WTLauncher.Core;

namespace WTLauncher.Tests;

internal static class MainWindowEntryCardTestFactory
{
    public static Border Card(LauncherEntryViewModel entry)
    {
        return new Border
        {
            Classes = { MainWindowEntryClasses.Card },
            DataContext = entry,
        };
    }

    public static MainWindowViewModel CreateOwner(string rootPath)
    {
        return CreateOwner(
            Path.Combine(rootPath, "config.json"),
            Path.Combine(rootPath, "settings.json"),
            CreateEntry("owner"));
    }

    public static LauncherEntryViewModel CreateEntryViewModel(MainWindowViewModel owner, string id)
    {
        return new LauncherEntryViewModel(CreateEntry(id), owner);
    }

    public static LauncherEntryViewModel CreateEntryViewModel(string rootPath, string id)
    {
        var configPath = Path.Combine(rootPath, $"{id}-config.json");
        var settingsPath = Path.Combine(rootPath, $"{id}-settings.json");
        var entry = CreateEntry(id);

        return new LauncherEntryViewModel(entry, CreateOwner(configPath, settingsPath, entry));
    }

    private static MainWindowViewModel CreateOwner(string configPath, string settingsPath, LaunchEntry entry)
    {
        LauncherConfigStore.Save([entry], configPath, backup: true);
        LauncherPresetStore.Save(
            new LauncherPresetFile
            {
                DefaultPresetId = "default",
                Presets =
                [
                    new LauncherPreset
                    {
                        Id = "default",
                        Name = "Default",
                        EntryIds = [entry.Id],
                    },
                ],
            },
            LauncherPresetStore.ResolvePresetPath(configPath),
            backup: false);
        return new MainWindowViewModel(
            configPath,
            settingsPath,
            new LauncherSettings(),
            CreateLaunchWorkflow(),
            static () => DateTimeOffset.UtcNow);
    }

    private static LaunchEntry CreateEntry(string id)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
        };
    }

    private static LauncherLaunchWorkflow CreateLaunchWorkflow() => new(_ => { });
}
