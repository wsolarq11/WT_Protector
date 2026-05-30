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

    [Theory]
    [InlineData(" Project Pair ", "Project Pair")]
    [InlineData(null, "")]
    public void NormalizePresetName_TrimsUserInput(string? name, string expected)
    {
        Assert.Equal(expected, LauncherPresetWorkflow.NormalizeName(name));
    }

    [Fact]
    public void PresetIdMatchUsesOrdinalComparison()
    {
        Assert.True(LauncherPresetIds.Same("daily", "daily"));
        Assert.False(LauncherPresetIds.Same("Daily", "daily"));
        Assert.True(LauncherPresetIds.Comparer.Equals("daily", "daily"));
        Assert.False(LauncherPresetIds.Comparer.Equals("Daily", "daily"));
    }

    [Fact]
    public void ClonePresetFile_CreatesDeepCopyForRollback()
    {
        var original = PresetFile("daily", Preset("daily", "Daily", true, "one"));
        original.SchemaVersion = 1;

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
        var presetFile = PresetFile(Preset("project-pair", "Existing", "one"));

        var preset = LauncherPresetWorkflow.AddPreset(presetFile, " Project Pair ", ["one", "two"]);

        Assert.Equal("project-pair-2", preset.Id);
        Assert.Equal("Project Pair", preset.Name);
        Assert.Equal(["one", "two"], preset.EntryIds);
        Assert.Equal(preset, Assert.Single(presetFile.Presets, item => item.Id == "project-pair-2"));
    }

    [Fact]
    public void AddPreset_TrimsAndDedupesEntryIds()
    {
        var presetFile = PresetFile();

        var preset = LauncherPresetWorkflow.AddPreset(presetFile, "Project Pair", [" one ", "one", "", "two", "two"]);

        Assert.Equal(["one", "two"], preset.EntryIds);
    }

    [Fact]
    public void AddPreset_UsesNewPresetAsDefaultWhenCurrentDefaultIsMissing()
    {
        var presetFile = PresetFile("missing", Preset("daily", "Daily", "one"));

        var preset = LauncherPresetWorkflow.AddPreset(presetFile, " Project Pair ", ["one", "two"]);

        Assert.Equal(preset.Id, presetFile.DefaultPresetId);
    }

    [Fact]
    public void AddPreset_KeepsExistingDefaultPreset()
    {
        var presetFile = PresetFile("daily", Preset("daily", "Daily", "one"));

        LauncherPresetWorkflow.AddPreset(presetFile, " Project Pair ", ["one", "two"]);

        Assert.Equal("daily", presetFile.DefaultPresetId);
    }

    [Fact]
    public void AddPreset_RejectsBlankPresetNameBeforeMutatingFile()
    {
        var presetFile = PresetFile(Preset("daily", "Daily", "one"));

        AssertPresetNameRequired(() => LauncherPresetWorkflow.AddPreset(presetFile, " ", ["one"]));
        Assert.Single(presetFile.Presets);
    }

    [Fact]
    public void UpdatePreset_ChangesUnlockedPresetButNotLockedOrMissingPreset()
    {
        var presetFile = PresetFile(
            Preset("daily", "Daily", "one"),
            Preset("blank-manual", "Manual", locked: true));

        Assert.True(LauncherPresetWorkflow.CanUpdatePreset(presetFile, "daily"));
        Assert.False(LauncherPresetWorkflow.CanUpdatePreset(presetFile, "blank-manual"));
        Assert.False(LauncherPresetWorkflow.CanUpdatePreset(presetFile, "missing"));

        var updated = LauncherPresetWorkflow.UpdatePreset(presetFile, "daily", " Daily Updated ", ["one", "two"]);
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
    public void UpdatePreset_RejectsBlankPresetNameBeforeMutatingPreset()
    {
        var presetFile = PresetFile(Preset("daily", "Daily", "one"));

        AssertPresetNameRequired(() => LauncherPresetWorkflow.UpdatePreset(presetFile, "daily", " ", ["two"]));
        var preset = Assert.Single(presetFile.Presets);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void UpdatePreset_TrimsAndDedupesEntryIds()
    {
        var presetFile = PresetFile(Preset("daily", "Daily", "one"));

        var updated = LauncherPresetWorkflow.UpdatePreset(presetFile, "daily", "Daily Updated", [" one ", "one", "", "two", "two"]);

        Assert.NotNull(updated);
        Assert.Equal(["one", "two"], presetFile.Presets.Single().EntryIds);
    }

    [Fact]
    public void DeletePreset_RemovesOnlyAllowedPresetAndUpdatesDefaultFallback()
    {
        var presetFile = PresetFile(
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"),
            Preset("blank-manual", "Manual", locked: true));

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

    [Fact]
    public void DeletePreset_RepairsMissingDefaultFallback()
    {
        var presetFile = PresetFile(
            "missing",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));

        var deleted = LauncherPresetWorkflow.DeletePreset(presetFile, "secondary", out var fallbackPresetId);

        Assert.Equal("secondary", deleted?.Id);
        Assert.Equal("daily", fallbackPresetId);
        Assert.Equal("daily", presetFile.DefaultPresetId);
    }

    [Fact]
    public void DeletePreset_PrefersNonEmptyFallbackPreset()
    {
        var presetFile = PresetFile(
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("blank-manual", "Manual", locked: true),
            Preset("secondary", "Secondary", "two"));

        var deleted = LauncherPresetWorkflow.DeletePreset(presetFile, "daily", out var fallbackPresetId);

        Assert.Equal("daily", deleted?.Id);
        Assert.Equal("secondary", fallbackPresetId);
        Assert.Equal("secondary", presetFile.DefaultPresetId);
    }

    private static LauncherPresetFile PresetFile(params LauncherPreset[] presets) =>
        new()
        {
            Presets = [.. presets],
        };

    private static LauncherPresetFile PresetFile(string defaultPresetId, params LauncherPreset[] presets) =>
        new()
        {
            DefaultPresetId = defaultPresetId,
            Presets = [.. presets],
        };

    private static LauncherPreset Preset(string id, string name, params string[] entryIds) =>
        Preset(id, name, locked: false, entryIds);

    private static LauncherPreset Preset(string id, string name, bool locked, params string[] entryIds) =>
        new()
        {
            Id = id,
            Name = name,
            EntryIds = [.. entryIds],
            Locked = locked,
        };

    private static void AssertPresetNameRequired(Action action)
    {
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("name", exception.ParamName);
    }
}
