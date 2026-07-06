using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly string _storePath;
    private LauncherSettings _settings;
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private readonly Func<DateTimeOffset> _utcNow;
    private LauncherPresetFile _presetFile = new();
    private readonly ObservableCollection<LauncherEntryViewModel> _entries = [];
    private readonly ObservableCollection<LauncherEntryViewModel> _visibleEntries = [];
    private readonly ObservableCollection<LauncherPresetViewModel> _presets = [];

    [ObservableProperty]
    private string _statusText = LauncherMainWindowText.Ready;

    [ObservableProperty]
    private string _dryRunText = LauncherMainWindowText.DryRunPlaceholder;

    [ObservableProperty]
    private string _presetSelectionStatusText = LauncherMainWindowText.PresetSelectionPlaceholder;

    [ObservableProperty]
    private string _applyPresetButtonText = LauncherPresetSelectionPreview.DefaultApplyButtonText;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _visibleCount;

    public bool IsVisibleEmpty => VisibleCount == 0;

    partial void OnVisibleCountChanged(int value)
    {
        OnPropertyChanged(nameof(IsVisibleEmpty));
    }

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _presetCount;

    [ObservableProperty]
    private LauncherPresetViewModel? _selectedPreset;

    [ObservableProperty]
    private bool _requireConfirmationForLaunches;

    private string? _keyboardFocusedEntryId;

    public string? KeyboardFocusedEntryId
    {
        get => _keyboardFocusedEntryId;
        set => SetKeyboardFocusedEntryId(value);
    }

    public MainWindowViewModel(
        string storePath,
        LauncherStoreFile storeFile,
        LauncherLaunchWorkflow launchWorkflow,
        Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(storeFile);
        _storePath = storePath;
        _settings = storeFile.Settings;
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _launchWorkflow.CommandTemplates = _settings.CommandTemplates;
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        Entries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_entries);
        VisibleEntries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_visibleEntries);
        Presets = new ReadOnlyObservableCollection<LauncherPresetViewModel>(_presets);
        SortModes = new ReadOnlyObservableCollection<LauncherSortModeViewModel>(_sortModes);
        DisplayModes = new ReadOnlyObservableCollection<LauncherDisplayModeViewModel>(_displayModes);
        ConfigPathDisplay = LauncherMainWindowText.StorePathDisplay(storePath);
        _filterText = RequireSettingsText(_settings.LastFilterText, "lastFilterText");
        _requireConfirmationForLaunches = _settings.RequireConfirmationForLaunches;
        OnPropertyChanged(nameof(LaunchConfirmationToggleText));
        OnPropertyChanged(nameof(LaunchConfirmationToggleTip));
        _selectedSortMode = ResolveSortMode(_settings.SortMode);
        _settings = _settings with { SortMode = _selectedSortMode.Id };
        _selectedDisplayMode = ResolveDisplayMode(_settings.EntryViewMode);
        _settings = _settings with { EntryViewMode = _selectedDisplayMode.Id };
        NotifyDisplayModeProjectionChanged();
        // Construct workflow AFTER settings normalization so it captures
        // the normalized values — DiscardDragReorderPreview syncs from it.
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            _settings,
            _entries,
            SaveSettingsToStore);
        // Load entries and presets from the unified store.
        ReplaceEntries(storeFile.Entries);
        _presetFile = new LauncherPresetFile
        {
            SchemaVersion = 1,
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        ApplyPresetFileState(_presetFile, preferredPresetId: null);
        // Sync custom entry order with loaded entries so ValidateCustomOrder
        // passes even when the stored CustomEntryOrder is empty or stale.
        _settings = _settings with { CustomEntryOrder = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds()) };
        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entries);
        StatusText = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
    }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; }

    public ReadOnlyObservableCollection<LauncherPresetViewModel> Presets { get; }

    /// <summary>Current settings snapshot. Immutable — replaced on every mutation.
    /// Tests should read from this, not from a stale pre-construction reference.</summary>
    public LauncherSettings Settings => _settings;

    public IReadOnlyDictionary<string, string>? CommandTemplates => _launchWorkflow.CommandTemplates;


    public string ConfigPathDisplay { get; }

    [RelayCommand]
    private void Refresh()
    {
        try
        {
            DiscardDragReorderPreview();
            var storeFile = LauncherStore.Load(_storePath);
            ReplaceEntries(storeFile.Entries);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Refresh.LoadStore");
            StatusText = $"配置加载失败: {ExceptionMessageFormatter.ForUser(ex)}";
            return;
        }

        try
        {
            _settings = _settings with { CustomEntryOrder = LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds()) };

            // Cross-validate $key command references against commandTemplates.
            ValidateCommandTemplateReferences();

            LoadPresetFile(preferredPresetId: null);

            ValidatePresetEntryReferences();
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Refresh.Validate");
            StatusText = $"配置验证失败: {ExceptionMessageFormatter.ForUser(ex)}";
            // Reload preset file to ensure _presetFile and UI state are consistent
            // after a partial failure during Phase 2 validation.
            try { LoadPresetFile(preferredPresetId: null); } catch { /* best-effort */ }
            RefreshSelectionState();
            ApplyFilter();
            return;
        }

        RefreshSelectionState();
        ApplyFilter();
        LauncherEntryLayoutPresentation.Refresh(_entries);
        StatusText = LauncherMainWindowText.Loaded(TotalCount, PresetCount);
    }

    private void ValidateCommandTemplateReferences()
    {
        var missingKeys = LauncherCommandTemplateResolver.ValidateTemplateReferences(
            _entries.Select(ToLaunchEntry),
            _settings.CommandTemplates);

        if (missingKeys.Count > 0)
            StatusText = $"警告：以下命令模板键未定义: {string.Join(", ", missingKeys)}";
    }

    /// <summary>
    /// Cross-validate that every preset entry ID references a loaded entry.
    /// Logs dangling references to ErrorLog but does not block loading.
    /// </summary>
    private void ValidatePresetEntryReferences()
    {
        var entryIdSet = GetEntryIds().ToHashSet(EntryId.Comparer);
        foreach (var preset in _presetFile.Presets)
        {
            foreach (var entryId in preset.EntryIds)
            {
                if (!entryIdSet.Contains(entryId))
                {
                    ErrorLog.Write(
                        new InvalidDataException(
                            $"启动组 \"{preset.Name}\" 引用了不存在的启动项 ID: {entryId}"),
                        "PresetValidation");
                }
            }
        }
    }

    private void ApplyStatusText(string? statusText)
    {
        if (statusText is not null)
            StatusText = statusText;
    }

    private bool SaveConfig()
    {
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return false;
        }

        return true;
    }

    /// <summary>Builds a LauncherStoreFile snapshot from current in-memory state.</summary>
    private LauncherStoreFile BuildStoreFile() =>
        new()
        {
            Entries = _entries.Select(ToLaunchEntry).ToImmutableList(),
            Settings = _settings,
            Presets = _presetFile.Presets,
            DefaultPresetId = _presetFile.DefaultPresetId,
        };

    /// <summary>Saves the given store file to disk. Returns null on success,
    /// or an error message on failure.</summary>
    private string? SaveStoreFile(LauncherStoreFile storeFile)
    {
        try { LauncherStore.Save(storeFile, _storePath); return null; }
        catch (Exception ex) { return LauncherPersistenceStatus.StoreSaveFailed(ex); }
    }

    /// <summary>Saves settings to the unified store. Used as callback by
    /// LauncherDragReorderWorkflow during drag commit.</summary>
    private string? SaveSettingsToStore(LauncherSettings settings)
    {
        var storeFile = new LauncherStoreFile
        {
            Entries = _entries.Select(ToLaunchEntry).ToImmutableList(),
            Settings = settings,
            Presets = _presetFile.Presets,
            DefaultPresetId = _presetFile.DefaultPresetId,
        };
        return SaveStoreFile(storeFile);
    }

    private void DiscardDragReorderPreview()
    {
        var result = _dragReorderWorkflow.Cancel();
        if (result.RefreshEntries)
        {
            _settings = _dragReorderWorkflow.Settings;
            ApplyFilter();
        }
    }

    private LauncherSettings CaptureSettingsMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return _settings with { SchemaVersion = 1 };
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    private static string RequireSettingsText(string? value, string fieldName) =>
        value ?? throw new InvalidDataException($"Launcher settings {fieldName} is null.");

    private string? SaveSettingsChange(LauncherSettings snapshot)
    {
        var saveFailure = SaveStoreFile(BuildStoreFile());
        if (saveFailure is not null)
            _settings = snapshot;
        return saveFailure;
    }

    internal void SetKeyboardFocusedEntryId(string? value)
    {
        if (EntryId.Same(_keyboardFocusedEntryId, value))
            return;

        // Clear old focus.
        if (_keyboardFocusedEntryId is not null)
        {
            foreach (var e in _entries)
            {
                if (e is null) continue;
                if (EntryId.Same(e.Id, _keyboardFocusedEntryId))
                    e.IsKeyboardFocused = false;
            }
        }

        _keyboardFocusedEntryId = value;

        // Set new focus.
        if (value is not null)
        {
            foreach (var e in _entries)
            {
                if (e is null) continue;
                if (EntryId.Same(e.Id, value))
                    e.IsKeyboardFocused = true;
            }
        }

        OnPropertyChanged(nameof(KeyboardFocusedEntryId));
    }    public static string EmptyStateText => "没有匹配的启动项";

}

