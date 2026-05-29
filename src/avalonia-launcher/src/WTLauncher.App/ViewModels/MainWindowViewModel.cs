using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly string _configPath;
    private readonly string _presetPath;
    private readonly string _settingsPath;
    private readonly LauncherSettings _settings;
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private LauncherPresetFile _presetFile = new();


    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _dryRunText = "选择一个启动项查看预演命令。";

    [ObservableProperty]
    private string _presetSelectionStatusText = "选择启动组后可查看与当前勾选的差异。";

    [ObservableProperty]
    private string _applyPresetButtonText = "应用启动组";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _presetCount;

    [ObservableProperty]
    private LauncherPresetViewModel? _selectedPreset;


    [ObservableProperty]
    private bool _requireConfirmationForLaunches;

    public MainWindowViewModel(string configPath, string settingsPath, LauncherSettings settings, LauncherLaunchWorkflow launchWorkflow)
    {
        _configPath = configPath;
        _presetPath = LauncherPresetStore.ResolvePresetPath(configPath);
        _settingsPath = settingsPath;
        _settings = settings;
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            _settings,
            _settingsPath,
            Entries,
            EnsureCustomOrderContainsEntries,
            ApplyFilter);
        ConfigPathDisplay = $"配置: {configPath} · 启动组: {_presetPath}";
        _filterText = settings.LastFilterText ?? string.Empty;
        _requireConfirmationForLaunches = settings.RequireConfirmationForLaunches;
        _selectedSortMode = ResolveSortMode(settings.SortMode);
        _settings.SortMode = _selectedSortMode.Id;
        _selectedDisplayMode = ResolveDisplayMode(settings.EntryViewMode);
        _settings.EntryViewMode = _selectedDisplayMode.Id;
        Refresh();
    }

    public ObservableCollection<LauncherEntryViewModel> Entries { get; } = [];

    public ObservableCollection<LauncherEntryViewModel> VisibleEntries { get; } = [];

    public ObservableCollection<LauncherPresetViewModel> Presets { get; } = [];


    public string ConfigPathDisplay { get; }

    public Func<IReadOnlyList<LaunchEntry>, Task<bool>> ConfirmBatchLaunchAsync { get; set; } =
        static _ => Task.FromResult(false);

    public Func<IReadOnlyList<string>, Task<string?>> RequestNewPresetAsync { get; set; } =
        static _ => Task.FromResult<string?>(null);

    public Func<LauncherPresetViewModel, Task<bool>> ConfirmDeletePresetAsync { get; set; } =
        static _ => Task.FromResult(false);

    public Func<LaunchEntry?, Task<EntryDraft?>> RequestEntryDraftAsync { get; set; } =
        static _ => Task.FromResult<EntryDraft?>(null);

    public Func<LauncherEntryViewModel, Task<bool>> ConfirmDeleteEntryAsync { get; set; } =
        static _ => Task.FromResult(false);


    [RelayCommand]
    private void Refresh()
    {
        ReplaceEntries(LauncherConfigStore.Load(_configPath));
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds());

        var presetError = LoadPresetFile(preferredPresetId: null);

        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(Entries);
        StatusText = presetError
            ?? $"已加载 {TotalCount} 个启动项、{PresetCount} 个启动组；勾选自动保存到 config\\config.json";
    }


    private void ApplyStatusText(string? statusText)
    {
        if (statusText is not null)
            StatusText = statusText;
    }


    private bool SaveConfig()
    {
        try
        {
            LauncherConfigStore.Save(Entries.Select(ToLaunchEntry), _configPath);
            return true;
        }
        catch (Exception ex)
        {
            ApplyStatusText(LauncherPersistenceStatus.ConfigSaveFailed(ex));
            return false;
        }
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.Entry;


    private void SaveSettingsChange(
        LauncherSettingsSnapshot snapshot,
        Action<LauncherSettingsSnapshot> onRollback,
        Action onSuccess)
    {
        var saveFailure = LauncherSettingsMutationPersistence.Save(
            _settings,
            _settingsPath,
            snapshot,
            () => onRollback(snapshot));
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return;
        }

        onSuccess();
    }

}
