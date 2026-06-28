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
        var storePath = Path.Combine(rootPath, LauncherStore.StoreFileName);
        var entry = CreateEntry("owner");
        var storeFile = new LauncherStoreFile
        {
            Entries = [entry],
            Settings = new LauncherSettings(),
            Presets =
            [
                new LauncherPreset
                {
                    Id = "default",
                    Name = "Default",
                    EntryIds = [entry.Id],
                },
            ],
            DefaultPresetId = "default",
        };
        LauncherStore.Save(storeFile, storePath, backup: true);
        return new MainWindowViewModel(
            storePath,
            storeFile,
            CreateLaunchWorkflow(),
            static () => DateTimeOffset.UtcNow);
    }

    public static LauncherEntryViewModel CreateEntryViewModel(MainWindowViewModel owner, string id)
    {
        return new LauncherEntryViewModel(CreateEntry(id), owner);
    }

    public static LauncherEntryViewModel CreateEntryViewModel(string rootPath, string id)
    {
        var storePath = Path.Combine(rootPath, $"{id}-store.json");
        var entry = CreateEntry(id);
        var storeFile = new LauncherStoreFile
        {
            Entries = [entry],
            Settings = new LauncherSettings(),
            Presets =
            [
                new LauncherPreset
                {
                    Id = "default",
                    Name = "Default",
                    EntryIds = [entry.Id],
                },
            ],
            DefaultPresetId = "default",
        };
        LauncherStore.Save(storeFile, storePath, backup: true);
        return new LauncherEntryViewModel(entry, new MainWindowViewModel(
            storePath,
            storeFile,
            CreateLaunchWorkflow(),
            static () => DateTimeOffset.UtcNow));
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
