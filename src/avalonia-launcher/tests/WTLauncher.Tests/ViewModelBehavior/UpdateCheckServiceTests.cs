using WTLauncher.App.Features.Updates;
using WTLauncher.Core;

using WTLauncher.Tests;
namespace WTLauncher.Tests.ViewModelBehavior;

[Trait("Category", TestCategories.ViewModelBehavior)]
public sealed class UpdateCheckServiceTests
{
    [Fact]
    public async Task CheckAsync_ReportsNewerManifest()
    {
        var service = new UpdateCheckService(new FakeUpdateManifestReader(_ =>
            Task.FromResult("""
            {
              "schemaVersion": 1,
              "version": "2.0.0",
              "fileName": "WTLauncher.App.exe",
              "sha256": "ABCDEF",
              "bytes": 1,
              "publishedAtUtc": "2026-08-14T00:00:00Z"
            }
            """)));

        var result = await service.CheckAsync("https://example.invalid/manifest.json", "1.0.0");

        Assert.True(result.HasUpdate);
        Assert.Equal("2.0.0", result.LatestVersion);
        Assert.False(result.HasError);
    }

    [Fact]
    public async Task CheckAsync_ReturnsUpToDateForSameVersion()
    {
        var service = new UpdateCheckService(new FakeUpdateManifestReader(_ =>
            Task.FromResult("""
            {
              "schemaVersion": 1,
              "version": "1.0.0",
              "fileName": "WTLauncher.App.exe",
              "sha256": "ABCDEF",
              "bytes": 1,
              "publishedAtUtc": "2026-08-14T00:00:00Z"
            }
            """)));

        var result = await service.CheckAsync("file:///update/manifest.json", "1.0.0");

        Assert.False(result.HasUpdate);
        Assert.False(result.HasError);
    }

    [Fact]
    public async Task CheckAsync_SurfacesFetchFailureAndMissingUrl()
    {
        var failing = new UpdateCheckService(new FakeUpdateManifestReader(_ => throw new IOException("offline")));
        var failed = await failing.CheckAsync("https://example.invalid/manifest.json", "1.0.0");
        Assert.True(failed.HasError);
        Assert.Contains("offline", failed.ErrorMessage, StringComparison.Ordinal);

        var noUrl = await failing.CheckAsync(null, "1.0.0");
        Assert.True(noUrl.HasError);
        Assert.Contains("updateManifestUrl", noUrl.ErrorMessage, StringComparison.Ordinal);
    }
}
