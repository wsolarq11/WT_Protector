using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// 排序/过滤/视图模式/分组 — 布局状态管理。
/// 卡片尺寸由 <see cref="CardLayout"/> (CardLayoutProvider) 单一管理。
/// 不持有 entry 数据。输出排序/过滤参数。
/// 失败半径: Medium — 无效参数抛 InvalidOperationException。
/// </summary>
public sealed partial class LayoutManager : ObservableObject
{
    public CardLayoutProvider CardLayout { get; } = new();

    private Size _cardsAvailableSize = new(900, 420);

    private readonly Dictionary<LauncherEntryViewMode, double> _scrollOffsets = new();

    private string _cardsGroupKey = LauncherCardsGroupKeys.None;

    private readonly ObservableCollection<LauncherSortModeViewModel> _sortModes =
    [
        new(LauncherEntryQuery.CustomSortModeId, "自定义拖拽"),
        new(LauncherEntryQuery.RecentSortModeId, "最近启动"),
        new(LauncherEntryQuery.UpdatedSortModeId, "更新时间"),
        new(LauncherEntryQuery.AlphabeticalSortModeId, "首字母顺序"),
        new(LauncherEntryQuery.FrequentSortModeId, "使用频率"),
    ];

    private readonly ObservableCollection<LauncherDisplayModeViewModel> _displayModes =
    [
        new(LauncherEntryViewMode.Cards, "卡片"),
        new(LauncherEntryViewMode.Details, "详细列表"),
        new(LauncherEntryViewMode.Names, "名称墙"),
    ];

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private LauncherSortModeViewModel? _selectedSortMode;

    [ObservableProperty]
    private LauncherDisplayModeViewModel? _selectedDisplayMode;

    public ReadOnlyObservableCollection<LauncherSortModeViewModel> SortModes { get; }
    public ReadOnlyObservableCollection<LauncherDisplayModeViewModel> DisplayModes { get; }

    public IReadOnlyList<LauncherCardsGroupOption> CardsGroupOptions { get; } =
    [
        new("无分组", LauncherCardsGroupKeys.None),
        new("按 command", LauncherCardsGroupKeys.Command),
        new("按 directory", LauncherCardsGroupKeys.Directory),
    ];

    public event Action? FilterChanged;
    public event Action? SortChanged;
    public event Action? ViewModeChanged;
    public event Action? GroupKeyChanged;

    public LayoutManager()
    {
        SortModes = new ReadOnlyObservableCollection<LauncherSortModeViewModel>(_sortModes);
        DisplayModes = new ReadOnlyObservableCollection<LauncherDisplayModeViewModel>(_displayModes);
    }

    /// <summary>Initialize state from settings without firing events.</summary>
    public void LoadSettings(LauncherSettings settings)
    {
        // Use properties — safe because event subscriptions are not yet set up
        // in the MWVM constructor when this is called, so no event handlers fire.
        FilterText = settings.LastFilterText;
        SelectedSortMode = ResolveSortMode(settings.SortMode);
        SelectedDisplayMode = ResolveDisplayMode(settings.EntryViewMode);
        _cardsGroupKey = LauncherCardsGroupKeys.Normalize(settings.CardsGroupKey);
        OnPropertyChanged(nameof(CardsGroupKey));
        OnPropertyChanged(nameof(SelectedCardsGroupOption));
    }

    partial void OnFilterTextChanging(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    partial void OnFilterTextChanged(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length != value.Length)
            _filterText = trimmed;
        FilterChanged?.Invoke();
    }

    partial void OnSelectedSortModeChanging(LauncherSortModeViewModel? value)
    {
        _ = RequireSortModeSelection(value);
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        SortChanged?.Invoke();
    }

    partial void OnSelectedDisplayModeChanging(LauncherDisplayModeViewModel? value)
    {
        _ = RequireDisplayModeSelection(value);
    }

    partial void OnSelectedDisplayModeChanged(LauncherDisplayModeViewModel? value)
    {
        ViewModeChanged?.Invoke();
    }

