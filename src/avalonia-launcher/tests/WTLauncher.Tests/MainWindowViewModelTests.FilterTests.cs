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
        Assert.Equal("one", visible.Entry.Id);
        Assert.Equal("CODEX", viewModel.FilterText);
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
        Assert.Equal("one", visible.Entry.Id);
        Assert.Equal("CODEX", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "D:\\TWO";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("two", visible.Entry.Id);
        Assert.Equal("D:\\TWO", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "ONE";

        visible = Assert.Single(viewModel.VisibleEntries);
        Assert.Equal("one", visible.Entry.Id);
        Assert.Equal("ONE", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "   ";

        Assert.Equal(["one", "two"], viewModel.VisibleEntries.Select(entry => entry.Id).ToArray());
        Assert.Equal("   ", LauncherSettingsStore.Load(settings).LastFilterText);

        viewModel.FilterText = "missing";

        Assert.Empty(viewModel.VisibleEntries);
        Assert.Equal(0, viewModel.VisibleCount);
        Assert.Equal("missing", LauncherSettingsStore.Load(settings).LastFilterText);
    }
}
