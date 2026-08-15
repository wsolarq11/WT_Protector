using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void LayoutWorkflowRecordsSearchHistory()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One"));
        var viewModel = CreateViewModel(config, settings);

        viewModel.FilterText = "codex";
        viewModel._layoutWorkflow.RecordSearchTerm();

        Assert.Contains("codex", viewModel.SearchHistoryItems);
    }

    [Fact]
    public void LayoutWorkflowPersistsWindowBounds()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One"));
        var viewModel = CreateViewModel(config, settings);

        var saved = viewModel.TrySaveWindowBounds(1280, 720, 10, 20);
        var window = viewModel._storeGrain.Select(s => s.Settings.Window);

        Assert.True(saved);
        Assert.Equal(1280, window.Width);
        Assert.Equal(720, window.Height);
    }
}
