using System.Collections.Immutable;
using System.Text.Json;

namespace WTLauncher.Core;

/// <summary>
/// Zone 0: Declared defaults for optional launcher settings plus the load-time
/// normalization that applies them when a config omits (or nulls) a key.
///
/// Why this exists: System.Text.Json deserializes records through the
/// constructor-based converter, which does NOT run property initializers.
/// A missing optional key therefore arrives as null/false instead of the
/// record's declared default — e.g. <c>quickLaunchHotkey</c> became null and
/// <c>runInTray</c> became false for every pre-feature config, silently
/// disabling the quick-launch hotkey (FormatException logged every startup)
/// and flipping close-to-tray off. This class restores the documented
/// contract ("optional settings default to ...") at load time without
/// weakening Loose Read forward compatibility: keys that are present with
/// explicit values are always honored; only missing (or blank/null) values
/// fall back to the declared defaults.
/// </summary>
public static class LauncherSettingsDefaults
{
    /// <summary>Declared default global quick-launch hotkey text (see <see cref="LauncherHotkey.DefaultText"/>).</summary>
    public const string QuickLaunchHotkey = LauncherHotkey.DefaultText;

    /// <summary>Declared default for close-to-tray behavior.</summary>
    public const bool RunInTray = true;

    /// <summary>Declared default update-manifest URL (empty = remote checks disabled).</summary>
    public const string UpdateManifestUrl = "";

    /// <summary>Declared default last filter text (empty = no filter).</summary>
    public const string LastFilterText = "";

    /// <summary>Declared default cards group key (none = no grouping).</summary>
    public const string CardsGroupKey = LauncherCardsGroupKeys.None;

    /// <summary>Declared default background mode.</summary>
    public const string BackgroundMode = "solid";

    /// <summary>Declared default accent color (hex, #RRGGBB).</summary>
    public const string AccentColor = AccentColorRamp.DefaultAccentColor;

    /// <summary>Declared default accent palette (hex, #RRGGBB), seeded from the built-in preset colors.</summary>
    public static readonly ImmutableList<string> AccentPresets = ImmutableList.CreateRange(AccentColorRamp.PresetColors);

    private const string SettingsSectionKey = "settings";
    private const string QuickLaunchHotkeyJsonKey = "quickLaunchHotkey";
    private const string RunInTrayJsonKey = "runInTray";
    private const string UpdateManifestUrlJsonKey = "updateManifestUrl";
    private const string LastFilterTextJsonKey = "lastFilterText";
    private const string CardsGroupKeyJsonKey = "cardsGroupKey";
    private const string CommandTemplatesJsonKey = "commandTemplates";
    private const string SearchHistoryJsonKey = "searchHistory";
    private const string BackgroundModeJsonKey = "backgroundMode";
    private const string AccentColorJsonKey = "accentColor";
    private const string AccentPresetsJsonKey = "accentPresets";

    /// <summary>
    /// Applies the declared defaults to settings whose JSON keys are missing,
    /// or whose values are null/blank (e.g. a previously persisted
    /// <c>"quickLaunchHotkey": null</c>). <paramref name="rawJson"/> is the
    /// exact JSON the store was deserialized from, so key presence is
    /// determined from the source document — a missing bool key is
    /// distinguishable from an explicit <c>false</c>.
    /// </summary>
    public static LauncherStoreFile ApplyMissingSettingsDefaults(LauncherStoreFile store, string rawJson)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(rawJson);

        IReadOnlySet<string> presentKeys;
        try
        {
            presentKeys = ParsePresentSettingsKeys(rawJson);
        }
        catch (JsonException)
        {
            // The raw JSON already failed deserialization upstream — never
            // mask the real error; leave the store untouched.
            return store;
        }

        var settings = store.Settings;
        if (settings is null)
            return store;

