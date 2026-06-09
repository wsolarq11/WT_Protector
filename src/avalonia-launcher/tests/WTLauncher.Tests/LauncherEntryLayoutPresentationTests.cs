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

        Assert.Equal(LauncherEntryLayoutPresentation.MinimumCardsWidth, size.Width);
        Assert.Equal(LauncherEntryLayoutPresentation.MinimumCardsHeight, size.Height);
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

        // Only visible entries receive layout; hidden entries keep defaults.
        foreach (var entry in visibleEntries)
        {
            Assert.Equal(expected.CardWidth, entry.CardWidth);
            Assert.Equal(expected.CardHeight, entry.CardHeight);
            Assert.Equal(expected.NameTileWidth, entry.NameTileWidth);
            Assert.Equal(expected.NameTileHeight, entry.NameTileHeight);
        }
    }

    [Fact]
    public void ApplyCardSizeSkipsEntriesWhenNothingVisible()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = Entries(owner, "one", "two", "three");

        // When nothing is visible, layout is calculated but only applied to visible entries (none).
        LauncherEntryLayoutPresentation.ApplyCardSize(entries, [], new Size(640, 260));

        // All entries keep their default sizes.
        Assert.All(entries, entry => Assert.Equal(300, entry.CardWidth));
    }

    private static List<LauncherEntryViewModel> Entries(MainWindowViewModel owner, params string[] ids)
    {
        return ids
            .Select(id => CreateEntryViewModel(owner, id))
            .ToList();
    }
}
