using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static WTLauncher.Tests.LauncherPersistenceTestFixtures;

namespace WTLauncher.Tests;

public sealed class LauncherPresetMutationPersistenceTests
{
    [Fact]
    public void SavePersistsPresetFileWhenSaveSucceeds()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var presetFile = PresetFile(NamedPreset("daily", "Daily", "one"));

        var saveFailure = LauncherPresetMutationPersistence.Save(presetFile, presetPath);

        Assert.Null(saveFailure);
        var saved = LauncherPresetStore.Load(presetPath);
        Assert.Equal("daily", Assert.Single(saved.Presets).Id);
    }

    [Fact]
    public void SaveReturnsPresetSaveStatusWhenSaveFails()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        Directory.CreateDirectory(presetPath);

        var saveFailure = LauncherPresetMutationPersistence.Save(
            PresetFile(NamedPreset("daily", "Daily", "one")),
            presetPath);

        Assert.Contains("启动组保存失败", saveFailure);
    }

    [Fact]
    public void SaveRejectsInvalidPresetDataInsteadOfReturningSaveStatus()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");

        var saveFailure = LauncherPresetMutationPersistence.Save(
            PresetFile("missing", NamedPreset("daily", "Daily", "one")),
            presetPath);

        Assert.NotNull(saveFailure);
        Assert.False(File.Exists(presetPath));
    }
}
