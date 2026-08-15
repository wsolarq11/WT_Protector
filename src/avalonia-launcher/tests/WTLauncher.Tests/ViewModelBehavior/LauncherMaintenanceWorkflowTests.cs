using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void MaintenanceWorkflowRenamesCommandAndPersistsStore()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One", command: "codex"));
        var viewModel = CreateViewModel(config, settings);

        var result = viewModel._maintenanceWorkflow.ApplyCommandManagementEdit(
            new CommandManagementEdit.RenameCommand("codex", "wt"));
        var store = LauncherStore.Load(config);

        Assert.True(result.Success);
        Assert.Equal("wt", viewModel.Entries.Single().Command);
        Assert.Equal("wt", store.Entries.Single().Command);
    }
}
