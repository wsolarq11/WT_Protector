namespace WTLauncher.Core;

public static class LauncherPresetFiles
{
    public static LauncherPresetFile Clone(LauncherPresetFile presetFile)
    {
        ArgumentNullException.ThrowIfNull(presetFile);

        return new LauncherPresetFile
        {
            SchemaVersion = presetFile.SchemaVersion,
            DefaultPresetId = presetFile.DefaultPresetId,
            Presets = presetFile.Presets
                .Select(preset => new LauncherPreset
                {
                    Id = preset.Id,
                    Name = preset.Name,
                    EntryIds = [.. preset.EntryIds],
                    Locked = preset.Locked,
                })
                .ToList(),
        };
    }
}
