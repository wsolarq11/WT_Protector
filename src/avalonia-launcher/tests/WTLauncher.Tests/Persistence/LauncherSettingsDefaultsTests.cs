using System.IO;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Persistence;

/// <summary>
/// Contract tests for load-time defaults of optional settings
/// (<c>quickLaunchHotkey</c>, <c>runInTray</c>, <c>updateManifestUrl</c>).
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
    [Fact]
    public void MissingOptionalSettingsKeys_Load_AppliesDeclaredDefaults()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: null,      // key absent
            runInTray: null,              // key absent
            updateManifestUrl: null));    // key absent

        var store = LauncherStore.Load(path);

        Assert.Equal(LauncherHotkey.DefaultText, store.Settings.QuickLaunchHotkey);
        Assert.True(store.Settings.RunInTray);
        Assert.Equal("", store.Settings.UpdateManifestUrl);
    }

    [Fact]
    public void ExplicitSettingValues_Load_AreHonored()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "store.json");
        File.WriteAllText(path, StoreJson(
            quickLaunchHotkey: "\"Shift+Alt+Space\"",
            runInTray: "false",
            updateManifestUrl: "\"file:///C:/tmp/update-manifest.json\""));

        var store = LauncherStore.Load(path);

        Assert.Equal("Shift+Alt+Space", store.Settings.QuickLaunchHotkey);
        Assert.False(store.Settings.RunInTray);
        Assert.Equal("file:///C:/tmp/update-manifest.json", store.Settings.UpdateManifestUrl);
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
            updateManifestUrl: null));

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
            updateManifestUrl: null));

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
            updateManifestUrl: "null"));

        var store = LauncherStore.Load(path);

        Assert.Equal("", store.Settings.UpdateManifestUrl);
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
    private static string StoreJson(string? quickLaunchHotkey, string? runInTray, string? updateManifestUrl)
    {
        var settingsFields = string.Join(",", new[]
        {
            "\"lastFilterText\":\"\"",
            "\"sortMode\":\"custom\"",
            "\"entryViewMode\":\"cards\"",
            "\"requireConfirmationForLaunches\":false",
            "\"customEntryOrder\":[]",
            "\"commandTemplates\":{}",
            "\"searchHistory\":[]",
            "\"window\":{\"width\":1180,\"height\":760}",
            quickLaunchHotkey is null ? null : $"\"quickLaunchHotkey\":{quickLaunchHotkey}",
            runInTray is null ? null : $"\"runInTray\":{runInTray}",
            updateManifestUrl is null ? null : $"\"updateManifestUrl\":{updateManifestUrl}",
        }.Where(field => field is not null)!);

        return $"{{\"schemaVersion\":2,\"settings\":{{{settingsFields}}},\"entries\":[],\"presets\":[],\"defaultPresetId\":\"\"}}";
    }
}
