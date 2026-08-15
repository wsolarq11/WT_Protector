using System.Collections.Immutable;
using WTLauncher.App.Features;
using WTLauncher.App.Features.ConfigExchange;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Exchange command workflow: import/export/migration through ConfigExchangeService
/// with one StoreMutationSession commit for imported stores.
/// </summary>
public sealed partial class LauncherExchangeCommandWorkflow
{
    private readonly ICommandExecutionContext _context;
    private readonly EntryListViewModel _entries;
    private readonly PresetManager _presets;
    private readonly IDialogService _dialogService;
    private readonly IConfigExchangeService _exchange;
    private readonly LaunchOrchestrator _launch;
    private readonly ICommandHost _host;

    public LauncherExchangeCommandWorkflow(
        ICommandExecutionContext context,
        EntryListViewModel entries,
        PresetManager presets,
        IDialogService dialogService,
        IConfigExchangeService exchange,
        LaunchOrchestrator launch,
        ICommandHost host)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _presets = presets ?? throw new ArgumentNullException(nameof(presets));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        _launch = launch ?? throw new ArgumentNullException(nameof(launch));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public async Task ExportAllAsync()
    {
        await ExportAsync(_entries.MutableEntries.Select(entry => entry.Id).ToImmutableList()).ConfigureAwait(true);
    }

    public async Task ExportSelectedAsync()
    {
        var ids = _entries.MutableEntries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Id)
            .ToImmutableList();
        if (ids.Count == 0)
        {
            _context.Status.Apply(LauncherStatusContent.Warning("没有勾选条目，请先勾选要导出的条目。"));
            return;
        }

        await ExportAsync(ids).ConfigureAwait(true);
    }

    public async Task ImportAsync()
    {
        _host.DiscardDragReorderPreview();
        try
        {
            var path = await _dialogService.PickImportPathAsync();
            if (string.IsNullOrWhiteSpace(path))
                return;

            var imported = _exchange.LoadImport(path);
            var plan = await _dialogService.RequestImportAsync(_host.BuildStoreFile(), imported);
            if (plan is null)
            {
                _context.Status.Apply(LauncherRefreshStatus.DialogCanceled("导入"));
                return;
            }

            var merged = _exchange.ApplyImport(_host.BuildStoreFile(), imported, plan);
            using var transaction = new StoreMutationSession(_host.SaveStoreFile, _host.RestoreVmStateFromStore);
            var saveFailure = transaction.Commit(merged);
            if (saveFailure is not null)
            {
                ApplyStatus(LauncherStatusContent.Error(saveFailure));
                return;
            }

            ApplyStoreFileToVm(merged);
            ApplyStatus(LauncherStatusContent.Success(
                plan.Mode == LauncherImportMode.Replace
                    ? "已替换为导入配置。"
                    : "已新增导入条目与启动组。"));
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Migration.Import");
            ApplyStatus(LauncherRefreshStatus.DialogOpenFailed("导入", ex));
        }
    }

    private async Task ExportAsync(IReadOnlyList<string> entryIds)
    {
        try
        {
            var path = await _dialogService.PickExportPathAsync();
            if (string.IsNullOrWhiteSpace(path))
                return;

            var export = _exchange.BuildExport(_host.BuildStoreFile(), entryIds);
            _exchange.SaveExport(export, path);
            ApplyStatus(LauncherStatusContent.Success($"已导出 {export.Entries.Count} 个条目。"));
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Migration.Export");
            ApplyStatus(LauncherRefreshStatus.DialogOpenFailed("导出", ex));
        }
    }

    private void ApplyStoreFileToVm(LauncherStoreFile store)
    {
        var preferredPresetId = store.Presets.Any(preset =>
            string.Equals(preset.Id, _presets.SelectedPreset?.Id, StringComparison.OrdinalIgnoreCase))
            ? _presets.SelectedPreset?.Id
            : null;
        _presets.Load(
            new LauncherPresetFile
            {
                Presets = store.Presets,
                DefaultPresetId = store.DefaultPresetId,
            },
            preferredPresetId);
        _entries.ReplaceEntries(store.Entries);
        _host.EnsureCustomOrderContainsEntries();
        _context.SynchronizeProjections(ProjectionSyncMode.FullReload);
        _launch.CommandTemplates = store.Settings.CommandTemplates;
    }

    private void ApplyStatus(LauncherStatusContent? status)
    {
        if (status is not null)
            _context.Status.Apply(status);
    }
}
