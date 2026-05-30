using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class LauncherPresetViewModelTests
{
    [Fact]
    public void EntryCountUsesNormalizedUsableEntryIds()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one", " one ", "two", " "],
        };

        var viewModel = new LauncherPresetViewModel(preset);

        Assert.Equal(2, viewModel.EntryCount);
        Assert.Equal("Daily (2项)", viewModel.Label);
    }

    [Fact]
    public void LockedLabelUsesNormalizedEntryCount()
    {
        var preset = new LauncherPreset
        {
            Id = "daily",
            Name = "Daily",
            EntryIds = ["one", "one", ""],
            Locked = true,
        };

        var viewModel = new LauncherPresetViewModel(preset);

        Assert.Equal("Daily (系统组, 1项)", viewModel.Label);
    }
}
