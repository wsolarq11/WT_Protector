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
        var codeBehind = File.ReadAllText(TestRepositoryPaths.MainWindowCodeBehind);

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

        AssertInOrder(codeBehind, "ShowDragGhost(item, card", "DragDrop.DoDragDropAsync", "HideDragGhost()");
        AssertInOrder(
            codeBehind,
            "if (_activeDragEntryId is not null)",
            "LauncherDragGhostLayoutCalculator.CalculatePosition",
            "SetDragGhostPosition(ghostPosition.Left, ghostPosition.Top)");
        Assert.Contains("private void ApplyDragReorderPreview(", codeBehind);
        Assert.Equal(1, Count(codeBehind, "viewModel.PreviewMoveEntry("));
        Assert.Contains("if (viewModel.PreviewMoveEntry", codeBehind);
        AssertInOrder(codeBehind, "CaptureEntryCardBounds()", "ScheduleReorderAnimation(before)");
        Assert.DoesNotContain("_pendingReorderAnimationAttached", codeBehind);
        Assert.DoesNotContain("_pendingReorderAnimationSnapshot", codeBehind);
        AssertInOrder(
            codeBehind,
            "_reorderAnimationScheduler.Schedule(",
            "handler => root.LayoutUpdated -= handler",
            "handler => root.LayoutUpdated += handler");
        Assert.Contains("_reorderAnimationScheduler.Consume(", codeBehind);
        Assert.Equal(1, Count(codeBehind, "handler => root.LayoutUpdated += handler"));
        Assert.Equal(2, Count(codeBehind, "handler => root.LayoutUpdated -= handler"));
        Assert.Contains("FLIP: jump back without transition", codeBehind);
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
