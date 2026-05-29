using Avalonia;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static WTLauncher.Tests.MainWindowEntryCardTestFactory;

namespace WTLauncher.Tests;

public sealed class LauncherEntryLayoutPresentationTests
{
    [Fact]
    public void NormalizeAvailableSizeClampsToMinimumViewport()
    {
        var size = LauncherEntryLayoutPresentation.NormalizeAvailableSize(new Size(120, 80));

        Assert.Equal(220, size.Width);
        Assert.Equal(140, size.Height);
    }

    [Fact]
    public void ApplyCardSizeUsesVisibleEntryCountWhenFiltered()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = Entries(owner, "one", "two", "three");
        var visibleEntries = new List<LauncherEntryViewModel> { entries[0], entries[2] };
        var expected = LauncherEntryLayoutCalculator.Calculate(900, 520, itemCount: 2);

        LauncherEntryLayoutPresentation.ApplyCardSize(entries, visibleEntries, new Size(900, 520));

        foreach (var entry in entries)
        {
            Assert.Equal(expected.CardWidth, entry.CardWidth);
            Assert.Equal(expected.CardHeight, entry.CardHeight);
            Assert.Equal(expected.NameTileWidth, entry.NameTileWidth);
            Assert.Equal(expected.NameTileHeight, entry.NameTileHeight);
        }
    }

    [Fact]
    public void ApplyCardSizeFallsBackToAllEntriesWhenNothingVisible()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = Entries(owner, "one", "two", "three");
        var expected = LauncherEntryLayoutCalculator.Calculate(640, 260, itemCount: 3);

        LauncherEntryLayoutPresentation.ApplyCardSize(entries, [], new Size(640, 260));

        Assert.All(entries, entry => Assert.Equal(expected.CardWidth, entry.CardWidth));
    }

    private static List<LauncherEntryViewModel> Entries(MainWindowViewModel owner, params string[] ids)
    {
        return ids
            .Select(id => CreateEntryViewModel(owner, id))
            .ToList();
    }
}
