using System.Collections.ObjectModel;
using Avalonia;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryVisibilityWorkflowTests
{
    [Fact]
    public void ApplyFilterReplacesVisibleEntriesAndAppliesCardSize()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = new Collection<LauncherEntryViewModel>
        {
            ViewModel(owner, "one", "Zulu"),
            ViewModel(owner, "two", "Alpha"),
            ViewModel(owner, "three", "Gamma"),
        };
        var visibleEntries = new ObservableCollection<LauncherEntryViewModel>
        {
            ViewModel(owner, "stale", "Stale"),
        };
        var expected = LauncherEntryLayoutCalculator.Calculate(640, 260, itemCount: 1);

        var result = LauncherEntryVisibilityWorkflow.ApplyFilter(
            visibleEntries,
            entries,
            filterText: "gam",
            sortModeId: "alphabetical",
            customEntryOrder: ["three", "one", "two"],
            availableSize: new Size(640, 260));

        Assert.Equal(1, result.VisibleCount);
        Assert.Equal(["three"], visibleEntries.Select(entry => entry.Id).ToArray());
        Assert.All(entries, entry =>
        {
            Assert.Equal(expected.CardWidth, entry.CardWidth);
            Assert.Equal(expected.CardHeight, entry.CardHeight);
        });
    }

    [Fact]
    public void UpdateAvailableSizeNormalizesSizeAndAppliesCardSize()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = new Collection<LauncherEntryViewModel>
        {
            ViewModel(owner, "one", "One"),
            ViewModel(owner, "two", "Two"),
        };
        var visibleEntries = new Collection<LauncherEntryViewModel> { entries[0] };
        var expected = LauncherEntryLayoutCalculator.Calculate(220, 140, itemCount: 1);

        var normalized = LauncherEntryVisibilityWorkflow.UpdateAvailableSize(
            entries,
            visibleEntries,
            new Size(120, 80));

        Assert.Equal(new Size(220, 140), normalized);
        Assert.All(entries, entry =>
        {
            Assert.Equal(expected.CardWidth, entry.CardWidth);
            Assert.Equal(expected.CardHeight, entry.CardHeight);
        });
    }

    private static MainWindowViewModel CreateOwner(string rootPath)
    {
        var configPath = Path.Combine(rootPath, "config.json");
        var settingsPath = Path.Combine(rootPath, "settings.json");
        LauncherConfigStore.Save([Entry("owner", "Owner")], configPath);
        return new MainWindowViewModel(configPath, settingsPath, new LauncherSettings(), new LauncherProcess());
    }

    private static LauncherEntryViewModel ViewModel(MainWindowViewModel owner, string id, string name)
    {
        return new LauncherEntryViewModel(Entry(id, name), owner);
    }

    private static LaunchEntry Entry(string id, string name)
    {
        return new LaunchEntry
        {
            Id = id,
            Name = name,
            Directory = $"D:\\{id}",
            Command = "pwsh",
            Confirm = false,
        };
    }
}