public sealed record LauncherSortModeViewModel(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record LauncherDisplayModeViewModel(LauncherEntryViewMode Mode, string Label)
{
    public string Id => LauncherEntryViewModes.ToId(Mode);

    public override string ToString() => Label;
}

public static class LauncherMainWindowText
{
    public static string Ready => "就绪";

    public static string DryRunPlaceholder => "选择一个启动项查看预演命令。";

    public static string PresetSelectionPlaceholder => "选择启动组后可查看与当前勾选的差异。";

    public static string StorePathDisplay(string storePath) =>
        $"存储: {storePath}";

    public static string Loaded(int entryCount, int presetCount) =>
        $"已加载 {entryCount} 个启动项、{presetCount} 个启动组；勾选自动保存到 config\\launcher.json";
}

public static class ExceptionMessageFormatter
{
    /// <summary>
    /// Sanitize exception messages for user-facing display.
    /// File-system exceptions leak paths in their Message property;
    /// this replaces them with stable, user-safe strings.
    /// </summary>
    public static string ForUser(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception switch
        {
            // File-system exceptions leak paths in their Message — replace with safe text.
            FileNotFoundException => "找不到文件",
            DirectoryNotFoundException => "找不到目录",
            UnauthorizedAccessException => "没有访问权限",
            IOException => "文件操作失败",
            // Application-layer exceptions carry intentionally user-safe messages crafted
            // by the throwing code — pass them through.
            InvalidDataException or ArgumentException or InvalidOperationException => exception.Message,
            // Unknown exception types: use generic text to avoid leaking internal details.
            _ => "操作失败，请查看错误日志了解详情",
        };
    }
}

public static class LauncherPersistenceStatus
{
    public static string StoreSaveFailed(Exception exception) =>
        Failed("存储保存失败", exception);

    public static string ConfigSaveFailed(Exception exception) =>
        Failed("配置保存失败", exception);

    public static string PresetSaveFailed(Exception exception) =>
        Failed("启动组保存失败", exception);

    public static string SettingsSaveFailed(Exception exception) =>
        Failed("设置保存失败", exception);

    private static string Failed(string prefix, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{prefix}: {ExceptionMessageFormatter.ForUser(exception)}";
    }
}

public static class LauncherEntryStatus
{
    public static string AddCanceled => "已取消: 新增按钮";

    public static string AddDialogFailed(Exception exception) =>
        $"新增按钮对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string EditCanceled(string entryName) =>
        $"已取消: 编辑 {entryName}";

    public static string EditDialogFailed(string entryName, Exception exception) =>
        $"编辑 {entryName} 对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string DeleteCanceled(string entryName) =>
        $"已取消: 删除 {entryName}";

    public static string DeleteConfirmationFailed(string entryName, Exception exception) =>
        $"删除确认失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}";

    public static string Added(string entryName) =>
        $"已新增按钮: {entryName}（已保存）";

    public static string Updated(string entryName) =>
        $"已更新按钮: {entryName}（已保存）";

    public static string Deleted(string entryName) =>
        $"已删除按钮: {entryName}（已保存）";
}

public static class LauncherPresetStatus
{
    public static string NoPresetSelected => "未选择启动组";

    public static string SaveCanceled => "已取消: 保存启动组";

    public static string SaveDialogFailed(Exception exception) =>
        $"保存启动组对话框失败: {ExceptionMessageFormatter.ForUser(exception)}";

    public static string NameRequired => "启动组名称不能为空";

    public static string CurrentPresetMissing => "当前启动组已不存在，请重新加载";

    public static string MissingEntryReferences(IEnumerable<string> entryIds) =>
        $"启动组引用了不存在的启动项: {string.Join(", ", entryIds)}";

    public static string NoPresetEntries(string presetName) =>
        $"启动组\u201c{presetName}\u201d没有启动项";

    public static string Applied(string presetName, int entryCount) =>
        $"已应用启动组: {presetName}（{entryCount} 项，已保存）";

    public static string Previewed(string presetName, int entryCount) =>
        $"预演启动组: {presetName}（{entryCount} 项）";

    public static string PreviewFailed(string presetName, LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return $"启动组\u201c{presetName}\u201d预演失败: {report.FailedCount} 个失败";
    }

    public static string PreviewFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组\u201c{presetName}\u201d预演失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Launched(string presetName, LauncherBatchLaunchReport report) =>
        $"启动组\u201c{presetName}\u201d: {LauncherSelectionStatus.BatchLaunchCompleted(report, "启动项")}";

    public static string LaunchCanceled(string presetName, int entryCount) =>
        $"已取消: 启动组 {presetName}（{entryCount} 项）";

    public static string LaunchFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组\u201c{presetName}\u201d启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Saved(string presetName, int entryCount) =>
        $"已保存启动组: {presetName}（{entryCount} 项）";

    public static string UpdateFailed(string presetName, Exception exception) =>
        $"更新启动组失败: {presetName} — {ExceptionMessageFormatter.ForUser(exception)}";

    public static string UpdateBlocked(string presetName) =>
        $"系统启动组不能更新: {presetName}";

    public static string Updated(string presetName, int entryCount) =>
        $"已更新启动组: {presetName}（{entryCount} 项）";

    public static string DeleteBlocked(string presetName) =>
        $"启动组不能删除: {presetName}";

    public static string DeleteCanceled(string presetName) =>
        $"已取消: 删除启动组 {presetName}";

    public static string DeleteConfirmationFailed(string presetName, Exception exception) =>
        $"删除启动组确认失败: {presetName} \u2014 {ExceptionMessageFormatter.ForUser(exception)}";

    public static string Deleted(string presetName, string selectedPresetName) =>
        $"已删除启动组: {presetName}；当前启动组: {selectedPresetName}";
}

public static class LauncherSelectionStatus
{
    public static string NoVisibleEntries => "当前没有可勾选的可见项";

    public static string VisibleAlreadySelected => "可见项已全部勾选";

    public static string NoSelectedEntries => "未选择任何启动项";

    public static string NoSelectedEntriesToClear => "当前没有已勾选项";

    public static string SelectionRequired => "请先勾选至少一个启动项";

    public static string VisibleSelected(int entryCount) =>
        $"已勾选可见 {entryCount} 项（已保存）";

    public static string Cleared(int entryCount) =>
        $"已清空 {entryCount} 个勾选项（已保存）";

    public static string BatchLaunchCanceled(int entryCount) =>
        $"已取消: 批量启动 {entryCount} 项";

    public static string BatchLaunchCompleted(LauncherBatchLaunchReport report)
    {
        return BatchLaunchCompleted(report, "勾选项");
    }

    public static string BatchLaunchCompleted(LauncherBatchLaunchReport report, string itemLabel)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemLabel);
        return report.FailedCount == 0
            ? $"已请求启动 {report.RequestedCount} 个{itemLabel}"
            : $"已请求启动 {report.RequestedCount} 个{itemLabel}，{report.FailedCount} 个失败{FormatSkipped(report)}：{FormatBatchFailures(report.FailureMessages)}";
    }

    private static string FormatSkipped(LauncherBatchLaunchReport report) =>
        report.SkippedCount == 0 ? "" : $"，{report.SkippedCount} 个未启动";

    public static string BatchLaunchFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"批量启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    private static string FormatBatchFailures(IReadOnlyList<string> failures)
    {
        var preview = string.Join("；", failures.Take(3));
        if (failures.Count > 3)
            preview += $"；…另有 {failures.Count - 3} 项";

        return preview;
    }
}

