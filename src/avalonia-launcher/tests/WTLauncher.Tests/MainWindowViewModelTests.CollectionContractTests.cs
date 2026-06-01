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

    private static void AssertReadOnlyProjection<T>(IList<T> collection, T sample)
    {
        var count = collection.Count;

        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(sample));
        Assert.Equal(count, collection.Count);
    }
}
