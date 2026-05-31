namespace WTLauncher.Tests;

public sealed class MainWindowXamlContractTests
{
    [Fact]
    public void MainWindow_UsesSingleFourBoxControlGrid()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);
        var gridStart = xaml.IndexOf("x:Name=\"ControlBoxesGrid\"", StringComparison.Ordinal);
        Assert.True(gridStart >= 0, "MainWindow must keep search/sort/view/preset controls in one named parent Grid.");

        var nextRowStart = xaml.IndexOf("<StackPanel Grid.Row=\"3\"", gridStart, StringComparison.Ordinal);
        Assert.True(nextRowStart > gridStart, "ControlBoxesGrid should be the complete row-2 control band before action row 3.");

        var controlGrid = xaml[gridStart..nextRowStart];
        Assert.Contains("ColumnDefinitions=\"2.2*,1.1*,1.1*,2.2*\"", controlGrid);
        Assert.Equal(4, Count(controlGrid, "Classes=\"control-box\""));
        AssertInOrder(controlGrid, "Text=\"搜索\"", "Text=\"排序\"", "Text=\"视图\"", "Text=\"启动组\"");
        Assert.Contains("Text=\"{Binding FilterText, Mode=TwoWay}\"", controlGrid);
        Assert.Contains("ItemsSource=\"{Binding SortModes}\"", controlGrid);
        Assert.Contains("ItemsSource=\"{Binding DisplayModes}\"", controlGrid);
        Assert.Contains("ItemsSource=\"{Binding Presets}\"", controlGrid);
        Assert.Contains("Content=\"{Binding ApplyPresetButtonText}\"", controlGrid);
    }

    [Fact]
    public void MainWindow_DoesNotRenderUpdatedTimeInEntryViews()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);

        Assert.DoesNotContain("UpdatedLabel", xaml);
        Assert.DoesNotContain("ShowUpdatedLabel", xaml);
        Assert.DoesNotContain("更新时间", xaml);
    }

    [Fact]
    public void MainWindow_DragReorderKeepsGhostFlipAndInsertPreviewContracts()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);
        var dragCodeBehind = File.ReadAllText(TestRepositoryPaths.MainWindowDragCodeBehind);

        Assert.Equal(3, Count(xaml, "Classes.drag-target=\"{Binding IsDragPreviewTarget}\""));
        Assert.Equal(3, Count(xaml, "Classes.drag-source=\"{Binding IsDragPreviewSource}\""));
        Assert.Equal(3, Count(xaml, "IsVisible=\"{Binding IsDragInsertBefore}\""));
        Assert.Equal(3, Count(xaml, "IsVisible=\"{Binding IsDragInsertAfter}\""));
        Assert.Equal(6, Count(xaml, "Classes=\"drag-insert\""));
        Assert.Equal(1, Count(xaml, "<Style Selector=\"Border.drag-insert\">"));
        AssertInOrder(
            xaml,
            "<Style Selector=\"Border.drag-insert\">",
            "<Setter Property=\"CornerRadius\" Value=\"2\"/>",
            "<Setter Property=\"Background\" Value=\"#38BDF8\"/>",
            "<Setter Property=\"Opacity\" Value=\"0.9\"/>",
            "<Setter Property=\"IsHitTestVisible\" Value=\"False\"/>");

        AssertInOrder(
            dragCodeBehind,
            "ShowDragGhost(item, card, e);",
            "DragDrop.DoDragDropAsync",
            "finally",
            "HideDragGhost();",
            "FinishDragReorder(viewModel, effect);",
            "_activeDragEntryId = null;");
        AssertInOrder(
            dragCodeBehind,
            "private void FinishDragReorder(",
            "if (_activeDragEntryId is null)",
            "return;",
            "viewModel.CommitDragReorder();",
            "viewModel.CancelDragReorder();");
        AssertInOrder(
            dragCodeBehind,
            "private void ShowDragGhost(",
            "_overlayLayer ??= OverlayLayer.GetOverlayLayer(this);",
            "MainWindowDragGhostFactory.Create(",
            "_overlayLayer.Children.Add(_dragGhost)",
            "SetDragGhostPosition(_dragGhost, ghost.Left, ghost.Top)");
        AssertInOrder(
            dragCodeBehind,
            "private void MoveDragGhost(",
            "LauncherDragGhostLayoutCalculator.CalculatePosition",
            "SetDragGhostPosition(_dragGhost, ghostPosition.Left, ghostPosition.Top)");
        AssertInOrder(
            dragCodeBehind,
            "private void HideDragGhost(",
            "_overlayLayer.Children.Remove(_dragGhost)",
            "_dragGhost = null");
        Assert.Contains("private void ApplyDragReorderPreview(", dragCodeBehind);
        Assert.Equal(1, Count(dragCodeBehind, "viewModel.PreviewMoveEntry("));
        Assert.Contains("if (viewModel.PreviewMoveEntry", dragCodeBehind);
        AssertInOrder(
            dragCodeBehind,
            "var root = MainWindowEntriesRootSelection.Select(",
            "MainWindowEntryCardBoundsCapture.Capture(root, this)",
            "if (viewModel.PreviewMoveEntry",
            "_reorderAnimationScheduler.Schedule(");
        Assert.DoesNotContain("_pendingReorderAnimationAttached", dragCodeBehind);
        Assert.DoesNotContain("_pendingReorderAnimationSnapshot", dragCodeBehind);
        AssertInOrder(
            dragCodeBehind,
            "_reorderAnimationScheduler.Schedule(",
            "handler => root.LayoutUpdated -= handler",
            "handler => root.LayoutUpdated += handler");
        Assert.Contains("_reorderAnimationScheduler.Consume(", dragCodeBehind);
        Assert.Equal(1, Count(dragCodeBehind, "handler => root.LayoutUpdated += handler"));
        Assert.Equal(2, Count(dragCodeBehind, "handler => root.LayoutUpdated -= handler"));
        Assert.Contains("FLIP: jump back without transition", dragCodeBehind);
    }

    [Fact]
    public void MainWindow_MoreMenuButtonsExposeKeyboardReachableLabel()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);
        var codeBehind = File.ReadAllText(TestRepositoryPaths.MainWindowCodeBehind);

        Assert.Equal(2, Count(xaml, "Content=\"⋯\""));
        Assert.Equal(2, Count(xaml, "Click=\"OnEntryMoreClicked\""));
        Assert.Equal(2, Count(xaml, "ToolTip.Tip=\"更多操作\""));
        Assert.Equal(2, Count(xaml, "AutomationProperties.Name=\"更多操作\""));
        AssertInOrder(codeBehind, "private void OnEntryMoreClicked", "TryOpenEntryCardContextMenu(control)", "e.Handled = true");
    }

    [Fact]
    public void MainWindow_ContextMenusExposeNonDragReorderCommands()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);

        Assert.Equal(3, Count(xaml, "Header=\"上移排序\""));
        Assert.Equal(3, Count(xaml, "Header=\"下移排序\""));
        Assert.Equal(3, Count(xaml, "Command=\"{Binding MoveUpCommand}\""));
        Assert.Equal(3, Count(xaml, "Command=\"{Binding MoveDownCommand}\""));
        AssertInOrder(
            xaml,
            "Header=\"启动\"",
            "Header=\"预演\"",
            "Header=\"上移排序\"",
            "Header=\"下移排序\"",
            "Header=\"编辑\"",
            "Header=\"删除\"");
    }

    [Fact]
    public void MainWindow_DragHandlesExposeAccessibilityLabel()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.MainWindowXaml);

        Assert.Equal(3, Count(xaml, "AutomationProperties.Name=\"拖拽排序\""));
        Assert.Equal(3, Count(xaml, "ToolTip.Tip=\"拖拽排序\""));
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void AssertInOrder(string text, params string[] values)
    {
        var last = -1;
        foreach (var value in values)
        {
            var index = text.IndexOf(value, last + 1, StringComparison.Ordinal);
            Assert.True(index > last, $"Expected '{value}' after index {last}.");
            last = index;
        }
    }

}
