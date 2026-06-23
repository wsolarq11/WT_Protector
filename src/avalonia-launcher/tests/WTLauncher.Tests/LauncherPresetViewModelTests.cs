using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetViewModelTests
{
    [Fact]
    public void EntryCountUsesNormalizedEntryIds()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = [" one ", "two"],
        };

        var viewModel = new LauncherPresetViewModel(preset);

        Assert.Equal(2, viewModel.EntryCount);
        Assert.Equal("Daily (2项)", viewModel.Label);
    }

    [Fact]
    public void ViewModelSnapshotsPresetFacts()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one"],
            Locked = true,
        };

        var viewModel = new LauncherPresetViewModel(preset);
        preset.Id = "broken";
        preset.Name = "";
        preset.EntryIds.Clear();
        preset.Locked = false;

        Assert.Equal("daily", viewModel.Id);
        Assert.Equal("Daily", viewModel.Name);
        Assert.Equal(["one"], viewModel.EntryIds);
        Assert.True(viewModel.IsLocked);
        Assert.Equal("Daily (系统组, 1项)", viewModel.Label);
    }

    [Fact]
    public void ViewModelEntryIdsDoNotExposeMutablePresetStorage()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one"],
        };

        var viewModel = new LauncherPresetViewModel(preset);

        Assert.IsNotType<List<string>>(viewModel.EntryIds);
        var entryIds = Assert.IsAssignableFrom<IList<string>>(viewModel.EntryIds);
        Assert.True(entryIds.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => entryIds.Add("fake"));
        Assert.Equal(["one"], viewModel.EntryIds);
        Assert.Equal(["one"], preset.EntryIds);
    }

    [Fact]
    public void EntryCountRejectsInvalidPresetReferences()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one", " one "],
        };

        var exception = Assert.Throws<ArgumentException>(() => new LauncherPresetViewModel(preset));

        Assert.Equal("EntryIds", exception.ParamName);
    }

    [Fact]
    public void LockedLabelRejectsInvalidPresetReferences()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one", ""],
            Locked = true,
        };

        var exception = Assert.Throws<ArgumentException>(() => new LauncherPresetViewModel(preset));

        Assert.Equal("EntryIds", exception.ParamName);
    }
}
