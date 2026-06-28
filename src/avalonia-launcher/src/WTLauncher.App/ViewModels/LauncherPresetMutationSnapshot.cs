using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed class LauncherPresetMutationSnapshot
{
    private readonly LauncherPresetFile _presetFile;

    private LauncherPresetMutationSnapshot(LauncherPresetFile presetFile, string? selectedPresetId)
    {
        _presetFile = presetFile;
        SelectedPresetId = selectedPresetId;
    }

    public LauncherPresetFile PresetFile => ClonePresetFile();

    public string? SelectedPresetId { get; }

    public static LauncherPresetMutationSnapshot Capture(
        LauncherPresetFile presetFile,
        string? selectedPresetId)
    {
        return new LauncherPresetMutationSnapshot(
            LauncherPresetFiles.Validate(presetFile),
            selectedPresetId);
    }

    public LauncherPresetFile ClonePresetFile()
    {
        return LauncherPresetFiles.Validate(_presetFile);
    }
}
