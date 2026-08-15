using WTLauncher.App.Features;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void MainWindowViewModelExposesCommandExecutionContextPorts()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var viewModel = CreateViewModel(config, settings);

        ICommandExecutionContext context = viewModel;
        Assert.Same(viewModel._storeGrain, context.Store);
        Assert.Same(viewModel, context.Status);
        Assert.False(string.IsNullOrWhiteSpace(context.GetTimestampUtc()));
    }

    [Fact]
    public void StatusSinkRoutesStatusAndDryRunToMainViewModel()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("a", "A"));
        var viewModel = CreateViewModel(config, settings);

        IStatusSink sink = viewModel;
        var status = LauncherStatusContent.Neutral("context status");
        sink.Apply(status);
        sink.SetDryRunText("wt --context");

        Assert.Equal("context status", viewModel.StatusText);
        Assert.Equal("wt --context", viewModel.DryRunText);
    }
}