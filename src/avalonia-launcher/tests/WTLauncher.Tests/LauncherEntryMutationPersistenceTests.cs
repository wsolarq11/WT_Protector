using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static WTLauncher.Tests.LauncherPersistenceTestFixtures;

namespace WTLauncher.Tests;

public sealed class LauncherEntryMutationPersistenceTests
{
    [Fact]
    public void SaveReturnsConfigFailureBeforeLaterPersistenceSteps()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        var snapshot = Snapshot();

        var saveFailure = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: temp.Path,
            presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
            presetPath: presetPath,
            settings: new LauncherSettings(),
            settingsPath: settingsPath,
            savePresets: true,
            rollbackSnapshot: snapshot);

        Assert.Contains("配置保存失败", saveFailure);
        Assert.False(File.Exists(presetPath));
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void SavePresetCommitFailureDoesNotRollBackAlreadyCommittedConfig()
    {
        // Commit journal provides crash recovery; live rename failures still allow
        // partial commit (File.Move is irreversible per-file on NTFS).
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        LauncherConfigStore.Save([Entry("one")], configPath, backup: true);
        var snapshot = Snapshot();

        var saveFailure = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: configPath,
            presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
            presetPath: temp.Path,
            settings: new LauncherSettings(),
            settingsPath: settingsPath,
            savePresets: true,
            rollbackSnapshot: snapshot);

        Assert.Contains("配置保存失败", saveFailure);
        // Config was committed before presets commit failed.
        Assert.Equal("mutated", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
    }

    [Fact]
    public void SaveSettingsCommitFailureDoesNotRollBackAlreadyCommittedConfigAndPresets()
    {
        // Commit journal provides crash recovery; live rename failures still allow
        // partial commit.
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherConfigStore.Save([Entry("one")], configPath, backup: true);
        LauncherPresetStore.Save(PresetFile("daily", Preset("daily", "one")), presetPath, backup: true);
        var snapshot = Snapshot();

        var saveFailure = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: configPath,
            presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
            presetPath: presetPath,
            settings: new LauncherSettings(),
            settingsPath: temp.Path,
            savePresets: true,
            rollbackSnapshot: snapshot);

        Assert.Contains("配置保存失败", saveFailure);
        // Config and presets were committed before settings commit failed.
        Assert.Equal("mutated", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
        var preset = Assert.Single(LauncherPresetStore.Load(presetPath).Presets);
        Assert.Equal("mutated", preset.Id);
    }

    [Fact]
    public void SaveRejectsInvalidSettingsInsteadOfRollingBack()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var settingsPath = Path.Combine(temp.Path, "settings.json");
        LauncherConfigStore.Save([Entry("one")], configPath, backup: true);
        LauncherPresetStore.Save(PresetFile("daily", Preset("daily", "one")), presetPath, backup: true);
        var snapshot = Snapshot();
        var settings = new LauncherSettings
        {
            SortMode = "missing",
        };

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherEntryMutationPersistence.Save(
                entries: [Entry("mutated")],
                configPath: configPath,
                presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
                presetPath: presetPath,
                settings: settings,
                settingsPath: settingsPath,
                savePresets: true,
                rollbackSnapshot: snapshot));

        Assert.Equal("Unknown launcher sort mode: missing.", exception.Message);
        // Settings staging failed (validation threw InvalidDataException), so nothing
        // was committed. The originals remain.
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public void SaveReturnsCommitFailureStatusForBadDestinationPath()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");
        var presetPath = Path.Combine(temp.Path, "presets.json");
        LauncherConfigStore.Save([Entry("one")], configPath, backup: true);
        LauncherPresetStore.Save(PresetFile("daily", Preset("daily", "one")), presetPath, backup: true);
        var snapshot = Snapshot();

        // settingsPath pointing to a directory causes the settings commit rename to fail.
        // The invalid rollback snapshot (Name=null) is irrelevant — file rollback is gone.
        var invalidEntry = Entry("one") with { Name = null! };
        var invalidSnapshot = LauncherEntryMutationSnapshot.Capture(
            [invalidEntry],
            PresetFile("daily", Preset("daily", "one")),
            customEntryOrder: ["one"],
            selectedPresetId: "daily");

        var saveFailure = LauncherEntryMutationPersistence.Save(
            entries: [Entry("mutated")],
            configPath: configPath,
            presetFile: PresetFile("mutated", Preset("mutated", "mutated")),
            presetPath: presetPath,
            settings: new LauncherSettings(),
            settingsPath: temp.Path,
            savePresets: true,
            rollbackSnapshot: invalidSnapshot);

        // Config and presets were committed before the settings commit failed.
        // The invalid snapshot was never used (no file rollback occurs).
        Assert.Contains("配置保存失败", saveFailure);
        Assert.Equal("mutated", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
        var preset = Assert.Single(LauncherPresetStore.Load(presetPath).Presets);
        Assert.Equal("mutated", preset.Id);
    }

    private static LauncherEntryMutationSnapshot Snapshot()
    {
        return LauncherEntryMutationSnapshot.Capture(
            [Entry("one")],
            PresetFile("daily", Preset("daily", "one")),
            customEntryOrder: ["one"],
            selectedPresetId: "daily");
    }
}
