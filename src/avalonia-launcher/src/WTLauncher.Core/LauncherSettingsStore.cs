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
        var window = settings.Window ??= new LauncherWindowSettings();
        window.Width = width;
        window.Height = height;

        try
        {
            Save(settings, settingsPath);
            return true;
        }
        // Close-time bounds persistence must not leave unsaved sizes in memory.
        // Write failures can surface as IOException, UnauthorizedAccessException,
        // or other path/storage exceptions depending on the underlying cause.
        catch (Exception)
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
        settings.LastFilterText ??= string.Empty;
        settings.SortMode = LauncherEntryVisibility.NormalizeSortModeId(settings.SortMode);
        settings.EntryViewMode = LauncherEntryViewModes.NormalizeId(settings.EntryViewMode);
        settings.CustomEntryOrder = RequireCustomEntryOrder(settings.CustomEntryOrder);
        settings.Window ??= new LauncherWindowSettings();
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
}
