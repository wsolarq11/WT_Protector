using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherVisibleEntryCollectionTests
{
    [Fact]
    public void ReplaceFiltersVisibleEntriesAndReturnsCount()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var target = new ObservableCollection<LauncherEntryViewModel>
        {
            new(new LaunchEntry { Id = "stale", Name = "Stale" }, owner),
        };
        var source = Entries(owner);

        var count = LauncherVisibleEntryCollection.Replace(
            target,
            source,
            filterText: "gam",
            sortModeId: "alphabetical",
            customEntryOrder: ["three", "one", "two"]);

        Assert.Equal(1, count);
        Assert.Equal(["three"], target.Select(entry => entry.Id).ToArray());
        Assert.DoesNotContain(target, entry => entry.Id == "stale");
    }

    [Fact]
    public void ReplaceSortsVisibleEntriesByCustomOrder()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var target = new ObservableCollection<LauncherEntryViewModel>();
        var source = Entries(owner);
        List<string> customOrder = ["three", "one", "two"];

        var count = LauncherVisibleEntryCollection.Replace(
            target,
            source,
            filterText: "",
            sortModeId: null,
            customEntryOrder: customOrder);

        Assert.Equal(3, count);
        Assert.Equal(["three", "one", "two"], target.Select(entry => entry.Id).ToArray());
        Assert.Equal(["three", "one", "two"], customOrder);
    }

    private static MainWindowViewModel CreateOwner(string rootPath)
    {
        var configPath = Path.Combine(rootPath, "config.json");
        var settingsPath = Path.Combine(rootPath, "settings.json");
        LauncherConfigStore.Save(
            [
                new LaunchEntry
                {
                    Id = "one",
                    Name = "One",
                    Directory = $"D:\\{rootPath}",
                    Command = "pwsh",
                    Confirm = false,
                },
            ],
            configPath);
        return new MainWindowViewModel(configPath, settingsPath, new LauncherSettings(), new LauncherProcess());
    }

    private static List<LauncherEntryViewModel> Entries(MainWindowViewModel owner)
    {
        return
        [
            new LauncherEntryViewModel(
                new LaunchEntry
                {
                    Id = "one",
                    Name = "Zulu",
                    Directory = "D:\\one",
                    Command = "codex",
                    Confirm = false,
                },
                owner),
            new LauncherEntryViewModel(
                new LaunchEntry
                {
                    Id = "two",
                    Name = "Alpha",
                    Directory = "D:\\two",
                    Command = "opencode",
                    Confirm = false,
                },
                owner),
            new LauncherEntryViewModel(
                new LaunchEntry
                {
                    Id = "three",
                    Name = "Gamma",
                    Directory = "D:\\three",
                    Command = "pwsh",
                    Confirm = false,
                },
                owner),
        ];
    }
}
