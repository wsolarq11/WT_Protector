using Avalonia;
using Avalonia.Controls;
using WTLauncher.App.Views;
using static WTLauncher.Tests.MainWindowEntryCardTestFactory;

namespace WTLauncher.Tests;

public sealed class MainWindowReorderFlipAnimationTests
{
    [Fact]
    public void FindTargetsReturnsOnlyEntryCardsWithMeaningfulMovement()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var moved = Card(CreateEntryViewModel(owner, "moved"));
        var still = Card(CreateEntryViewModel(owner, "still"));
        var missingAfter = Card(CreateEntryViewModel(owner, "missing-after"));
        var notCard = new Border { DataContext = CreateEntryViewModel(owner, "not-card") };
        var notEntry = new Border { Classes = { "card" }, DataContext = new object() };
        var before = new Dictionary<string, Rect>
        {
            ["moved"] = Rect(20, 40),
            ["still"] = Rect(70, 90),
            ["missing-after"] = Rect(10, 10),
            ["not-card"] = Rect(100, 100),
        };
        var after = new Dictionary<string, Rect>
        {
            ["moved"] = Rect(5, 60),
            ["still"] = Rect(70.25, 90.25),
            ["not-card"] = Rect(50, 50),
        };

        var targets = MainWindowReorderFlipAnimation.FindTargets(
            [moved, still, missingAfter, notCard, notEntry],
            before,
            after).ToArray();

        var target = Assert.Single(targets);
        Assert.Same(moved, target.Card);
        Assert.Equal("moved", target.EntryId);
        Assert.Equal(15, target.Offset.X);
        Assert.Equal(-20, target.Offset.Y);
    }

    [Fact]
    public void ApplyStartSetsTransientTransformWithoutUsingTransitions()
    {
        var card = new Border();

        MainWindowReorderFlipAnimation.ApplyStart(card, new MainWindowReorderFlipOffset(12, -8));

        Assert.NotNull(card.Transitions);
        Assert.NotNull(card.RenderTransform);
    }

    [Fact]
    public void TryApplyFinishResetsOnlyWhenVisible()
    {
        var card = new Border();
        MainWindowReorderFlipAnimation.ApplyStart(card, new MainWindowReorderFlipOffset(12, -8));

        var resetHidden = MainWindowReorderFlipAnimation.TryApplyFinish(card, isEffectivelyVisible: false);

        Assert.False(resetHidden);
        Assert.NotNull(card.Transitions);

        var resetVisible = MainWindowReorderFlipAnimation.TryApplyFinish(card, isEffectivelyVisible: true);

        Assert.True(resetVisible);
        Assert.Null(card.Transitions);
        Assert.NotNull(card.RenderTransform);
    }

    private static Rect Rect(double x, double y)
    {
        return new Rect(x, y, 100, 60);
    }
}
