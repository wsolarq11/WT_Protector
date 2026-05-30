using Avalonia;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void CardLayoutDensitySwitchesBetweenNormalCompactAndMicro()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            new LaunchEntry
            {
                Id = "one",
                Name = "One",
                Directory = "D:\\one",
                Command = "codex",
                Confirm = false,
            });

        var viewModel = CreateViewModel(config, settings);
        var entry = Assert.Single(viewModel.Entries);

        entry.CardWidth = 210;
        entry.CardHeight = 90;
        Assert.True(entry.IsMicroLayout);
        Assert.True(entry.IsCompactLayout);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(15, entry.TitleFontSize);

        entry.CardWidth = 240;
        entry.CardHeight = 110;
        Assert.False(entry.IsMicroLayout);
        Assert.True(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(16, entry.TitleFontSize);

        entry.CardWidth = 280;
        entry.CardHeight = 140;
        Assert.False(entry.IsMicroLayout);
        Assert.False(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.Equal(18, entry.TitleFontSize);
    }

    [Fact]
    public void CardLayoutDensityUsesOneTypedStateSource()
    {
        var densityFields = typeof(LauncherEntryViewModel)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field =>
                field.Name.Contains("Layout", StringComparison.OrdinalIgnoreCase)
                || field.Name.Contains("Density", StringComparison.OrdinalIgnoreCase)
                || field.Name.Contains("Compact", StringComparison.OrdinalIgnoreCase)
                || field.Name.Contains("Micro", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["_layoutDensity"], densityFields);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsCompactLayout))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsMicroLayout))?.SetMethod);
    }

    [Fact]
    public void CardSizingUsesOneTypedAvailableSizeStateSource()
    {
        Assert.Null(typeof(MainWindowViewModel).GetField("_cardsAvailableWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.Null(typeof(MainWindowViewModel).GetField("_cardsAvailableHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.NotNull(typeof(MainWindowViewModel).GetField("_cardsAvailableSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
        Assert.Null(typeof(MainWindowViewModel).GetMethod("RecalculateCardSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));

        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);

        viewModel.UpdateCardsAvailableSize(new Size(640, 260));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.True(first.CardWidth > 0);
        Assert.True(first.CardHeight > 0);
    }

    [Fact]
    public void CardSizingShrinksButtonsToFitVisibleEntriesInsideWindowRegion()
    {
        using var temp = new TempDirectory();
        var entries = NumberedEntries(6);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, settings);

        viewModel.UpdateCardsAvailableSize(new Size(640, 260));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.True(first.CardWidth < 300, "expected card width to shrink below the default width");
        Assert.True(first.CardHeight < 164, "expected card height to shrink below the default height");
        Assert.All(viewModel.VisibleEntries, entry =>
        {
            Assert.Equal(first.CardWidth, entry.CardWidth);
            Assert.Equal(first.CardHeight, entry.CardHeight);
        });

        const double gap = 8;
        var columns = Math.Max(1, (int)Math.Floor((640 + gap) / (first.CardWidth + gap)));
        var rows = (int)Math.Ceiling(viewModel.VisibleEntries.Count / (double)columns);
        var usedWidth = (columns * first.CardWidth) + ((columns - 1) * gap);
        var usedHeight = (rows * first.CardHeight) + ((rows - 1) * gap);
        Assert.True(usedWidth <= 640.001, $"expected cards to fit available width, got {usedWidth}");
        Assert.True(usedHeight <= 260.001, $"expected cards to fit available height, got {usedHeight}");
    }

    [Fact]
    public void CardSizingUsesGoldenRatioWhenSpaceAllows()
    {
        using var temp = new TempDirectory();
        var entries = NumberedEntries(6);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, settings);

        viewModel.UpdateCardsAvailableSize(new Size(900, 520));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        const double goldenRatio = 21d / 13d;
        var actualRatio = first.CardWidth / first.CardHeight;
        Assert.InRange(actualRatio, goldenRatio - 0.02, goldenRatio + 0.02);
    }

    [Fact]
    public void NameWallSizingUsesAdaptiveMultiColumnTiles()
    {
        using var temp = new TempDirectory();
        var entries = NumberedEntries(19);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, settings);

        viewModel.UpdateCardsAvailableSize(new Size(892, 330));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.Equal(24, first.NameTileHeight);
        Assert.InRange(first.NameTileWidth, 132, 220);

        const double gap = 6;
        const double rowGap = 4;
        var columns = Math.Max(1, (int)Math.Floor((892 + gap) / (first.NameTileWidth + gap)));
        var rows = (int)Math.Ceiling(viewModel.VisibleEntries.Count / (double)columns);
        var usedHeight = (rows * first.NameTileHeight) + ((rows - 1) * rowGap);
        Assert.True(columns >= 4, $"expected name wall to adapt to multiple columns, got {columns}");
        Assert.True(usedHeight <= 330.001, $"expected name wall to fit all 19 root-scale items in the available height, got {usedHeight}");
    }

    [Fact]
    public void CardSizingPrefersScrollFallbackBeforeShrinkingBelowReadableMinimum()
    {
        using var temp = new TempDirectory();
        var entries = NumberedEntries(19);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, settings);

        viewModel.UpdateCardsAvailableSize(new Size(892, 330));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.True(first.CardHeight >= 96, "expected card height to stop shrinking before actions become unreachable; scrolling is the fallback");

        const double gap = 8;
        var columns = Math.Max(1, (int)Math.Floor((892 + gap) / (first.CardWidth + gap)));
        var rows = (int)Math.Ceiling(viewModel.VisibleEntries.Count / (double)columns);
        var usedWidth = (columns * first.CardWidth) + ((columns - 1) * gap);
        var usedHeight = (rows * first.CardHeight) + ((rows - 1) * gap);
        Assert.True(usedWidth <= 892.001, $"expected cards to fit minimum-window width, got {usedWidth}");
        Assert.True(usedHeight > 330.001, $"expected min-window height to overflow so the ScrollViewer can act as fallback, got {usedHeight}");
    }
}
