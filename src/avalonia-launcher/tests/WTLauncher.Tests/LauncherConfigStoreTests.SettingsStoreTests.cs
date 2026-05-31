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
                LastFilterText = "爬虫",
                SortMode = LauncherEntryVisibility.AlphabeticalSortModeId,
                EntryViewMode = LauncherEntryViewModes.DetailsId,
                RequireConfirmationForLaunches = false,
                CustomEntryOrder = ["two", "one"],
                Window = new LauncherWindowSettings { Width = 1200, Height = 800 },
            },
            settings);

        var loaded = LauncherSettingsStore.Load(settings);

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
        Assert.DoesNotContain("theme", savedJson);
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
                LastFilterText = null!,
                SortMode = null!,
                EntryViewMode = null!,
                CustomEntryOrder = [" one ", "two "],
                Window = null!,
            },
            settings);

        var savedJson = File.ReadAllText(settings);
        Assert.DoesNotContain("null", savedJson);
        var loaded = LauncherSettingsStore.Load(settings);
        Assert.Equal(string.Empty, loaded.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, loaded.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, loaded.EntryViewMode);
        Assert.Equal(["one", "two"], loaded.CustomEntryOrder);
        Assert.NotNull(loaded.Window);
    }

    [Fact]
    public void SettingsStore_RejectsNullCustomEntryOrderFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": null,
          "sortMode": null,
          "entryViewMode": null,
          "requireConfirmationForLaunches": true,
          "customEntryOrder": null,
          "window": null
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings customEntryOrder is null.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsNullSettingsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, "null");

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings parsed to null.", exception.Message);
    }

    [Fact]
    public void SettingsStore_TrimsCustomEntryOrderIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "customEntryOrder": [" one ", "two "]
        }
        """);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal(["one", "two"], loaded.CustomEntryOrder);
    }

    [Fact]
    public void SettingsStore_RejectsDuplicateCustomEntryOrderIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "customEntryOrder": ["one", " two ", "one"]
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings customEntryOrder[2] is duplicate: one.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsBlankCustomEntryOrderIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "customEntryOrder": ["one", " ", "two"]
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings customEntryOrder[1] is empty.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsUnknownSortModeFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "sortMode": " missing ",
          "entryViewMode": "cards"
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Unknown launcher sort mode: missing.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsUnknownEntryViewModeFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "sortMode": "custom",
          "entryViewMode": " missing "
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Unknown launcher entry view mode: missing.", exception.Message);
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
    public void SettingsStore_TrimsSettingsDataBeforeWriting()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            LastFilterText = null!,
            SortMode = null!,
            EntryViewMode = null!,
            CustomEntryOrder = [" one ", "two "],
            Window = null!,
        };

        LauncherSettingsStore.Save(settings, settingsPath);

        Assert.Null(settings.LastFilterText);
        Assert.Null(settings.SortMode);
        Assert.Null(settings.EntryViewMode);
        Assert.Equal([" one ", "two "], settings.CustomEntryOrder);
        Assert.Null(settings.Window);
        var loaded = LauncherSettingsStore.Load(settingsPath);
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
    public void SettingsStore_RejectsUnknownModeIdsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsStore.Save(
                new LauncherSettings
                {
                    SortMode = " missing ",
                    EntryViewMode = LauncherEntryViewModes.CardsId,
                },
                settingsPath));

        Assert.Equal("Unknown launcher sort mode: missing.", exception.Message);
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void SettingsStore_RejectsNullCustomEntryOrderBeforeWriting()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsStore.Save(
                new LauncherSettings
                {
                    CustomEntryOrder = null!,
                },
                settingsPath));

        Assert.Equal("Launcher settings customEntryOrder is null.", exception.Message);
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsPersistsPortableCloseSize()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            Window = new LauncherWindowSettings
            {
                Width = LauncherWindowSettings.DefaultWidth,
                Height = LauncherWindowSettings.DefaultHeight,
            },
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
            SortMode = LauncherEntryVisibility.CustomSortModeId,
            EntryViewMode = LauncherEntryViewModes.CardsId,
            CustomEntryOrder = ["one", "two"],
            Window = new LauncherWindowSettings
            {
                Width = LauncherWindowSettings.DefaultWidth,
                Height = LauncherWindowSettings.DefaultHeight,
            },
        };
        Directory.CreateDirectory(settingsPath);

        var saved = LauncherSettingsStore.TrySaveWindowBounds(settings, settingsPath, width: 1320, height: 840);

        Assert.False(saved);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, settings.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, settings.EntryViewMode);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder);
        Assert.Equal(LauncherWindowSettings.DefaultWidth, settings.Window.Width);
        Assert.Equal(LauncherWindowSettings.DefaultHeight, settings.Window.Height);
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
