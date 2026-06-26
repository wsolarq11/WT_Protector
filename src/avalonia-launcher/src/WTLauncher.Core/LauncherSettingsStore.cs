
using System.Collections.Immutable;

namespace WTLauncher.Core;

public static class LauncherSettingsStore
{
    public const int ExpectedSettingsSchemaVersion = 1;
    public const int MaxSearchHistoryTerms = 20;

    private static readonly LauncherSettings ValidatedDefaultSettings = Normalize(new LauncherSettings());

    public static LauncherSettings Load(string settingsPath)
    {
        ThrowIfSettingsPathMissing(settingsPath);

        LauncherSettings LoadAndValidate(string path)
        {
            var settings = LauncherJson.LoadRequired<LauncherSettings>(path, "Launcher settings");

            // Schema migration contract:
            // - SchemaVersion indicates the format version of this file.
            // - OLDER versions (schema < expected): rejected — the app cannot read them.
            // - EXACT version: loaded normally.
            // - NEWER versions (schema > expected): rejected with an upgrade message.
            //   This prevents data loss from downgrading after a schema change.
            // - When bumping ExpectedSchemaVersion, add a migration path that reads
            //   the old version and transforms it to the new format before validation.
            if (settings.SchemaVersion < ExpectedSettingsSchemaVersion)
                throw new InvalidDataException($"Launcher settings schema version {settings.SchemaVersion} is too old (expected at least {ExpectedSettingsSchemaVersion}).");
            if (settings.SchemaVersion > ExpectedSettingsSchemaVersion)
                throw new InvalidDataException($"Launcher settings schema version {settings.SchemaVersion} is from a newer version of the app. Please upgrade WT Launcher.");
            return Normalize(settings);
        }

        return LauncherJson.LoadWithRecovery(settingsPath, LoadAndValidate, "SettingsLoad.RecoveredFromBackup", ValidatedDefaultSettings);
    }

