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
        var id = LauncherPresetIds.GenerateUnique("Project Pair", [" Project Pair ", "project-pair-2"]);

        Assert.Equal("project-pair-3", id);
    }

    [Fact]
    public void PresetIdMatchUsesOrdinalComparison()
    {
        Assert.True(LauncherPresetIds.Same("daily", "daily"));
        Assert.False(LauncherPresetIds.Same("Daily", "daily"));
        Assert.True(LauncherPresetIds.Comparer.Equals("daily", "daily"));
        Assert.False(LauncherPresetIds.Comparer.Equals("Daily", "daily"));
    }

    [Theory]
    [InlineData(" Daily ", "daily")]
    [InlineData(null, "")]
    [InlineData(" ", "")]
    public void NormalizePresetReference_DoesNotGenerateIdsForBlankReferences(string? id, string expected)
    {
        Assert.Equal(expected, LauncherPresetIds.NormalizeReference(id));
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
    public void ClonePresetFile_RejectsInvalidPresetData()
    {
        var original = new LauncherPresetFile
        {
            DefaultPresetId = " Daily ",
            Presets =
            [
                new LauncherPreset
                {
                    Id = " Daily ",
                    Name = null!,
                    EntryIds = ["one"],
                },
                new LauncherPreset
                {
                    Id = " Daily ",
                    Name = "Second",
                    EntryIds = ["two"],
                },
            ],
        };

        var exception = Assert.Throws<InvalidDataException>(() => LauncherPresetFiles.Clone(original));

        Assert.Equal("Launcher preset 0 name is empty.", exception.Message);
        Assert.Equal(" Daily ", original.DefaultPresetId);
        Assert.Equal(2, original.Presets.Count);
        Assert.Null(original.Presets[0].Name);
        Assert.Equal(["two"], original.Presets[1].EntryIds);
    }

    [Fact]
    public void ClonePresetFile_RejectsNullPresetRows()
    {
        var original = new LauncherPresetFile
        {
            Presets = [null!],
        };

        var exception = Assert.Throws<InvalidDataException>(() => LauncherPresetFiles.Clone(original));

        Assert.Equal("Launcher preset 0 is null.", exception.Message);
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
    public void AddPreset_TrimsEntryIds()
    {
        var presetFile = PresetFile();

        var preset = LauncherPresetWorkflow.AddPreset(presetFile, "Project Pair", [" one ", "two "]);

        Assert.Equal(["one", "two"], preset.EntryIds);
    }

    [Fact]
    public void AddPreset_RejectsBlankEntryIdsBeforeMutatingFile()
    {
        var presetFile = PresetFile("daily", Preset("daily", "Daily", "one"));

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherPresetWorkflow.AddPreset(presetFile, "Project Pair", ["one", " "]));

        Assert.Equal("entryIds", exception.ParamName);
        Assert.Equal("daily", presetFile.DefaultPresetId);
        Assert.Equal(["daily"], presetFile.Presets.Select(preset => preset.Id).ToArray());
    }

    [Fact]
    public void AddPreset_RejectsDuplicateEntryIdsBeforeMutatingFile()
    {
        var presetFile = PresetFile("daily", Preset("daily", "Daily", "one"));

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherPresetWorkflow.AddPreset(presetFile, "Project Pair", ["one", " one "]));

        Assert.Equal("entryIds", exception.ParamName);
        Assert.Equal("daily", presetFile.DefaultPresetId);
        Assert.Equal(["daily"], presetFile.Presets.Select(preset => preset.Id).ToArray());
    }

    [Fact]
    public void AddPreset_RejectsMissingDefaultBeforeMutatingExistingPresetFile()
    {
        var presetFile = PresetFile("missing", Preset("daily", "Daily", "one"));

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherPresetWorkflow.AddPreset(presetFile, " Project Pair ", ["one", "two"]));

        Assert.Equal("Launcher preset default id does not match any preset: missing.", exception.Message);
        Assert.Equal("missing", presetFile.DefaultPresetId);
        Assert.Single(presetFile.Presets);
    }

    [Fact]
    public void AddPreset_KeepsExistingDefaultPreset()
    {
        var presetFile = PresetFile("daily", Preset("daily", "Daily", "one"));

        LauncherPresetWorkflow.AddPreset(presetFile, " Project Pair ", ["one", "two"]);

        Assert.Equal("daily", presetFile.DefaultPresetId);
    }

    [Fact]
    public void AddPreset_NormalizesExistingDefaultPresetReferenceBeforeFallback()
    {
        var presetFile = PresetFile(" Daily ", Preset("daily", "Daily", "one"));

        LauncherPresetWorkflow.AddPreset(presetFile, " Project Pair ", ["two"]);

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
    public void UpdatePreset_TrimsEntryIds()
    {
        var presetFile = PresetFile(Preset("daily", "Daily", "one"));

        var updated = LauncherPresetWorkflow.UpdatePreset(presetFile, "daily", "Daily Updated", [" one ", "two "]);

        Assert.NotNull(updated);
        Assert.Equal(["one", "two"], presetFile.Presets.Single().EntryIds);
    }

    [Fact]
    public void UpdatePreset_RejectsBlankEntryIdsBeforeMutatingPreset()
    {
        var presetFile = PresetFile(Preset("daily", "Daily", "one"));

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherPresetWorkflow.UpdatePreset(presetFile, "daily", "Daily Updated", [" "]));

        Assert.Equal("entryIds", exception.ParamName);
        var preset = Assert.Single(presetFile.Presets);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void UpdatePreset_RejectsDuplicateEntryIdsBeforeMutatingPreset()
    {
        var presetFile = PresetFile(Preset("daily", "Daily", "one"));

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherPresetWorkflow.UpdatePreset(presetFile, "daily", "Daily Updated", ["one", " one "]));

        Assert.Equal("entryIds", exception.ParamName);
        var preset = Assert.Single(presetFile.Presets);
        Assert.Equal("Daily", preset.Name);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void PresetWorkflow_NormalizesPresetIdArgumentsBeforeLookup()
    {
        var presetFile = PresetFile(
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));

        Assert.True(LauncherPresetWorkflow.CanUpdatePreset(presetFile, " Daily "));
        var updated = LauncherPresetWorkflow.UpdatePreset(presetFile, " Daily ", "Daily Updated", ["one"]);
        Assert.NotNull(updated);
        Assert.Equal("Daily Updated", presetFile.Presets[0].Name);

        Assert.True(LauncherPresetWorkflow.CanDeletePreset(presetFile, " Daily "));
        var deleted = LauncherPresetWorkflow.DeletePreset(presetFile, " Daily ", out var fallbackPresetId);

        Assert.Equal("daily", deleted?.Id);
        Assert.Equal("secondary", fallbackPresetId);
    }

    [Fact]
    public void PresetWorkflow_DoesNotTreatBlankPresetIdAsGeneratedPresetId()
    {
        var presetFile = PresetFile(
            "preset",
            Preset("preset", "Preset", "one"),
            Preset("secondary", "Secondary", "two"));

        Assert.False(LauncherPresetWorkflow.CanUpdatePreset(presetFile, " "));
        Assert.Null(LauncherPresetWorkflow.UpdatePreset(presetFile, " ", "Changed", ["two"]));
        Assert.Null(LauncherPresetWorkflow.DeletePreset(presetFile, " ", out var fallbackPresetId));

        Assert.Equal("", fallbackPresetId);
        Assert.Equal("Preset", presetFile.Presets[0].Name);
        Assert.Equal(["preset", "secondary"], presetFile.Presets.Select(preset => preset.Id).ToArray());
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
    public void DeletePreset_RejectsMissingDefaultBeforeMutatingPresetFile()
    {
        var presetFile = PresetFile(
            "missing",
            Preset("daily", "Daily", "one"),
            Preset("secondary", "Secondary", "two"));

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherPresetWorkflow.DeletePreset(presetFile, "secondary", out _));

        Assert.Equal("Launcher preset default id does not match any preset: missing.", exception.Message);
        Assert.Equal("missing", presetFile.DefaultPresetId);
        Assert.Equal(["daily", "secondary"], presetFile.Presets.Select(preset => preset.Id).ToArray());
    }

    [Fact]
    public void DeletePreset_NormalizesExistingDefaultFallback()
    {
        var presetFile = PresetFile(
            " Daily ",
            Preset("secondary", "Secondary", "two"),
            Preset("daily", "Daily", "one"));

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

    [Fact]
    public void DeletePreset_RejectsBlankFallbackPresetIdBeforeMutatingPresetFile()
    {
        var presetFile = PresetFile(
            "daily",
            Preset("daily", "Daily", "one"),
            Preset(" ", "Broken", "two"));

        var exception = Assert.Throws<InvalidDataException>(() =>
            LauncherPresetWorkflow.DeletePreset(presetFile, "daily", out _));

        Assert.Equal("Launcher preset fallback id is required after delete.", exception.Message);
        Assert.Equal("daily", presetFile.DefaultPresetId);
        Assert.Equal(["daily", " "], presetFile.Presets.Select(preset => preset.Id).ToArray());
    }

    [Fact]
    public void DeletePreset_RejectsBlankFallbackReferencesBeforeMutatingPresetFile()
    {
        var presetFile = PresetFile(
            "daily",
            Preset("daily", "Daily", "one"),
            Preset("blank", "Blank", " "));

        var deleted = LauncherPresetWorkflow.DeletePreset(presetFile, "daily", out var fallbackPresetId);

        Assert.Equal("daily", deleted?.Id);
        Assert.Equal("blank", fallbackPresetId);
        Assert.Equal("blank", presetFile.DefaultPresetId);
        Assert.Equal(["blank"], presetFile.Presets.Select(preset => preset.Id).ToArray());
    }

    private static LauncherPresetFile PresetFile(params LauncherPreset[] presets) =>
        new()
        {
            DefaultPresetId = presets.FirstOrDefault()?.Id ?? string.Empty,
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
