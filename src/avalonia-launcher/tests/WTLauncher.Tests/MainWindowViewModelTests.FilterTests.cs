using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void ConstructorAppliesSavedFilterWithoutPersistingSettings()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { LastFilterText = "CODEX" });

        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
        Assert.Equal("CODEX", viewModel.FilterText);
        Assert.False(File.Exists(settings));
    }

    [Fact]
    public void ConstructorRejectsNullSavedFilterInsteadOfInventingEmptyFilter()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);

        var exception = Assert.Throws<InvalidDataException>(() =>
            CreateViewModel(
                config,
                settings,
                new LauncherSettings { LastFilterText = null! }));

        Assert.Equal("Launcher settings lastFilterText is null.", exception.Message);
        Assert.False(File.Exists(settings));
    }

    [Fact]
    public void FilterTextPersistsPortableSettingsAndFiltersVisibleEntries()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);

        Assert.Equal(["one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());

        viewModel.FilterText = "CODEX";

        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
        Assert.Equal("CODEX", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "D:\\TWO";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("two", visible.Id);
        Assert.Equal("D:\\TWO", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "ONE";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
        Assert.Equal("ONE", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "   ";

        Assert.Equal(["one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal("   ", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "missing";

        Assert.Empty(viewModel.VisibleEntries);
        Assert.Equal(0, viewModel.VisibleCount);
        Assert.Equal("missing", LauncherSettingsStore.Load(settings).LastFilterText);
    }

    [Fact]
    public void FilterTextRejectsNullMutationInsteadOfInventingEmptyFilter()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);

        viewModel.FilterText = "CODEX";

        var exception = Assert.Throws<ArgumentNullException>(() => viewModel.FilterText = null!);

        Assert.Equal("value", exception.ParamName);
        Assert.Equal("CODEX", viewModel.FilterText);
        Assert.Equal("CODEX", LauncherSettingsStore.Load(settings).LastFilterText);
        var visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Id);
    }

    [Fact]
    public void FilterTextDoesNotStackOverflowWhenSaveFailsRepeatedly()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, oneSelected: false, twoSelected: false);
        var viewModel = CreateViewModel(config, settings);
        // Make settings path a directory so LauncherSettingsStore.Save always fails
        File.Delete(settings);
        Directory.CreateDirectory(settings);
        // Rapid filter changes should NOT stack overflow (H1 guard)
        for (var i = 0; i < 10; i++)
            viewModel.FilterText = $"filter-{i}";
        // After all failed saves, filter should be back to the last successful value
        Assert.Equal("", viewModel.FilterText);
    }
}
