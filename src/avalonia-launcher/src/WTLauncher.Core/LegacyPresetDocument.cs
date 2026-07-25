using System.Collections.Immutable;

namespace WTLauncher.Core;

/// <summary>
/// Temporary type for legacy preset file deserialization (presets.json format).
/// Used by <c>LegacyMigration</c> during one-time migration from three-file config.
/// </summary>
internal sealed record LegacyPresetDocument
{
    public int SchemaVersion { get; init; }
    public string? DefaultPresetId { get; init; }
    public ImmutableList<LauncherPreset>? Presets { get; init; }
}
