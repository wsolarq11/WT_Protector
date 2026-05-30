using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetMutationPersistenceTests
{
    [Fact]
    public void SavePersistsPresetFileWhenSaveSucceeds()
    {
        using var temp = new TempDirectory();
        var presetPath = Path.Combine(temp.Path, "presets.json");
        var presetFile = PresetFile(Preset("daily", "Daily", "one"));

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
            PresetFile(Preset("daily", "Daily", "one")),
            presetPath);

        Assert.Contains("启动组保存失败", saveFailure);
    }

    private static LauncherPresetFile PresetFile(params LauncherPreset[] presets) =>
        new()
        {
            Presets = [.. presets],
        };

    private static LauncherPreset Preset(string id, string name, params string[] entryIds) =>
        new()
        {
            Id = id,
            Name = name,
            EntryIds = [.. entryIds],
        };
}
