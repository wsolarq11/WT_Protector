using WTLauncher.App.Features.Dialogs;
using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Presets;

public sealed class PresetService : IPresetService
{
    private readonly IDialogService _dialog;

    public PresetService(IDialogService dialog)
    {
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    public int CountEntryReferences(LauncherPresetFile presetFile, string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return 0;

        var normalized = EntryId.Normalize(entryId);
        return presetFile.Presets.Count(preset =>
            preset.EntryIds.Any(id => EntryId.Same(id, normalized)));
    }

    public LauncherPresetSelectionPreview BuildSelectionPreview(
        string presetName,
        IEnumerable<string> selectedEntryIds,
        IEnumerable<string> configuredEntryIds,
        IEnumerable<string> presetEntryIds) =>
        LauncherPresetSelectionPreview.ForPreset(
            presetName,
            selectedEntryIds,
            configuredEntryIds,
            presetEntryIds);

    public bool CanUpdatePreset(LauncherPresetFile presetFile, string presetId) =>
        LauncherPresetWorkflow.CanUpdatePreset(presetFile, presetId);

    public bool CanDeletePreset(LauncherPresetFile presetFile, string presetId) =>
        LauncherPresetWorkflow.CanDeletePreset(presetFile, presetId);

    public async Task<ServiceResult<string>> RequestNewPresetAsync(
        LauncherPresetSaveTarget target, HashSet<string> existingNames)
    {
        string? presetName;
        try
        {
            presetName = await _dialog.RequestNewPresetAsync(target, existingNames).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "RequestPresetName");
            return new ServiceFailure<string>(LauncherPresetStatus.SaveDialogFailed(ex));
        }

        if (presetName is null)
            return new ServiceCanceled<string>(LauncherPresetStatus.SaveCanceled);

        var normalizedName = EntryName.Normalize(presetName);
        if (normalizedName.Length == 0)
            return new ServiceFailure<string>(LauncherPresetStatus.NameRequired);

        return new ServiceSuccess<string>(normalizedName);
    }

    public async Task<ServiceResult<LauncherPresetDeleteTarget>> ConfirmDeletePresetAsync(
        LauncherPresetDeleteTarget target)
    {
        bool accepted;
        try
        {
            accepted = await _dialog.ConfirmDeletePresetAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeletePresetConfirmation");
            return new ServiceFailure<LauncherPresetDeleteTarget>(LauncherPresetStatus.DeleteConfirmationFailed(target.PresetName, ex));
        }

        if (!accepted)
            return new ServiceCanceled<LauncherPresetDeleteTarget>(LauncherPresetStatus.DeleteCanceled(target.PresetName));

        return new ServiceSuccess<LauncherPresetDeleteTarget>(target);
    }

    public ServiceResult<PresetMutation> AddPreset(
        LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds, IEnumerable<string> knownEntryIds)
    {
        var invalidEntryIds = FindInvalidEntryIds(entryIds, knownEntryIds);
        if (invalidEntryIds.Count > 0)
            return new ServiceFailure<PresetMutation>(LauncherPresetStatus.AddFailedInvalidEntryIds(invalidEntryIds));

        try
        {
            var mutable = presetFile;
            var preset = LauncherPresetWorkflow.AddPreset(ref mutable, name, entryIds);
            return new ServiceSuccess<PresetMutation>(new PresetMutation(mutable, preset));
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddPreset");
            return new ServiceFailure<PresetMutation>(LauncherRefreshStatus.AddGroupFailed(ex));
        }
    }

    public ServiceResult<PresetMutation> UpdatePreset(
        LauncherPresetFile presetFile, string presetId, string name, IReadOnlyList<string> entryIds, IEnumerable<string> knownEntryIds)
    {
        var invalidEntryIds = FindInvalidEntryIds(entryIds, knownEntryIds);
        if (invalidEntryIds.Count > 0)
            return new ServiceFailure<PresetMutation>(LauncherPresetStatus.UpdateFailedInvalidEntryIds(invalidEntryIds));

        try
        {
            var mutable = presetFile;
            var preset = LauncherPresetWorkflow.UpdatePreset(ref mutable, presetId, name, entryIds);
            return preset is not null
                ? new ServiceSuccess<PresetMutation>(new PresetMutation(mutable, preset))
                : new ServiceFailure<PresetMutation>(LauncherPresetStatus.CurrentPresetMissing);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "UpdatePreset");
            return new ServiceFailure<PresetMutation>(LauncherPresetStatus.UpdateFailed(name, ex));
        }
    }

    private static IReadOnlyList<string> FindInvalidEntryIds(
        IReadOnlyList<string> entryIds, IEnumerable<string> knownEntryIds)
    {
        var knownSet = knownEntryIds.ToHashSet(EntryId.Comparer);
        return entryIds.Where(id => !knownSet.Contains(id)).ToList();
    }

    public ServiceResult<PresetDeleteMutation> DeletePreset(
        LauncherPresetFile presetFile, string presetId)
    {
        try
        {
            var mutable = presetFile;
            var preset = LauncherPresetWorkflow.DeletePreset(ref mutable, presetId, out var fallback);
            return preset is not null
                ? new ServiceSuccess<PresetDeleteMutation>(new PresetDeleteMutation(mutable, preset, fallback))
                : new ServiceFailure<PresetDeleteMutation>(LauncherPresetStatus.CurrentPresetMissing);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeletePreset");
            return new ServiceFailure<PresetDeleteMutation>(LauncherRefreshStatus.DeleteGroupFailed(ex));
        }
    }
}
