using CommunityToolkit.Mvvm.ComponentModel;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// 状态文本/干运行/预设选择/按钮文字 + 计数投影。
/// 失败半径: Zero — status 更新失败不影响程序功能。
/// </summary>
public sealed partial class StatusPresenter : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusTone))]
    [NotifyPropertyChangedFor(nameof(StatusIndicator))]
    [NotifyPropertyChangedFor(nameof(IsStatusNeutral))]
    [NotifyPropertyChangedFor(nameof(IsStatusSuccess))]
    [NotifyPropertyChangedFor(nameof(IsStatusWarning))]
    [NotifyPropertyChangedFor(nameof(IsStatusError))]
    private LauncherStatusContent _statusContent = LauncherMainWindowText.Ready;

    public string StatusText => StatusContent.Text;
    public LauncherStatusTone StatusTone => StatusContent.Tone;
    public string StatusIndicator => LauncherStatusPresentation.IndicatorFor(StatusTone);
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
    [NotifyPropertyChangedFor(nameof(EmptyStateText))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisibleEmpty))]
    private int _visibleCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _presetCount;

    public bool IsVisibleEmpty => VisibleCount == 0;

    public string EmptyStateText => TotalCount == 0
        ? "欢迎使用 WT Launcher — 使用上方菜单添加启动项或发现工具"
        : "没有匹配的启动项";

    public void SetStatus(LauncherStatusContent? status)
    {
        if (status is not null)
            StatusContent = status;
    }

    public void RefreshCount(int total, int visible, int selected, int presets)
    {
        TotalCount = total;
        VisibleCount = visible;
        SelectedCount = selected;
        PresetCount = presets;
    }
}
