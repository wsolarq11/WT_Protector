using WTLauncher.App.ViewModels;
using WTLauncher.Core;
using static WTLauncher.Tests.LauncherPersistenceTestFixtures;

namespace WTLauncher.Tests;

public sealed class LauncherConfigMutationPersistenceTests
{
    [Fact]
    public void SavePersistsConfigEntriesWhenSaveSucceeds()
    {
        using var temp = new TempDirectory();
        var configPath = Path.Combine(temp.Path, "config.json");

        var saveFailure = LauncherConfigMutationPersistence.Save([Entry("one")], configPath);

        Assert.Null(saveFailure);
        Assert.Equal("one", Assert.Single(LauncherConfigStore.Load(configPath)).Id);
    }

    [Fact]
    public void SaveReturnsConfigSaveStatusWhenSaveFails()
    {
        using var temp = new TempDirectory();

        var saveFailure = LauncherConfigMutationPersistence.Save([Entry("one")], temp.Path);

        Assert.Contains("配置保存失败", saveFailure);
    }
}
