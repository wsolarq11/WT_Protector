using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    /// <summary>
    /// H5 equivalence validation: verifies that <see cref="ReorderVisibleEntryForDrag"/>
    /// (the O(n) fast path used during drag reorder) produces the same visible entry
    /// order as the full <see cref="LauncherEntryQuery.FilterAndSort{TEntry}"/>
    /// pipeline after the equivalent <see cref="LauncherEntryOrder.MoveRelative"/>
    /// mutation on the full <c>CustomEntryOrder</c>.
    ///
    /// Each test calls <see cref="MainWindowViewModel.BeginDragReorder"/> +
    /// <see cref="MainWindowViewModel.PreviewMoveEntry"/> which internally invokes
    /// the private ReorderVisibleEntryForDrag. The ground truth is computed separately
    /// via FilterAndSort using the already-updated CustomEntryOrder from the ViewModel.
    /// </summary>

    [Fact]
    public void Reorder_InsertBefore_DraggedBeforeTarget_MatchesFilterAndSort()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateEquivalenceFixture(temp);
        var viewModel = CreateEquivalenceViewModel(config, settings);

        // Drag "a" before "c" (insertAfter=false).
        // Initial: ["a","b","c","d","e"]
        // Expected: "a" moves between "b" and "c" → ["b","a","c","d","e"]
        viewModel.BeginDragReorder("a");
        Assert.True(viewModel.PreviewMoveEntry("a", "c", insertAfter: false));

        AssertReorderEquivalence(viewModel, ["b", "a", "c", "d", "e"]);
    }

    [Fact]
    public void Reorder_InsertAfter_DraggedBeforeTarget_MatchesFilterAndSort()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateEquivalenceFixture(temp);
        var viewModel = CreateEquivalenceViewModel(config, settings);

        // Drag "a" after "c" (insertAfter=true).
        // Initial: ["a","b","c","d","e"]
        // Expected: "a" moves after "c" and before "d" → ["b","c","a","d","e"]
        viewModel.BeginDragReorder("a");
        Assert.True(viewModel.PreviewMoveEntry("a", "c", insertAfter: true));

        AssertReorderEquivalence(viewModel, ["b", "c", "a", "d", "e"]);
    }

    [Fact]
    public void Reorder_InsertBefore_DraggedAfterTarget_MatchesFilterAndSort()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateEquivalenceFixture(temp);
        var viewModel = CreateEquivalenceViewModel(config, settings);

        // Drag "e" before "c" (insertAfter=false).
        // Initial: ["a","b","c","d","e"]
        // Expected: "e" moves before "c" → ["a","b","e","c","d"]
        viewModel.BeginDragReorder("e");
        Assert.True(viewModel.PreviewMoveEntry("e", "c", insertAfter: false));

        AssertReorderEquivalence(viewModel, ["a", "b", "e", "c", "d"]);
    }

    [Fact]
    public void Reorder_InsertAfter_DraggedAfterTarget_MatchesFilterAndSort()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateEquivalenceFixture(temp);
        var viewModel = CreateEquivalenceViewModel(config, settings);

        // Drag "e" after "c" (insertAfter=true).
        // Initial: ["a","b","c","d","e"]
        // Expected: "e" moves after "c" → ["a","b","c","e","d"]
        viewModel.BeginDragReorder("e");
        Assert.True(viewModel.PreviewMoveEntry("e", "c", insertAfter: true));

        AssertReorderEquivalence(viewModel, ["a", "b", "c", "e", "d"]);
    }

    [Fact]
    public void Reorder_SameDraggedAndTarget_IsNoOp()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateEquivalenceFixture(temp);
        var viewModel = CreateEquivalenceViewModel(config, settings);

        // Drag "c" onto itself — PreviewMoveEntry returns false, no mutation.
        viewModel.BeginDragReorder("c");
        Assert.False(viewModel.PreviewMoveEntry("c", "c", insertAfter: false));

        // Both fast path and ground truth should preserve original order.
        AssertNotReordered(viewModel, ["a", "b", "c", "d", "e"]);
    }

    [Fact]
    public void Reorder_WithFilter_OnlyReordersVisibleEntries_MatchesFilterAndSort()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateFilterEquivalenceFixture(temp);
        var viewModel = CreateEquivalenceViewModel(config, settings);

        // Apply filter BEFORE drag: only entries with "Red" in name are visible.
        // Names: Red Apple (a), Green Banana (b), Red Cherry (c),
        //        Green Date (d), Red Elderberry (e).
        // Visible: a, c, e  (in custom-order positions 0, 2, 4).
        viewModel.FilterText = "Red";
        Assert.Equal(["a", "c", "e"], viewModel.VisibleEntries.Select(e => e.Id));

        // Drag "a" before "e" (insertAfter=false).
        // Fast path: within [a,c,e], move a(idx=0) before e(idx=2) → [c,a,e]
        // Ground truth: MoveRelative removes "a", inserts before "e" in full order,
        //   yielding ["b","c","a","d","e"]. Then FilterAndSort with "Red":
        //   b=no-match, c=yes, a=yes, d=no-match, e=yes → [c,a,e]
        viewModel.BeginDragReorder("a");
        Assert.True(viewModel.PreviewMoveEntry("a", "e", insertAfter: false));

        AssertFilteredReorderEquivalence(viewModel, "Red", ["c", "a", "e"]);
    }

    [Fact]
    public void Reorder_WithFilter_ReverseDirection_MatchesFilterAndSort()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreateFilterEquivalenceFixture(temp);
        var viewModel = CreateEquivalenceViewModel(config, settings);

        viewModel.FilterText = "Red";
        Assert.Equal(["a", "c", "e"], viewModel.VisibleEntries.Select(e => e.Id));

        // Drag "e" after "a" (insertAfter=true).
        // Fast path: within [a,c,e], move e(idx=2) after a(idx=0) → [a,e,c]
        // Ground truth: MoveRelative removes "e", inserts after "a" in full order,
        //   yielding ["a","e","b","c","d"]. Then FilterAndSort with "Red":
        //   a=yes, e=yes, b=no, c=yes, d=no → [a,e,c]
        viewModel.BeginDragReorder("e");
        Assert.True(viewModel.PreviewMoveEntry("e", "a", insertAfter: true));

        AssertFilteredReorderEquivalence(viewModel, "Red", ["a", "e", "c"]);
    }

    // -----------------------------------------------------------------------
    // Assertion helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Compares the fast-path result (ReorderVisibleEntryForDrag via
    /// PreviewMoveEntry) against the ground truth (FilterAndSort with
    /// the already-updated CustomEntryOrder from MoveRelative).
    /// Also asserts the hardcoded expected order for regression safety.
    /// </summary>
    private static void AssertReorderEquivalence(
        MainWindowViewModel viewModel,
        string[] expectedOrder)
    {
        var fastPath = viewModel.VisibleEntries.Select(e => e.Id).ToArray();

        // Ground truth: FilterAndSort on the full entry set with the
        // updated CustomEntryOrder (already mutated by MoveRelative
        // inside the drag workflow).
        var expected = LauncherEntryQuery.FilterAndSort(
            viewModel.Entries,
            viewModel.FilterText,
            LauncherEntryQuery.CustomSortModeId,
            viewModel.Settings.CustomEntryOrder,
            e => e.SnapshotEntry());

        var groundTruth = expected.Select(e => e.Id).ToArray();
        Assert.Equal(expectedOrder, fastPath);
        Assert.Equal(groundTruth, fastPath);
    }

    /// <summary>
    /// Same as <see cref="AssertReorderEquivalence"/> but accepts an explicit
    /// filterText for the ground-truth computation (the ViewModel's FilterText
    /// property is already correct, but this documents the intent).
    /// </summary>
    private static void AssertFilteredReorderEquivalence(
        MainWindowViewModel viewModel,
        string filterText,
        string[] expectedOrder)
    {
        var fastPath = viewModel.VisibleEntries.Select(e => e.Id).ToArray();

        var expected = LauncherEntryQuery.FilterAndSort(
            viewModel.Entries,
            filterText,
            LauncherEntryQuery.CustomSortModeId,
            viewModel.Settings.CustomEntryOrder,
            e => e.SnapshotEntry());

        var groundTruth = expected.Select(e => e.Id).ToArray();
        Assert.Equal(expectedOrder, fastPath);
        Assert.Equal(groundTruth, fastPath);
    }

    /// <summary>
    /// Asserts that the visible entries match the given order and that
    /// FilterAndSort agrees (no-op drag case).
    /// </summary>
    private static void AssertNotReordered(
        MainWindowViewModel viewModel,
        string[] expectedOrder)
    {
        var fastPath = viewModel.VisibleEntries.Select(e => e.Id).ToArray();
        Assert.Equal(expectedOrder, fastPath);

        var groundTruth = LauncherEntryQuery.FilterAndSort(
            viewModel.Entries,
            viewModel.FilterText,
            LauncherEntryQuery.CustomSortModeId,
            viewModel.Settings.CustomEntryOrder,
            e => e.SnapshotEntry())
            .Select(e => e.Id)
            .ToArray();
        Assert.Equal(fastPath, groundTruth);
    }

    // -----------------------------------------------------------------------
    // Fixture factories
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates 5 entries named "Entry A" through "Entry E" with IDs a-e.
    /// All names are distinct and all contain the word "Entry" for uniform
    /// filter testing.
    /// </summary>
    private static (string Config, string Settings, string Presets) CreateEquivalenceFixture(
        TempDirectory temp) =>
        CreatePortableFixture(
            temp,
            Entry("a", "Entry A", command: "pwsh"),
            Entry("b", "Entry B", command: "pwsh"),
            Entry("c", "Entry C", command: "pwsh"),
            Entry("d", "Entry D", command: "pwsh"),
            Entry("e", "Entry E", command: "pwsh"));

    /// <summary>
    /// Creates 5 entries with "Red" and "Green" name prefixes so that
    /// filtering by "Red" produces a subset (a, c, e) while filtering
    /// by "Green" produces another (b, d).
    /// </summary>
    private static (string Config, string Settings, string Presets) CreateFilterEquivalenceFixture(
        TempDirectory temp) =>
        CreatePortableFixture(
            temp,
            Entry("a", "Red Apple", command: "pwsh"),
            Entry("b", "Green Banana", command: "pwsh"),
            Entry("c", "Red Cherry", command: "pwsh"),
            Entry("d", "Green Date", command: "pwsh"),
            Entry("e", "Red Elderberry", command: "pwsh"));

    /// <summary>
    /// Creates a ViewModel in custom-sort mode with known initial order.
    /// </summary>
    private static MainWindowViewModel CreateEquivalenceViewModel(
        string config,
        string settings)
    {
        var launcherSettings = new LauncherSettings
        {
            LastFilterText = "",
            SortMode = LauncherEntryQuery.CustomSortModeId,
            EntryViewMode = LauncherEntryViewModes.CardsId,
            RequireConfirmationForLaunches = false,
            CustomEntryOrder = ["a", "b", "c", "d", "e"],
        };
        return CreateViewModel(config, settings, launcherSettings);
    }
}