    public static void Save(LauncherSettings settings, string settingsPath, bool backup = true)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        LauncherJson.Save(CloneNormalized(settings), settingsPath, backup);
    }

    public static bool TrySaveWindowBounds(ref LauncherSettings settings, string settingsPath,
        double width, double height,
        double x = 0, double y = 0, string windowState = "Normal")
    {
        ArgumentNullException.ThrowIfNull(settings);
        ThrowIfSettingsPathMissing(settingsPath);

        var next = settings with { Window = RequireWindow(settings.Window) with { Width = width, Height = height, X = x, Y = y, WindowState = windowState } };
        var normalized = CloneNormalized(next);

        try
        {
            LauncherJson.Save(normalized, settingsPath, backup: true);
            settings = normalized;
            return true;
        }
        // Close-time bounds persistence must not leave unsaved sizes in memory.
        // Write failures can surface as IOException, UnauthorizedAccessException,
        // PathTooLongException, or other storage exceptions depending on the underlying cause.
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException or System.Security.SecurityException)
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
            LastFilterText = NormalizeFilterText(settings.LastFilterText),
            SortMode = NormalizeRequiredMode(settings.SortMode, "sortMode", LauncherEntryVisibility.NormalizeSortModeId),
            EntryViewMode = NormalizeRequiredMode(settings.EntryViewMode, "entryViewMode", LauncherEntryViewModes.NormalizeId),
            CommandTemplates = NormalizeCommandTemplates(settings.CommandTemplates),
            CustomEntryOrder = RequireCustomEntryOrder(settings.CustomEntryOrder),
            SearchHistory = RequireSearchHistory(settings.SearchHistory),
            Window = NormalizeWindow(RequireWindow(settings.Window)),
        };
    }

    private static LauncherSettings CloneNormalized(LauncherSettings settings)
    {
        return Normalize(LauncherSettingsCopies.Clone(settings));
    }

    private static ImmutableList<string> RequireCustomEntryOrder(IReadOnlyList<string>? customEntryOrder)
    {
        if (customEntryOrder is null)
            throw new InvalidDataException("Launcher settings customEntryOrder is null.");

        if (customEntryOrder.Count > 10000)
            throw new InvalidDataException("Launcher settings customEntryOrder exceeds 10000 entry limit.");

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

        return normalized.ToImmutableList();
    }

    /// <summary>
    /// Normalizes and deduplicates the search history list.
    /// Preserves the caller-provided ordering — earlier items are more recent.
    /// RecordSearchTerm enforces this contract by inserting new terms at index 0
    /// (most recent first). This method must not reorder; it only filters and truncates.
    /// </summary>
    private static ImmutableList<string> RequireSearchHistory(ImmutableList<string>? searchHistory)
    {
        if (searchHistory is null)
            throw new InvalidDataException("Launcher settings searchHistory is null.");

        var normalized = new List<string>(searchHistory.Count);
        foreach (var term in searchHistory)
        {
            var trimmed = LauncherEntryText.Normalize(term);
            if (trimmed.Length == 0 || trimmed.Length > 200)
                continue;

            if (!normalized.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                normalized.Add(trimmed);
        }

        // Keep the first MaxSearchHistoryTerms entries. Because RecordSearchTerm
        // inserts at index 0 (most recent first), the first N entries are the newest.
        if (normalized.Count > MaxSearchHistoryTerms)
            normalized.RemoveRange(MaxSearchHistoryTerms, normalized.Count - MaxSearchHistoryTerms);

        return normalized.ToImmutableList();
    }

    private static string NormalizeFilterText(string? value)
    {
        if (value is null)
            throw new InvalidDataException("Launcher settings lastFilterText is null.");

        var normalized = LauncherEntryText.Normalize(value);
        if (normalized.Length > 500)
            throw new InvalidDataException("Launcher settings lastFilterText exceeds 500 character limit.");

        return normalized;
    }

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
        // Validate optional position fields. NaN is valid (not yet persisted).
        // Negative values are valid on multi-monitor setups.
        if (!double.IsFinite(window.X))
            throw new InvalidDataException("Launcher settings window x must be finite.");
        if (!double.IsFinite(window.Y))
            throw new InvalidDataException("Launcher settings window y must be finite.");
        if (window.WindowState is not "Normal" and not "Maximized" and not "Minimized")
            throw new InvalidDataException($"Launcher settings window state must be Normal, Maximized, or Minimized. Got: '{window.WindowState}'.");
        return window;
    }

    private static void ValidateWindowSize(double value, string fieldName)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new InvalidDataException($"Launcher settings window {fieldName} must be positive.");
    }

    private static ImmutableDictionary<string, string> NormalizeCommandTemplates(ImmutableDictionary<string, string>? templates)
    {
        if (templates is null)
            return ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

        if (templates.Count > 1000)
            throw new InvalidDataException("Launcher settings commandTemplates exceeds 1000 template limit.");

        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in templates)
        {
            var trimmedKey = LauncherEntryText.Normalize(key);
            if (trimmedKey.Length == 0)
                throw new InvalidDataException("Launcher settings commandTemplates has an empty key.");
            if (trimmedKey.Length > 256)
                throw new InvalidDataException($"Launcher settings commandTemplates key exceeds 256 character limit: '{trimmedKey[..Math.Min(trimmedKey.Length, 50)]}...'.");

            var trimmedValue = LauncherEntryText.Normalize(value);
            if (trimmedValue.Length == 0)
                throw new InvalidDataException($"Launcher settings commandTemplates key '{trimmedKey}' has an empty value.");
            if (trimmedValue.Length > 8192)
                throw new InvalidDataException($"Launcher settings commandTemplates value for key '{trimmedKey}' exceeds 8192 character limit.");

            if (!builder.TryAdd(trimmedKey, trimmedValue))
                throw new InvalidDataException($"Launcher settings commandTemplates has duplicate key: {trimmedKey}.");
        }

        return builder.ToImmutable();
    }
}
