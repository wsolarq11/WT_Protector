using System.Net;
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

    [Fact]
    public async Task HttpUpdateManifestReader_ReadsLocalFile()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "manifest.json");
        await File.WriteAllTextAsync(path, "file-manifest-content", TestContext.Current.CancellationToken);

        var reader = new HttpUpdateManifestReader();
        var content = await reader.ReadManifestAsync(path);

        Assert.Equal("file-manifest-content", content);
    }

    [Fact]
    public async Task CheckAsync_NonSuccessHttpStatus_SurfacesError()
    {
        var reader = new HttpUpdateManifestReader(new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "boom"));
        var service = new UpdateCheckService(reader);

        var result = await service.CheckAsync("https://example.invalid/manifest.json", "1.0.0");

        Assert.True(result.HasError);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAsync_HttpSuccess_ReportsNewerManifest()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "version": "2.0.0",
              "fileName": "WTLauncher.App.exe",
              "sha256": "ABCDEF",
              "bytes": 1,
              "publishedAtUtc": "2026-08-14T00:00:00Z"
            }
            """;
        var reader = new HttpUpdateManifestReader(new StubHttpMessageHandler(HttpStatusCode.OK, json));
        var service = new UpdateCheckService(reader);

        var result = await service.CheckAsync("https://example.invalid/manifest.json", "1.0.0");

        Assert.True(result.HasUpdate);
        Assert.Equal("2.0.0", result.LatestVersion);
        Assert.False(result.HasError);
    }

    [Fact]
    public async Task CheckAsync_HttpTimeout_SurfacesError()
    {
        var reader = new HttpUpdateManifestReader(
            StubHttpMessageHandler.Throwing(new TaskCanceledException("timeout")));
        var service = new UpdateCheckService(reader);

        var result = await service.CheckAsync("https://example.invalid/manifest.json", "1.0.0");

        Assert.True(result.HasError);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;
        private readonly Exception? _exception;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        private StubHttpMessageHandler(Exception exception)
        {
            _exception = exception;
            _statusCode = HttpStatusCode.OK;
            _content = string.Empty;
        }

        public static StubHttpMessageHandler Throwing(Exception exception) => new(exception);

#pragma warning disable xUnit1051 // Test double's required HttpMessageHandler override.
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _exception is not null
                ? Task.FromException<HttpResponseMessage>(_exception)
                : Task.FromResult(new HttpResponseMessage(_statusCode) { Content = new StringContent(_content) });
#pragma warning restore xUnit1051
    }
}
