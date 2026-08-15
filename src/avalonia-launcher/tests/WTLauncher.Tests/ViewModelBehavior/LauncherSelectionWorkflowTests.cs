using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void SelectionWorkflowReplaceSelectionPersistsStore()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(
            temp,
            Entry("one", "One", command: "codex", selected: true),
            Entry("two", "Two", command: "opencode"));
        var viewModel = CreateViewModel(config, settings);

        var saved = viewModel._selectionWorkflow.ReplaceSelection(new HashSet<string> { "two" });
        var store = LauncherStore.Load(config);

        Assert.True(saved);
        var selected = Assert.Single(viewModel.Entries, entry => entry.IsSelected);
        Assert.Equal("two", selected.Id);
        Assert.Equal("two", store.Entries.Single(entry => entry.Selected).Id.Value);
    }
}