        var next = settings with
        {
            QuickLaunchHotkey = NormalizeQuickLaunchHotkey(settings.QuickLaunchHotkey, presentKeys),
            RunInTray = !presentKeys.Contains(RunInTrayJsonKey) || settings.RunInTray,
            UpdateManifestUrl = NormalizeUpdateManifestUrl(settings.UpdateManifestUrl, presentKeys),
            LastFilterText = NormalizeLastFilterText(settings.LastFilterText, presentKeys),
            CardsGroupKey = NormalizeCardsGroupKey(settings.CardsGroupKey, presentKeys),
            CommandTemplates = NormalizeCommandTemplates(settings.CommandTemplates, presentKeys),
            SearchHistory = NormalizeSearchHistory(settings.SearchHistory, presentKeys),
            BackgroundMode = NormalizeBackgroundMode(settings.BackgroundMode, presentKeys),
            AccentColor = NormalizeAccentColor(settings.AccentColor, presentKeys),
            AccentPresets = NormalizeAccentPresets(settings.AccentPresets, presentKeys),
        };

        return next == settings ? store : store with { Settings = next };
    }

    private static string NormalizeQuickLaunchHotkey(string? value, IReadOnlySet<string> presentKeys)
    {
        if (!presentKeys.Contains(QuickLaunchHotkeyJsonKey) || string.IsNullOrWhiteSpace(value))
            return LauncherHotkey.DefaultText;
        return value;
    }

    private static string NormalizeUpdateManifestUrl(string? value, IReadOnlySet<string> presentKeys) =>
        presentKeys.Contains(UpdateManifestUrlJsonKey) ? value ?? UpdateManifestUrl : UpdateManifestUrl;

    private static string NormalizeLastFilterText(string? value, IReadOnlySet<string> presentKeys) =>
        presentKeys.Contains(LastFilterTextJsonKey) ? value ?? LastFilterText : LastFilterText;

    private static string NormalizeCardsGroupKey(string? value, IReadOnlySet<string> presentKeys) =>
        presentKeys.Contains(CardsGroupKeyJsonKey) ? value ?? CardsGroupKey : CardsGroupKey;

    private static ImmutableDictionary<string, string> NormalizeCommandTemplates(ImmutableDictionary<string, string>? value, IReadOnlySet<string> presentKeys) =>
        presentKeys.Contains(CommandTemplatesJsonKey) ? value ?? ImmutableDictionary<string, string>.Empty : ImmutableDictionary<string, string>.Empty;

    private static ImmutableList<string> NormalizeSearchHistory(ImmutableList<string>? value, IReadOnlySet<string> presentKeys) =>
        presentKeys.Contains(SearchHistoryJsonKey) ? value ?? ImmutableList<string>.Empty : ImmutableList<string>.Empty;

    private static string NormalizeBackgroundMode(string? value, IReadOnlySet<string> presentKeys) =>
        presentKeys.Contains(BackgroundModeJsonKey) ? value ?? BackgroundMode : BackgroundMode;

    private static string NormalizeAccentColor(string? value, IReadOnlySet<string> presentKeys)
    {
        if (!presentKeys.Contains(AccentColorJsonKey))
            return AccentColorRamp.DefaultAccentColor;
        return AccentColorRamp.TryNormalize(value, out var normalized)
            ? normalized
            : AccentColorRamp.DefaultAccentColor;
    }

    private static ImmutableList<string> NormalizeAccentPresets(ImmutableList<string>? value, IReadOnlySet<string> presentKeys)
    {
        if (!presentKeys.Contains(AccentPresetsJsonKey) || value is null)
            return AccentPresets;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var builder = ImmutableList.CreateBuilder<string>();
        foreach (var item in value)
        {
            if (!AccentColorRamp.TryNormalize(item, out var normalized) || !seen.Add(normalized))
                continue;
            builder.Add(normalized);
        }

        return builder.Count == 0 ? AccentPresets : builder.ToImmutable();
    }

    private static IReadOnlySet<string> ParsePresentSettingsKeys(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        if (!document.RootElement.TryGetProperty(SettingsSectionKey, out var settingsElement))
            return new HashSet<string>(StringComparer.Ordinal);

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in settingsElement.EnumerateObject())
        {
            // Skip null values — a key with explicit null is treated the same
            // as a missing key, so the declared default applies.
            if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Null)
                continue;
            keys.Add(property.Name);
        }
        return keys;
    }
}
