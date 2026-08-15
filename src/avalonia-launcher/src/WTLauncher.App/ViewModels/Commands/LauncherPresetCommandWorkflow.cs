using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using WTLauncher.App.Features;
using WTLauncher.App.Features.Entries;
using WTLauncher.App.Features.Presets;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

/// <summary>
/// Preset command workflow: apply/preview/launch, CRUD, selection preview and
/// preset mutation persistence. MainWindowViewModel keeps the command surface.
/// </summary>
public sealed partial class LauncherPresetCommandWorkflow
{
    private readonly ICommandExecutionContext _context;
    private readonly EntryListViewModel _entries;
    private readonly PresetManager _presets;
    private readonly IPresetService _service;
    private readonly LaunchOrchestrator _launch;
    private readonly ICommandHost _host;

    public LauncherPresetCommandWorkflow(
        ICommandExecutionContext context,
        EntryListViewModel entries,
        PresetManager presets,
        IPresetService service,
        LaunchOrchestrator launch,
        ICommandHost host)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _presets = presets ?? throw new ArgumentNullException(nameof(presets));
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _launch = launch ?? throw new ArgumentNullException(nameof(launch));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public int GetReferenceCount(string entryId) =>
        _presets.GetReferenceCount(entryId, _service);

    public bool CanApplyPreset() =>
        TryBuildPresetSelectionPreviewForCommand(out _, out var selection)
        && selection.MissingConfigIds.Count == 0
        && selection.PresetIds.Count > 0;

    public bool CanLaunchSelectedPreset() =>
        TryBuildPresetSelectionPreviewForCommand(out _, out var selection)
        && selection.MissingConfigIds.Count == 0
        && selection.PresetIds.Count > 0;

    public bool CanUpdateSelectedPreset()
    {
        if (!TryBuildPresetSelectionPreviewForCommand(out var selectedPreset, out var selection))
            return false;

        if (!_presets.CanUpdatePreset(selectedPreset.Id, _service))
            return false;

        return selection.SelectedIds.Count > 0 && selection.HasChanges;
    }

    public bool CanDeleteSelectedPreset() =>
        TryGetSelectedPreset(out var selectedPreset)
        && _presets.CanDeletePreset(selectedPreset.Id, _service);

    public LauncherPresetSelectionPreview BuildSelectionPreview(IReadOnlyList<string> selectedEntryIds)
    {
        if (!TryGetSelectedPreset(out var selectedPreset))
            return LauncherPresetSelectionPreview.NoPreset(_presets.PresetCount);

        return _service.BuildSelectionPreview(
            selectedPreset.Name,
            selectedEntryIds,
            GetEntryIds(),
            selectedPreset.EntryIds);
    }

    public void LoadPresetFile(string? preferredPresetId)
    {
        LauncherStoreFile storeFile;
        try
        {
            storeFile = _context.Store.Select(s => s);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "LoadPresetFile");
            storeFile = new LauncherStoreFile();
        }

