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
    public void EntryCountRejectsInvalidPresetReferences()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one", " one "],
        };

        var viewModel = new LauncherPresetViewModel(preset);

        var exception = Assert.Throws<ArgumentException>(() => viewModel.EntryCount);

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

        var viewModel = new LauncherPresetViewModel(preset);

        var exception = Assert.Throws<ArgumentException>(() => viewModel.Label);

        Assert.Equal("EntryIds", exception.ParamName);
    }
}
