using WTLauncher.App.ViewModels;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void PublicCollectionsExposeReadOnlyProjections()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One"),
            Entry("two", "Two"));
        var viewModel = CreateViewModel(config, settings);

        AssertReadOnlyProjection(viewModel.Entries, viewModel.Entries.First());
        AssertReadOnlyProjection(viewModel.VisibleEntries, viewModel.VisibleEntries.First());
        AssertReadOnlyProjection(viewModel.Presets, viewModel.Presets.First());
        AssertReadOnlyProjection(viewModel.SortModes, viewModel.SortModes.First());
        AssertReadOnlyProjection(viewModel.DisplayModes, viewModel.DisplayModes.First());
    }

    [Fact]
    public void EntryViewModelsExposeEntrySnapshotsNotLiveRows()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "pwsh", selected: true));
        var viewModel = CreateViewModel(config, settings);
        var entry = viewModel.Entries.Single();

        var snapshot = entry.SnapshotEntry();
        snapshot = snapshot with { Id = "broken", Name = "", Command = "bad", Selected = false };

        Assert.Equal("one", entry.Id);
        Assert.Equal("One", entry.Name);
        Assert.Equal("pwsh", entry.Command);
        Assert.True(entry.IsSelected);
        Assert.Equal("one", entry.SnapshotEntry().Id);
    }

    [Fact]
    public void EntryViewModelsRejectMissingOwner()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new LauncherEntryViewModel(Entry("one"), null!));

        Assert.Equal("owner", exception.ParamName);
    }

    private static void AssertReadOnlyProjection<T>(IList<T> collection, T sample)
    {
        var count = collection.Count;

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(sample));
        Assert.Equal(count, collection.Count);
    }
}
