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

    private LauncherEntryLayoutDensity _layoutDensity = LauncherEntryLayoutDensity.Normal;

    public bool IsCompactLayout => _layoutDensity is LauncherEntryLayoutDensity.Compact or LauncherEntryLayoutDensity.Micro;

    public bool IsMicroLayout => _layoutDensity is LauncherEntryLayoutDensity.Micro;

    public bool ShowDirectory => ShouldShowDetailText(hideWhenCardsShrink: IsMicroLayout);

    public bool ShowCommand => ShouldShowDetailText(hideWhenCardsShrink: IsCompactLayout);

    public double TitleFontSize => MeasureTitleFontSize();

    partial void OnCardWidthChanged(double value) => UpdateLayoutFlags();

    partial void OnCardHeightChanged(double value) => UpdateLayoutFlags();

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
        var density = MeasureLayoutDensity(CardWidth, CardHeight);

        if (density == _layoutDensity)
            return;

        _layoutDensity = density;
        OnPropertyChanged(nameof(IsCompactLayout));
        OnPropertyChanged(nameof(IsMicroLayout));
        RefreshLayoutPresentation();
    }

    private static LauncherEntryLayoutDensity MeasureLayoutDensity(double width, double height)
    {
        if (height <= MicroLayoutMaxHeight || width <= MicroLayoutMaxWidth)
            return LauncherEntryLayoutDensity.Micro;

        if (height <= CompactLayoutMaxHeight || width <= CompactLayoutMaxWidth)
            return LauncherEntryLayoutDensity.Compact;

        return LauncherEntryLayoutDensity.Normal;
    }
}

internal enum LauncherEntryLayoutDensity
{
    Normal,
    Compact,
    Micro,
}
