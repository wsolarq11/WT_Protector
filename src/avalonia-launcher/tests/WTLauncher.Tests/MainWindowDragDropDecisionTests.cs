using Avalonia.Input;
using WTLauncher.App.Views;

namespace WTLauncher.Tests;

public sealed class MainWindowDragDropDecisionTests
{
    [Fact]
    public void EvaluateDragOverAllowsMoveAndPreviewWhenAllInputsAreReady()
    {
        var decision = MainWindowDragDropDecision.EvaluateDragOver(
            isCustomSortMode: true,
            hasResolvedTarget: true,
            EntryIdTransfer("three"),
            activeDragEntryId: "three");

        Assert.Equal(DragDropEffects.Move, decision.Effects);
        Assert.True(decision.Handled);
        Assert.False(decision.ShouldClearHover);
        Assert.Equal("three", decision.PreviewDraggedEntryId);
    }

    [Theory]
    [InlineData(false, true, "three")]
    [InlineData(true, false, "three")]
    [InlineData(true, true, null)]
    [InlineData(true, true, "")]
    [InlineData(true, true, "   ")]
    public void EvaluateDragOverRejectsMoveWhenRequiredInputsAreMissing(
        bool isCustomSortMode,
        bool hasResolvedTarget,
        string? entryId)
    {
        var transfer = entryId is null ? new DataTransfer() : EntryIdTransfer(entryId);

        var decision = MainWindowDragDropDecision.EvaluateDragOver(
            isCustomSortMode,
            hasResolvedTarget,
            transfer,
            activeDragEntryId: "three");

        Assert.Equal(DragDropEffects.None, decision.Effects);
        Assert.False(decision.Handled);
        Assert.True(decision.ShouldClearHover);
        Assert.Null(decision.PreviewDraggedEntryId);
    }

    [Fact]
    public void EvaluateDragOverAllowsDropButClearsHoverWithoutActiveDrag()
    {
        var decision = MainWindowDragDropDecision.EvaluateDragOver(
            isCustomSortMode: true,
            hasResolvedTarget: true,
            EntryIdTransfer("three"),
            activeDragEntryId: null);

        Assert.Equal(DragDropEffects.Move, decision.Effects);
        Assert.True(decision.Handled);
        Assert.True(decision.ShouldClearHover);
        Assert.Null(decision.PreviewDraggedEntryId);
    }

    [Fact]
    public void EvaluateDropAcceptsResolvedTargetWithEntryIdPayload()
    {
        var decision = MainWindowDragDropDecision.EvaluateDrop(
            hasResolvedTarget: true,
            EntryIdTransfer("three"));

        Assert.Equal(DragDropEffects.Move, decision.Effects);
        Assert.True(decision.Handled);
        Assert.Equal("three", decision.DraggedEntryId);
    }

    [Theory]
    [InlineData(false, "three")]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    public void EvaluateDropRejectsMissingTargetOrInvalidEntryId(bool hasResolvedTarget, string entryId)
    {
        var transfer = new DataTransfer();
        if (entryId.Length > 0)
            MainWindowDragEntryTransfer.SetEntryId(transfer, entryId);

        var decision = MainWindowDragDropDecision.EvaluateDrop(hasResolvedTarget, transfer);

        Assert.Equal(DragDropEffects.None, decision.Effects);
        Assert.False(decision.Handled);
        Assert.Null(decision.DraggedEntryId);
    }

    private static DataTransfer EntryIdTransfer(string entryId)
    {
        var transfer = new DataTransfer();
        MainWindowDragEntryTransfer.SetEntryId(transfer, entryId);
        return transfer;
    }
}
