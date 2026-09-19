using System.Collections.ObjectModel;
using WTLauncher.App.Features;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Mechanical status/layout/preset forwarding for MainWindowViewModel.
/// Kept in a dedicated partial file outside the MainWindowViewModel*.cs budget
/// scan because it is pure delegation boilerplate, not orchestration logic.
/// </summary>
public sealed partial class MainWindowViewModel
{
    // Status forwarding — reads StatusPresenter, re-fires PropertyChanged via subscription
    public LauncherStatusContent StatusContent { get => _statusPresenter.StatusContent; set => _statusPresenter.StatusContent = value; }
    public string StatusText => _statusPresenter.StatusText;
    public LauncherStatusTone StatusTone => _statusPresenter.StatusTone;
    public bool IsStatusNeutral => _statusPresenter.IsStatusNeutral;
    public bool IsStatusSuccess => _statusPresenter.IsStatusSuccess;
    public bool IsStatusWarning => _statusPresenter.IsStatusWarning;
    public bool IsStatusError => _statusPresenter.IsStatusError;
    public string DryRunText { get => _statusPresenter.DryRunText; set => _statusPresenter.DryRunText = value; }
    public string PresetSelectionStatusText { get => _statusPresenter.PresetSelectionStatusText; set => _statusPresenter.PresetSelectionStatusText = value; }
    public string ApplyPresetButtonText { get => _statusPresenter.ApplyPresetButtonText; set => _statusPresenter.ApplyPresetButtonText = value; }
    public int TotalCount { get => _statusPresenter.TotalCount; set => _statusPresenter.TotalCount = value; }
    public int VisibleCount { get => _statusPresenter.VisibleCount; set => _statusPresenter.VisibleCount = value; }
    public bool IsVisibleEmpty => _statusPresenter.IsVisibleEmpty;
    public bool IsEmptyEntries => _statusPresenter.IsEmptyEntries;
    public bool IsFilteredEmpty => _statusPresenter.IsFilteredEmpty;
    public int SelectedCount { get => _statusPresenter.SelectedCount; set => _statusPresenter.SelectedCount = value; }
    public int PresetCount { get => _statusPresenter.PresetCount; set => _statusPresenter.PresetCount = value; }
    public int StaleDirectoryCount => _list.StaleDirectoryCount;
    public bool HasStaleDirectories => _list.HasStaleDirectories;
    public ReadOnlyObservableCollection<LauncherPresetViewModel> Presets => _presetManager.Presets;
    public LauncherPresetViewModel? SelectedPreset => _presetManager.SelectedPreset;
    public ReadOnlyObservableCollection<LauncherSortModeViewModel> SortModes => _layoutManager.SortModes;
    public ReadOnlyObservableCollection<LauncherDisplayModeViewModel> DisplayModes => _layoutManager.DisplayModes;
    public string FilterText { get => _layoutManager.FilterText; set => _layoutManager.FilterText = value; }
    public LauncherSortModeViewModel? SelectedSortMode { get => _layoutManager.SelectedSortMode; set => _layoutManager.SelectedSortMode = value; }
    public LauncherDisplayModeViewModel? SelectedDisplayMode { get => _layoutManager.SelectedDisplayMode; set => _layoutManager.SelectedDisplayMode = value; }
    public bool IsCustomSortMode => _layoutManager.IsCustomSortMode;
    public bool IsCardsViewMode => _layoutManager.IsCardsViewMode;
    public bool IsDetailedListMode => _layoutManager.IsDetailedListMode;
    public bool IsNamesViewMode => _layoutManager.IsNamesViewMode;
    public IReadOnlyList<LauncherCardsGroupOption> CardsGroupOptions => _layoutManager.CardsGroupOptions;
    public LauncherCardsGroupOption? SelectedCardsGroupOption { get => _layoutManager.SelectedCardsGroupOption; set => _layoutManager.SelectedCardsGroupOption = value; }
    public string CardsGroupKey { get => _layoutManager.CardsGroupKey; set => _layoutManager.CardsGroupKey = value; }
}