public static class LauncherSettingsStatus
{
    public static string SortModeSaved(string modeLabel) =>
        $"排序模式: {modeLabel}（已保存）";

    public static string DisplayModeSaved(string modeLabel) =>
        $"视图模式: {modeLabel}（已保存）";

    public static string LaunchConfirmationSaved(bool requireConfirmation) =>
        requireConfirmation
            ? "启动确认: 所有启动项启动前统一确认"
            : "启动确认: 所有启动项直接启动";

    public static string WindowBoundsSaveFailed =>
        "窗口尺寸保存失败；下次将使用上次保存的尺寸";
}

public static class LauncherSingleLaunchStatus
{
    public static string Preview(string entryName, bool requiresConfirmation) =>
        requiresConfirmation
            ? $"预演: {entryName}（需要确认）"
            : $"预演: {entryName}";

    public static string PreviewFailed(string entryName) =>
        $"预演失败: {entryName}";

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Canceled(string entryName) =>
        $"已取消: {entryName}";

    public static string Requested(string entryName) =>
        $"已请求启动: {entryName}";

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {entryName} — {ExceptionMessageFormatter.ForUser(exception)}";
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {ExceptionMessageFormatter.ForUser(exception)}";
    }
}

public static class LauncherSingleLaunchReceiptText
{
    public static string Requested(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已请求启动: {RequireName(entryName)}",
            plan.CommandLine,
            $"（实际执行: {plan.ExecutionCommandLine}）");
    }

    public static string Canceled(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已取消: {RequireName(entryName)}",
            plan.CommandLine,
            $"（实际执行: {plan.ExecutionCommandLine}）");
    }

    public static string PreviewFailed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {RequireName(entryName)}",
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string ConfirmationFailed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动确认失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    public static string Failed(string entryName, LaunchPlan plan, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            plan.CommandLine,
            $"错误: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    private static string RequireName(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        var normalizedName = EntryName.Normalize(entryName);
        if (normalizedName.Length == 0)
            throw new ArgumentException("Entry name is required for single launch receipt.", nameof(entryName));

        return normalizedName;
    }
}

