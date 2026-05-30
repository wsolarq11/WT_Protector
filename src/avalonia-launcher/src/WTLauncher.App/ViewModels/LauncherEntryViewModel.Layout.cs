namespace WTLauncher.App.ViewModels;

public sealed partial class LauncherEntryViewModel
{
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
            return 13;
        if (IsMicroLayout)
            return 15;
        if (IsCompactLayout)
            return 16;

        return 18;
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
        if (height <= 96 || width <= 216)
            return LauncherEntryLayoutDensity.Micro;

        if (height <= 124 || width <= 248)
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
