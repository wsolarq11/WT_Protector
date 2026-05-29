using Avalonia;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void SavedUnknownModesFallbackToDefaultModes()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "missing", EntryViewMode = "missing" });

        Assert.Equal("custom", viewModel.SelectedSortMode?.Id);
        Assert.Equal(new[] { "卡片", "详细列表", "名称墙" }, viewModel.DisplayModes.Select(mode => mode.Label));
        Assert.Equal(LauncherEntryViewMode.Cards, viewModel.SelectedDisplayMode?.Mode);
        Assert.Equal("cards", viewModel.SelectedDisplayMode?.Id);
    }

    [Fact]
    public void SortModeUsesSelectedModeAsOnlyCustomSortState()
    {
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsCustomSortMode))?.SetMethod);
        Assert.DoesNotContain(
            typeof(MainWindowViewModel).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            field => field.Name.Contains("CustomSort", StringComparison.OrdinalIgnoreCase));

        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom" });

        Assert.True(viewModel.IsCustomSortMode);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "alphabetical");

        Assert.False(viewModel.IsCustomSortMode);
    }

    [Fact]
    public void VisibleSettingsRollBackWhenSettingsSaveFails()
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
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "Two",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
            });

        var launcherSettings = new LauncherSettings
        {
            SortMode = "custom",
            EntryViewMode = "cards",
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ["one", "two"],
        };
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        Directory.CreateDirectory(settings);

        viewModel.FilterText = "two";
        Assert.Equal("", viewModel.FilterText);
        Assert.Equal("", launcherSettings.LastFilterText);
        Assert.Equal(2, viewModel.VisibleCount);
        Assert.Contains("设置保存失败", viewModel.StatusText);

        viewModel.SelectedSortMode = viewModel.SortModes.Single(mode => mode.Id == "alphabetical");
        Assert.Equal("custom", viewModel.SelectedSortMode?.Id);
        Assert.Equal("custom", launcherSettings.SortMode);
        Assert.Equal(["one", "two"], launcherSettings.CustomEntryOrder);
        Assert.Contains("设置保存失败", viewModel.StatusText);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "details");
        Assert.Equal("cards", viewModel.SelectedDisplayMode?.Id);
        Assert.Equal("cards", launcherSettings.EntryViewMode);
        Assert.Contains("设置保存失败", viewModel.StatusText);

        viewModel.RequireConfirmationForLaunches = false;
        Assert.True(viewModel.RequireConfirmationForLaunches);
        Assert.True(launcherSettings.RequireConfirmationForLaunches);
        Assert.Contains("设置保存失败", viewModel.StatusText);
    }

    [Fact]
    public void DragPreviewReordersInMemoryAndCanBeCancelledOrCommitted()
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
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "Two",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
            },
            new LaunchEntry
            {
                Id = "three",
                Name = "Three",
                Directory = "D:\\three",
                Command = "pwsh",
                Confirm = false,
            });

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { SortMode = "custom", CustomEntryOrder = ["one", "two", "three"] });
        LauncherSettingsStore.Save(new LauncherSettings
        {
            SortMode = "custom",
            CustomEntryOrder = ["one", "two", "three"],
        }, settings);

        Assert.Equal(["one", "two", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        Assert.False(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "three").IsDragPreviewSource);
        var target = viewModel.Entries.Single(entry => entry.Id == "one");
        Assert.True(target.IsDragPreviewTarget);
        Assert.True(target.IsDragInsertBefore);
        Assert.False(target.IsDragInsertAfter);
        Assert.Equal(["one", "two", "three"], LauncherSettingsStore.Load(settings).CustomEntryOrder);

        viewModel.CancelDragReorder();

        Assert.Equal(["one", "two", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.DoesNotContain(viewModel.Entries, entry =>
            entry.IsDragPreviewSource
            || entry.IsDragPreviewTarget
            || entry.IsDragInsertBefore
            || entry.IsDragInsertAfter);

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["three", "one", "two"], LauncherSettingsStore.Load(settings).CustomEntryOrder);
    }

    [Fact]
    public void CommitDragReorderRollsBackWhenSettingsSaveFails()
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
            },
            new LaunchEntry
            {
                Id = "two",
                Name = "Two",
                Directory = "D:\\two",
                Command = "opencode",
                Confirm = false,
            },
            new LaunchEntry
            {
                Id = "three",
                Name = "Three",
                Directory = "D:\\three",
                Command = "pwsh",
                Confirm = false,
            });

        var launcherSettings = new LauncherSettings { SortMode = "custom", CustomEntryOrder = ["one", "two", "three"] };
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        Directory.CreateDirectory(settings);

        viewModel.CommitDragReorder();

        Assert.Equal(["one", "two", "three"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["one", "two", "three"], launcherSettings.CustomEntryOrder);
        Assert.Contains("设置保存失败", viewModel.StatusText);
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(settings)!, "settings.json.tmp-*"));
    }

    [Fact]
    public void DragPreviewStateUsesOneTypedWorkflowField()
    {
        var viewModelDragFields = typeof(MainWindowViewModel)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field => field.Name.Contains("Drag", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var workflowDragFields = typeof(LauncherDragReorderWorkflow)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field => field.Name.Contains("Drag", StringComparison.OrdinalIgnoreCase) || field.Name.Contains("Session", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["_dragReorderWorkflow"], viewModelDragFields);
        Assert.Equal(["_session"], workflowDragFields);
    }

    [Fact]
    public void EntryDragCueStateUsesOneTypedStateSource()
    {
        var dragFields = typeof(LauncherEntryViewModel)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(field => field.Name.Contains("Drag", StringComparison.OrdinalIgnoreCase))
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["_dragCueState"], dragFields);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragPreviewTarget))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragPreviewSource))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragInsertBefore))?.SetMethod);
        Assert.Null(typeof(LauncherEntryViewModel).GetProperty(nameof(LauncherEntryViewModel.IsDragInsertAfter))?.SetMethod);
    }

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
    public void EntryViewModePersistsAndOverridesCardDensityForDetailedScanning()
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
        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(15, entry.TitleFontSize);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "details");

        Assert.True(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.True(entry.ShowDirectory);
        Assert.True(entry.ShowCommand);
        Assert.Equal(13, entry.TitleFontSize);
        Assert.Equal("details", LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "names");

        Assert.False(viewModel.IsDetailedListMode);
        Assert.True(viewModel.IsNamesViewMode);
        Assert.False(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(13, entry.TitleFontSize);
        Assert.Equal("names", LauncherSettingsStore.Load(settings).EntryViewMode);

        viewModel.SelectedDisplayMode = viewModel.DisplayModes.Single(mode => mode.Id == "cards");

        Assert.False(viewModel.IsDetailedListMode);
        Assert.False(viewModel.IsNamesViewMode);
        Assert.True(viewModel.IsCardsViewMode);
        Assert.False(entry.ShowDirectory);
        Assert.False(entry.ShowCommand);
        Assert.Equal(15, entry.TitleFontSize);
        Assert.Equal("cards", LauncherSettingsStore.Load(settings).EntryViewMode);
    }

    [Fact]
    public void EntryViewModeUsesOneTypedStateSource()
    {
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsCardsViewMode))?.SetMethod);
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsDetailedListMode))?.SetMethod);
        Assert.Null(typeof(MainWindowViewModel).GetProperty(nameof(MainWindowViewModel.IsNamesViewMode))?.SetMethod);
        Assert.DoesNotContain(
            typeof(MainWindowViewModel).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            field => field.Name.Contains("Refreshing", StringComparison.OrdinalIgnoreCase));

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

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { EntryViewMode = "details" });

        Assert.Equal(new[] { "卡片", "详细列表", "名称墙" }, viewModel.DisplayModes.Select(mode => mode.Label));
        Assert.Equal(LauncherEntryViewMode.Details, viewModel.SelectedDisplayMode?.Mode);
        Assert.Equal("details", viewModel.SelectedDisplayMode?.Id);
        Assert.Equal(
            1,
            new[] { viewModel.IsCardsViewMode, viewModel.IsDetailedListMode, viewModel.IsNamesViewMode }.Count(value => value));
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
