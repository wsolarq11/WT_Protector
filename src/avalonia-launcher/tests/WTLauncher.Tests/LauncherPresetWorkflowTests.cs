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
}
