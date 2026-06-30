namespace WTLauncher.Core;

/// <summary>Preset ID helpers. Prefer <see cref="PresetId"/> for new code —
/// the static methods here delegate to PresetId internally.</summary>
public static class LauncherPresetIds
{
    /// <summary>Prefer <see cref="PresetId.Comparer"/>.</summary>
    public static readonly StringComparer Comparer = PresetId.Comparer;

    /// <summary>Maximum ID length.</summary>
    public const int MaxIdLength = 256;

    [Obsolete("Use PresetId.Same() instead.")]
    public static bool Same(string? left, string? right) => PresetId.Same(left, right);

    [Obsolete("Use PresetId.GenerateUnique() instead.")]
    public static string GenerateUnique(string name, IEnumerable<string> existingIds)
        => PresetId.GenerateUnique(name, existingIds);

    [Obsolete("Use PresetId.Normalize() instead.")]
    public static string Normalize(string? value) => PresetId.Normalize(value);

    [Obsolete("Use PresetId.NormalizeReference() instead.")]
    public static string NormalizeReference(string? value) => PresetId.NormalizeReference(value);
}
