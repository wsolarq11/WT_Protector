using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void Refresh_WhenStoreIsCorrupt_SetsConfigLoadFailedStatus()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreateSingleEntryFixture(temp);
        var viewModel = CreateViewModel(config, config);

        File.WriteAllText(config, "not-json{");
        if (File.Exists(config + ".bak"))
            File.Delete(config + ".bak");
        viewModel.RefreshCommand.Execute(null);

        Assert.Contains("配置加载失败", viewModel.StatusContent.ToString(), StringComparison.Ordinal);
        Assert.Single(viewModel.Entries);
    }
}
