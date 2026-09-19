using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Features.Updates;
using WTLauncher.App.Localization;

namespace WTLauncher.App.ViewModels;

public sealed partial class UpdateCheckViewModel : ObservableObject
{
    private readonly UpdateCheckService _service;

    public string CurrentVersion { get; }

#pragma warning disable CA1822
    public string WindowTitle => LauncherLocalization.Format("update.title");

    public string Note => LauncherLocalization.Format("update.note");
#pragma warning restore CA1822

    public string? ManifestUrl { get; }

    [ObservableProperty]
    private string _status = LauncherLocalization.Format("update.checking");

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
        Status = LauncherLocalization.Format("update.checking");
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
            ? LauncherLocalization.Format("update.failed")
            : result.HasUpdate
                ? LauncherLocalization.Format("update.available")
                : LauncherLocalization.Format("update.current");
    }
}
