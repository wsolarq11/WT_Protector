using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Presets;

/// <summary>Preset CRUD dialog + workflow logic. Each method is the dialog→validate→mutate flow.
/// Does NOT own state — the ViewModel passes preset file and coordinates rollback.</summary>
public interface IPresetService
{
    public int CountEntryReferences(LauncherPresetFile presetFile, string entryId);

    public LauncherPresetSelectionPreview BuildSelectionPreview(
        string presetName,
        IEnumerable<string> selectedEntryIds,
        IEnumerable<string> configuredEntryIds,
        IEnumerable<string> presetEntryIds);

    public bool CanUpdatePreset(LauncherPresetFile presetFile, string presetId);

    public bool CanDeletePreset(LauncherPresetFile presetFile, string presetId);

    /// <summary>Walk the user through creating a new preset name.</summary>
    public Task<ServiceResult<string>> RequestNewPresetAsync(
        LauncherPresetSaveTarget target, HashSet<string> existingNames);

    /// <summary>Confirm deletion with the user.</summary>
    public Task<ServiceResult<LauncherPresetDeleteTarget>> ConfirmDeletePresetAsync(
        LauncherPresetDeleteTarget target);

    /// <summary>Add a preset to the file. Returns the updated file and the new preset, or an error status.</summary>
    public ServiceResult<PresetMutation> AddPreset(
        LauncherPresetFile presetFile, string name, IReadOnlyList<string> entryIds);

    /// <summary>Update a preset in the file. Returns the updated file and preset, or null with status.</summary>
    public ServiceResult<PresetMutation> UpdatePreset(
        LauncherPresetFile presetFile, string presetId, string name, IReadOnlyList<string> entryIds);

    /// <summary>Delete a preset from the file. Returns the updated file and deleted preset, or null with status.</summary>
    public ServiceResult<PresetDeleteMutation> DeletePreset(
        LauncherPresetFile presetFile, string presetId);
}
