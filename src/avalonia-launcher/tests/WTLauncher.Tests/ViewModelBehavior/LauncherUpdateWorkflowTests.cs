using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public async Task UpdateWorkflowOpensUpdateCheckWithCurrentContext()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One"));
        string? seenVersion = null;
        string? seenUrl = null;
        var dialogs = new StubDialogService
        {
            OnOpenUpdateCheck = (version, url) =>
            {
                seenVersion = version;
                seenUrl = url;
                return Task.CompletedTask;
            },
        };
        var viewModel = CreateViewModel(config, settings, dialogService: dialogs);

        await viewModel._updateWorkflow.CheckForUpdatesAsync();

        Assert.False(string.IsNullOrWhiteSpace(seenVersion));
        Assert.Equal(string.Empty, seenUrl);
    }
}
