using System.Collections.Immutable;
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
        Assert.Equal(["two", "one"], loaded.CustomEntryOrder!);
        Assert.Equal(1200d, loaded.Window!.Width);
        Assert.Equal(800d, loaded.Window!.Height);
        var savedJson = File.ReadAllText(settings);
        Assert.Contains("爬虫", savedJson);
        Assert.Contains("alphabetical", savedJson);
        Assert.Contains("details", savedJson);
        Assert.Contains("requireConfirmationForLaunches", savedJson);
        Assert.DoesNotContain("theme", savedJson);
        AssertUsesRepositoryLineEndings(savedJson);
    }

    [Fact]
    public void SettingsStore_TrimsDirtyModeAndOrderBeforeWriting()
    {
        using var temp = new TempDirectory();
        var settings = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        LauncherSettingsStore.Save(
            new LauncherSettings
            {
                LastFilterText = "爬虫",
                SortMode = " custom ",
                EntryViewMode = " cards ",
                CustomEntryOrder = [" one ", "two "],
                Window = new LauncherWindowSettings(),
            },
            settings);

        var savedJson = File.ReadAllText(settings);
        Assert.DoesNotContain("null", savedJson);
        var loaded = LauncherSettingsStore.Load(settings);
        Assert.Equal("爬虫", loaded.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, loaded.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, loaded.EntryViewMode);
        Assert.Equal(["one", "two"], loaded.CustomEntryOrder!);
        Assert.NotNull(loaded.Window);
    }

    [Fact]
    public void SettingsStore_RejectsNullLastFilterTextFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": null,
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": 1180,
            "height": 760
          }
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings lastFilterText is null.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsNullSortModeFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": null,
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": 1180,
            "height": 760
          }
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings sortMode is null.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsBlankSortModeFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": " ",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": 1180,
            "height": 760
          }
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings sortMode is empty.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsNullEntryViewModeFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": null,
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": 1180,
            "height": 760
          }
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings entryViewMode is null.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsNullCustomEntryOrderFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": null,
          "window": {
            "width": 1180,
            "height": 760
          }
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings customEntryOrder is null.", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsUnusedThemeFieldFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "theme": "light",
          "customEntryOrder": []
        }
        """);

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherSettingsStore.Load(settings));

        Assert.Contains("theme", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsWrongCaseSettingFieldsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "SortMode": "custom",
          "customEntryOrder": []
        }
        """);

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherSettingsStore.Load(settings));

        Assert.Contains("SortMode", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsMissingWindowFieldFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": []
        }
        """);

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherSettingsStore.Load(settings));

        Assert.Contains("window", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsMissingWindowWidthFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "height": 760
          }
        }
        """);

        var exception = Assert.Throws<System.Text.Json.JsonException>(() => LauncherSettingsStore.Load(settings));

        Assert.Contains("width", exception.Message);
    }

    [Fact]
    public void SettingsStore_RejectsNullWindowFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": null
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings window is null.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SettingsStore_RejectsNonPositiveWindowWidthFromConfig(double width)
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, $$"""
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": {{width}},
            "height": 760
          }
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings window width must be positive.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SettingsStore_RejectsNonPositiveWindowHeightFromConfig(double height)
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, $$"""
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": 1180,
            "height": {{height}}
          }
        }
        """);

        var exception = Assert.Throws<InvalidDataException>(() => LauncherSettingsStore.Load(settings));

        Assert.Equal("Launcher settings window height must be positive.", exception.Message);
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
    public void SettingsStore_ReturnsDefaultsForMissingSettingsFile()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        var settings = LauncherSettingsStore.Load(settingsPath);

        Assert.Equal("", settings.LastFilterText);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, settings.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, settings.EntryViewMode);
        Assert.True(settings.RequireConfirmationForLaunches);
        Assert.Empty(settings.CustomEntryOrder!);
        Assert.NotNull(settings.Window);
        Assert.Equal(LauncherWindowSettings.DefaultWidth, settings.Window!.Width);
        Assert.Equal(LauncherWindowSettings.DefaultHeight, settings.Window!.Height);
    }

    [Fact]
    public void SettingsStore_TrimsCustomEntryOrderIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [" one ", "two "],
          "window": {
            "width": 1180,
            "height": 760
          }
        }
        """);

        var loaded = LauncherSettingsStore.Load(settings);

        Assert.Equal(["one", "two"], loaded.CustomEntryOrder!);
    }

    [Fact]
    public void SettingsStore_RejectsDuplicateCustomEntryOrderIdsFromConfig()
    {
        using var temp = new TempDirectory();
        var settings = WriteConfigDirectoryFile(temp.Path, LauncherConfigStore.SettingsFileName, """
        {
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": ["one", " two ", "one"],
          "window": {
            "width": 1180,
            "height": 760
          }
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
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": ["one", " ", "two"],
          "window": {
            "width": 1180,
            "height": 760
          }
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
          "lastFilterText": "",
          "sortMode": " missing ",
          "entryViewMode": "cards",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": 1180,
            "height": 760
          }
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
          "lastFilterText": "",
          "sortMode": "custom",
          "entryViewMode": " missing ",
          "requireConfirmationForLaunches": true,
          "customEntryOrder": [],
          "window": {
            "width": 1180,
            "height": 760
          }
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
    public void SettingsStore_RejectsNullSettingsFieldsBeforeWriting()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsStore.Save(
                new LauncherSettings
                {
                    LastFilterText = null!,
                    SortMode = LauncherEntryVisibility.CustomSortModeId,
                    EntryViewMode = LauncherEntryViewModes.CardsId,
                    CustomEntryOrder = [],
                    Window = new LauncherWindowSettings(),
                },
                settingsPath));

        Assert.Equal("Launcher settings lastFilterText is null.", exception.Message);
        Assert.False(File.Exists(settingsPath));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void SettingsStore_RejectsInvalidWindowWidthBeforeWriting(double width)
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsStore.Save(
                new LauncherSettings
                {
                    LastFilterText = "",
                    CustomEntryOrder = [],
                    Window = new LauncherWindowSettings { Width = width, Height = 760 },
                },
                settingsPath));

        Assert.Equal("Launcher settings window width must be positive.", exception.Message);
        Assert.False(File.Exists(settingsPath));
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
                    LastFilterText = "",
                    CustomEntryOrder = [],
                    SortMode = " missing ",
                    EntryViewMode = LauncherEntryViewModes.CardsId,
                },
                settingsPath));

        Assert.Equal("Unknown launcher sort mode: missing.", exception.Message);
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void SettingsStore_AcceptsEmptyCustomEntryOrderAsDefault()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);

        LauncherSettingsStore.Save(
            new LauncherSettings
            {
                LastFilterText = "",
                CustomEntryOrder = ImmutableList<string>.Empty,
            },
            settingsPath);

        Assert.True(File.Exists(settingsPath));
        var loaded = LauncherSettingsStore.Load(settingsPath);
        Assert.Empty(loaded.CustomEntryOrder!);
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsPersistsPortableCloseSize()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            LastFilterText = "",
            CustomEntryOrder = [],
            Window = new LauncherWindowSettings
            {
                Width = LauncherWindowSettings.DefaultWidth,
                Height = LauncherWindowSettings.DefaultHeight,
            },
        };

        var saved = LauncherSettingsStore.TrySaveWindowBounds(ref settings, settingsPath, width: 1320, height: 840);

        Assert.True(saved);
        Assert.Equal(1320d, settings.Window!.Width);
        Assert.Equal(840d, settings.Window!.Height);
        var loaded = LauncherSettingsStore.Load(settingsPath);
        Assert.Equal(1320d, loaded.Window!.Width);
        Assert.Equal(840d, loaded.Window!.Height);
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsReturnsFalseAndRollsBackOnCloseSaveFailure()
    {
        using var temp = new TempDirectory();
        var configDir = ConfigDirectoryPath(temp.Path);
        var settingsPath = Path.Combine(configDir, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            LastFilterText = "",
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

        var saved = LauncherSettingsStore.TrySaveWindowBounds(ref settings, settingsPath, width: 1320, height: 840);

        Assert.False(saved);
        Assert.Equal(LauncherEntryVisibility.CustomSortModeId, settings.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, settings.EntryViewMode);
        Assert.Equal(["one", "two"], settings.CustomEntryOrder!);
        Assert.Equal(LauncherWindowSettings.DefaultWidth, settings.Window!.Width);
        Assert.Equal(LauncherWindowSettings.DefaultHeight, settings.Window!.Height);
        Assert.Empty(Directory.EnumerateFiles(configDir, $"{LauncherConfigStore.SettingsFileName}.tmp-*"));
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsRejectsInvalidSettingsBeforeWriteCatch()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            LastFilterText = "",
            CustomEntryOrder = ["one", "one"],
            Window = new LauncherWindowSettings
            {
                Width = LauncherWindowSettings.DefaultWidth,
                Height = LauncherWindowSettings.DefaultHeight,
            },
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsStore.TrySaveWindowBounds(ref settings, settingsPath, width: 1320, height: 840));

        Assert.Equal("Launcher settings customEntryOrder[1] is duplicate: one.", exception.Message);
        Assert.Equal(LauncherWindowSettings.DefaultWidth, settings.Window.Width);
        Assert.Equal(LauncherWindowSettings.DefaultHeight, settings.Window.Height);
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void SettingsStore_TrySaveWindowBoundsRejectsNullWindow()
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            LastFilterText = "",
            CustomEntryOrder = [],
            Window = null!,
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsStore.TrySaveWindowBounds(ref settings, settingsPath, width: 1320, height: 840));

        Assert.Equal("Launcher settings window is null.", exception.Message);
        Assert.Null(settings.Window);
    }

    [Theory]
    [InlineData(0, 840)]
    [InlineData(1320, 0)]
    [InlineData(double.NaN, 840)]
    public void SettingsStore_TrySaveWindowBoundsRejectsInvalidDimensions(double width, double height)
    {
        using var temp = new TempDirectory();
        var settingsPath = ConfigFilePath(temp.Path, LauncherConfigStore.SettingsFileName);
        var settings = new LauncherSettings
        {
            LastFilterText = "",
            CustomEntryOrder = [],
            Window = new LauncherWindowSettings
            {
                Width = LauncherWindowSettings.DefaultWidth,
                Height = LauncherWindowSettings.DefaultHeight,
            },
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherSettingsStore.TrySaveWindowBounds(ref settings, settingsPath, width, height));

        Assert.Contains("must be positive", exception.Message);
        Assert.Equal(LauncherWindowSettings.DefaultWidth, settings.Window.Width);
        Assert.Equal(LauncherWindowSettings.DefaultHeight, settings.Window.Height);
        Assert.False(File.Exists(settingsPath));
    }

}