public static class LauncherBatchLaunchPlanText
{
    public static string Format(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> lines = [];
        AppendPlanLines(lines, items, "• ");
        return JoinBlocks(lines);
    }

    public static string FailedPreflight(LauncherBatchLaunchPreflight preflight)
    {
        ArgumentNullException.ThrowIfNull(preflight);

        List<string> lines = [
            "预演回执",
            $"预演失败: 失败 {preflight.Report.FailedCount} 项，未启动 {preflight.Items.Count} 项",
        ];

        AppendPlanLines(lines, preflight.Items, "• 未启动: ");
        AppendFailureLines(lines, preflight.Report.FailureMessages);
        return JoinBlocks(lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return JoinBlocks(
        [
            "预演回执",
            $"预演失败: {ExceptionMessageFormatter.ForUser(exception)}",
        ]);
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        string prefix)
    {
        foreach (var item in items)
            lines.Add($"{prefix}{item.EntryName}{Environment.NewLine}{item.Plan.CommandLine}{Environment.NewLine}（实际: {item.Plan.ExecutionCommandLine}）");
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("失败:");
        lines.AddRange(failures.Select(failure => $"- {failure}"));
    }

    private static string JoinBlocks(IReadOnlyList<string> lines) =>
        string.Join(Environment.NewLine + Environment.NewLine, lines);
}

public static class LauncherBatchLaunchReceiptText
{
    public static string Format(
        LauncherBatchLaunchPreflight preflight,
        LauncherBatchLaunchReport report)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(report);

