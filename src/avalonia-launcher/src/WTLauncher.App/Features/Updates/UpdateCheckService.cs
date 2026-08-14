using WTLauncher.App.Infrastructure;
using WTLauncher.App.ViewModels;
using WTLauncher.Core;

namespace WTLauncher.App.Features.Updates;

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
    private readonly Func<string, Task<string>> _fetch;

    public UpdateCheckService(Func<string, Task<string>>? fetch = null)
    {
        _fetch = fetch ?? DefaultFetch;
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
                ErrorMessage = "未配置更新清单地址；可在 launcher.json 的 settings.updateManifestUrl 设置。",
            };
        }

        try
        {
            var json = await _fetch(url).ConfigureAwait(true);
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

    private static async Task<string> DefaultFetch(string manifestUrl)
    {
        if (Uri.TryCreate(manifestUrl, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https")
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(uri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        return await File.ReadAllTextAsync(manifestUrl).ConfigureAwait(false);
    }
}
