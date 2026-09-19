using System.Collections.ObjectModel;
using WTLauncher.App.Features;
using WTLauncher.App.Localization;
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

    // Localized control-bar text resolves through the embedded JSON language pack.
    // These are intentionally instance properties for compiled XAML binding, so
    // CA1822 does not apply even though they do not read instance fields.
#pragma warning disable CA1822
    public string BarSubtitle => LauncherLocalization.Format("bar.subtitle");
    public string BarSelectedPrefix => LauncherLocalization.Format("bar.selectedPrefix");
    public string BarSelectedSuffix => LauncherLocalization.Format("bar.selectedSuffix");
    public string BarSelectionMenu => LauncherLocalization.Format("bar.selectionMenu");
    public string BarSelectionMenuAutomation => LauncherLocalization.Format("bar.selectionMenuAutomation");
    public string BarSelectAll => LauncherLocalization.Format("bar.selectAll");
    public string BarClearSelection => LauncherLocalization.Format("bar.clearSelection");
    public string BarReload => LauncherLocalization.Format("bar.reload");
    public string BarLaunchSelected => LauncherLocalization.Format("bar.launchSelected");
    public string BarSearch => LauncherLocalization.Format("bar.search");
    public string BarSearchPlaceholder => LauncherLocalization.Format("bar.searchPlaceholder");
    public string BarSort => LauncherLocalization.Format("bar.sort");
    public string BarView => LauncherLocalization.Format("bar.view");
    public string BarPresetGroup => LauncherLocalization.Format("bar.presetGroup");
    public string BarPresetPlaceholder => LauncherLocalization.Format("bar.presetPlaceholder");
    public string BarPreview => LauncherLocalization.Format("bar.preview");
    public string BarLaunch => LauncherLocalization.Format("bar.launch");
    public string BarAddEntry => LauncherLocalization.Format("bar.addEntry");
    public string BarCardsGroup => LauncherLocalization.Format("bar.cardsGroup");
    public string BarDiscover => LauncherLocalization.Format("bar.discover");
    public string BarManage => LauncherLocalization.Format("bar.manage");
    public string BarManageAutomation => LauncherLocalization.Format("bar.manageAutomation");
    public string BarExportAll => LauncherLocalization.Format("bar.exportAll");
    public string BarExportAllAutomation => LauncherLocalization.Format("bar.exportAllAutomation");
    public string BarExportSelected => LauncherLocalization.Format("bar.exportSelected");
    public string BarExportSelectedAutomation => LauncherLocalization.Format("bar.exportSelectedAutomation");
    public string BarImport => LauncherLocalization.Format("bar.import");
    public string BarImportAutomation => LauncherLocalization.Format("bar.importAutomation");
    public string BarRunHistory => LauncherLocalization.Format("bar.runHistory");
    public string BarRunHistoryAutomation => LauncherLocalization.Format("bar.runHistoryAutomation");
    public string BarCheckUpdate => LauncherLocalization.Format("bar.checkUpdate");
    public string BarCheckUpdateAutomation => LauncherLocalization.Format("bar.checkUpdateAutomation");
    public string BarCommands => LauncherLocalization.Format("bar.commands");
    public string BarCommandsAutomation => LauncherLocalization.Format("bar.commandsAutomation");
    public string BarAddress => LauncherLocalization.Format("bar.address");
    public string BarAddressAutomation => LauncherLocalization.Format("bar.addressAutomation");
    public string BarPresetMaintenance => LauncherLocalization.Format("bar.presetMaintenance");
    public string BarPresetMaintenanceAutomation => LauncherLocalization.Format("bar.presetMaintenanceAutomation");
    public string BarSaveAsNewPreset => LauncherLocalization.Format("bar.saveAsNewPreset");
    public string BarSaveAsNewPresetAutomation => LauncherLocalization.Format("bar.saveAsNewPresetAutomation");
    public string BarUpdatePreset => LauncherLocalization.Format("bar.updatePreset");
    public string BarUpdatePresetAutomation => LauncherLocalization.Format("bar.updatePresetAutomation");
    public string BarDeletePreset => LauncherLocalization.Format("bar.deletePreset");
    public string BarDeletePresetAutomation => LauncherLocalization.Format("bar.deletePresetAutomation");
    public string BarStaleTooltip => LauncherLocalization.Format("bar.staleTooltip");
    public string BarStalePrefix => LauncherLocalization.Format("bar.stalePrefix");
    public string BarStaleSuffix => LauncherLocalization.Format("bar.staleSuffix");
#pragma warning restore CA1822
}
