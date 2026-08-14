using WTLauncher.App.ViewModels;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed class LauncherCardGroupViewModelTests
{
    [Fact]
    public void ConstructorSnapshotsEntriesAndExposesBindingFacts()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var source = new List<LauncherEntryViewModel>
        {
            MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "one"),
        };
        var group = CreateGroup(source);

        source.Add(MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "two"));

        Assert.Equal("command:pwsh", group.Key);
        Assert.Equal("pwsh", group.DisplayName);
        Assert.Equal(1, group.Count);
        Assert.Equal("1 个", group.CountLabel);
        Assert.True(group.ShowHeader);
        Assert.Single(group.Entries);
    }

    [Fact]
    public void ToggleExpandedCommandUpdatesIconAndRaisesNotification()
    {
        var group = CreateGroup([]);
        var changed = new List<string?>();
        group.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        group.ToggleExpandedCommand.Execute(null);

        Assert.False(group.IsExpanded);
        Assert.Contains(nameof(group.IsExpanded), changed);
    }

    [Fact]
    public async Task CommandsPassPointInTimePayloadsInVisibleOrder()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var one = MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "one");
        var two = MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "two");
        IReadOnlyList<LauncherEntryViewModel>? launched = null;
        IReadOnlyList<string>? saved = null;
        var group = new LauncherCardGroupViewModel(
            "all", "", 2, false, [one, two],
            entries => { launched = entries; return Task.CompletedTask; },
            ids => { saved = ids; return Task.CompletedTask; });

        await group.LaunchAllCommand.ExecuteAsync(null);
        await group.SaveAsPresetCommand.ExecuteAsync(null);
        group.Entries.Clear();

        Assert.Equal([one, two], launched);
        Assert.Equal(["one", "two"], saved);
    }

    private static LauncherCardGroupViewModel CreateGroup(IEnumerable<LauncherEntryViewModel> entries) =>
        new(
            "command:pwsh",
            "pwsh",
            1,
            true,
            entries,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask);
}
