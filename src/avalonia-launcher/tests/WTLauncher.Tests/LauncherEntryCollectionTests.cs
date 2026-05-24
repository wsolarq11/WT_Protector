using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryCollectionTests
{
    [Fact]
    public void ReplacePopulatesEntriesAndCountsSelectedItems()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        LauncherConfigStore.Save(
            [
                Entry("one", selected: true),
                Entry("two", selected: false),
            ],
            configPath);
        var owner = CreateOwner(temp.Path, configPath);
        var target = new ObservableCollection<LauncherEntryViewModel>
        {
            new(new LaunchEntry { Id = "stale", Name = "Stale" }, owner),
        };

        var summary = LauncherEntryCollection.Replace(
            target,
            LauncherConfigStore.Load(configPath),
            entry => new LauncherEntryViewModel(entry, owner));

        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(1, summary.SelectedCount);
        Assert.Equal(["one", "two"], target.Select(entry => entry.Id).ToArray());
        Assert.True(target.Single(entry => entry.Id == "one").IsSelected);
        Assert.False(target.Single(entry => entry.Id == "two").IsSelected);
    }

    [Fact]
    public void ReplaceClearsExistingEntriesBeforePopulating()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        LauncherConfigStore.Save([Entry("one", selected: false)], configPath);
        var owner = CreateOwner(temp.Path, configPath);
        var target = new ObservableCollection<LauncherEntryViewModel>
        {
            new(new LaunchEntry { Id = "stale", Name = "Stale" }, owner),
        };

        LauncherEntryCollection.Replace(
            target,
            LauncherConfigStore.Load(configPath),
            entry => new LauncherEntryViewModel(entry, owner));

        Assert.DoesNotContain(target, entry => entry.Id == "stale");
    }

    private static MainWindowViewModel CreateOwner(string rootPath, string configPath)
    {
        var settingsPath = Path.Combine(rootPath, "settings.json");
        var settings = new LauncherSettings();
        return new MainWindowViewModel(configPath, settingsPath, settings, new LauncherProcess());
    }

    private static LaunchEntry Entry(string id, bool selected)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = id,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
            Selected = selected,
        };
    }
}
