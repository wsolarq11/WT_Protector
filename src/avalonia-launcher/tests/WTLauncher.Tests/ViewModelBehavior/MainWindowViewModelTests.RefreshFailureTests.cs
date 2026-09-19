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

    [Fact]
    public void Refresh_WhenReloadValidationSaveFails_SetsConfigValidationFailedStatus()
    {
        using var temp = new TempDirectory();
        var (config, _, _) = CreateSingleEntryFixture(temp);
        var viewModel = CreateViewModel(config, config);

        var emptyOrderStore = LauncherStore.Load(config) with { Settings = new LauncherSettings() };
        LauncherStore.Save(emptyOrderStore, config, backup: false);
        File.SetAttributes(config, FileAttributes.ReadOnly);

        try
        {
            viewModel.RefreshCommand.Execute(null);
        }
        finally
        {
            File.SetAttributes(config, FileAttributes.Normal);
        }

        Assert.Contains("配置验证失败", viewModel.StatusContent.ToString(), StringComparison.Ordinal);
        Assert.Single(viewModel.Entries);
    }
}
