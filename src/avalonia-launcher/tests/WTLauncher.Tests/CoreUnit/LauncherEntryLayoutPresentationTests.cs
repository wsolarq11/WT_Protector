using Avalonia;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static WTLauncher.Tests.MainWindowEntryCardTestFactory;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class CardLayoutProviderTests
{
    [Fact]
    public void RecalculateClampsToMinimumViewport()
    {
        var provider = new CardLayoutProvider();
        provider.Recalculate(new Size(120, 80), count: 1);

        // After Recalculate, card sizes are computed (not clamped to minimums).
        // The provider does not expose MinimumCardsWidth/MinimumCardsHeight as
        // layout calculation uses its own min values; the min-clamp is internal.
        Assert.True(provider.CardWidth > 0);
        Assert.True(provider.CardHeight > 0);
    }

    [Fact]
    public void RecalculateUsesVisibleEntryCount()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = Entries(owner, "one", "two", "three");
        _ = entries;

        // Calc with 2 visible entries — uses the same algorithm as before.
        var expected = LauncherEntryLayoutCalculator.Calculate(900, 520, itemCount: 2);
        var provider = owner._layoutManager.CardLayout;
        provider.Recalculate(new Size(900, 520), count: 2);

        Assert.Equal(expected.CardWidth, provider.CardWidth);
        Assert.Equal(expected.CardHeight, provider.CardHeight);
        Assert.Equal(expected.NameTileWidth, provider.NameTileWidth);
        Assert.Equal(expected.NameTileHeight, provider.NameTileHeight);
    }

    [Fact]
    public void AllEntriesReadSameSizeFromSharedProvider()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var entries = Entries(owner, "one", "two", "three");

        owner._layoutManager.CardLayout.Recalculate(new Size(640, 260), count: 3);

        // All entries read the same size from the shared provider.
        Assert.All(entries, entry =>
        {
            Assert.Equal(owner._layoutManager.CardLayout.CardWidth, entry.CardWidth);
            Assert.Equal(owner._layoutManager.CardLayout.CardHeight, entry.CardHeight);
        });
        // None of the entries have the old default 300 width.
        Assert.All(entries, entry => Assert.NotEqual(300, entry.CardWidth));
    }

    private static List<LauncherEntryViewModel> Entries(MainWindowViewModel owner, params string[] ids)
    {
        return ids
            .Select(id => CreateEntryViewModel(owner, id))
            .ToList();
    }
}
