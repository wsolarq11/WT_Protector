using WTLauncher.App.Features.Updates;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed partial class MainWindowViewModelTests
{
    [Fact]
    public void UpdateUi_ExposesManagementCommandAndReadOnlyWindow()
    {
        var controlBar = File.ReadAllText(TestRepositoryPaths.View("LauncherControlBar.axaml"));
        var window = File.ReadAllText(TestRepositoryPaths.View("UpdateCheckWindow.axaml"));

        Assert.Contains("CheckForUpdatesCommand", controlBar, StringComparison.Ordinal);
        Assert.Contains("Binding ErrorMessage", window, StringComparison.Ordinal);
        Assert.Contains("不会自动覆盖 config/launcher.json", window, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_OpensDialogWithConfiguredManifestUrl()
    {
        using var temp = new TempDirectory();
        var (config, settings, _) = CreatePortableFixture(temp, Entry("one", "One", directory: temp.Path));
        string? capturedUrl = null;
        string? capturedVersion = null;
        var dialog = new StubDialogService
        {
            OnOpenUpdateCheck = (version, url) =>
            {
                capturedVersion = version;
                capturedUrl = url;
                return Task.CompletedTask;
            },
        };
        var viewModel = CreateViewModel(
            config,
            settings,
            new LauncherSettings { UpdateManifestUrl = "https://example.invalid/update.json" },
            dialogService: dialog);

        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal("https://example.invalid/update.json", capturedUrl);
        Assert.False(string.IsNullOrWhiteSpace(capturedVersion));
    }

    [Fact]
    public async Task UpdateCheckViewModel_DisplaysNewVersion()
    {
        var service = new UpdateCheckService(new FakeUpdateManifestReader(_ =>
            Task.FromResult("""
            {
              "schemaVersion": 1,
              "version": "9.9.9",
              "fileName": "WTLauncher.App.exe",
              "sha256": "ABCDEF",
              "bytes": 1,
              "publishedAtUtc": "2026-08-14T00:00:00Z"
            }
            """)));
        var viewModel = new UpdateCheckViewModel(service, "1.0.0", "https://example.invalid/update.json");

        await viewModel.CheckAsync();

        Assert.True(viewModel.HasResult);
        Assert.True(viewModel.HasUpdate);
        Assert.Equal("9.9.9", viewModel.LatestVersion);
    }
}
