using Avalonia.Controls;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

internal static class MainWindowEntryCardTestFactory
{
    public static Border Card(LauncherEntryViewModel entry)
    {
        return new Border
        {
            Classes = { "card" },
            DataContext = entry,
        };
    }

    public static MainWindowViewModel CreateOwner(string rootPath)
    {
        var configPath = Path.Combine(rootPath, "config.json");
        var settingsPath = Path.Combine(rootPath, "settings.json");
        LauncherConfigStore.Save([CreateEntry("owner")], configPath);
        return new MainWindowViewModel(configPath, settingsPath, new LauncherSettings(), new LauncherProcess());
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

        LauncherConfigStore.Save([entry], configPath);

        var owner = new MainWindowViewModel(
            configPath,
            settingsPath,
            new LauncherSettings(),
            new LauncherProcess());

        return new LauncherEntryViewModel(entry, owner);
    }

    private static LaunchEntry CreateEntry(string id)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
        };
    }
}
