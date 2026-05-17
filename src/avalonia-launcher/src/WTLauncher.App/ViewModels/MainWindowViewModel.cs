using System.Collections.ObjectModel;
using System.Text;
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
    private readonly LauncherProcess _launcherProcess;
    private LauncherPresetFile _presetFile = new();
    private bool _isBulkSelectionUpdate;

    [ObservableProperty]
    private string _filterText = "";

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

    public MainWindowViewModel(string configPath, string settingsPath, LauncherSettings settings, LauncherProcess launcherProcess)
    {
        _configPath = configPath;
        _presetPath = LauncherPresetStore.ResolvePresetPath(configPath);
        _settingsPath = settingsPath;
        _settings = settings;
        _launcherProcess = launcherProcess;
        ConfigPathDisplay = $"配置: {configPath} · 启动组: {_presetPath}";
        FilterText = settings.LastFilterText;
        Refresh();
    }

    public ObservableCollection<LauncherEntryViewModel> Entries { get; } = [];

    public ObservableCollection<LauncherEntryViewModel> VisibleEntries { get; } = [];

    public ObservableCollection<LauncherPresetViewModel> Presets { get; } = [];

    public string ConfigPathDisplay { get; }

    public Func<LaunchEntry, LaunchPlan, Task<bool>> ConfirmLaunchAsync { get; set; } =
        static (_, _) => Task.FromResult(false);

    public Func<IReadOnlyList<LaunchEntry>, Task<bool>> ConfirmBatchLaunchAsync { get; set; } =
        static _ => Task.FromResult(false);

    public Func<Task<PresetDraft?>> RequestNewPresetAsync { get; set; } =
        static () => Task.FromResult<PresetDraft?>(null);

    public Func<LauncherPresetViewModel, Task<bool>> ConfirmDeletePresetAsync { get; set; } =
        static _ => Task.FromResult(false);

    partial void OnFilterTextChanged(string value)
    {
        _settings.LastFilterText = value ?? string.Empty;
        SaveSettings();
        ApplyFilter();
    }

    [RelayCommand]
    private void Refresh()
    {
        Entries.Clear();
        foreach (var entry in LauncherConfigStore.Load(_configPath))
        {
            Entries.Add(new LauncherEntryViewModel(entry, this));
        }

        TotalCount = Entries.Count;
        SelectedCount = Entries.Count(entry => entry.IsSelected);
        var presetLoadError = LoadPresets();
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        ApplyFilter();
        StatusText = presetLoadError
            ?? $"已加载 {TotalCount} 个启动项、{PresetCount} 个启动组；勾选自动保存到 config\\config.json";
    }

    [RelayCommand]
    private void SelectVisible()
    {
        var targets = VisibleEntries.Where(entry => !entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            StatusText = VisibleEntries.Count == 0
                ? "当前没有可勾选的可见项"
                : "可见项已全部勾选";
            return;
        }

        if (SetSelection(targets, selected: true))
            StatusText = $"已勾选可见 {targets.Count} 项（已保存）";
    }

    [RelayCommand(CanExecute = nameof(CanClearSelection))]
    private void ClearSelection()
    {
        var targets = Entries.Where(entry => entry.IsSelected).ToList();
        if (targets.Count == 0)
        {
            StatusText = "当前没有已勾选项";
            return;
        }

        if (SetSelection(targets, selected: false))
            StatusText = $"已清空 {targets.Count} 个勾选项（已保存）";
    }

    [RelayCommand(CanExecute = nameof(CanApplyPreset))]
    private void ApplyPreset()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }

        var presetIds = SelectedPreset.EntryIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var knownIds = Entries.Select(entry => entry.Entry.Id).ToHashSet(StringComparer.Ordinal);
        var missing = presetIds.Where(id => !knownIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            StatusText = $"启动组引用了不存在的启动项: {string.Join(", ", missing)}";
            return;
        }

        if (ReplaceSelection(presetIds))
            StatusText = $"已应用启动组: {SelectedPreset.Name}（{SelectedCount} 项，已保存）";
    }

    [RelayCommand(CanExecute = nameof(CanSaveSelectionAsPreset))]
    private async Task SaveSelectionAsPresetAsync()
    {
        if (SelectedCount == 0)
        {
            StatusText = "未选择任何启动项";
            return;
        }

        var draft = await RequestNewPresetAsync().ConfigureAwait(true);
        if (draft is null)
        {
            StatusText = "已取消: 保存启动组";
            return;
        }

        var name = (draft.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText = "启动组名称不能为空";
            return;
        }

        var description = (draft.Description ?? string.Empty).Trim();
        var entryIds = GetSelectedEntryIds();
        var preset = new LauncherPreset
        {
            Id = GenerateUniquePresetId(name),
            Name = name,
            Description = description,
            EntryIds = entryIds,
        };

        _presetFile.Presets.Add(preset);
        if (!SavePresets())
            return;

        LoadPresets(preset.Id);
        StatusText = $"已保存启动组: {preset.Name}（{preset.EntryIds.Count} 项）";
    }

    [RelayCommand(CanExecute = nameof(CanUpdateSelectedPreset))]
    private void UpdateSelectedPreset()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }
        if (SelectedPreset.IsLocked)
        {
            StatusText = $"系统启动组不能更新: {SelectedPreset.Name}";
            return;
        }

        var preset = _presetFile.Presets.FirstOrDefault(item => item.Id == SelectedPreset.Id);
        if (preset is null)
        {
            StatusText = "当前启动组已不存在，请重新加载";
            return;
        }

        var entryIds = GetSelectedEntryIds();
        if (entryIds.Count == 0)
        {
            StatusText = "请先勾选至少一个启动项";
            return;
        }

        preset.Name = SelectedPreset.Name;
        preset.Description = SelectedPreset.Description;
        preset.EntryIds = entryIds;
        if (!SavePresets())
            return;

        LoadPresets(preset.Id);
        StatusText = $"已更新启动组: {preset.Name}（{preset.EntryIds.Count} 项）";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedPreset))]
    private async Task DeleteSelectedPresetAsync()
    {
        if (SelectedPreset is null)
        {
            StatusText = "未选择启动组";
            return;
        }
        if (SelectedPreset.IsLocked || !CanDeletePreset(SelectedPreset.Id))
        {
            StatusText = $"启动组不能删除: {SelectedPreset.Name}";
            return;
        }

        var accepted = await ConfirmDeletePresetAsync(SelectedPreset).ConfigureAwait(true);
        if (!accepted)
        {
            StatusText = $"已取消: 删除启动组 {SelectedPreset.Name}";
            return;
        }

        var preset = _presetFile.Presets.FirstOrDefault(item => item.Id == SelectedPreset.Id);
        if (preset is null)
        {
            StatusText = "当前启动组已不存在，请重新加载";
            return;
        }

        _presetFile.Presets.Remove(preset);
        if (_presetFile.DefaultPresetId == preset.Id)
            _presetFile.DefaultPresetId = _presetFile.Presets.FirstOrDefault()?.Id ?? string.Empty;

        if (!SavePresets())
            return;

        var fallbackPresetId = _presetFile.DefaultPresetId;
        LoadPresets(fallbackPresetId);
        StatusText = $"已删除启动组: {preset.Name}";
    }

    [RelayCommand(CanExecute = nameof(CanLaunchSelected))]
    private async Task LaunchSelectedAsync()
    {
        var selected = Entries.Where(entry => entry.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "未选择任何启动项";
            return;
        }

        try
        {
            var accepted = await ConfirmBatchLaunchAsync(selected.Select(entry => entry.Entry).ToList()).ConfigureAwait(true);
            if (!accepted)
            {
                StatusText = $"已取消: 批量启动 {selected.Count} 项";
                return;
            }

            var ok = 0;
            var fail = 0;
            foreach (var item in selected)
            {
                try
                {
                    var plan = LaunchArgumentBuilder.Build(item.Entry);
                    DryRunText = $"{plan.Executable} {plan.Arguments}";
                    _launcherProcess.Start(plan);
                    ok++;
                }
                catch
                {
                    fail++;
                }
            }

            StatusText = fail == 0
                ? $"已启动 {ok} 个勾选项"
                : $"已启动 {ok} 个勾选项，{fail} 个失败";
        }
        catch (Exception ex)
        {
            StatusText = $"批量启动失败: {ex.Message}";
        }
    }

    internal void ShowDryRun(LauncherEntryViewModel item)
    {
        try
        {
            var plan = LaunchArgumentBuilder.Build(item.Entry);
            DryRunText = $"{plan.Executable} {plan.Arguments}";
            StatusText = plan.RequiresConfirmation
                ? $"预演: {item.Name}（需要确认）"
                : $"预演: {item.Name}";
        }
        catch (Exception ex)
        {
            DryRunText = ex.Message;
            StatusText = $"预演失败: {item.Name}";
        }
    }

    internal void UpdateSelection(LauncherEntryViewModel item, bool selected)
    {
        item.Entry.Selected = selected;
        SelectedCount = Entries.Count(entry => entry.IsSelected);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        if (!_isBulkSelectionUpdate)
            SaveConfig();
    }

    partial void OnSelectedPresetChanged(LauncherPresetViewModel? value)
    {
        ApplyPresetCommand.NotifyCanExecuteChanged();
        UpdatePresetSelectionStatus();
    }

    internal async Task LaunchAsync(LauncherEntryViewModel item)
    {
        try
        {
            var plan = LaunchArgumentBuilder.Build(item.Entry);
            DryRunText = $"{plan.Executable} {plan.Arguments}";
            if (plan.RequiresConfirmation)
            {
                var accepted = await ConfirmLaunchAsync(item.Entry, plan).ConfigureAwait(true);
                if (!accepted)
                {
                    StatusText = $"已取消: {item.Name}";
                    return;
                }
            }

            _launcherProcess.Start(plan);
            StatusText = $"已启动: {item.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"启动失败: {item.Name} — {ex.Message}";
        }
    }

    private bool CanLaunchSelected()
    {
        return SelectedCount > 0;
    }

    private bool CanClearSelection()
    {
        return SelectedCount > 0;
    }

    private bool CanApplyPreset()
    {
        return SelectedPreset is not null && Entries.Count > 0 && PresetSelectionHasChanges();
    }

    private bool CanSaveSelectionAsPreset()
    {
        return SelectedCount > 0;
    }

    private bool CanUpdateSelectedPreset()
    {
        return SelectedPreset is not null
            && !SelectedPreset.IsLocked
            && SelectedCount > 0
            && PresetSelectionHasChanges();
    }

    private bool CanDeleteSelectedPreset()
    {
        if (SelectedPreset is null || SelectedPreset.IsLocked)
            return false;

        return CanDeletePreset(SelectedPreset.Id);
    }

    private void ApplyFilter()
    {
        var filter = (FilterText ?? "").Trim();
        VisibleEntries.Clear();
        foreach (var entry in Entries)
        {
            if (filter.Length == 0
                || entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || entry.Directory.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || entry.Command.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                VisibleEntries.Add(entry);
            }
        }

        VisibleCount = VisibleEntries.Count;
    }

    private bool SetSelection(IReadOnlyList<LauncherEntryViewModel> entries, bool selected)
    {
        _isBulkSelectionUpdate = true;
        try
        {
            foreach (var entry in entries)
            {
                entry.IsSelected = selected;
            }
        }
        finally
        {
            _isBulkSelectionUpdate = false;
        }

        SelectedCount = Entries.Count(entry => entry.IsSelected);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        return SaveConfig();
    }

    private bool ReplaceSelection(IReadOnlySet<string> selectedIds)
    {
        _isBulkSelectionUpdate = true;
        try
        {
            foreach (var entry in Entries)
            {
                entry.IsSelected = selectedIds.Contains(entry.Entry.Id);
            }
        }
        finally
        {
            _isBulkSelectionUpdate = false;
        }

        SelectedCount = Entries.Count(entry => entry.IsSelected);
        NotifySelectionCommands();
        UpdatePresetSelectionStatus();
        return SaveConfig();
    }

    private void NotifySelectionCommands()
    {
        LaunchSelectedCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        ApplyPresetCommand.NotifyCanExecuteChanged();
        NotifyPresetManagementCommands();
    }

    private void UpdatePresetSelectionStatus()
    {
        if (SelectedPreset is null)
        {
            ApplyPresetButtonText = "应用启动组";
            PresetSelectionStatusText = PresetCount == 0
                ? "暂无启动组；可以继续手动勾选。"
                : "请选择一个启动组查看差异。";
            return;
        }

        var selectedIds = Entries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Entry.Id)
            .ToHashSet(StringComparer.Ordinal);
        var knownIds = Entries.Select(entry => entry.Entry.Id).ToHashSet(StringComparer.Ordinal);
        var presetIds = SelectedPreset.EntryIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var missingConfigIds = presetIds.Where(id => !knownIds.Contains(id)).ToList();
        if (missingConfigIds.Count > 0)
        {
            ApplyPresetButtonText = "检查启动组";
            PresetSelectionStatusText = $"启动组“{SelectedPreset.Name}”引用了 {missingConfigIds.Count} 个不存在的启动项；请先修 config\\presets.json。";
            return;
        }

        var matched = selectedIds.Count(id => presetIds.Contains(id));
        var missing = presetIds.Count(id => !selectedIds.Contains(id));
        var extra = selectedIds.Count(id => !presetIds.Contains(id));

        if (presetIds.Count == 0)
        {
            ApplyPresetButtonText = extra == 0 ? "手动模式" : "清空勾选";
            PresetSelectionStatusText = extra == 0
                ? $"已是“{SelectedPreset.Name}”：当前没有勾选项。"
                : $"“{SelectedPreset.Name}”会清空当前 {extra} 个勾选项。";
            return;
        }

        ApplyPresetButtonText = missing == 0 && extra == 0 ? "已匹配" : "应用启动组";
        PresetSelectionStatusText = missing == 0 && extra == 0
            ? $"已匹配启动组“{SelectedPreset.Name}”：{matched}/{presetIds.Count} 项。"
            : $"启动组“{SelectedPreset.Name}”：已匹配 {matched}/{presetIds.Count}，缺 {missing}，额外 {extra}；应用会替换当前勾选。";
    }

    private bool PresetSelectionHasChanges()
    {
        if (SelectedPreset is null)
            return false;

        var selectedIds = Entries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Entry.Id)
            .ToHashSet(StringComparer.Ordinal);
        var presetIds = SelectedPreset.EntryIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        return !selectedIds.SetEquals(presetIds);
    }

    private bool CanDeletePreset(string presetId)
    {
        if (_presetFile.Presets.Count == 0)
            return false;

        var target = _presetFile.Presets.FirstOrDefault(preset => preset.Id == presetId);
        if (target is null || target.Locked)
            return false;

        var remainingNonEmpty = _presetFile.Presets
            .Count(preset => preset.Id != presetId && preset.EntryIds.Count > 0);
        return remainingNonEmpty > 0;
    }

    private List<string> GetSelectedEntryIds()
    {
        return Entries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Entry.Id)
            .ToList();
    }

    private string GenerateUniquePresetId(string name)
    {
        var baseId = NormalizePresetId(name);
        var candidate = baseId;
        var index = 2;
        var existing = _presetFile.Presets.Select(preset => preset.Id).ToHashSet(StringComparer.Ordinal);
        while (existing.Contains(candidate))
        {
            candidate = $"{baseId}-{index++}";
        }

        return candidate;
    }

    private static string NormalizePresetId(string value)
    {
        var builder = new StringBuilder(value.Length);
        var separatorPending = false;
        foreach (var ch in value.Normalize(NormalizationForm.FormKC))
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                separatorPending = false;
            }
            else if (ch is '-' or '_')
            {
                builder.Append(ch);
                separatorPending = false;
            }
            else if (char.IsWhiteSpace(ch) || ch is '/' or '\\' or '.' or ':')
            {
                if (!separatorPending && builder.Length > 0)
                {
                    builder.Append('-');
                    separatorPending = true;
                }
            }
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "preset" : normalized;
    }

    private void NotifyPresetManagementCommands()
    {
        SaveSelectionAsPresetCommand.NotifyCanExecuteChanged();
        UpdateSelectedPresetCommand.NotifyCanExecuteChanged();
        DeleteSelectedPresetCommand.NotifyCanExecuteChanged();
    }

    private string? LoadPresets(string? preferredPresetId = null)
    {
        Presets.Clear();
        LauncherPresetFile presetFile;
        try
        {
            presetFile = LauncherPresetStore.Load(_presetPath);
            _presetFile = presetFile;
        }
        catch (Exception ex)
        {
            PresetCount = 0;
            SelectedPreset = null;
            return $"启动组加载失败: {ex.Message}";
        }

        foreach (var preset in presetFile.Presets)
        {
            Presets.Add(new LauncherPresetViewModel(preset));
        }

        PresetCount = Presets.Count;
        var targetPresetId = preferredPresetId ?? presetFile.DefaultPresetId;
        SelectedPreset = Presets.FirstOrDefault(preset => preset.Id == targetPresetId)
            ?? Presets.FirstOrDefault();
        NotifyPresetManagementCommands();
        return null;
    }

    private bool SavePresets()
    {
        try
        {
            LauncherPresetStore.Save(_presetFile, _presetPath);
            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"启动组保存失败: {ex.Message}";
            return false;
        }
    }

    private bool SaveConfig()
    {
        try
        {
            LauncherConfigStore.Save(Entries.Select(entry => entry.Entry), _configPath);
            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"配置保存失败: {ex.Message}";
            return false;
        }
    }

    private void SaveSettings()
    {
        try
        {
            LauncherSettingsStore.Save(_settings, _settingsPath);
        }
        catch (Exception ex)
        {
            StatusText = $"设置保存失败: {ex.Message}";
        }
    }
}
