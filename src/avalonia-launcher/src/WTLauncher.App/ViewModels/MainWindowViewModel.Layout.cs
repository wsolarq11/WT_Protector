using Avalonia;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private Size _cardsAvailableSize = new(900, 420);

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private LauncherSortModeViewModel? _selectedSortMode;

    [ObservableProperty]
    private LauncherDisplayModeViewModel? _selectedDisplayMode;

    public ObservableCollection<LauncherSortModeViewModel> SortModes { get; } =
    [
        new(LauncherEntryVisibility.CustomSortModeId, "自定义拖拽"),
        new(LauncherEntryVisibility.UpdatedSortModeId, "更新时间"),
        new(LauncherEntryVisibility.AlphabeticalSortModeId, "首字母顺序"),
    ];

    public ObservableCollection<LauncherDisplayModeViewModel> DisplayModes { get; } =
    [
        new(LauncherEntryViewMode.Cards, "卡片"),
        new(LauncherEntryViewMode.Details, "详细列表"),
        new(LauncherEntryViewMode.Names, "名称墙"),
    ];

    private LauncherSortModeViewModel ResolveSortMode(string? sortModeId) =>
        SortModes.First(mode => mode.Id == LauncherEntryVisibility.NormalizeSortModeId(sortModeId));

    private LauncherDisplayModeViewModel ResolveDisplayMode(string? entryViewMode) =>
        DisplayModes.First(mode => mode.Id == LauncherEntryViewModes.NormalizeId(entryViewMode));

    private LauncherEntryViewMode CurrentEntryViewMode => RequireDisplayModeSelection(SelectedDisplayMode).Mode;

    public bool IsCustomSortMode => RequireSortModeSelection(SelectedSortMode).Id == LauncherEntryVisibility.CustomSortModeId;

    public bool IsCardsViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Cards;

    public bool IsDetailedListMode => CurrentEntryViewMode == LauncherEntryViewMode.Details;

    public bool IsNamesViewMode => CurrentEntryViewMode == LauncherEntryViewMode.Names;

    partial void OnFilterTextChanging(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    partial void OnFilterTextChanged(string value)
    {
        var snapshot = CaptureSettingsMutationSnapshot();
        _settings.LastFilterText = value;
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _filterText = snapshot.LastFilterText;
            OnPropertyChanged(nameof(FilterText));
            ApplyFilter();
            ApplyStatusText(saveFailure);
            return;
        }

        ApplyFilter();
    }

    partial void OnSelectedSortModeChanging(LauncherSortModeViewModel? value)
    {
        _ = RequireSortModeSelection(value);
    }

    partial void OnSelectedSortModeChanged(LauncherSortModeViewModel? value)
    {
        value = RequireSortModeSelection(value);
        OnPropertyChanged(nameof(IsCustomSortMode));

        var snapshot = CaptureSettingsMutationSnapshot();
        _settings.SortMode = value.Id;
        EnsureCustomOrderContainsEntries();
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _selectedSortMode = ResolveSortMode(snapshot.SortMode);
            OnPropertyChanged(nameof(SelectedSortMode));
            OnPropertyChanged(nameof(IsCustomSortMode));
            ApplyFilter();
            ApplyStatusText(saveFailure);
            return;
        }

        ApplyFilter();
        StatusText = LauncherSettingsStatus.SortModeSaved(value.Label);
    }

    partial void OnSelectedDisplayModeChanging(LauncherDisplayModeViewModel? value)
    {
        _ = RequireDisplayModeSelection(value);
    }

    partial void OnSelectedDisplayModeChanged(LauncherDisplayModeViewModel? value)
    {
        value = RequireDisplayModeSelection(value);
        NotifyDisplayModeProjectionChanged();

        var snapshot = CaptureSettingsMutationSnapshot();
        _settings.EntryViewMode = value.Id;
        LauncherEntryLayoutPresentation.Refresh(Entries);
        var saveFailure = SaveSettingsChange(snapshot);
        if (saveFailure is not null)
        {
            _selectedDisplayMode = ResolveDisplayMode(snapshot.EntryViewMode);
            OnPropertyChanged(nameof(SelectedDisplayMode));
            NotifyDisplayModeProjectionChanged();
            LauncherEntryLayoutPresentation.Refresh(Entries);
            ApplyStatusText(saveFailure);
            return;
        }

        StatusText = LauncherSettingsStatus.DisplayModeSaved(value.Label);
    }

    private void NotifyDisplayModeProjectionChanged()
    {
        OnPropertyChanged(nameof(IsCardsViewMode));
        OnPropertyChanged(nameof(IsDetailedListMode));
        OnPropertyChanged(nameof(IsNamesViewMode));
    }

    private void ApplyFilter()
    {
        var sorted = LauncherEntryVisibility.FilterAndSort(
            Entries,
            FilterText,
            RequireSortModeSelection(SelectedSortMode).Id,
            _settings.CustomEntryOrder,
            ToLaunchEntry);

        VisibleEntries.Clear();
        foreach (var entry in sorted)
        {
            VisibleEntries.Add(entry);
        }

        VisibleCount = VisibleEntries.Count;
        LauncherEntryLayoutPresentation.ApplyCardSize(Entries, VisibleEntries, _cardsAvailableSize);
        RefreshReorderCommandState();
    }

    public void UpdateCardsAvailableSize(Size availableSize)
    {
        _cardsAvailableSize = LauncherEntryLayoutPresentation.NormalizeAvailableSize(availableSize);
        LauncherEntryLayoutPresentation.ApplyCardSize(Entries, VisibleEntries, _cardsAvailableSize);
    }

    public bool TrySaveWindowBounds(double width, double height)
    {
        DiscardDragReorderPreview();
        if (LauncherSettingsStore.TrySaveWindowBounds(_settings, _settingsPath, width, height))
            return true;

        StatusText = LauncherSettingsStatus.WindowBoundsSaveFailed;
        return false;
    }

    private void EnsureCustomOrderContainsEntries() =>
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds());

    private static LauncherSortModeViewModel RequireSortModeSelection(LauncherSortModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher sort mode selection is required.");

    private static LauncherDisplayModeViewModel RequireDisplayModeSelection(LauncherDisplayModeViewModel? value) =>
        value ?? throw new InvalidOperationException("Launcher entry view mode selection is required.");

    private IEnumerable<string> GetEntryIds() =>
        Entries.Select(entry => entry.Id);
}