        var presetFile = new LauncherPresetFile
        {
            Presets = storeFile.Presets,
            DefaultPresetId = storeFile.DefaultPresetId,
        };
        _presets.Load(presetFile, preferredPresetId);
    }

    public void ApplyPreset()
    {
        _host.DiscardDragReorderPreview();

        if (!TryGetApplicablePresetSelection(out var selectedPreset, out var selection))
        {
            _host.RefreshSelectionState();
            return;
        }

        if (_host.ReplaceSelection(selection.PresetIds))
            ApplyStatus(LauncherPresetStatus.Applied(selectedPreset.Name, selection.PresetIds.Count));
    }

    public async Task LaunchSelectedPresetAsync()
    {
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out _))
            return;

        await _launch.LaunchBatchAsync(
            GetPresetEntries(selectedPreset.EntryIds),
            $"启动组“{selectedPreset.Name}”条目",
            LauncherPresetStatus.NoPresetEntries(selectedPreset.Name),
            count => LauncherPresetStatus.LaunchCanceled(selectedPreset.Name, count),
            ex => LauncherPresetStatus.LaunchFailed(selectedPreset.Name, ex),
            report => LauncherPresetStatus.Launched(selectedPreset.Name, report),
            LauncherRunSource.Preset).ConfigureAwait(true);
    }

    public void PreviewSelectedPreset()
    {
        if (!TryGetApplicablePresetSelection(out var selectedPreset, out _))
            return;

        var entries = GetPresetEntries(selectedPreset.EntryIds);
        if (entries.Count == 0)
        {
            ApplyStatus(LauncherPresetStatus.NoPresetEntries(selectedPreset.Name));
            return;
        }

        _host.DiscardDragReorderPreview();
        try
        {
            var preflight = _launch.PreflightBatch(entries.Select(ToLaunchEntry));
            if (preflight.Report.FailedCount > 0)
            {
                _context.Status.SetDryRunText(LauncherBatchLaunchPlanText.FailedPreflight(preflight));
                ApplyStatus(LauncherPresetStatus.PreviewFailed(selectedPreset.Name, preflight.Report));
                return;
            }

            _context.Status.SetDryRunText(LauncherBatchLaunchPlanText.Format(preflight.Items));
            ApplyStatus(LauncherPresetStatus.Previewed(selectedPreset.Name, preflight.Items.Count));
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "PresetPreview");
            _context.Status.SetDryRunText(LauncherBatchLaunchPlanText.Failed(ex));
            ApplyStatus(LauncherPresetStatus.PreviewFailed(selectedPreset.Name, ex));
        }
    }

    public async Task LaunchGroupAsync(IReadOnlyList<LauncherEntryViewModel> entries)
    {
        if (entries.Count == 0)
        {
            ApplyStatus(LauncherRefreshStatus.GroupNoEntries);
            return;
        }

        await _launch.LaunchBatchAsync(
            entries,
            confirmationTargetLabel: $"分组 \"{entries[0].Command}\"",
            emptyStatus: LauncherRefreshStatus.GroupNoEntries,
            canceledStatus: _ => LauncherRefreshStatus.CanceledBatch,
            failedStatus: LauncherSelectionStatus.BatchLaunchFailed,
            completedStatus: LauncherGroupStatus.BatchCompleted,
            source: LauncherRunSource.Group).ConfigureAwait(true);
    }

    public async Task SaveGroupAsPresetAsync(IReadOnlyList<string> entryIds)
    {
        if (entryIds.Count == 0)
        {
            ApplyStatus(LauncherRefreshStatus.GroupNoEntriesCannotSave);
            return;
        }

        await SaveAsPresetAsync(entryIds).ConfigureAwait(true);
    }

    public async Task<bool> SaveAsPresetAsync(IReadOnlyList<string> entryIds)
    {
        if (entryIds.Count == 0)
        {
            ApplyStatus(LauncherSelectionStatus.NoSelectedEntries);
            return false;
        }

        _host.DiscardDragReorderPreview();
        var target = new LauncherPresetSaveTarget(entryIds);
        var presetName = await RequestPresetNameIfRequiredAsync(target).ConfigureAwait(true);
        if (presetName is null)
            return false;

        var snapshot = _presets.CaptureSnapshot();
        var addResult = _service.AddPreset(_presets.File, presetName, target.EntryIds, GetEntryIds());
        if (addResult is not ServiceSuccess<PresetMutation> addSuccess)
        {
            _presets.Load(snapshot.File, snapshot.SelectedPresetId);
            ApplyStatus(addResult switch
            {
                ServiceCanceled<PresetMutation> canceled => canceled.Status,
                ServiceFailure<PresetMutation> failure => failure.Status,
                _ => throw new UnreachableException(),
            });
            return false;
        }

        var preset = addSuccess.Value.Preset;
        if (!SavePresetMutationAndApply(snapshot, addSuccess.Value.PresetFile, preset.Id, out var savedPreset))
            return false;

        if (savedPreset is null) return false;
        ApplyStatus(LauncherPresetStatus.Saved(savedPreset.Name, savedPreset.EntryIds.Count));
        return true;
    }

    public void UpdateSelectedPreset()
    {
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

        if (!_presets.CanUpdatePreset(selectedPreset.Id, _service))
        {
            ApplyStatus(LauncherPresetStatus.UpdateBlocked(selectedPreset.Name));
            return;
        }

        var entryIds = GetSelectedEntryIds();
        if (entryIds.Count == 0)
        {
            ApplyStatus(LauncherSelectionStatus.SelectionRequired);
            return;
        }

        var preview = BuildSelectionPreview(selectedPreset, entryIds);
        if (!preview.HasChanges)
        {
            ApplyStatus(LauncherRefreshStatus.GroupAlreadyUpToDate(selectedPreset.Name));
            return;
        }

        var snapshot = _presets.CaptureSnapshot();
        var updateResult = _service.UpdatePreset(_presets.File, selectedPreset.Id, selectedPreset.Name, entryIds, GetEntryIds());
        if (updateResult is not ServiceSuccess<PresetMutation> updateSuccess)
        {
            _presets.Load(snapshot.File, snapshot.SelectedPresetId);
            ApplyStatus(updateResult switch
            {
                ServiceCanceled<PresetMutation> canceled => canceled.Status,
                ServiceFailure<PresetMutation> failure => failure.Status,
                _ => throw new UnreachableException(),
            });
            return;
        }

        var preset = updateSuccess.Value.Preset;
        if (!SavePresetMutationAndApply(snapshot, updateSuccess.Value.PresetFile, preset.Id, out var savedPreset))
            return;

        if (savedPreset is not null)
            ApplyStatus(LauncherPresetStatus.Updated(savedPreset.Name, savedPreset.EntryIds.Count));
    }

    public async Task DeleteSelectedPresetAsync()
    {
        _host.DiscardDragReorderPreview();
        var selectedPreset = GetSelectedPresetOrReportMissing();
        if (selectedPreset is null)
            return;

        var target = new LauncherPresetDeleteTarget(
            selectedPreset.Id,
            selectedPreset.Name,
            selectedPreset.EntryIds);
        if (!_presets.CanDeletePreset(target.PresetId, _service))
        {
            ApplyStatus(LauncherPresetStatus.DeleteBlocked(target.PresetName));
            return;
        }

        if (!await ConfirmDeletePresetIfRequiredAsync(target).ConfigureAwait(true))
            return;

        var snapshot = _presets.CaptureSnapshot();
        var deleteResult = _service.DeletePreset(_presets.File, target.PresetId);
        if (deleteResult is not ServiceSuccess<PresetDeleteMutation> deleteSuccess)
        {
            _presets.Load(snapshot.File, snapshot.SelectedPresetId);
            ApplyStatus(deleteResult switch
            {
                ServiceCanceled<PresetDeleteMutation> canceled => canceled.Status,
                ServiceFailure<PresetDeleteMutation> failure => failure.Status,
                _ => throw new UnreachableException(),
            });
            return;
        }

        if (!SavePresetMutationAndApply(snapshot, deleteSuccess.Value.PresetFile, deleteSuccess.Value.FallbackPresetId, out var fallbackPreset))
            return;

        ApplyStatus(fallbackPreset is not null
            ? LauncherPresetStatus.Deleted(target.PresetName, fallbackPreset.Name)
            : LauncherPresetStatus.Deleted(target.PresetName, string.Empty));
    }

    private bool TryGetApplicablePresetSelection(
        [NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset,
        [NotNullWhen(true)] out LauncherPresetSelectionPreview? selection)
    {
        selectedPreset = GetSelectedPresetOrReportMissing();
        selection = null;
        if (selectedPreset is null)
            return false;

        selection = BuildSelectionPreview(GetSelectedEntryIds());
        if (selection.MissingConfigIds.Count > 0)
        {
            ApplyStatus(LauncherPresetStatus.MissingEntryReferences(selection.MissingConfigIds));
            return false;
        }

        return true;
    }

    private LauncherPresetViewModel? GetSelectedPresetOrReportMissing()
    {
        if (TryGetSelectedPreset(out var selectedPreset))
            return selectedPreset;

        ApplyStatus(LauncherPresetStatus.NoPresetSelected);
        return null;
    }

    private bool TryGetSelectedPreset([NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset)
    {
        selectedPreset = _presets.SelectedPreset;
        return selectedPreset is not null;
    }

    private async Task<bool> ConfirmDeletePresetIfRequiredAsync(LauncherPresetDeleteTarget target)
    {
        var result = await _service.ConfirmDeletePresetAsync(target).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<LauncherPresetDeleteTarget>:
                return true;
            case ServiceCanceled<LauncherPresetDeleteTarget> canceled:
                ApplyStatus(canceled.Status);
                return false;
            case ServiceFailure<LauncherPresetDeleteTarget> failure:
                ApplyStatus(failure.Status);
                return false;
            default:
                throw new UnreachableException();
        }
    }

    private async Task<string?> RequestPresetNameIfRequiredAsync(LauncherPresetSaveTarget target)
    {
        var result = await _service.RequestNewPresetAsync(
            target, _presets.Presets.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)).ConfigureAwait(true);
        switch (result)
        {
            case ServiceSuccess<string> success:
                return success.Value;
            case ServiceCanceled<string> canceled:
                ApplyStatus(canceled.Status);
                return null;
            case ServiceFailure<string> failure:
                ApplyStatus(failure.Status);
                return null;
            default:
                throw new UnreachableException();
        }
    }

    private bool TryBuildPresetSelectionPreviewForCommand(
        [NotNullWhen(true)] out LauncherPresetViewModel? selectedPreset,
        [NotNullWhen(true)] out LauncherPresetSelectionPreview? selection)
    {
        selectedPreset = null;
        selection = null;
        if (!TryGetSelectedPreset(out selectedPreset) || _entries.MutableEntries.Count == 0)
            return false;

        selection = BuildSelectionPreview(selectedPreset, GetSelectedEntryIds());
        return true;
    }

    private LauncherPresetSelectionPreview BuildSelectionPreview(
        LauncherPresetViewModel selectedPreset,
        IReadOnlyList<string> selectedEntryIds) =>
        _service.BuildSelectionPreview(
            selectedPreset.Name,
            selectedEntryIds,
            GetEntryIds(),
            selectedPreset.EntryIds);

    private List<LauncherEntryViewModel> GetPresetEntries(IEnumerable<string> entryIds)
    {
        var entriesById = new Dictionary<string, LauncherEntryViewModel>(EntryId.Comparer);
        foreach (var entry in _entries.MutableEntries)
        {
            if (!entriesById.ContainsKey(entry.Id))
                entriesById[entry.Id] = entry;
        }

        var uniqueIds = EntryId.ToRequiredUniqueList(entryIds, nameof(entryIds));
        var result = new List<LauncherEntryViewModel>(uniqueIds.Count);
        foreach (var id in uniqueIds)
        {
            if (!entriesById.TryGetValue(id, out var entry))
            {
                ErrorLog.Write(new InvalidDataException($"Preset entry ID '{id}' does not match any loaded entry."), "GetPresetEntries");
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    private bool SavePresetMutationAndApply(
        (LauncherPresetFile PresetFile, string? SelectedPresetId) snapshot,
        LauncherPresetFile newPresetFile,
        string selectedPresetId,
        out LauncherPresetViewModel? selectedPreset)
    {
        selectedPreset = null;
        var storeFile = _context.Store.Select(s => s) with
        {
            Presets = newPresetFile.Presets,
            DefaultPresetId = newPresetFile.DefaultPresetId,
        };
        try
        {
            _context.Store.Dispatch(_ => storeFile);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "SavePresetMutation");
            _presets.Load(snapshot.PresetFile, snapshot.SelectedPresetId);
            ApplyErrorStatus(LauncherPersistenceStatus.StoreSaveFailed(ex).Text);
            return false;
        }

        _presets.ApplyPresetMutation(newPresetFile, selectedPresetId);
        selectedPreset = _presets.SelectedPreset;
        return true;
    }

    private IEnumerable<string> GetEntryIds() =>
        _entries.MutableEntries.Select(entry => entry.Id);

    private IReadOnlyList<string> GetSelectedEntryIds()
    {
        var ids = new List<string>(_entries.MutableEntries.Count);
        foreach (var entry in _entries.MutableEntries)
            if (entry.IsSelected) ids.Add(entry.Id);
        return ids;
    }

    private static LaunchEntry ToLaunchEntry(LauncherEntryViewModel entry) => entry.SnapshotEntry();

    private void ApplyStatus(LauncherStatusContent? status)
    {
        if (status is not null)
            _context.Status.Apply(status);
    }

    private void ApplyErrorStatus(string? statusText)
    {
        if (statusText is not null)
            ApplyStatus(LauncherStatusContent.Error(statusText));
    }
}
