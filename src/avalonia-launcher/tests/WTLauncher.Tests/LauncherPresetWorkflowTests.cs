using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetWorkflowTests
{
    [Theory]
    [InlineData("Project Pair", "project-pair")]
    [InlineData("中文 启动组", "中文-启动组")]
    [InlineData("A/B.C:D\\E", "a-b-c-d-e")]
    [InlineData("Already_ok-2", "already_ok-2")]
    [InlineData("!!!", "preset")]
    public void NormalizePresetId_ProducesStableHumanReadableIds(string name, string expected)
    {
        Assert.Equal(expected, LauncherPresetIds.Normalize(name));
    }

    [Fact]
    public void GenerateUniquePresetId_AppendsStableNumericSuffix()
    {
        var id = LauncherPresetIds.GenerateUnique("Project Pair", ["project-pair", "project-pair-2"]);

        Assert.Equal("project-pair-3", id);
    }

    [Fact]
    public void ClonePresetFile_CreatesDeepCopyForRollback()
    {
        var original = new LauncherPresetFile
        {
            SchemaVersion = 1,
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = "Daily",
                    EntryIds = ["one"],
                    Locked = true,
                },
            ],
        };

        var clone = LauncherPresetFiles.Clone(original);

        original.DefaultPresetId = "changed";
        original.Presets[0].Name = "Changed";
        original.Presets[0].EntryIds.Add("two");

        Assert.Equal("daily", clone.DefaultPresetId);
        var preset = Assert.Single(clone.Presets);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
        Assert.True(preset.Locked);
    }
    [Fact]
    public void AddPreset_AppendsUniquePresetWithSelectedEntries()
    {
        var presetFile = new LauncherPresetFile
        {
            Presets =
            [
                new LauncherPreset
                {
                    Id = "project-pair",
                    Name = "Existing",
                    EntryIds = ["one"],
                },
            ],
        };

        var preset = LauncherPresetWorkflow.AddPreset(presetFile, "Project Pair", ["one", "two"]);

        Assert.Equal("project-pair-2", preset.Id);
        Assert.Equal("Project Pair", preset.Name);
        Assert.Equal(["one", "two"], preset.EntryIds);
        Assert.Equal(preset, Assert.Single(presetFile.Presets, item => item.Id == "project-pair-2"));
    }

    [Fact]
    public void UpdatePreset_ChangesUnlockedPresetButNotLockedOrMissingPreset()
    {
        var presetFile = new LauncherPresetFile
        {
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = "Daily",
                    EntryIds = ["one"],
                },
                new LauncherPreset
                {
                    Id = "blank-manual",
                    Name = "Manual",
                    EntryIds = [],
                    Locked = true,
                },
            ],
        };

        var updated = LauncherPresetWorkflow.UpdatePreset(presetFile, "daily", "Daily Updated", ["one", "two"]);
        var locked = LauncherPresetWorkflow.UpdatePreset(presetFile, "blank-manual", "Changed", ["one"]);
        var missing = LauncherPresetWorkflow.UpdatePreset(presetFile, "missing", "Missing", ["one"]);

        Assert.NotNull(updated);
        Assert.Equal("Daily Updated", updated.Name);
        Assert.Equal(["one", "two"], updated.EntryIds);
        Assert.Null(locked);
        Assert.Equal("Manual", presetFile.Presets.Single(item => item.Id == "blank-manual").Name);
        Assert.Null(missing);
    }

    [Fact]
    public void DeletePreset_RemovesOnlyAllowedPresetAndUpdatesDefaultFallback()
    {
        var presetFile = new LauncherPresetFile
        {
            DefaultPresetId = "daily",
            Presets =
            [
                new LauncherPreset
                {
                    Id = "daily",
                    Name = "Daily",
                    EntryIds = ["one"],
                },
                new LauncherPreset
                {
                    Id = "secondary",
                    Name = "Secondary",
                    EntryIds = ["two"],
                },
                new LauncherPreset
                {
                    Id = "blank-manual",
                    Name = "Manual",
                    EntryIds = [],
                    Locked = true,
                },
            ],
        };

        Assert.True(LauncherPresetWorkflow.CanDeletePreset(presetFile, "daily"));

        var deleted = LauncherPresetWorkflow.DeletePreset(presetFile, "daily", out var fallbackPresetId);
        var locked = LauncherPresetWorkflow.DeletePreset(presetFile, "blank-manual", out var lockedFallbackId);

        Assert.Equal("daily", deleted?.Id);
        Assert.Equal("secondary", fallbackPresetId);
        Assert.Equal("secondary", presetFile.DefaultPresetId);
        Assert.DoesNotContain(presetFile.Presets, preset => preset.Id == "daily");
        Assert.Null(locked);
        Assert.Equal("secondary", lockedFallbackId);
        Assert.Contains(presetFile.Presets, preset => preset.Id == "blank-manual");
    }

}
