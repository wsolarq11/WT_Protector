using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.Tests;

public sealed class DiscoverAgentsViewModelTests
{
    private static DiscoverAgentsViewModel CreateViewModel(Func<AgentDiscoveryResult> scanFunc) =>
        new(scanFunc, alreadyAddedExecNames: Array.Empty<string>());

    [Fact]
    public async Task ScanAsync_WhenScanFuncThrowsRecoverable_SurfacesErrorAndClearsScanning()
    {
        // InvalidOperationException is one of the scanner's own per-agent catch types,
        // so it is representative of an orchestration-level escape here.
        Func<AgentDiscoveryResult> throws = () => throw new InvalidOperationException("boom");
        var viewModel = CreateViewModel(throws);

        await viewModel.ScanAsync();

        Assert.False(viewModel.IsScanning);
        Assert.Equal("扫描过程中发生错误", viewModel.StatusText);
        Assert.Empty(viewModel.Rows);
    }
}
