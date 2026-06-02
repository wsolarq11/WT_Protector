namespace WTLauncher.Core;

public static class LauncherSettingsStore
{
    public static LauncherSettings Load(string settingsPath)
    {
        ThrowIfSettingsPathMissing(settingsPath);
        if (!File.Exists(settingsPath))
            throw new FileNotFoundException("Launcher settings not found.", settingsPath);

        var settings = LauncherJson.LoadRequired<LauncherSettings>(settingsPath, "Launcher settings");
        Normalize(settings);
        return settings;
    }

    public static void Save(LauncherSettings settings, string settingsPath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        LauncherJson.Save(CloneNormalized(settings), settingsPath, backup: false);
    }

    public static bool TrySaveWindowBounds(LauncherSettings settings, string settingsPath, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        var snapshot = LauncherSettingsSnapshot.Capture(settings);
        var next = LauncherSettingsCopies.Clone(settings);
        var window = RequireWindow(next.Window);
        window.Width = width;
        window.Height = height;
        var normalized = CloneNormalized(next);

        try
        {
            LauncherJson.Save(normalized, settingsPath, backup: false);
            LauncherSettingsCopies.CopyTo(normalized, settings);
            return true;
        }
        // Close-time bounds persistence must not leave unsaved sizes in memory.
        // Write failures can surface as IOException, UnauthorizedAccessException,
        // or other path/storage exceptions depending on the underlying cause.
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            snapshot.Restore(settings);
            return false;
        }
    }

    private static void ThrowIfSettingsPathMissing(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
    }

    private static void Normalize(LauncherSettings settings)
    {
        settings.LastFilterText = RequireNullableText(settings.LastFilterText, "lastFilterText");
        settings.SortMode = NormalizeRequiredMode(settings.SortMode, "sortMode", LauncherEntryVisibility.NormalizeSortModeId);
        settings.EntryViewMode = NormalizeRequiredMode(settings.EntryViewMode, "entryViewMode", LauncherEntryViewModes.NormalizeId);
        settings.CustomEntryOrder = RequireCustomEntryOrder(settings.CustomEntryOrder);
        settings.Window = NormalizeWindow(RequireWindow(settings.Window));
    }

    private static LauncherSettings CloneNormalized(LauncherSettings settings)
    {
        var clone = LauncherSettingsCopies.Clone(settings);
        Normalize(clone);
        return clone;
    }

    private static List<string> RequireCustomEntryOrder(IReadOnlyList<string>? customEntryOrder)
    {
        if (customEntryOrder is null)
            throw new InvalidDataException("Launcher settings customEntryOrder is null.");

        var normalized = new List<string>(customEntryOrder.Count);
        var seen = new HashSet<string>(LauncherEntryIds.Comparer);
        for (var i = 0; i < customEntryOrder.Count; i++)
        {
            var entryId = LauncherEntryIds.Normalize(customEntryOrder[i]);
            if (entryId.Length == 0)
                throw new InvalidDataException($"Launcher settings customEntryOrder[{i}] is empty.");

            if (!seen.Add(entryId))
                throw new InvalidDataException($"Launcher settings customEntryOrder[{i}] is duplicate: {entryId}.");

            normalized.Add(entryId);
        }

        return normalized;
    }

    private static string RequireNullableText(string? value, string fieldName) =>
        value ?? throw new InvalidDataException($"Launcher settings {fieldName} is null.");

    private static string NormalizeRequiredMode(
        string? value,
        string fieldName,
        Func<string?, string> normalize)
    {
        if (value is null)
            throw new InvalidDataException($"Launcher settings {fieldName} is null.");

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException($"Launcher settings {fieldName} is empty.");

        return normalize(value);
    }

    private static LauncherWindowSettings RequireWindow(LauncherWindowSettings? window) =>
        window ?? throw new InvalidDataException("Launcher settings window is null.");

    private static LauncherWindowSettings NormalizeWindow(LauncherWindowSettings window)
    {
        ValidateWindowSize(window.Width, "width");
        ValidateWindowSize(window.Height, "height");
        return window;
    }

    private static void ValidateWindowSize(double value, string fieldName)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new InvalidDataException($"Launcher settings window {fieldName} must be positive.");
    }
}
