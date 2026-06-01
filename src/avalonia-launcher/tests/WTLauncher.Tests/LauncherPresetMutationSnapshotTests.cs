using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static WTLauncher.Tests.LauncherPersistenceTestFixtures;

namespace WTLauncher.Tests;

public sealed class LauncherPresetMutationSnapshotTests
{
    [Fact]
    public void CaptureDeepCopiesPresetFileAndSelectedPresetId()
    {
        var presetFile = PresetFile("daily", NamedPreset("daily", "Daily", "one"));

        var snapshot = LauncherPresetMutationSnapshot.Capture(presetFile, selectedPresetId: "daily");
        presetFile.DefaultPresetId = "changed";
        presetFile.Presets[0].Name = "Changed";
        presetFile.Presets[0].EntryIds[0] = "changed";

        Assert.Equal("daily", snapshot.SelectedPresetId);
        Assert.Equal("daily", snapshot.PresetFile.DefaultPresetId);
        var preset = Assert.Single(snapshot.PresetFile.Presets);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void ClonePresetFileReturnsFreshMutableCopy()
    {
        var snapshot = LauncherPresetMutationSnapshot.Capture(
            PresetFile("daily", NamedPreset("daily", "Daily", "one")),
            selectedPresetId: "daily");

        var clone = snapshot.ClonePresetFile();
        clone.Presets[0].EntryIds[0] = "changed";

        Assert.Equal("one", Assert.Single(snapshot.PresetFile.Presets[0].EntryIds));
    }

    [Fact]
    public void PublicAccessorDoesNotExposeRollbackStorage()
    {
        var snapshot = LauncherPresetMutationSnapshot.Capture(
            PresetFile("daily", NamedPreset("daily", "Daily", "one")),
            selectedPresetId: "daily");

        snapshot.PresetFile.Presets[0].EntryIds[0] = "changed";

        Assert.Equal("one", Assert.Single(snapshot.ClonePresetFile().Presets[0].EntryIds));
    }
}
