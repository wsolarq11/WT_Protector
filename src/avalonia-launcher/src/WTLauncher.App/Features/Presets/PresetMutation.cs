using WTLauncher.Core;

namespace WTLauncher.App.Features.Presets;

public sealed record PresetMutation(
    LauncherPresetFile PresetFile,
    LauncherPreset Preset);

public sealed record PresetDeleteMutation(
    LauncherPresetFile PresetFile,
    LauncherPreset Preset,
    string FallbackPresetId);
