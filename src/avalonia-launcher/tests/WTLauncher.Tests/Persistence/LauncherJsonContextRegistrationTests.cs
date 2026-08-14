using System.Collections.Immutable;
using System.Text.Json;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Persistence;

/// <summary>Contract tests verifying all JSON-serialized types are registered
/// in <see cref="LauncherJsonContext"/> for Native AOT safety.</summary>
[Trait("Category", TestCategories.Persistence)]
public sealed class LauncherJsonContextRegistrationTests
{
    [Fact]
    public void AllKnownTypes_AreRegisteredInJsonContext()
    {
        // Each type must have a JsonTypeInfo in the source-generated context.
        // Without registration, LauncherJson.GetRequiredTypeInfo throws at runtime.
        AssertRegistered<LauncherStoreFile>();
        AssertRegistered<LauncherSettings>();
        AssertRegistered<LaunchEntry>();
        AssertRegistered<LauncherPreset>();
        AssertRegistered<LauncherRunHistoryEntry>();
        AssertRegistered<LauncherUpdateManifest>();
    }

    [Fact]
    public void RootType_RoundTripsSuccessfully()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "roundtrip.json");

        var store = new LauncherStoreFile
        {
            SchemaVersion = 2,
            Settings = new LauncherSettings
            {
                LastFilterText = "test",
                SortMode = "custom",
                EntryViewMode = "cards",
                RequireConfirmationForLaunches = false,
                CustomEntryOrder = ImmutableList<string>.Empty,
                CommandTemplates = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase),
                SearchHistory = ImmutableList<string>.Empty,
                QuickLaunchHotkey = LauncherHotkey.DefaultText,
                RunInTray = true,
                Window = new LauncherWindowSettings { Width = 1180, Height = 760 },
            },
            Entries = [new LaunchEntry
            {
                Id = "test-entry",
                Name = "Test",
                Command = "cmd.exe",
                Directory = (WTLauncher.Core.DirectoryPath)Environment.SystemDirectory,
                UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
            }],
            Presets = [new LauncherPreset
            {
                Id = "test-preset",
                Name = "Test Preset",
                EntryIds = ["test-entry"],
            }],
            DefaultPresetId = "",
            RunHistory = [LauncherRunHistoryEntry.Create(
                "test-entry",
                "Test",
                "2026-08-14T00:00:00Z",
                LauncherRunSource.Individual,
                LauncherRunStatus.Requested)],
        };

        LauncherJson.Save(store, path, backup: false);
        var loaded = LauncherJson.LoadRequired<LauncherStoreFile>(path, "roundtrip");

        Assert.Equal(2, loaded.SchemaVersion);
        Assert.Single(loaded.Entries);
        Assert.Single(loaded.Presets);
        Assert.Equal("test", loaded.Settings.LastFilterText);
        var historyEntry = Assert.Single(loaded.RunHistory);
        Assert.Equal("test-entry", historyEntry.EntryId);
        Assert.Equal(nameof(LauncherRunStatus.Requested), historyEntry.Status);
    }

    [Fact]
    public void LauncherSettings_RoundTripsAllProperties()
    {
        // Verifies all known properties of LauncherSettings survive
        // a full serialize/deserialize through the source-generated context.
        var settings = new LauncherSettings
        {
            LastFilterText = "roundtrip",
            SortMode = "custom",
            EntryViewMode = "details",
            RequireConfirmationForLaunches = true,
            CustomEntryOrder = ImmutableList.Create("entry-1"),
            CommandTemplates = ImmutableDictionary<string, string>.Empty
                .WithComparers(StringComparer.OrdinalIgnoreCase)
                .Add("term", "wt.exe"),
            SearchHistory = ImmutableList.Create("ping", "tracert"),
            QuickLaunchHotkey = "Shift+Alt+Space",
            RunInTray = false,
            UpdateManifestUrl = "https://example.invalid/update.json",
            Window = new LauncherWindowSettings { Width = 1000, Height = 800 },
        };

        var json = JsonSerializer.Serialize(settings, LauncherJson.Options);
        Assert.NotNull(json);

        var deserialized = JsonSerializer.Deserialize<LauncherSettings>(json, LauncherJson.Options);
        Assert.NotNull(deserialized);
        Assert.Equal(settings.LastFilterText, deserialized.LastFilterText);
        Assert.Equal(settings.SortMode, deserialized.SortMode);
        Assert.Equal(settings.EntryViewMode, deserialized.EntryViewMode);
        Assert.Equal(settings.RequireConfirmationForLaunches, deserialized.RequireConfirmationForLaunches);
        Assert.Equal(settings.CustomEntryOrder, deserialized.CustomEntryOrder);
        Assert.Equal(settings.SearchHistory, deserialized.SearchHistory);
        Assert.Equal(settings.QuickLaunchHotkey, deserialized.QuickLaunchHotkey);
        Assert.Equal(settings.RunInTray, deserialized.RunInTray);
        Assert.Equal(settings.Window.Width, deserialized.Window.Width);
        Assert.Equal(settings.UpdateManifestUrl, deserialized.UpdateManifestUrl);
    }

    [Fact]
    public void LauncherJsonContext_ReturnsNullForUnregisteredType()
    {
        // An unregistered type returns null from the source-generated context.
        // LauncherJson.GetRequiredTypeInfo checks for null and throws at runtime.
        var typeInfo = LauncherJsonContext.Default.GetTypeInfo(typeof(DummyUnregistered));

        Assert.Null(typeInfo);
    }

    private static void AssertRegistered<T>() where T : notnull
    {
        var typeInfo = LauncherJsonContext.Default.GetTypeInfo(typeof(T));
        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(T), typeInfo.Type);
    }

    private sealed class DummyUnregistered { }
}
