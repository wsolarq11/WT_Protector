using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void QuickLaunchSettings_DefaultToGlobalHotkeyAndTray()
    {
        var settings = new LauncherSettings();

        Assert.Equal(LauncherHotkey.DefaultText, settings.QuickLaunchHotkey);
        Assert.True(settings.RunInTray);
    }

    [Fact]
    public void QuickLaunchWindow_ExposesSearchAndResultContracts()
    {
        var xaml = File.ReadAllText(TestRepositoryPaths.View("QuickLaunchWindow.axaml"));

        Assert.Contains("{Binding Query, Mode=TwoWay}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding Results}", xaml, StringComparison.Ordinal);
        Assert.Contains("{Binding SelectedResult}", xaml, StringComparison.Ordinal);
        Assert.Contains("PlaceholderText=\"{Binding Placeholder}\"", xaml, StringComparison.Ordinal);
    }
    [Fact]
    public void QuickLaunch_EmptyQuery_ShowsAllEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One"),
            Entry("two", "Two"));
        var viewModel = CreateViewModel(config, settings);
        var quickLaunch = new QuickLaunchViewModel(viewModel);

        Assert.Equal(2, quickLaunch.Results.Count);
        Assert.Same(quickLaunch.Results[0], quickLaunch.SelectedResult);
    }

    [Fact]
    public void QuickLaunch_Query_FiltersAndRanksByName()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("alpha", "Alpha Crawler", command: "alpha"),
            Entry("beta", "Beta Crawler", command: "beta"),
            Entry("gamma", "Gamma", command: "gamma"));
        var viewModel = CreateViewModel(config, settings);
        var quickLaunch = new QuickLaunchViewModel(viewModel);

        quickLaunch.Query = "crawler";

        Assert.Equal(2, quickLaunch.Results.Count);
        Assert.Equal("Alpha Crawler", quickLaunch.Results[0].Name);
        Assert.Equal("Beta Crawler", quickLaunch.Results[1].Name);
    }

    [Fact]
    public void QuickLaunch_EmptyQuery_RanksByFrecency()
    {
        using var temp = new TempDirectory();
        var recent = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O");
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("rare", "Rare Entry"),
            Entry("frequent", "Frequent Entry", launchCount: 5, launchedAtUtc: recent));
        var viewModel = CreateViewModel(config, settings);
        var quickLaunch = new QuickLaunchViewModel(viewModel);

        Assert.Equal(2, quickLaunch.Results.Count);
        Assert.Equal("Frequent Entry", quickLaunch.Results[0].Name);
        Assert.Equal("Rare Entry", quickLaunch.Results[1].Name);
    }

    [Fact]
    public void QuickLaunch_Query_FrequentEntrySurfacesAboveEqualMatch()
    {
        using var temp = new TempDirectory();
        var recent = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O");
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("alpha-rare", "Alpha Crawler", command: "alpha"),
            Entry("beta-frequent", "Beta Crawler", command: "beta", launchCount: 5, launchedAtUtc: recent));
        var viewModel = CreateViewModel(config, settings);
        var quickLaunch = new QuickLaunchViewModel(viewModel);

        quickLaunch.Query = "crawler";

        Assert.Equal("Beta Crawler", quickLaunch.Results[0].Name);
    }

    [Fact]
    public void QuickLaunch_MoveSelection_StaysWithinResults()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One"),
            Entry("two", "Two"),
            Entry("three", "Three"));
        var viewModel = CreateViewModel(config, settings);
        var quickLaunch = new QuickLaunchViewModel(viewModel);

        quickLaunch.MoveSelection(1);
        Assert.Equal("Three", quickLaunch.SelectedResult?.Name);
        quickLaunch.MoveSelection(10);
        Assert.Equal("Two", quickLaunch.SelectedResult?.Name);
        quickLaunch.MoveSelection(-10);
        Assert.Equal("One", quickLaunch.SelectedResult?.Name);
    }

    [Fact]
    public void QuickLaunch_LaunchCommand_RequiresSelection()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One"));
        var viewModel = CreateViewModel(config, settings);
        var quickLaunch = new QuickLaunchViewModel(viewModel);

        Assert.True(quickLaunch.LaunchSelectedCommand.CanExecute(null));
        quickLaunch.SelectedResult = null;
        Assert.False(quickLaunch.LaunchSelectedCommand.CanExecute(null));
    }
}
