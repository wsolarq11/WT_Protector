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

    private const string SettingsSectionKey = "settings";
    private const string QuickLaunchHotkeyJsonKey = "quickLaunchHotkey";
    private const string RunInTrayJsonKey = "runInTray";
    private const string UpdateManifestUrlJsonKey = "updateManifestUrl";

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

    private static IReadOnlySet<string> ParsePresentSettingsKeys(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        if (!document.RootElement.TryGetProperty(SettingsSectionKey, out var settingsElement))
            return new HashSet<string>(StringComparer.Ordinal);

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in settingsElement.EnumerateObject())
            keys.Add(property.Name);
        return keys;
    }
}
