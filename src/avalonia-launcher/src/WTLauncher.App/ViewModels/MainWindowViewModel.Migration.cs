using System.Collections.Immutable;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task ExportAllConfigAsync()
    {
        await ExportConfigAsync(_list.MutableEntries.Select(entry => entry.Id).ToImmutableList());
    }

    [RelayCommand]
    private async Task ExportSelectedConfigAsync()
    {
        var ids = _list.MutableEntries
            .Where(entry => entry.IsSelected)
            .Select(entry => entry.Id)
            .ToImmutableList();
        if (ids.Count == 0)
        {
            StatusContent = LauncherStatusContent.Warning("没有勾选条目，请先勾选要导出的条目。");
            return;
        }

        await ExportConfigAsync(ids);
    }

    [RelayCommand]
    private async Task ImportConfigAsync()
    {
        DiscardDragReorderPreview();
        try
        {
            var path = await _dialogService.PickImportPathAsync();
            if (string.IsNullOrWhiteSpace(path))
                return;

            var imported = LauncherStore.Load(path);
            var plan = await _dialogService.RequestImportAsync(BuildStoreFile(), imported);
            if (plan is null)
            {
                StatusContent = LauncherRefreshStatus.DialogCanceled("导入");
                return;
            }

            var merged = LauncherConfigExchange.ApplyImport(BuildStoreFile(), imported, plan);
            var saveFailure = SaveStoreFile(merged);
            if (saveFailure is not null)
            {
                RestoreVmStateFromStore();
                ApplyStatusText(saveFailure);
                return;
            }

            ApplyStoreFileToVm(merged);
            StatusContent = LauncherStatusContent.Success(
                plan.Mode == LauncherImportMode.Replace
                    ? "已替换为导入配置。"
                    : "已新增导入条目与启动组。");
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Migration.Import");
            StatusContent = LauncherRefreshStatus.DialogOpenFailed("导入", ex);
        }
    }

    private void ApplyStoreFileToVm(LauncherStoreFile store)
    {
        var preferredPresetId = store.Presets.Any(preset =>
            string.Equals(preset.Id, SelectedPreset?.Id, StringComparison.OrdinalIgnoreCase))
            ? SelectedPreset?.Id
            : null;
        _presetManager.Load(
            new LauncherPresetFile
            {
                Presets = store.Presets,
                DefaultPresetId = store.DefaultPresetId,
            },
            preferredPresetId);
        _list.ReplaceEntries(store.Entries);
        EnsureCustomOrderContainsEntries();
        RefreshAllProjections();
        _launchOrchestrator.CommandTemplates = store.Settings.CommandTemplates;
    }

    private async Task ExportConfigAsync(IReadOnlyList<string> entryIds)
    {
        try
        {
            var path = await _dialogService.PickExportPathAsync();
            if (string.IsNullOrWhiteSpace(path))
                return;

            var export = LauncherConfigExchange.BuildExportFile(BuildStoreFile(), entryIds);
            LauncherJson.Save(export, path, backup: false);
            StatusContent = LauncherStatusContent.Success($"已导出 {export.Entries.Count} 个条目。");
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "Migration.Export");
            StatusContent = LauncherRefreshStatus.DialogOpenFailed("导出", ex);
        }
    }
}
