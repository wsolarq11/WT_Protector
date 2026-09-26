using Avalonia;
using WTLauncher.App.ViewModels;

using WTLauncher.Tests;
namespace WTLauncher.Tests.CoreUnit;

[Trait("Category", TestCategories.CoreUnit)]
public sealed class CardLayoutProviderTests
{
    [Fact]
    public void AllEntriesReadSameSizeFromSharedProvider()
    {
        using var temp = new TempDirectory();
        var owner = MainWindowEntryCardTestFactory.CreateOwner(temp.Path);
        var entries = new[]
        {
            MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "one"),
            MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "two"),
            MainWindowEntryCardTestFactory.CreateEntryViewModel(owner, "three"),
        };

        owner._layoutManager.CardLayout.Recalculate(new Size(640, 260), count: 3);

        Assert.All(entries, entry =>
        {
            Assert.Equal(owner._layoutManager.CardLayout.CardWidth, entry.CardWidth);
            Assert.Equal(owner._layoutManager.CardLayout.CardHeight, entry.CardHeight);
        });
        Assert.All(entries, entry => Assert.NotEqual(300, entry.CardWidth));
    }
}
