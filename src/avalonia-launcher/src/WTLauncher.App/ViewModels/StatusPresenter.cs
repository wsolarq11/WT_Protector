using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Features;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// 状态文本/干运行/预设选择/按钮文字 + 计数投影。
/// 失败半径: Zero — status 更新失败不影响程序功能。
/// </summary>
public sealed partial class StatusPresenter : ObservableObject
{
    /// <summary>Maximum number of receipt history entries to retain (FIFO).</summary>
    public const int MaxHistorySize = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusTone))]
    [NotifyPropertyChangedFor(nameof(IsStatusNeutral))]
    [NotifyPropertyChangedFor(nameof(IsStatusSuccess))]
    [NotifyPropertyChangedFor(nameof(IsStatusWarning))]
    [NotifyPropertyChangedFor(nameof(IsStatusError))]
    private LauncherStatusContent _statusContent = LauncherMainWindowText.Ready;

    /// <summary>In-memory bounded history of non-neutral status receipts.</summary>
    public ObservableCollection<string> ReceiptHistory { get; } = [];

    public string StatusText => StatusContent.Text;
    public LauncherStatusTone StatusTone => StatusContent.Tone;
    public bool IsStatusNeutral => StatusTone == LauncherStatusTone.Neutral;
    public bool IsStatusSuccess => StatusTone == LauncherStatusTone.Success;
    public bool IsStatusWarning => StatusTone == LauncherStatusTone.Warning;
    public bool IsStatusError => StatusTone == LauncherStatusTone.Error;

    [ObservableProperty]
    private string _dryRunText = LauncherMainWindowText.DryRunPlaceholder;

    [ObservableProperty]
    private string _presetSelectionStatusText = LauncherMainWindowText.PresetSelectionPlaceholder;

    [ObservableProperty]
    private string _applyPresetButtonText = LauncherPresetSelectionPreview.DefaultApplyButtonText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyEntries))]
    [NotifyPropertyChangedFor(nameof(IsFilteredEmpty))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisibleEmpty))]
    [NotifyPropertyChangedFor(nameof(IsFilteredEmpty))]
    private int _visibleCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _presetCount;

    public bool IsVisibleEmpty => VisibleCount == 0;

    /// <summary>True when the store has zero entries at all (first-run welcome).</summary>
    public bool IsEmptyEntries => TotalCount == 0;

    /// <summary>True when entries exist but none match the current filter.</summary>
    public bool IsFilteredEmpty => IsVisibleEmpty && !IsEmptyEntries;

    public void SetStatus(LauncherStatusContent? status)
    {
        if (status is not null)
        {
            StatusContent = status;
            AddToReceiptHistory(status);
        }
    }

    /// <summary>
    /// Records non-neutral status texts into the bounded receipt history.
    /// Success, Warning, and Error tones are preserved for review.
    /// </summary>
    private void AddToReceiptHistory(LauncherStatusContent status)
    {
        if (status.Tone == LauncherStatusTone.Neutral)
            return;

        ReceiptHistory.Add(status.Text);
        if (ReceiptHistory.Count > MaxHistorySize)
        {
            ReceiptHistory.RemoveAt(0);
        }
    }
}
