using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherPresetMutationSnapshot
{
    private LauncherPresetMutationSnapshot(LauncherPresetFile presetFile, string? selectedPresetId)
    {
        PresetFile = presetFile;
        SelectedPresetId = selectedPresetId;
    }

    public LauncherPresetFile PresetFile { get; }

    public string? SelectedPresetId { get; }

    public static LauncherPresetMutationSnapshot Capture(
        LauncherPresetFile presetFile,
        string? selectedPresetId)
    {
        return new LauncherPresetMutationSnapshot(
            LauncherPresetFiles.Clone(presetFile),
            selectedPresetId);
    }

    public LauncherPresetFile ClonePresetFile()
    {
        return LauncherPresetFiles.Clone(PresetFile);
    }
}
