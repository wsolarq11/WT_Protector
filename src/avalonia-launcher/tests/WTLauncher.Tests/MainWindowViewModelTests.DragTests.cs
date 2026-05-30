using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
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
}
