using Avalonia.Controls;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowEntriesRootSelectionTests
{
    [Fact]
    public void SelectReturnsDetailsRootForDetailedListMode()
    {
        var roots = CreateRoots();

        var selected = MainWindowEntriesRootSelection.Select(
            isDetailedListMode: true,
            isNamesViewMode: false,
            roots.Details,
            roots.Names,
            roots.Cards);

        Assert.Same(roots.Details, selected);
    }

    [Fact]
    public void SelectReturnsNamesRootForNamesMode()
    {
        var roots = CreateRoots();

        var selected = MainWindowEntriesRootSelection.Select(
            isDetailedListMode: false,
            isNamesViewMode: true,
            roots.Details,
            roots.Names,
            roots.Cards);

        Assert.Same(roots.Names, selected);
    }

    [Fact]
    public void SelectReturnsCardsRootByDefault()
    {
        var roots = CreateRoots();

        var selected = MainWindowEntriesRootSelection.Select(
            isDetailedListMode: false,
            isNamesViewMode: false,
            roots.Details,
            roots.Names,
            roots.Cards);

        Assert.Same(roots.Cards, selected);
    }

    [Fact]
    public void SelectPrefersDetailedRootIfFlagsConflict()
    {
        var roots = CreateRoots();

        var selected = MainWindowEntriesRootSelection.Select(
            isDetailedListMode: true,
            isNamesViewMode: true,
            roots.Details,
            roots.Names,
            roots.Cards);

        Assert.Same(roots.Details, selected);
    }

    private static (Control Details, Control Names, Control Cards) CreateRoots() =>
        (new Border(), new Border(), new Border());
}
