namespace WTLauncher.Core;

public static class LauncherSettingsStore
{
    public static LauncherSettings Load(string settingsPath)
    {
        ThrowIfSettingsPathMissing(settingsPath);
        if (!File.Exists(settingsPath))
            throw new FileNotFoundException("Launcher settings not found.", settingsPath);

        var settings = LauncherJson.LoadRequired<LauncherSettings>(settingsPath, "Launcher settings");
        return Normalize(settings);
    }

    public static void Save(LauncherSettings settings, string settingsPath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        LauncherJson.Save(CloneNormalized(settings), settingsPath, backup: true);
    }

    public static bool TrySaveWindowBounds(ref LauncherSettings settings, string settingsPath, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        var next = settings with { Window = RequireWindow(settings.Window) with { Width = width, Height = height } };
        var normalized = CloneNormalized(next);

        try
        {
            LauncherJson.Save(normalized, settingsPath, backup: true);
            settings = normalized;
            return true;
        }
        // Close-time bounds persistence must not leave unsaved sizes in memory.
        // Write failures can surface as IOException, UnauthorizedAccessException,
        // or other path/storage exceptions depending on the underlying cause.
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidDataException)
        {
            return false;
        }
    }

    private static void ThrowIfSettingsPathMissing(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
    }

    private static LauncherSettings Normalize(LauncherSettings settings)
    {
        return settings with
        {
            LastFilterText = RequireNullableText(settings.LastFilterText, "lastFilterText"),
            SortMode = NormalizeRequiredMode(settings.SortMode, "sortMode", LauncherEntryVisibility.NormalizeSortModeId),
            EntryViewMode = NormalizeRequiredMode(settings.EntryViewMode, "entryViewMode", LauncherEntryViewModes.NormalizeId),
            CommandTemplates = RequireCommandTemplates(settings.CommandTemplates),
            CustomEntryOrder = RequireCustomEntryOrder(settings.CustomEntryOrder),
            SearchHistory = RequireSearchHistory(settings.SearchHistory),
            Window = NormalizeWindow(RequireWindow(settings.Window)),
        };
    }

    private static LauncherSettings CloneNormalized(LauncherSettings settings)
    {
        return Normalize(LauncherSettingsCopies.Clone(settings));
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

    private static List<string> RequireSearchHistory(List<string>? searchHistory)
    {
        if (searchHistory is null)
            throw new InvalidDataException("Launcher settings searchHistory is null.");

        var normalized = new List<string>(searchHistory.Count);
        foreach (var term in searchHistory)
        {
            var trimmed = LauncherEntryText.Normalize(term);
            if (trimmed.Length == 0)
                continue;

            if (!normalized.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                normalized.Add(trimmed);
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

    private static Dictionary<string, string> RequireCommandTemplates(Dictionary<string, string>? templates)
    {
        if (templates is null)
            return [];

        var normalized = new Dictionary<string, string>(templates.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in templates)
        {
            var trimmedKey = LauncherEntryText.Normalize(key);
            if (trimmedKey.Length == 0)
                throw new InvalidDataException("Launcher settings commandTemplates has an empty key.");

            var trimmedValue = LauncherEntryText.Normalize(value);
            if (trimmedValue.Length == 0)
                throw new InvalidDataException($"Launcher settings commandTemplates key '{trimmedKey}' has an empty value.");

            if (!normalized.TryAdd(trimmedKey, trimmedValue))
                throw new InvalidDataException($"Launcher settings commandTemplates has duplicate key: {trimmedKey}.");
        }

        return normalized;
    }
}
