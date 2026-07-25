using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WTLauncher.App.Infrastructure;

namespace WTLauncher.App.ViewModels;

public sealed partial class LauncherEntryViewModel
{
    public const double MicroLayoutMaxWidth = 216;
    public const double MicroLayoutMaxHeight = 96;
    public const double CompactLayoutMaxWidth = 248;
    public const double CompactLayoutMaxHeight = 124;
    public const double ScanningTitleFontSize = 13;
    public const double MicroTitleFontSize = 15;
    public const double CompactTitleFontSize = 16;
    public const double NormalTitleFontSize = 18;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompactLayout))]
    [NotifyPropertyChangedFor(nameof(IsMicroLayout))]
    [NotifyPropertyChangedFor(nameof(ShowDirectory))]
    [NotifyPropertyChangedFor(nameof(ShowCommand))]
    [NotifyPropertyChangedFor(nameof(TitleFontSize))]
    private LauncherEntryLayoutDensity _layoutDensity = LauncherEntryLayoutDensity.Normal;

    public bool IsCompactLayout => LayoutDensity == LauncherEntryLayoutDensity.Compact;

    public bool IsMicroLayout => LayoutDensity is LauncherEntryLayoutDensity.Micro;

    public bool ShowDirectory => ShouldShowDetailText(hideWhenCardsShrink: IsMicroLayout);

    public bool ShowCommand => ShouldShowDetailText(hideWhenCardsShrink: IsCompactLayout || IsMicroLayout);

    public double TitleFontSize => MeasureTitleFontSize();

    /// <summary>
    /// 订阅 CardLayoutProvider 的 PropertyChanged，将卡片尺寸变更转发到本 VM。
    /// 订阅通过 AddDisposable 注册，在 ViewModel 被 Dispose 时自动取消订阅。
    /// </summary>
    private void SubscribeToLayoutProvider()
    {
        PropertyChangedEventHandler handler = OnCardLayoutPropertyChanged;
        _owner.CardLayout.PropertyChanged += handler;
        AddDisposable(new DisposableAction(() => _owner.CardLayout.PropertyChanged -= handler));
    }

    private void OnCardLayoutPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CardLayoutProvider.CardWidth):
                OnPropertyChanged(nameof(CardWidth));
                UpdateLayoutFlags();
                break;
            case nameof(CardLayoutProvider.CardHeight):
                OnPropertyChanged(nameof(CardHeight));
                UpdateLayoutFlags();
                break;
            case nameof(CardLayoutProvider.NameTileWidth):
                OnPropertyChanged(nameof(NameTileWidth));
                break;
            case nameof(CardLayoutProvider.NameTileHeight):
                OnPropertyChanged(nameof(NameTileHeight));
                break;
        }
    }

    private double MeasureTitleFontSize()
    {
        if (_owner.IsDetailedListMode || _owner.IsNamesViewMode)
            return ScanningTitleFontSize;
        if (IsMicroLayout)
            return MicroTitleFontSize;
        if (IsCompactLayout)
            return CompactTitleFontSize;

        return NormalTitleFontSize;
    }

    private bool ShouldShowDetailText(bool hideWhenCardsShrink)
    {
        if (_owner.IsNamesViewMode)
            return false;
        if (_owner.IsDetailedListMode)
            return true;

        return !hideWhenCardsShrink;
    }

    private void UpdateLayoutFlags()
    {
        LayoutDensity = MeasureLayoutDensity(CardWidth, CardHeight);
    }

    /// <summary>
    /// Anti-flicker: evaluates density at both current and offset (+5px) dimensions,
    /// returning the least-dense result via LeastDense. The offset pushes toward the
    /// less-dense threshold, creating a narrow (~1px effective) hysteresis band.
    /// </summary>
    private static LauncherEntryLayoutDensity MeasureLayoutDensity(double width, double height)
    {
        return LeastDense(
            MeasureDensityAtSize(width, height),
            MeasureDensityAtSize(width + 5, height + 5));
    }

    private static LauncherEntryLayoutDensity MeasureDensityAtSize(double width, double height)
    {
        if (height <= MicroLayoutMaxHeight || width <= MicroLayoutMaxWidth)
            return LauncherEntryLayoutDensity.Micro;

        if (height <= CompactLayoutMaxHeight || width <= CompactLayoutMaxWidth)
            return LauncherEntryLayoutDensity.Compact;

        return LauncherEntryLayoutDensity.Normal;
    }

    private static LauncherEntryLayoutDensity LeastDense(LauncherEntryLayoutDensity a, LauncherEntryLayoutDensity b)
        => (int)a < (int)b ? a : b;
}

public enum LauncherEntryLayoutDensity
{
    Normal,
    Compact,
    Micro,
}
