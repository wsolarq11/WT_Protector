using Avalonia;
using Avalonia.Controls;
using WTLauncher.App.Views;
using static WTLauncher.Tests.MainWindowEntryCardTestFactory;

namespace WTLauncher.Tests;

public sealed class MainWindowEntryCardBoundsCaptureTests
{
    [Fact]
    public void CaptureRecordsTranslatedBoundsForEntryCardsOnly()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var firstEntry = CreateEntryViewModel(owner, "one");
        var secondEntry = CreateEntryViewModel(owner, "two");
        var first = Card(firstEntry);
        var second = Card(secondEntry);
        var ignoredWithoutCardClass = new Border { DataContext = CreateEntryViewModel(owner, "not-card") };
        var ignoredWithoutEntry = new Border { Classes = { "card" }, DataContext = new object() };
        var ignoredWithoutOrigin = Card(CreateEntryViewModel(owner, "no-origin"));
        var origins = new Dictionary<Border, Point>
        {
            [first] = new(10, 20),
            [second] = new(30, 40),
            [ignoredWithoutCardClass] = new(50, 60),
            [ignoredWithoutEntry] = new(70, 80),
        };
        var sizes = new Dictionary<Border, Size>
        {
            [first] = new(100, 60),
            [second] = new(120, 80),
            [ignoredWithoutCardClass] = new(140, 90),
            [ignoredWithoutEntry] = new(160, 100),
            [ignoredWithoutOrigin] = new(180, 110),
        };

        var captured = MainWindowEntryCardBoundsCapture.Capture(
            [first, second, ignoredWithoutCardClass, ignoredWithoutEntry, ignoredWithoutOrigin],
            card => origins.TryGetValue(card, out var origin) ? origin : null,
            card => sizes[card]);

        Assert.Equal(["one", "two"], captured.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(new Rect(new Point(10, 20), new Size(100, 60)), captured["one"]);
        Assert.Equal(new Rect(new Point(30, 40), new Size(120, 80)), captured["two"]);
    }

    [Fact]
    public void CaptureUsesOrdinalDictionaryForEntryIds()
    {
        using var temp = new TempDirectory();
        var owner = CreateOwner(temp.Path);
        var lower = Card(CreateEntryViewModel(owner, "entry"));
        var upper = Card(CreateEntryViewModel(owner, "ENTRY"));

        var captured = MainWindowEntryCardBoundsCapture.Capture(
            [lower, upper],
            _ => new Point(1, 2),
            _ => new Size(3, 4));

        Assert.True(captured.ContainsKey("entry"));
        Assert.True(captured.ContainsKey("ENTRY"));
        Assert.Equal(2, captured.Count);
    }

}
