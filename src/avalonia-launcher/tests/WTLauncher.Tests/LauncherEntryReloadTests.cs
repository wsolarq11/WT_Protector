using System.Collections.ObjectModel;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryReloadTests
{
    [Fact]
    public void LoadReadsConfigAndReplacesEntryCollection()
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

        var summary = LauncherEntryReload.Load(
            configPath,
            target,
            entry => new LauncherEntryViewModel(entry, owner));

        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(1, summary.SelectedCount);
        Assert.Equal(["one", "two"], target.Select(entry => entry.Id).ToArray());
        Assert.DoesNotContain(target, entry => entry.Id == "stale");
    }

    [Fact]
    public void LoadPropagatesMissingConfigFailure()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwnerWithConfig(temp.Path);
        var target = new ObservableCollection<LauncherEntryViewModel>();
        var missingConfig = Path.Combine(temp.Path, "missing.json");

        Assert.Throws<FileNotFoundException>(() => LauncherEntryReload.Load(
            missingConfig,
            target,
            entry => new LauncherEntryViewModel(entry, owner)));
    }

    private static MainWindowViewModel CreateOwnerWithConfig(string rootPath)
    {
        var configPath = Path.Combine(rootPath, "owner-config.json");
        LauncherConfigStore.Save([Entry("owner", selected: false)], configPath);
        return CreateOwner(rootPath, configPath);
    }

    private static MainWindowViewModel CreateOwner(string rootPath, string configPath)
    {
        var settingsPath = Path.Combine(rootPath, "settings.json");
        return new MainWindowViewModel(configPath, settingsPath, new LauncherSettings(), new LauncherProcess());
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
