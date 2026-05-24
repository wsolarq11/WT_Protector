using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetMutationPersistenceTests
{
    [Fact]
    public void AddPresetRestoresSnapshotWhenSaveFails()
    {
        var presetFile = Presets();
        LauncherPresetFile? restoredFile = null;
        string? restoredPresetId = null;

        var result = LauncherPresetMutationPersistence.AddPreset(
            presetFile,
            "Project Pair",
            ["two"],
            preferredPresetId: "daily",
            savePresets: () => false,
            restorePresetFile: (snapshot, preferredPresetId) =>
            {
                restoredFile = snapshot;
                restoredPresetId = preferredPresetId;
            });

        Assert.Equal(LauncherPresetMutationStatus.SaveFailed, result.Status);
        Assert.Equal("project-pair", result.Preset?.Id);
        Assert.Equal("daily", result.NextPresetId);
        Assert.Equal("daily", restoredPresetId);
        Assert.NotNull(restoredFile);
        var restoredPreset = Assert.Single(restoredFile!.Presets);
        Assert.Equal("daily", restoredPreset.Id);
        Assert.Equal(["one"], restoredPreset.EntryIds);
    }

    [Fact]
    public void UpdatePresetRestoresSnapshotWhenPresetIsMissing()
    {
        var presetFile = Presets();
        LauncherPresetFile? restoredFile = null;

        var result = LauncherPresetMutationPersistence.UpdatePreset(
            presetFile,
            presetId: "missing",
            name: "Missing",
            entryIds: ["two"],
            preferredPresetId: "daily",
            savePresets: () => throw new InvalidOperationException("Save should not run."),
            restorePresetFile: (snapshot, _) => restoredFile = snapshot);

        Assert.Equal(LauncherPresetMutationStatus.MissingPreset, result.Status);
        Assert.Null(result.Preset);
        Assert.Equal("daily", result.NextPresetId);
        Assert.NotNull(restoredFile);
        var restoredPreset = Assert.Single(restoredFile!.Presets);
        Assert.Equal("Daily", restoredPreset.Name);
        Assert.Equal(["one"], restoredPreset.EntryIds);
    }

    [Fact]
    public void DeletePresetReturnsFallbackAfterSuccessfulSave()
    {
        var presetFile = Presets(
            new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
            new LauncherPreset { Id = "secondary", Name = "Secondary", EntryIds = ["two"] });

        var result = LauncherPresetMutationPersistence.DeletePreset(
            presetFile,
            presetId: "daily",
            preferredPresetId: "daily",
            savePresets: () => true,
            restorePresetFile: (_, _) => throw new InvalidOperationException("Restore should not run."));

        Assert.True(result.IsSaved);
        Assert.Equal("daily", result.Preset?.Id);
        Assert.Equal("secondary", result.NextPresetId);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "daily");
        Assert.Equal("secondary", presetFile.DefaultPresetId);
    }

    private static LauncherPresetFile Presets(params LauncherPreset[] presets)
    {
        return new LauncherPresetFile
        {
            DefaultPresetId = presets.Length > 0 ? presets[0].Id : "daily",
            Presets = presets.Length > 0
                ? presets.ToList()
                :
                [
                    new LauncherPreset { Id = "daily", Name = "Daily", EntryIds = ["one"] },
                ],
        };
    }
}
