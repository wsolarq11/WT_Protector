using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace WTLauncher.App.Localization;

/// <summary>
/// Loads user-facing strings from embedded JSON language packs. Pack format is
/// a flat string-to-template dictionary; missing keys fall back to zh-CN, then
/// to the key itself so a gap is visible instead of throwing.
/// </summary>
public static class LauncherLocalization
{
    private const string ResourcePrefix = "WTLauncher.App.Localization.";

    private static readonly IReadOnlyDictionary<string, string> FallbackPack =
        LoadPack("zh-CN") ?? new Dictionary<string, string>();

    private static readonly IReadOnlyDictionary<string, string> ActivePack =
        LoadPack(CultureInfo.CurrentUICulture.Name)
        ?? LoadPack(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
        ?? FallbackPack;

    public static string Format(string key, params object?[] args)
    {
        var template = ActivePack.TryGetValue(key, out var active)
            ? active
            : FallbackPack.TryGetValue(key, out var fallback)
                ? fallback
                : key;

        return args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentCulture, template, args);
    }

    private static IReadOnlyDictionary<string, string>? LoadPack(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return null;

        var resourceName = ResourcePrefix + cultureName + ".json";
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using (stream)
        {
            return JsonSerializer.Deserialize(stream, LauncherLocalizationContext.Default.DictionaryStringString);
        }
    }
}