        List<string> lines = [
            "启动回执",
            $"已请求启动 {report.RequestedCount} 项，失败 {report.FailedCount} 项，未启动 {report.SkippedCount} 项",
        ];

        AppendPlanLines(lines, preflight.Items, report);
        AppendFailureLines(lines, report.FailureMessages);
        return string.Join(Environment.NewLine, lines);
    }

    public static string Canceled(IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        List<string> lines = [
            "启动回执",
            $"已取消: 已请求启动 0 项，未启动 {items.Count} 项",
        ];

        AppendUnstartedPlanLines(lines, items);

        return string.Join(Environment.NewLine, lines);
    }

    public static string ConfirmationFailed(
        IReadOnlyList<LauncherBatchLaunchItem> items,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(exception);

        var lines = new List<string>
        {
            "启动回执",
            $"启动确认失败: {ExceptionMessageFormatter.ForUser(exception)}",
            $"已请求启动 0 项，未启动 {items.Count} 项",
        };

        AppendUnstartedPlanLines(lines, items);

        return string.Join(Environment.NewLine, lines);
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"批量启动失败: {ExceptionMessageFormatter.ForUser(exception)}");
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        LauncherBatchLaunchReport report)
    {
        if (items.Count == 0)
            return;

        lines.Add("");
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            lines.Add($"• {StatusFor(index, report)}: {item.EntryName}");
            lines.Add(item.Plan.CommandLine);
        }
    }

    private static string StatusFor(int index, LauncherBatchLaunchReport report)
    {
        // R9: With continue-on-failure, failures may be interspersed with successes.
        // Use the full failure-indices set rather than a single FailureIndex.
        if (report.FailureIndices.Contains(index))
            return "启动失败";

        // All non-failure items up through (RequestedCount + FailedCount - 1) are "已请求".
        // Items past that range are "未启动" (only happens when items are skipped).
        return index < report.RequestedCount + report.FailedCount ? "已请求" : "未启动";
    }

    private static void AppendFailureLines(List<string> lines, IReadOnlyList<string> failures)
    {
        if (failures.Count == 0)
            return;

        lines.Add("");
        lines.Add("失败:");
        lines.AddRange(failures.Select(failure => $"- {failure}"));
    }

    private static void AppendUnstartedPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items)
    {
        if (items.Count == 0)
            return;

        lines.Add("");
        foreach (var item in items)
        {
            lines.Add($"• 未启动: {item.EntryName}");
            lines.Add(item.Plan.CommandLine);
        }
    }
}
