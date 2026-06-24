using System.Collections.Immutable;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherEntryReferencesTests
{
    [Fact]
    public void RenameEntryIdUpdatesPresetsAndCustomOrder()
    {
        var presetFile = PresetFile(
            Preset("daily", "one", "two"),
            Preset("manual", "three"));
        var customEntryOrder = ImmutableList.Create("one", "two", "three");

        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RenameEntryId(
            presetFile,
            customEntryOrder,
            oldEntryId: "one",
            newEntryId: "one-renamed");

        Assert.Equal(["one-renamed", "two"], updatedPresetFile.Presets[0].EntryIds);
        Assert.Equal(["three"], updatedPresetFile.Presets[1].EntryIds);
        Assert.Equal(["one-renamed", "two", "three"], updatedOrder);
    }

    [Fact]
    public void RenameEntryIdMatchesTrimmedReferences()
    {
        var presetFile = PresetFile(Preset("daily", " one ", "two"));
        var customEntryOrder = ImmutableList.Create(" one ", "two");

        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RenameEntryId(
            presetFile,
            customEntryOrder,
            oldEntryId: "one",
            newEntryId: "one-renamed");

        Assert.Equal(["one-renamed", "two"], updatedPresetFile.Presets[0].EntryIds);
        Assert.Equal(["one-renamed", "two"], updatedOrder);
    }

    [Fact]
    public void RenameEntryIdNormalizesReplacementId()
    {
        var presetFile = PresetFile(Preset("daily", "one"));
        var customEntryOrder = ImmutableList.Create("one");

        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RenameEntryId(
            presetFile,
            customEntryOrder,
            oldEntryId: "one",
            newEntryId: " one-renamed ");

        Assert.Equal(["one-renamed"], updatedPresetFile.Presets[0].EntryIds);
        Assert.Equal(["one-renamed"], updatedOrder);
    }

    [Fact]
    public void RenameEntryIdRejectsBlankSourceBeforeMutatingReferences()
    {
        var presetFile = PresetFile(Preset("daily", "one", " "));
        var customEntryOrder = ImmutableList.Create("one", " ");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryReferences.RenameEntryId(
                presetFile,
                customEntryOrder,
                oldEntryId: " ",
                newEntryId: "one-renamed"));

        Assert.Equal("oldEntryId", exception.ParamName);
    }

    [Fact]
    public void RenameEntryIdRejectsBlankReplacementBeforeMutatingReferences()
    {
        var presetFile = PresetFile(Preset("daily", "one"));
        var customEntryOrder = ImmutableList.Create("one");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryReferences.RenameEntryId(
                presetFile,
                customEntryOrder,
                oldEntryId: "one",
                newEntryId: " "));

        Assert.Equal("newEntryId", exception.ParamName);
    }

    [Fact]
    public void RemoveEntryIdRemovesPresetsAndCustomOrderReferences()
    {
        var presetFile = PresetFile(
            Preset("daily", "one", "two"),
            Preset("manual", "three"));
        var customEntryOrder = ImmutableList.Create("one", "two", "three");

        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RemoveEntryId(
            presetFile,
            customEntryOrder,
            entryId: "one");

        Assert.Equal(["two"], updatedPresetFile.Presets[0].EntryIds);
        Assert.Equal(["three"], updatedPresetFile.Presets[1].EntryIds);
        Assert.Equal(["two", "three"], updatedOrder);
    }

    [Fact]
    public void RemoveEntryIdMatchesTrimmedReferences()
    {
        var presetFile = PresetFile(Preset("daily", " one ", "two"));
        var customEntryOrder = ImmutableList.Create(" one ", "two");

        var (updatedPresetFile, updatedOrder) = LauncherEntryReferences.RemoveEntryId(
            presetFile,
            customEntryOrder,
            entryId: "one");

        Assert.Equal(["two"], updatedPresetFile.Presets[0].EntryIds);
        Assert.Equal(["two"], updatedOrder);
    }

    [Fact]
    public void RenameEntryIdRejectsDuplicateReferencesBeforeMutatingReferences()
    {
        var presetFile = PresetFile(Preset("daily", "one", " one "));
        var customEntryOrder = ImmutableList.Create("one", "two");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryReferences.RenameEntryId(
                presetFile,
                customEntryOrder,
                oldEntryId: "one",
                newEntryId: "one-renamed"));

        Assert.Equal("entryIds", exception.ParamName);
    }

    [Fact]
    public void RenameEntryIdRejectsBlankReferencesBeforeMutatingAnyReferences()
    {
        var presetFile = PresetFile(Preset("daily", "one"));
        var customEntryOrder = ImmutableList.Create("one", " ");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryReferences.RenameEntryId(
                presetFile,
                customEntryOrder,
                oldEntryId: "one",
                newEntryId: "one-renamed"));

        Assert.Equal("entryIds", exception.ParamName);
    }

    [Fact]
    public void RenameEntryIdRejectsResultingDuplicateReferencesBeforeMutatingReferences()
    {
        var presetFile = PresetFile(Preset("daily", "one", "two"));
        var customEntryOrder = ImmutableList.Create("one", "two");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryReferences.RenameEntryId(
                presetFile,
                customEntryOrder,
                oldEntryId: "one",
                newEntryId: "two"));

        Assert.Equal("newEntryId", exception.ParamName);
    }

    [Fact]
    public void RemoveEntryIdRejectsBlankEntryIdBeforeMutatingReferences()
    {
        var presetFile = PresetFile(Preset("daily", "one", " "));
        var customEntryOrder = ImmutableList.Create("one", " ");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryReferences.RemoveEntryId(
                presetFile,
                customEntryOrder,
                entryId: " "));

        Assert.Equal("entryId", exception.ParamName);
    }

    [Fact]
    public void RemoveEntryIdRejectsDuplicateReferencesBeforeMutatingReferences()
    {
        var presetFile = PresetFile(Preset("daily", "one", "two"));
        var customEntryOrder = ImmutableList.Create("one", " two ", "two");

        var exception = Assert.Throws<ArgumentException>(() =>
            LauncherEntryReferences.RemoveEntryId(
                presetFile,
                customEntryOrder,
                entryId: "one"));

        Assert.Equal("entryIds", exception.ParamName);
    }

    private static LauncherPresetFile PresetFile(params LauncherPreset[] presets) =>
        new()
        {
            Presets = presets.ToImmutableList(),
        };

    private static LauncherPreset Preset(string id, params string[] entryIds) =>
        new()
        {
            Id = id,
            Name = id,
            EntryIds = entryIds.ToImmutableList(),
        };
}
