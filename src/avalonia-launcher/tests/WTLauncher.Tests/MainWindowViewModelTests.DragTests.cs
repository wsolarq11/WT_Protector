using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void DragPreviewReordersInMemoryAndCanBeCancelledOrCommitted()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var viewModel = CreateViewModel(
            config,
            settings,
            CreateDragSettings());
        LauncherStore.Save(LauncherStore.Load(settings) with { Settings = CreateDragSettings() }, settings, backup: false);

        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        Assert.False(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));

        Assert.Equal(ReorderedDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.True(viewModel.Entries.Single(entry => entry.Id == "three").IsDragPreviewSource);
        var target = viewModel.Entries.Single(entry => entry.Id == "one");
        Assert.True(target.IsDragPreviewTarget);
        Assert.True(target.IsDragInsertBefore);
        Assert.False(target.IsDragInsertAfter);
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(settings).Settings.CustomEntryOrder);

        viewModel.CancelDragReorder();

        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(ReorderedDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(ReorderedDragOrder(), LauncherStore.Load(settings).Settings.CustomEntryOrder);
    }

    [Fact]
    public void CommitDragReorderRollsBackWhenSettingsSaveFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        File.Delete(settings);
        Directory.CreateDirectory(settings);

        viewModel.CommitDragReorder();

        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Contains("存储保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(settings);
    }

    [Fact]
    public void MenuReorderMovesOneStepAndPersistsCustomOrderWithoutDragSession()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        LauncherStore.Save(LauncherStore.Load(settings) with { Settings = CreateDragSettings() }, settings, backup: false);

        var three = viewModel.VisibleEntries.Single(entry => entry.Id == "three");
        Assert.True(three.MoveUpCommand.CanExecute(null));

        three.MoveUpCommand.Execute(null);

        Assert.Equal(["one", "three", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["one", "three", "two"], viewModel.Settings.CustomEntryOrder);
        Assert.Equal(["one", "three", "two"], LauncherStore.Load(settings).Settings.CustomEntryOrder);
        Assert.Contains("已保存自定义排序", viewModel.StatusText);
        AssertNoDragCues(viewModel);
        Assert.False(viewModel.VisibleEntries.First().MoveUpCommand.CanExecute(null));
        Assert.False(viewModel.VisibleEntries.Last().MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void MenuReorderIsUnavailableOutsideCustomSortMode()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        launcherSettings = launcherSettings with { SortMode = LauncherEntryQuery.AlphabeticalSortModeId };
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        Assert.All(viewModel.VisibleEntries, entry =>
        {
            Assert.False(entry.MoveUpCommand.CanExecute(null));
            Assert.False(entry.MoveDownCommand.CanExecute(null));
        });
    }

    [Fact]
    public void MenuReorderRollsBackWhenSettingsSaveFails()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);
        File.Delete(settings);
        Directory.CreateDirectory(settings);

        viewModel.VisibleEntries.Single(entry => entry.Id == "three").MoveUpCommand.Execute(null);

        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Contains("存储保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(settings);
    }

    [Fact]
    public void MenuReorderDuringDragPreviewResolvesAgainstCommittedOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.Entries.Single(entry => entry.Id == "three").MoveDownCommand.Execute(null);

        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(LauncherDragReorderStatus.MoveUnavailable, viewModel.StatusText);
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void DragPreviewRejectsUnknownEntryIdsWithoutMutatingOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        viewModel.BeginDragReorder("missing");
        Assert.False(viewModel.PreviewMoveEntry("missing", "one", insertAfter: false));
        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);

        viewModel.BeginDragReorder("three");
        Assert.False(viewModel.PreviewMoveEntry("three", "missing", insertAfter: false));

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
    }

    [Fact]
    public void DragPreviewRestoresPreviousOrderWhenNextDragSourceIsUnknown()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.BeginDragReorder("missing");

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void SettingsSaveDuringDragPreviewDoesNotCommitPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.FilterText = "one";

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), LauncherStore.Load(settings).Settings.CustomEntryOrder);
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void RefreshDuringDragPreviewDiscardsPreviewOrder()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);

        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.RefreshCommand.Execute(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void DragPreviewMatchesConfigEntryIdsAfterNormalization()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex"),
            Entry("two", "Two", command: "opencode"),
            Entry("three", "Three"));
        var launcherSettings = new LauncherSettings
        {
            LastFilterText = "",
            SortMode = LauncherEntryQuery.CustomSortModeId,
            EntryViewMode = LauncherEntryViewModes.CardsId,
            RequireConfirmationForLaunches = false,
            CustomEntryOrder = ["one", "two", "three"],
        };
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["three", "one", "two"], viewModel.Settings.CustomEntryOrder);
    }

    private static (string Config, string Settings, string Presets) CreateDragFixture(TempDirectory temp) =>
        CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex"),
            Entry("two", "Two", command: "opencode"),
            Entry("three", "Three"));

    private static LauncherSettings CreateDragSettings() => new()
    {
        LastFilterText = "",
        SortMode = LauncherEntryQuery.CustomSortModeId,
        EntryViewMode = LauncherEntryViewModes.CardsId,
        RequireConfirmationForLaunches = false,
        CustomEntryOrder = [.. DefaultDragOrder()],
    };

    private static string[] DefaultDragOrder() => ["one", "two", "three"];

    private static string[] ReorderedDragOrder() => ["three", "one", "two"];

    private static void StartDefaultDragPreview(MainWindowViewModel viewModel, LauncherSettings launcherSettings)
    {
        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        Assert.Equal(ReorderedDragOrder(), viewModel.Settings.CustomEntryOrder);
    }

    [Fact]
    public void SelectVisible_DiscardsDragPreview()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateDragFixture(temp);
        var launcherSettings = CreateDragSettings();
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        StartDefaultDragPreview(viewModel, launcherSettings);

        viewModel.SelectVisibleCommand.Execute(null);

        Assert.Equal(DefaultDragOrder(), viewModel.Settings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
    }

    private static void AssertNoDragCues(MainWindowViewModel viewModel) =>
        Assert.DoesNotContain(viewModel.Entries, HasDragCue);

    private static bool HasDragCue(LauncherEntryViewModel entry) =>
        entry.IsDragPreviewSource
        || entry.IsDragPreviewTarget
        || entry.IsDragInsertBefore
        || entry.IsDragInsertAfter;
}
