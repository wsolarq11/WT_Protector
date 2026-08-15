using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void EntryWorkflowAddExistingDirectoryPersistsEntry()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One"));
        var viewModel = CreateViewModel(config, settings);

        viewModel._entryWorkflow.AddExistingDirectoryEntry(
            new EntryDraft(Id: "work", Name: "Work", Directory: temp.Path, Command: "pwsh"));
        var store = LauncherStore.Load(config);

        Assert.Contains(viewModel.Entries, entry => entry.Name == "Work");
        Assert.Contains(store.Entries, entry => entry.Name == "Work");
    }
}