    public LauncherEntryViewMode CurrentEntryViewMode => RequireDisplayModeSelection(SelectedDisplayMode).Mode;

    // ponytail: null-conditional avoids throwing when SelectedSortMode is null
    // (corrupted settings / deserialization failure). Returns false safely.
    public bool IsCustomSortMode => SelectedSortMode?.Id == LauncherEntryQuery.CustomSortModeId;
    public bool IsCardsViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Cards;
    public bool IsDetailedListMode => CurrentEntryViewMode == LauncherEntryViewMode.Details;
    public bool IsNamesViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Names;

    public LauncherCardsGroupOption? SelectedCardsGroupOption
    {
        get => CardsGroupOptions.FirstOrDefault(o => string.Equals(o.Value, _cardsGroupKey, StringComparison.Ordinal));
        set
        {
            if (value is not null)
                CardsGroupKey = value.Value;
        }
    }

    public string CardsGroupKey
    {
        get => _cardsGroupKey;
        set => SetCardsGroupKey(value);
    }

    public Size CardsAvailableSize => _cardsAvailableSize;

    public double CardsAvailableHeight => _cardsAvailableSize.Height;

    public double CardsViewportHeight => CardsAvailableHeight;

    public void NotifyDisplayModeProjectionChanged()
    {
        OnPropertyChanged(nameof(IsCardsViewMode));
        OnPropertyChanged(nameof(IsDetailedListMode));
        OnPropertyChanged(nameof(IsNamesViewMode));
    }

    public void SaveScrollOffset(LauncherEntryViewMode mode, double offset) => _scrollOffsets[mode] = offset;

    public double GetScrollOffset(LauncherEntryViewMode mode) =>
        _scrollOffsets.TryGetValue(mode, out var v) ? v : 0;

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        var normalized = NormalizeAvailableSize(availableSize);
        if (_cardsAvailableSize == normalized)
            return; // Skip when size unchanged — avoids redundant PropertyChanged storm.
        _cardsAvailableSize = normalized;
        OnPropertyChanged(nameof(CardsAvailableHeight));
        OnPropertyChanged(nameof(CardsViewportHeight));
    }

    private static Size NormalizeAvailableSize(Size availableSize) => new(
        double.IsFinite(availableSize.Width) ? Math.Max(CardLayoutProvider.MinimumCardsWidth, availableSize.Width) : CardLayoutProvider.MinimumCardsWidth,
        double.IsFinite(availableSize.Height) ? Math.Max(CardLayoutProvider.MinimumCardsHeight, availableSize.Height) : CardLayoutProvider.MinimumCardsHeight);

    internal void SetGroupKeyDirect(string normalized)
    {
        _cardsGroupKey = normalized;
        OnPropertyChanged(nameof(CardsGroupKey));
        OnPropertyChanged(nameof(SelectedCardsGroupOption));
        OnPropertyChanged(nameof(CardsViewportHeight));
    }

    public LauncherSortModeViewModel ResolveSortMode(string? sortModeId) =>
        _sortModes.First(mode => mode.Id == LauncherEntryQuery.NormalizeSortModeId(sortModeId));

    public LauncherDisplayModeViewModel ResolveDisplayMode(string? entryViewMode) =>
        _displayModes.First(mode => mode.Id == LauncherEntryViewModes.NormalizeId(entryViewMode));

    private void SetCardsGroupKey(string value)
    {
        var normalized = LauncherCardsGroupKeys.Normalize(value);
        if (string.Equals(_cardsGroupKey, normalized, StringComparison.Ordinal))
            return;
        _cardsGroupKey = normalized;
        OnPropertyChanged(nameof(CardsGroupKey));
        OnPropertyChanged(nameof(SelectedCardsGroupOption));
        OnPropertyChanged(nameof(CardsViewportHeight));
        GroupKeyChanged?.Invoke();
    }

    private static LauncherSortModeViewModel RequireSortModeSelection(LauncherSortModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher sort mode selection is required.");

    private static LauncherDisplayModeViewModel RequireDisplayModeSelection(LauncherDisplayModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher entry view mode selection is required.");
}
