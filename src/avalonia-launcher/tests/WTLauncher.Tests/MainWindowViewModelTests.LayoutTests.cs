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
        var (config, settings, _) = CreateSingleEntryFixture(temp);

        var viewModel = CreateViewModel(config, settings);
        var entry = Assert.Single(viewModel.Entries);

        // L36: 5px hysteresis — must be below threshold by 5px to trigger denser layout.
        entry.CardWidth = LauncherEntryViewModel.MicroLayoutMaxWidth - 5;
        entry.CardHeight = LauncherEntryViewModel.MicroLayoutMaxHeight - 5;
        Assert.True(entry.IsMicroLayout);
        Assert.False(entry.IsCompactLayout);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.MicroTitleFontSize, entry.TitleFontSize);

        entry.CardWidth = LauncherEntryViewModel.CompactLayoutMaxWidth - 5;
        entry.CardHeight = LauncherEntryViewModel.CompactLayoutMaxHeight - 5;
        Assert.False(entry.IsMicroLayout);
        Assert.True(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.CompactTitleFontSize, entry.TitleFontSize);

        entry.CardWidth = LauncherEntryViewModel.CompactLayoutMaxWidth + 1;
        entry.CardHeight = LauncherEntryViewModel.CompactLayoutMaxHeight + 1;
        Assert.False(entry.IsMicroLayout);
        Assert.False(entry.IsCompactLayout);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.Equal(LauncherEntryViewModel.NormalTitleFontSize, entry.TitleFontSize);
    }

    [Fact]
    public void CardSizingShrinksButtonsToFitVisibleEntriesInsideWindowRegion()
    {
        using var temp = new TempDirectory();
        var entries = NumberedEntries(6);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, settings);
        const double availableWidth = 640;
        const double availableHeight = 260;

        viewModel.UpdateCardsAvailableSize(new Size(availableWidth, availableHeight));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.True(first.CardWidth < 300, "expected card width to shrink below the default width");
        Assert.True(first.CardHeight < 164, "expected card height to shrink below the default height");
        Assert.All(viewModel.VisibleEntries, entry =>
        {
            Assert.Equal(first.CardWidth, entry.CardWidth);
            Assert.Equal(first.CardHeight, entry.CardHeight);
        });

        var usage = MeasureLayoutGridUsage(
            viewModel.VisibleEntries.Count,
            availableWidth,
            first.CardWidth,
            first.CardHeight,
            columnGap: LauncherEntryLayoutCalculator.CardGap,
            rowGap: LauncherEntryLayoutCalculator.CardGap);
        Assert.True(usage.UsedWidth <= availableWidth + 0.001, $"expected cards to fit available width, got {usage.UsedWidth}");
        Assert.True(usage.UsedHeight <= availableHeight + 0.001, $"expected cards to fit available height, got {usage.UsedHeight}");
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
        var actualRatio = first.CardWidth / first.CardHeight;
        Assert.InRange(
            actualRatio,
            LauncherEntryLayoutCalculator.CardGoldenRatio - 0.02,
            LauncherEntryLayoutCalculator.CardGoldenRatio + 0.02);
    }

    [Fact]
    public void NameWallSizingUsesAdaptiveMultiColumnTiles()
    {
        using var temp = new TempDirectory();
        var entries = NumberedEntries(19);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, settings);
        const double availableWidth = 892;
        const double availableHeight = 330;

        viewModel.UpdateCardsAvailableSize(new Size(availableWidth, availableHeight));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.Equal(LauncherEntryLayoutCalculator.NameTileHeight, first.NameTileHeight);
        Assert.InRange(
            first.NameTileWidth,
            LauncherEntryLayoutCalculator.NameTileMinWidth,
            LauncherEntryLayoutCalculator.NameTilePreferredMaxWidth);

        var usage = MeasureLayoutGridUsage(
            viewModel.VisibleEntries.Count,
            availableWidth,
            first.NameTileWidth,
            first.NameTileHeight,
            columnGap: LauncherEntryLayoutCalculator.NameTileGap,
            rowGap: LauncherEntryLayoutCalculator.NameTileRowGap);
        Assert.True(usage.Columns >= 4, $"expected name wall to adapt to multiple columns, got {usage.Columns}");
        Assert.True(usage.UsedHeight <= availableHeight + 0.001, $"expected name wall to fit all 19 root-scale items in the available height, got {usage.UsedHeight}");
    }

    [Fact]
    public void CardSizingPrefersScrollFallbackBeforeShrinkingBelowReadableMinimum()
    {
        using var temp = new TempDirectory();
        var entries = NumberedEntries(19);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var viewModel = CreateViewModel(config, settings);
        const double availableWidth = 892;
        const double availableHeight = 330;

        viewModel.UpdateCardsAvailableSize(new Size(availableWidth, availableHeight));

        var first = Assert.IsType<LauncherEntryViewModel>(viewModel.VisibleEntries.First());
        Assert.True(
            first.CardHeight >= LauncherEntryLayoutCalculator.CardMinHeight,
            "expected card height to stop shrinking before actions become unreachable; scrolling is the fallback");

        var usage = MeasureLayoutGridUsage(
            viewModel.VisibleEntries.Count,
            availableWidth,
            first.CardWidth,
            first.CardHeight,
            columnGap: LauncherEntryLayoutCalculator.CardGap,
            rowGap: LauncherEntryLayoutCalculator.CardGap);
        Assert.True(usage.UsedWidth <= availableWidth + 0.001, $"expected cards to fit minimum-window width, got {usage.UsedWidth}");
        Assert.True(usage.UsedHeight > availableHeight + 0.001, $"expected min-window height to overflow so the ScrollViewer can act as fallback, got {usage.UsedHeight}");
    }

    [Fact]
    public async Task ApplyCardSize_RetriggersAfterAddingEntrySoAllCardsShareUniformSize()
    {
        // ponytail: 07-10 button shape bug 根因 —— AddEntry / CloneEntry / DiscoverAgents
        // 添加新 entry 后, ApplyFilter 的优化条件 (_cardsAvailableSize != _lastAppliedCardsAvailableSize)
        // 忽略了 count 变化, 导致新 entry 用默认 CardWidth=300/CardHeight=164, 其他 entries
        // 用旧 layout 平分值 → 卡片大小不一致。修复: 条件加 || _visibleEntries.Count != _lastAppliedVisibleCount。
        using var temp = new TempDirectory();
        var entries = NumberedEntries(6);
        var (config, settings, _) = CreatePortableFixture(temp, entries);
        var dialogService = new StubDialogService
        {
            OnDiscoverAgents = _ => Task.FromResult<IReadOnlyList<DiscoveredAgent>?>([
                SampleDiscoveredAgent("claude", "Claude Code", "Anthropic"),
                SampleDiscoveredAgent("aider", "Aider", "community"),
            ]),
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialogService);
        viewModel.UpdateCardsAvailableSize(new Size(640, 260));

        // Original entries got layout-computed sizes (NOT the 300x164 default).
        var originalWidth = viewModel.Entries[0].CardWidth;
        var originalHeight = viewModel.Entries[0].CardHeight;
        Assert.True(originalWidth < 300, "layout-computed width should be below default");
        Assert.True(originalHeight < 164, "layout-computed height should be below default");

        await viewModel.DiscoverAgentsCommand.ExecuteAsync(null);

        // After adding 2 entries (6 → 8), all 8 entries must share the SAME CardWidth/CardHeight.
        // The bug: new entries stayed at default 300x164 while originals kept old layout.
        Assert.Equal(8, viewModel.Entries.Count);
        // Specifically: post-add layout WILL differ from pre-add (count changed → recompute),
        // but ALL entries must agree on the new value.
        var firstWidth = viewModel.Entries[0].CardWidth;
        var firstHeight = viewModel.Entries[0].CardHeight;
        Assert.All(viewModel.Entries, entry =>
        {
            Assert.Equal(firstWidth, entry.CardWidth);
            Assert.Equal(firstHeight, entry.CardHeight);
        });
        // And the new value must be the layout-computed one, NOT the 300x164 default.
        Assert.True(firstWidth < 300, "post-add width must be layout-computed, not the 300 default");
        Assert.True(firstHeight < 164, "post-add height must be layout-computed, not the 164 default");
    }

    private static LayoutGridUsage MeasureLayoutGridUsage(
        int itemCount,
        double availableWidth,
        double itemWidth,
        double itemHeight,
        double columnGap,
        double rowGap)
    {
        var columns = Math.Max(1, (int)Math.Floor((availableWidth + columnGap) / (itemWidth + columnGap)));
        var rows = (int)Math.Ceiling(itemCount / (double)columns);
        return new LayoutGridUsage(
            columns,
            (columns * itemWidth) + ((columns - 1) * columnGap),
            (rows * itemHeight) + ((rows - 1) * rowGap));
    }

    private readonly record struct LayoutGridUsage(int Columns, double UsedWidth, double UsedHeight);
}
