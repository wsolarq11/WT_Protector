using WTLauncher.App.Infrastructure;
using WTLauncher.App.Localization;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Updates;

/// <summary>Reads a raw update manifest from an HTTP(S) URL or local file path.</summary>
public interface IUpdateManifestReader
{
    public Task<string> ReadManifestAsync(string manifestUrl);
}

public sealed class HttpUpdateManifestReader : IUpdateManifestReader
{
    private readonly HttpMessageHandler _handler;

    public HttpUpdateManifestReader(HttpMessageHandler? handler = null)
    {
        _handler = handler ?? new HttpClientHandler();
    }

    public async Task<string> ReadManifestAsync(string manifestUrl)
    {
        if (Uri.TryCreate(manifestUrl, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https")
        {
            using var client = new HttpClient(_handler);
            using var response = await client.GetAsync(uri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        return await File.ReadAllTextAsync(manifestUrl).ConfigureAwait(false);
    }
}

public sealed record LauncherUpdateCheckResult
{
    public bool HasUpdate { get; init; }

    public bool IsAvailable => HasUpdate;

    public string? LatestVersion { get; init; }

    public string? ManifestUrl { get; init; }

    public string? ErrorMessage { get; init; }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}

public sealed class UpdateCheckService
{
    private readonly IUpdateManifestReader _reader;

    public UpdateCheckService(IUpdateManifestReader? reader = null)
    {
        _reader = reader ?? new HttpUpdateManifestReader();
    }

    public async Task<LauncherUpdateCheckResult> CheckAsync(
        string? manifestUrl,
        string currentVersion)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        var url = manifestUrl?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return new LauncherUpdateCheckResult
            {
                ManifestUrl = null,
                ErrorMessage = LauncherLocalization.Format("update.manifestUrlMissing"),
            };
        }

        try
        {
            var json = await _reader.ReadManifestAsync(url).ConfigureAwait(true);
            var manifest = LauncherUpdateManifestParser.Parse(json);
            return new LauncherUpdateCheckResult
            {
                HasUpdate = manifest.IsNewerThan(currentVersion),
                LatestVersion = manifest.Version,
                ManifestUrl = url,
            };
        }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
        {
            return new LauncherUpdateCheckResult
            {
                ManifestUrl = url,
                ErrorMessage = ex.Message,
            };
        }
    }
}
