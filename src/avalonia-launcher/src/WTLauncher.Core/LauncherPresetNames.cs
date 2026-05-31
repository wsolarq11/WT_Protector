namespace WTLauncher.Core;

public static class LauncherPresetNames
{
    public static string Normalize(string? name) => (name ?? string.Empty).Trim();

    public static bool TryNormalizeRequired(string? name, out string normalizedName)
    {
        normalizedName = Normalize(name);
        return normalizedName.Length > 0;
    }
}
