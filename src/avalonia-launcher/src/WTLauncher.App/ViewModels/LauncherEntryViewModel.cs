using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class LauncherEntryViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;

    public LauncherEntryViewModel(LaunchEntry entry, MainWindowViewModel owner)
    {
        Entry = entry;
        _owner = owner;
        _isSelected = entry.Selected;
    }

    public LaunchEntry Entry { get; }

    public string Id => Entry.Id;

    public string Name => Entry.Name;

    public string Directory => Entry.Directory;

    public string Command => Entry.Command;

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                _owner.UpdateSelection(this, value);
        }
    }

    [ObservableProperty]
    private double _cardWidth = 300;

    [ObservableProperty]
    private double _cardHeight = 164;

    [ObservableProperty]
    private double _nameTileWidth = 180;

    [ObservableProperty]
    private double _nameTileHeight = 24;

    private LauncherEntryLayoutDensity _layoutDensity = LauncherEntryLayoutDensity.Normal;

    [ObservableProperty]
    private bool _isDragPreviewTarget;

    [ObservableProperty]
    private bool _isDragPreviewSource;

    [ObservableProperty]
    private bool _isDragInsertBefore;

    [ObservableProperty]
    private bool _isDragInsertAfter;

    public bool IsCompactLayout => _layoutDensity is LauncherEntryLayoutDensity.Compact or LauncherEntryLayoutDensity.Micro;

    public bool IsMicroLayout => _layoutDensity is LauncherEntryLayoutDensity.Micro;

    public bool ShowDirectory => !_owner.IsNamesViewMode && (_owner.IsDetailedListMode || !IsMicroLayout);

    public bool ShowCommand => !_owner.IsNamesViewMode && (_owner.IsDetailedListMode || !IsCompactLayout);

    public double TitleFontSize => _owner.IsDetailedListMode || _owner.IsNamesViewMode ? 13 : IsMicroLayout ? 15 : IsCompactLayout ? 16 : 18;

    internal void SetSelectionFromOwner(bool selected)
    {
        Entry.Selected = selected;
        SetProperty(ref _isSelected, selected, nameof(IsSelected));
    }

    partial void OnCardWidthChanged(double value)
    {
        UpdateLayoutFlags();
    }

    partial void OnCardHeightChanged(double value)
    {
        UpdateLayoutFlags();
    }

    private void UpdateLayoutFlags()
    {
        var density = MeasureLayoutDensity(CardWidth, CardHeight);

        if (density == _layoutDensity)
            return;

        _layoutDensity = density;
        OnPropertyChanged(nameof(IsCompactLayout));
        OnPropertyChanged(nameof(IsMicroLayout));
        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(TitleFontSize));
    }

    private static LauncherEntryLayoutDensity MeasureLayoutDensity(double width, double height)
    {
        if (height <= 96 || width <= 216)
            return LauncherEntryLayoutDensity.Micro;

        if (height <= 124 || width <= 248)
            return LauncherEntryLayoutDensity.Compact;

        return LauncherEntryLayoutDensity.Normal;
    }

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Directory));
        OnPropertyChanged(nameof(Command));
        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(TitleFontSize));
    }

    public void RefreshLayoutPresentation()
    {
        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(TitleFontSize));
    }

    [RelayCommand]
    private void DryRun()
    {
        _owner.ShowDryRun(this);
    }

    [RelayCommand]
    private Task LaunchAsync()
    {
        return _owner.LaunchAsync(this);
    }

    [RelayCommand]
    private Task EditAsync()
    {
        return _owner.EditEntryAsync(this);
    }

    [RelayCommand]
    private Task DeleteAsync()
    {
        return _owner.DeleteEntryAsync(this);
    }
}

internal enum LauncherEntryLayoutDensity
{
    Normal,
    Compact,
    Micro,
}
