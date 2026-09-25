using System.Collections.Immutable;
using System.IO;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Persistence;

/// <summary>
/// Contract tests for load-time defaults of optional settings
/// (<c>quickLaunchHotkey</c>, <c>runInTray</c>, <c>updateManifestUrl</c>,
/// <c>lastFilterText</c>, <c>cardsGroupKey</c>, <c>commandTemplates</c>,
/// <c>searchHistory</c>, <c>backgroundMode</c>).
///
/// STJ record deserialization does not run property initializers, so missing
/// optional keys arrive as null/false. <see cref="LauncherSettingsDefaults"/>
/// restores the documented defaults at load time; these tests pin that
/// behavior end-to-end through <see cref="LauncherStore.Load"/> and directly
/// against the pure function.
/// </summary>
[Trait("Category", TestCategories.Persistence)]
public sealed class LauncherSettingsDefaultsTests
{
    private static readonly string[] ExplicitPresetsFixture = ["#0EA5E0", "#DC2626"];
    private static readonly string[] DedupedPresetsFixture = ["#0EA5E0"];

    [Fact]
    public void MissingOptionalSettingsKeys_Load_AppliesDeclaredDefaults()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,      // key absent
            runInTray: null,              // key absent
            updateManifestUrl: null,      // key absent
            lastFilterText: null,         // key absent
            cardsGroupKey: null,          // key absent
            commandTemplates: null,       // key absent
            searchHistory: null,          // key absent
            backgroundMode: null));       // key absent

        var store = LauncherStore.Load(path);

        Assert.Equal(LauncherHotkey.DefaultText, store.Settings.QuickLaunchHotkey);
        Assert.True(store.Settings.RunInTray);
        Assert.Equal("", store.Settings.UpdateManifestUrl);
        Assert.Equal("", store.Settings.LastFilterText);
        Assert.Equal(LauncherCardsGroupKeys.None, store.Settings.CardsGroupKey);
        Assert.Equal(ImmutableDictionary<string, string>.Empty, store.Settings.CommandTemplates);
        Assert.Equal(ImmutableList<string>.Empty, store.Settings.SearchHistory);
        Assert.Equal("solid", store.Settings.BackgroundMode);
    }

    [Fact]
    public void ExplicitSettingValues_Load_AreHonored()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: "\"Shift+Alt+Space\"",
            runInTray: "false",
            updateManifestUrl: "\"file:///C:/tmp/update-manifest.json\"",
            lastFilterText: "\"my-filter\"",
            cardsGroupKey: "\"command\"",
            commandTemplates: "{\"key\":\"value\"}",
            searchHistory: "[\"term\"]",
            backgroundMode: "\"dark\""));

        var store = LauncherStore.Load(path);

        Assert.Equal("Shift+Alt+Space", store.Settings.QuickLaunchHotkey);
        Assert.False(store.Settings.RunInTray);
        Assert.Equal("file:///C:/tmp/update-manifest.json", store.Settings.UpdateManifestUrl);
        Assert.Equal("my-filter", store.Settings.LastFilterText);
        Assert.Equal("command", store.Settings.CardsGroupKey);
        Assert.Equal("value", store.Settings.CommandTemplates["key"]);
        Assert.Equal("term", store.Settings.SearchHistory[0]);
        Assert.Equal("dark", store.Settings.BackgroundMode);
    }

    [Fact]
    public void NullQuickLaunchHotkey_Load_FallsBackToDefaultText()
    {
        // A prior buggy version persisted "quickLaunchHotkey": null — the key
        // is present but null, which must still resolve to the default.
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: "null",
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: null,
            cardsGroupKey: null,
            commandTemplates: null,
            searchHistory: null,
            backgroundMode: null));

        var store = LauncherStore.Load(path);

        Assert.Equal(LauncherHotkey.DefaultText, store.Settings.QuickLaunchHotkey);
    }

    [Fact]
    public void BlankQuickLaunchHotkey_Load_FallsBackToDefaultText()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: "\"\"",
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: null,
            cardsGroupKey: null,
            commandTemplates: null,
            searchHistory: null,
            backgroundMode: null));

        var store = LauncherStore.Load(path);

        Assert.Equal(LauncherHotkey.DefaultText, store.Settings.QuickLaunchHotkey);
    }

    [Fact]
    public void NullUpdateManifestUrl_Load_FallsBackToEmpty()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,
            runInTray: null,
            updateManifestUrl: "null",
            lastFilterText: null,
            cardsGroupKey: null,
            commandTemplates: null,
            searchHistory: null,
            backgroundMode: null));

        var store = LauncherStore.Load(path);

        Assert.Equal("", store.Settings.UpdateManifestUrl);
    }

    [Fact]
    public void ExplicitLastFilterText_Load_IsHonored()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: "\"my-filter\""));

        var store = LauncherStore.Load(path);

        Assert.Equal("my-filter", store.Settings.LastFilterText);
    }

    [Fact]
    public void NullLastFilterText_Load_FallsBackToEmpty()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: "null"));   // key present but null

        var store = LauncherStore.Load(path);

        Assert.Equal("", store.Settings.LastFilterText);
    }

    [Fact]
    public void NullCardsGroupKey_Load_FallsBackToNone()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: null,
            cardsGroupKey: "null"));   // key present but null

        var store = LauncherStore.Load(path);

        Assert.Equal(LauncherCardsGroupKeys.None, store.Settings.CardsGroupKey);
    }

    [Fact]
    public void NullCommandTemplates_Load_FallsBackToEmpty()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: null,
            commandTemplates: "null"));   // key present but null

        var store = LauncherStore.Load(path);

        Assert.Equal(ImmutableDictionary<string, string>.Empty, store.Settings.CommandTemplates);
    }

    [Fact]
    public void NullSearchHistory_Load_FallsBackToEmpty()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: null,
            searchHistory: "null"));   // key present but null

        var store = LauncherStore.Load(path);

        Assert.Equal(ImmutableList<string>.Empty, store.Settings.SearchHistory);
    }

    [Fact]
    public void NullBackgroundMode_Load_FallsBackToSolid()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,
            runInTray: null,
            updateManifestUrl: null,
            lastFilterText: null,
            backgroundMode: "null"));   // key present but null

        var store = LauncherStore.Load(path);

        Assert.Equal("solid", store.Settings.BackgroundMode);
    }

    [Fact]
    public void MissingAccentColor_Load_DefaultsToBadgeBlue()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson());   // accentColor key absent

        var store = LauncherStore.Load(path);

        Assert.Equal(AccentColorRamp.DefaultAccentColor, store.Settings.AccentColor);
    }

    [Fact]
    public void ExplicitAccentColor_Load_IsHonored()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(accentColor: "\"#2563EB\""));

        var store = LauncherStore.Load(path);

        Assert.Equal("#2563EB", store.Settings.AccentColor);
    }

    [Fact]
    public void InvalidAccentColor_Load_FallsBackToDefault()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(accentColor: "\"not-a-color\""));

        var store = LauncherStore.Load(path);

        Assert.Equal(AccentColorRamp.DefaultAccentColor, store.Settings.AccentColor);
    }

    [Fact]
    public void MissingAccentPresets_Load_DefaultsToBuiltInPalette()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson());   // accentPresets key absent

        var store = LauncherStore.Load(path);

        Assert.Equal(AccentColorRamp.PresetColors, store.Settings.AccentPresets);
    }

    [Fact]
    public void ExplicitAccentPresets_Load_IsHonored()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(accentPresets: "[\"#0EA5E0\",\"#DC2626\"]"));

        var store = LauncherStore.Load(path);

        Assert.Equal(ExplicitPresetsFixture, store.Settings.AccentPresets);
    }

    [Fact]
    public void InvalidAccentPresets_Load_FiltersInvalidAndDedupes()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(accentPresets: "[\"#0EA5E0\",\"not-a-color\",\"#0ea5e0\"]"));

        var store = LauncherStore.Load(path);

        Assert.Equal(DedupedPresetsFixture, store.Settings.AccentPresets);
    }

    [Fact]
    public void ApplyMissingSettingsDefaults_NullSettings_ReturnsStoreUnchanged()
    {
        // Defensive guard: a store with a null Settings reference (possible via
        // direct construction, not via JSON load — settings is [JsonRequired])
        // must be returned untouched rather than throwing.
        var store = new LauncherStoreFile { Settings = null! };
        var result = LauncherSettingsDefaults.ApplyMissingSettingsDefaults(
            store, "{\"schemaVersion\":2,\"entries\":[],\"presets\":[]}");

        Assert.Same(store, result);
    }

    [Fact]
    public void ApplyMissingSettingsDefaults_InvalidJson_ReturnsStoreUnchanged()
    {
        var store = new LauncherStoreFile
        {
            Settings = new LauncherSettings(),
        };
        var result = LauncherSettingsDefaults.ApplyMissingSettingsDefaults(store, "{not json");

        Assert.Same(store, result);
    }

    /// <summary>
    /// Builds a minimal schema-version-2 store JSON. Passing <c>null</c> omits
    /// the key entirely; passing a raw value string emits it verbatim.
    /// </summary>
    private static string StoreJson(
        string? quickLaunchHotkey = null,
        string? runInTray = null,
        string? updateManifestUrl = null,
        string? lastFilterText = "\"\"",
        string? cardsGroupKey = null,
        string? commandTemplates = null,
        string? searchHistory = null,
        string? backgroundMode = null,
        string? accentColor = null,
        string? accentPresets = null)
    {
        var settingsFields = string.Join(",", new[]
        {
            lastFilterText is null ? null : $"\"lastFilterText\":{lastFilterText}",
            "\"sortMode\":\"custom\"",
            "\"entryViewMode\":\"cards\"",
            "\"requireConfirmationForLaunches\":false",
            "\"customEntryOrder\":[]",
            cardsGroupKey is null ? null : $"\"cardsGroupKey\":{cardsGroupKey}",
            commandTemplates is null ? null : $"\"commandTemplates\":{commandTemplates}",
            searchHistory is null ? null : $"\"searchHistory\":{searchHistory}",
            backgroundMode is null ? null : $"\"backgroundMode\":{backgroundMode}",
            accentColor is null ? null : $"\"accentColor\":{accentColor}",
            accentPresets is null ? null : $"\"accentPresets\":{accentPresets}",
            "\"window\":{\"width\":1180,\"height\":760}",
            quickLaunchHotkey is null ? null : $"\"quickLaunchHotkey\":{quickLaunchHotkey}",
            runInTray is null ? null : $"\"runInTray\":{runInTray}",
            updateManifestUrl is null ? null : $"\"updateManifestUrl\":{updateManifestUrl}",
        }.Where(field => field is not null)!);

        return $"{{\"schemaVersion\":2,\"settings\":{{{settingsFields}}},\"entries\":[],\"presets\":[],\"defaultPresetId\":\"\"}}";
    }
}
