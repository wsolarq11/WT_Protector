using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Features.Updates;

namespace WTLauncher.App.ViewModels;

public sealed partial class UpdateCheckViewModel : ObservableObject
{
    private readonly UpdateCheckService _service;

    public string CurrentVersion { get; }

    public string? ManifestUrl { get; }

    [ObservableProperty]
    private string _status = "正在检查更新…";

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private bool _isUpToDate;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _latestVersion;

    [ObservableProperty]
    private string? _errorMessage;

    public UpdateCheckViewModel(
        UpdateCheckService service,
        string currentVersion,
        string? manifestUrl)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        CurrentVersion = currentVersion;
        ManifestUrl = manifestUrl;
    }

    public async Task CheckAsync()
    {
        Status = "正在检查更新…";
        HasResult = false;
        HasUpdate = false;
        IsUpToDate = false;
        HasError = false;
        ErrorMessage = null;
        LatestVersion = null;

        var result = await _service.CheckAsync(ManifestUrl, CurrentVersion).ConfigureAwait(true);
        LatestVersion = result.LatestVersion;
        HasError = result.HasError;
        ErrorMessage = result.ErrorMessage;
        HasUpdate = result.HasUpdate;
        IsUpToDate = !result.HasError && !result.HasUpdate;
        HasResult = !result.HasError;
        Status = result.HasError
            ? "检查失败"
            : result.HasUpdate
                ? "发现新版本"
                : "当前已是最新版本";
    }
}
