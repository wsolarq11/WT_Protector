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
    private LauncherSettings _settings;
    private readonly LauncherLaunchWorkflow _launchWorkflow;
    private readonly Func<DateTimeOffset> _utcNow;
    private LauncherPresetFile _presetFile = new();
    private readonly ObservableCollection<LauncherEntryViewModel> _entries = [];
    private ObservableCollection<LauncherEntryViewModel> _visibleEntries = [];
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

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _presetCount;

    [ObservableProperty]
    private LauncherPresetViewModel? _selectedPreset;

    [ObservableProperty]
    private bool _requireConfirmationForLaunches;

    public MainWindowViewModel(
        string configPath,
        string settingsPath,
        LauncherSettings settings,
        LauncherLaunchWorkflow launchWorkflow,
        Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _configPath = configPath;
        _presetPath = LauncherPresetStore.ResolvePresetPath(configPath);
        _settingsPath = settingsPath;
        _settings = settings;
        _launchWorkflow = launchWorkflow ?? throw new ArgumentNullException(nameof(launchWorkflow));
        _launchWorkflow.CommandTemplates = settings.CommandTemplates;
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        Entries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_entries);
        VisibleEntries = new ReadOnlyObservableCollection<LauncherEntryViewModel>(_visibleEntries);
        Presets = new ReadOnlyObservableCollection<LauncherPresetViewModel>(_presets);
        SortModes = new ReadOnlyObservableCollection<LauncherSortModeViewModel>(_sortModes);
        DisplayModes = new ReadOnlyObservableCollection<LauncherDisplayModeViewModel>(_displayModes);
        ConfigPathDisplay = LauncherMainWindowText.ConfigPathDisplay(configPath, _presetPath);
        _filterText = RequireSettingsText(settings.LastFilterText, "lastFilterText");
        _requireConfirmationForLaunches = settings.RequireConfirmationForLaunches;
        _selectedSortMode = ResolveSortMode(settings.SortMode);
        _settings = _settings with { SortMode = _selectedSortMode.Id };
        _selectedDisplayMode = ResolveDisplayMode(settings.EntryViewMode);
        _settings = _settings with { EntryViewMode = _selectedDisplayMode.Id };
        // Construct workflow AFTER settings normalization so it captures
        // the normalized values — DiscardDragReorderPreview syncs from it.
        _dragReorderWorkflow = new LauncherDragReorderWorkflow(
            _settings,
            _settingsPath,
            _entries);
        Refresh();
    }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> Entries { get; }

    public ReadOnlyObservableCollection<LauncherEntryViewModel> VisibleEntries { get; private set; }

    public ReadOnlyObservableCollection<LauncherPresetViewModel> Presets { get; }

    /// <summary>Current settings snapshot. Immutable — replaced on every mutation.
    /// Tests should read from this, not from a stale pre-construction reference.</summary>
    public LauncherSettings Settings => _settings;

    public IReadOnlyDictionary<string, string>? CommandTemplates => _launchWorkflow.CommandTemplates;

    public string ConfigPathDisplay { get; }

    [RelayCommand]
    private void Refresh()
    {
        DiscardDragReorderPreview();
        ReplaceEntries(LauncherConfigStore.Load(_configPath));
        LauncherEntryOrder.SyncWithEntries(_settings.CustomEntryOrder, GetEntryIds());

        // E10: Cross-validate $key command references against commandTemplates.
        ValidateCommandTemplateReferences();

        LoadPresetFile(preferredPresetId: null);

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

    private void ApplyStatusText(string? statusText)
    {
        if (statusText is not null)
            StatusText = statusText;
    }

    private bool SaveConfig()
    {
        var saveFailure = LauncherConfigMutationPersistence.Save(_entries.Select(ToLaunchEntry), _configPath);
        if (saveFailure is not null)
        {
            ApplyStatusText(saveFailure);
            return false;
        }

        return true;
    }

    private void DiscardDragReorderPreview()
    {
        var result = _dragReorderWorkflow.Cancel();
        _settings = _dragReorderWorkflow.Settings;
        if (result.RefreshEntries)
            ApplyFilter();
    }

    private LauncherSettingsSnapshot CaptureSettingsMutationSnapshot()
    {
        DiscardDragReorderPreview();
        return LauncherSettingsSnapshot.Capture(_settings);
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    private static string RequireSettingsText(string? value, string fieldName) =>
        value ?? throw new InvalidDataException($"Launcher settings {fieldName} is null.");

    private string? SaveSettingsChange(LauncherSettingsSnapshot snapshot)
    {
        var saveFailure = LauncherSettingsMutationPersistence.Save(
            ref _settings,
            _settingsPath,
            snapshot);
        return saveFailure;
    }

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
    public static string Ready => "Ready";

    public static string DryRunPlaceholder => "选择一个启动项查看预演命令。";

    public static string PresetSelectionPlaceholder => "选择启动组后可查看与当前勾选的差异。";

    public static string ConfigPathDisplay(string configPath, string presetPath) =>
        $"配置: {configPath} · 启动组: {presetPath}";

    public static string Loaded(int entryCount, int presetCount) =>
        $"已加载 {entryCount} 个启动项、{presetCount} 个启动组；勾选自动保存到 config\\config.json";
}

public static class LauncherPersistenceStatus
{
    public static string ConfigSaveFailed(Exception exception) =>
        Failed("配置保存失败", exception);

    public static string PresetSaveFailed(Exception exception) =>
        Failed("启动组保存失败", exception);

    public static string SettingsSaveFailed(Exception exception) =>
        Failed("设置保存失败", exception);

    public static string RollbackFailed(string currentStatus, IReadOnlyList<string> restoreErrors) =>
        restoreErrors.Count == 0
            ? currentStatus
            : $"{currentStatus}；回滚磁盘状态失败: {string.Join("；", restoreErrors)}";

    private static string Failed(string prefix, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"{prefix}: {exception.Message}";
    }
}

public static class LauncherEntryStatus
{
    public static string AddCanceled => "已取消: 新增按钮";

    public static string AddDialogFailed(Exception exception) =>
        $"新增按钮对话框失败: {exception.Message}";

    public static string EditCanceled(string entryName) =>
        $"已取消: 编辑 {entryName}";

    public static string EditDialogFailed(string entryName, Exception exception) =>
        $"编辑 {entryName} 对话框失败: {exception.Message}";

    public static string DeleteCanceled(string entryName) =>
        $"已取消: 删除 {entryName}";

    public static string DeleteConfirmationFailed(string entryName, Exception exception) =>
        $"删除确认失败: {entryName} — {exception.Message}";

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
        $"保存启动组对话框失败: {exception.Message}";

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
        return $"启动组\u201c{presetName}\u201d预演失败: {exception.Message}";
    }

    public static string Launched(string presetName, LauncherBatchLaunchReport report) =>
        $"启动组\u201c{presetName}\u201d: {LauncherSelectionStatus.BatchLaunchCompleted(report, "启动项")}";

    public static string LaunchCanceled(string presetName, int entryCount) =>
        $"已取消: 启动组 {presetName}（{entryCount} 项）";

    public static string LaunchFailed(string presetName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动组\u201c{presetName}\u201d启动失败: {exception.Message}";
    }

    public static string Saved(string presetName, int entryCount) =>
        $"已保存启动组: {presetName}（{entryCount} 项）";

    public static string UpdateBlocked(string presetName) =>
        $"系统启动组不能更新: {presetName}";

    public static string Updated(string presetName, int entryCount) =>
        $"已更新启动组: {presetName}（{entryCount} 项）";

    public static string DeleteBlocked(string presetName) =>
        $"启动组不能删除: {presetName}";

    public static string DeleteCanceled(string presetName) =>
        $"已取消: 删除启动组 {presetName}";

    public static string DeleteConfirmationFailed(string presetName, Exception exception) =>
        $"删除启动组确认失败: {presetName} \u2014 {exception.Message}";

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
        return $"批量启动失败: {exception.Message}";
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
        return $"预演失败: {exception.Message}";
    }

    public static string Canceled(string entryName) =>
        $"已取消: {entryName}";

    public static string Requested(string entryName) =>
        $"已请求启动: {entryName}";

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {entryName} — {exception.Message}";
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"启动失败: {exception.Message}";
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
            plan.CommandLine);
    }

    public static string Canceled(string entryName, LaunchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"已取消: {RequireName(entryName)}",
            plan.CommandLine);
    }

    public static string PreviewFailed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {RequireName(entryName)}",
            $"错误: {exception.Message}");
    }

    public static string PreviewFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "预演回执",
            $"预演失败: {exception.Message}");
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
            $"错误: {exception.Message}");
    }

    public static string Failed(string entryName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {RequireName(entryName)}",
            $"错误: {exception.Message}");
    }

    public static string Failed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return string.Join(
            Environment.NewLine,
            "启动回执",
            $"启动失败: {exception.Message}");
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
            $"错误: {exception.Message}");
    }

    private static string RequireName(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        var normalizedName = LauncherEntryText.Normalize(entryName);
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
            $"预演失败: {exception.Message}",
        ]);
    }

    private static void AppendPlanLines(
        List<string> lines,
        IReadOnlyList<LauncherBatchLaunchItem> items,
        string prefix)
    {
        foreach (var item in items)
            lines.Add($"{prefix}{item.EntryName}{Environment.NewLine}{item.Plan.CommandLine}");
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
            $"启动确认失败: {exception.Message}",
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
            $"批量启动失败: {exception.Message}");
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
        if (report.FailureIndex == index)
            return "启动失败";

        return index < report.RequestedCount ? "已请求" : "未启动";
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
