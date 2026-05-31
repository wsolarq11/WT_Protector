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
        LauncherSettingsStore.Save(CreateDragSettings(), settings);

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
        Assert.Equal(DefaultDragOrder(), LauncherSettingsStore.Load(settings).CustomEntryOrder);

        viewModel.CancelDragReorder();

        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        viewModel.CommitDragReorder();

        Assert.Equal(ReorderedDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(ReorderedDragOrder(), LauncherSettingsStore.Load(settings).CustomEntryOrder);
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
        Directory.CreateDirectory(settings);

        viewModel.CommitDragReorder();

        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Contains("设置保存失败", viewModel.StatusText);
        AssertNoStoreTempFiles(settings);
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
        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);

        viewModel.BeginDragReorder("three");
        Assert.False(viewModel.PreviewMoveEntry("three", "missing", insertAfter: false));

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
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

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
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

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), LauncherSettingsStore.Load(settings).CustomEntryOrder);
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

        Assert.Equal(DefaultDragOrder(), launcherSettings.CustomEntryOrder);
        Assert.Equal(DefaultDragOrder(), viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        AssertNoDragCues(viewModel);
    }

    [Fact]
    public void DragPreviewMatchesConfigEntryIdsAfterNormalization()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry(" one ", "One", command: "codex"),
            Entry(" two ", "Two", command: "opencode"),
            Entry(" three ", "Three"));
        var launcherSettings = new LauncherSettings
        {
            SortMode = LauncherEntryVisibility.CustomSortModeId,
            CustomEntryOrder = [" one ", " two ", " three "],
        };
        var viewModel = CreateViewModel(config, settings, launcherSettings);

        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));

        Assert.Equal(["three", "one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal(["three", "one", "two"], launcherSettings.CustomEntryOrder);
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
        Assert.DoesNotContain(
            typeof(LauncherDragReorderWorkflow).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
            field => field.FieldType == typeof(Action));
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

    private static (string Config, string Settings, string Presets) CreateDragFixture(TempDirectory temp) =>
        CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex"),
            Entry("two", "Two", command: "opencode"),
            Entry("three", "Three"));

    private static LauncherSettings CreateDragSettings() => new()
    {
        SortMode = LauncherEntryVisibility.CustomSortModeId,
        CustomEntryOrder = [.. DefaultDragOrder()],
    };

    private static string[] DefaultDragOrder() => ["one", "two", "three"];

    private static string[] ReorderedDragOrder() => ["three", "one", "two"];

    private static void StartDefaultDragPreview(MainWindowViewModel viewModel, LauncherSettings launcherSettings)
    {
        viewModel.BeginDragReorder("three");
        Assert.True(viewModel.PreviewMoveEntry("three", "one", insertAfter: false));
        Assert.Equal(ReorderedDragOrder(), launcherSettings.CustomEntryOrder);
    }

    private static void AssertNoDragCues(MainWindowViewModel viewModel) =>
        Assert.DoesNotContain(viewModel.Entries, HasDragCue);

    private static bool HasDragCue(LauncherEntryViewModel entry) =>
        entry.IsDragPreviewSource
        || entry.IsDragPreviewTarget
        || entry.IsDragInsertBefore
        || entry.IsDragInsertAfter;
}
