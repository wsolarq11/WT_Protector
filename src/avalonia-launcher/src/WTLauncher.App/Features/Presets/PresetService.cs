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

    public async Task<(string? PresetName, string? Status)> RequestNewPresetAsync(
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
            return (null, LauncherPresetStatus.SaveDialogFailed(ex));
        }

        if (presetName is null)
            return (null, LauncherPresetStatus.SaveCanceled);

        var normalizedName = EntryName.Normalize(presetName);
        if (normalizedName.Length == 0)
            return (null, LauncherPresetStatus.NameRequired);

        return (normalizedName, null);
    }

    public async Task<(bool Accepted, string? Status)> ConfirmDeletePresetAsync(
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
            return (false, LauncherPresetStatus.DeleteConfirmationFailed(target.PresetName, ex));
        }

        if (!accepted)
            return (false, LauncherPresetStatus.DeleteCanceled(target.PresetName));

        return (true, null);
    }

    public (LauncherPresetFile PresetFile, LauncherPreset? Preset, string? Status) AddPreset(
        LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds)
    {
        try
        {
            var mutable = presetFile;
            var preset = LauncherPresetWorkflow.AddPreset(ref mutable, name, entryIds);
            return (mutable, preset, null);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "AddPreset");
            return (presetFile, null, LauncherRefreshStatus.AddGroupFailed(ex));
        }
    }

    public (LauncherPresetFile PresetFile, LauncherPreset? Preset, string? Status, string FallbackPresetId) UpdatePreset(
        LauncherPresetFile presetFile, string presetId, string name, IReadOnlyList<string> entryIds)
    {
        try
        {
            var mutable = presetFile;
            var preset = LauncherPresetWorkflow.UpdatePreset(ref mutable, presetId, name, entryIds);
            return (mutable, preset, null, string.Empty);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "UpdatePreset");
            return (presetFile, null, LauncherPresetStatus.UpdateFailed(name, ex), string.Empty);
        }
    }

    public (LauncherPresetFile PresetFile, LauncherPreset? Preset, string? Status, string FallbackPresetId) DeletePreset(
        LauncherPresetFile presetFile, string presetId)
    {
        try
        {
            var mutable = presetFile;
            var preset = LauncherPresetWorkflow.DeletePreset(ref mutable, presetId, out var fallback);
            return (mutable, preset, null, fallback);
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            ErrorLog.Write(ex, "DeletePreset");
            return (presetFile, null, LauncherRefreshStatus.DeleteGroupFailed(ex), string.Empty);
        }
    }
}
