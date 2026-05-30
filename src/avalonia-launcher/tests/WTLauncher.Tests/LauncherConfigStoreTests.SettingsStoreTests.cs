using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class LauncherConfigStoreTests
{
    [Fact]
    public void SettingsStore_RoundTripsPortableSettings()
    {
        using var temp = new TempDirectory();
        var settings = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        LauncherSettingsStore.Save(
            new LauncherSettings
            {
                Theme = "dark",
                LastFilterText = "爬虫",
                SortMode = LauncherEntryVisibility.AlphabeticalSortModeId,
                EntryViewMode = LauncherEntryViewModes.DetailsId,
                RequireConfirmationForLaunches = false,
                CustomEntryOrder = ["two", "one"],
                Window = new LauncherWindowSettings { Width = 1200, Height = 800 },
            },
            settings);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal("dark", loaded.Theme);
        Assert.Equal("爬虫", loaded.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.AlphabeticalSortModeId, loaded.SortMode);
        Assert.Equal(LauncherEntryViewModes.DetailsId, loaded.EntryViewMode);
        Assert.False(loaded.RequireConfirmationForLaunches);
        Assert.Equal(["two", "one"], loaded.CustomEntryOrder);
        Assert.Equal(1200d, loaded.Window.Width);
        Assert.Equal(800d, loaded.Window.Height);
        var savedJson = File.ReadAllText(settings);
        Assert.Contains("爬虫", savedJson);
        Assert.Contains("alphabetical", savedJson);
        Assert.Contains("details", savedJson);
        Assert.Contains("requireConfirmationForLaunches", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
    }

    [Fact]
    public void SettingsStore_NormalizesDirtySettingsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var settings = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        LauncherSettingsStore.Save(
            new LauncherSettings
            {
                Theme = null!,
                LastFilterText = null!,
                SortMode = null!,
                EntryViewMode = null!,
                CustomEntryOrder = [" one ", null!, "one", " ", "two", " two "],
                Window = null!,
            },
            settings);

        var savedJson = File.ReadAllText(settings);
        Assert.DoesNotContain("null", savedJson);
        var loaded = LauncherSettingsStore.Load(settings);
        Assert.Equal("light", loaded.Theme);
        Assert.Equal(string.Empty, loaded.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, loaded.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, loaded.EntryViewMode);
        Assert.Equal(["one", "two"], loaded.CustomEntryOrder);
        Assert.NotNull(loaded.Window);
    }

    [Fact]
    public void SettingsStore_NormalizesNullWindowAndOrderFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "theme": null,
          "lastFilterText": null,
          "sortMode": null,
          "entryViewMode": null,
          "requireConfirmationForLaunches": true,
          "customEntryOrder": null,
          "window": null
        }
        """);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal("light", loaded.Theme);
        Assert.Equal(string.Empty, loaded.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, loaded.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, loaded.EntryViewMode);
        Assert.NotNull(loaded.Window);
        Assert.Empty(loaded.CustomEntryOrder);
    }

    [Fact]
    public void SettingsStore_NormalizesCustomEntryOrderIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "customEntryOrder": [" one ", null, "one", " ", "two", " two "]
        }
        """);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal(["one", "two"], loaded.CustomEntryOrder);
    }

    [Fact]
    public void SettingsStore_NormalizesInvalidModeIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "sortMode": " missing ",
          "entryViewMode": " missing "
        }
        """);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, loaded.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, loaded.EntryViewMode);
    }

    [Fact]
    public void SettingsStore_RejectsNullSettingsOnSave()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        var exception = Assert.Throws<ArgumentNullException>(
            () => LauncherSettingsStore.Save(null!, settingsPath));

        Assert.Equal("settings", exception.ParamName);
    }

    [Fact]
    public void SettingsStore_NormalizesSettingsDataBeforeWriting()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            Theme = null!,
            LastFilterText = null!,
            SortMode = " missing ",
            EntryViewMode = " missing ",
            CustomEntryOrder = [null!, " one ", "one", " ", "two", " two "],
            Window = null!,
        };

        LauncherSettingsStore.Save(settings, settingsPath);

        var loaded = LauncherSettingsStore.Load(settingsPath);
        Assert.Equal("light", loaded.Theme);
        Assert.Equal(string.Empty, loaded.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, loaded.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, loaded.EntryViewMode);
        Assert.Equal(["one", "two"], loaded.CustomEntryOrder);
        Assert.NotNull(loaded.Window);
        var savedJson = File.ReadAllText(settingsPath);
        Assert.DoesNotContain("null", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsPersistsPortableCloseSize()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            Window = new LauncherWindowSettings { Width = 1180, Height = 760 },
        };

        var saved = LauncherSettingsStore.TrySaveWindowBounds(settings, settingsPath, width: 1320, height: 840);

        Assert.True(saved);
        Assert.Equal(1320d, settings.Window.Width);
        Assert.Equal(840d, settings.Window.Height);
        var loaded = LauncherSettingsStore.Load(settingsPath);
        Assert.Equal(1320d, loaded.Window.Width);
        Assert.Equal(840d, loaded.Window.Height);
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsReturnsFalseAndRollsBackOnCloseSaveFailure()
    {
        using var temp = new TempDirectory();
        var configDir = ConfigDirectoryPath(temp.Path);
        var settingsPath = Path.Combine(configDir, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            Theme = null!,
            SortMode = " missing ",
            EntryViewMode = " missing ",
            CustomEntryOrder = [" one ", "one"],
            Window = new LauncherWindowSettings { Width = 1180, Height = 760 },
        };
        Directory.CreateDirectory(settingsPath);

        var saved = LauncherSettingsStore.TrySaveWindowBounds(settings, settingsPath, width: 1320, height: 840);

        Assert.False(saved);
        Assert.Null(settings.Theme);
        Assert.Equal(" missing ", settings.SortMode);
        Assert.Equal(" missing ", settings.EntryViewMode);
        Assert.Equal([" one ", "one"], settings.CustomEntryOrder);
        Assert.Equal(1180d, settings.Window.Width);
        Assert.Equal(760d, settings.Window.Height);
        Assert.Empty(Directory.EnumerateFiles(configDir, $"{LauncherConfigStore.SettingsFileName}.tmp-*"));
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsRestoresNullWindowOnCloseSaveFailure()
    {
        using var temp = new TempDirectory();
        var configDir = ConfigDirectoryPath(temp.Path);
        var settingsPath = Path.Combine(configDir, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            Window = null!,
        };
        Directory.CreateDirectory(settingsPath);

        var saved = LauncherSettingsStore.TrySaveWindowBounds(settings, settingsPath, width: 1320, height: 840);

        Assert.False(saved);
        Assert.Null(settings.Window);
    }

}
