using System.Text.Json;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.Persistence;

[Trait("Category", TestCategories.Persistence)]
public sealed class LegacyMigrationTests
{
    private static string WriteLegacyConfig(string directory, string id = "one")
    {
        var configPath = Path.Combine(directory, "config.json");
        var entries = new List<LaunchEntry>
        {
            new()
            {
                Id = id,
                Name = id,
                Directory = (DirectoryPath)@"C:\tmp",
                Command = "pwsh",
            },
        };
        File.WriteAllText(configPath, JsonSerializer.Serialize(entries, LauncherJsonContext.Default.ListLaunchEntry));
        return configPath;
    }

    [Fact]
    public void MigrateFromLegacyFiles_CreatesUnifiedStoreAndMovesLegacyFiles()
    {
        using var temp = new TempDirectory();
        var configPath = WriteLegacyConfig(temp.Path);
        var presetsPath = Path.Combine(temp.Path, "presets.json");
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(new LauncherSettings(), LauncherJsonContext.Default.LauncherSettings));
        File.WriteAllText(presetsPath, JsonSerializer.Serialize(new LegacyPresetDocument(), LauncherJsonContext.Default.LegacyPresetDocument));

        var migrated = LegacyMigration.MigrateFromLegacyFiles(temp.Path);

        Assert.NotNull(migrated);
        Assert.Single(migrated.Entries);
        Assert.Equal("one", migrated.Entries[0].Id.Value);
        Assert.True(File.Exists(Path.Combine(temp.Path, LauncherStore.StoreFileName)));
        Assert.True(File.Exists(configPath + ".bak"));
        Assert.False(File.Exists(configPath));
    }

    [Fact]
    public void MigrateFromLegacyFiles_SkipsWhenUnifiedStoreExists()
    {
        using var temp = new TempDirectory();
        WriteLegacyConfig(temp.Path);
        var storePath = Path.Combine(temp.Path, LauncherStore.StoreFileName);
        File.WriteAllText(storePath, "{}");

        var migrated = LegacyMigration.MigrateFromLegacyFiles(temp.Path);

        Assert.Null(migrated);
        Assert.True(File.Exists(Path.Combine(temp.Path, "config.json")));
    }

    [Fact]
    public void MigrateFromLegacyFiles_CorruptSettingsFallsBackToDefaults()
    {
        using var temp = new TempDirectory();
        WriteLegacyConfig(temp.Path);
        File.WriteAllText(Path.Combine(temp.Path, "settings.json"), "not-json{");

        var migrated = LegacyMigration.MigrateFromLegacyFiles(temp.Path);

        Assert.NotNull(migrated);
        Assert.Equal(LauncherEntryQuery.CustomSortModeId, migrated.Settings.SortMode);
        Assert.Equal(LauncherEntryViewModes.CardsId, migrated.Settings.EntryViewMode);
    }

    [Fact]
    public void MigrateFromLegacyFiles_CorruptPresetsFallsBackToEmpty()
    {
        using var temp = new TempDirectory();
        WriteLegacyConfig(temp.Path);
        File.WriteAllText(Path.Combine(temp.Path, "presets.json"), "not-json{");

        var migrated = LegacyMigration.MigrateFromLegacyFiles(temp.Path);

        Assert.NotNull(migrated);
        Assert.Empty(migrated.Presets);
        Assert.Equal("", migrated.DefaultPresetId);
    }
}
