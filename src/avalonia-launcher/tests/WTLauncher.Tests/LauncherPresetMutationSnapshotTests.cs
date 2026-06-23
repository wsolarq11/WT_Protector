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
        presetFile = presetFile with
        {
            DefaultPresetId = "changed",
            Presets =
            [
                presetFile.Presets[0] with
                {
                    Name = "Changed",
                    EntryIds = ["changed"]
                }
            ]
        };

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
        clone = clone with
        {
            Presets =
            [
                clone.Presets[0] with
                {
                    EntryIds = ["changed"]
                }
            ]
        };

        Assert.Equal("one", Assert.Single(snapshot.PresetFile.Presets[0].EntryIds));
    }

    [Fact]
    public void PublicAccessorDoesNotExposeRollbackStorage()
    {
        var snapshot = LauncherPresetMutationSnapshot.Capture(
            PresetFile("daily", NamedPreset("daily", "Daily", "one")),
            selectedPresetId: "daily");

        var pf = snapshot.PresetFile;
        pf = pf with
        {
            Presets =
            [
                pf.Presets[0] with
                {
                    EntryIds = ["changed"]
                }
            ]
        };

        Assert.Equal("one", Assert.Single(snapshot.ClonePresetFile().Presets[0].EntryIds));
    }
}
