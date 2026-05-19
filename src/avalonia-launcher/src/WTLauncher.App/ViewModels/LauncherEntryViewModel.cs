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

    public string UpdatedLabel => string.IsNullOrWhiteSpace(Entry.UpdatedAtUtc)
        ? "未记录更新时间"
        : $"更新 {Entry.UpdatedAtUtc}";

    public bool IsDangerous => LaunchArgumentBuilder.IsDangerousCommand(Entry.Command);

    public bool RequiresConfirmation => Entry.Confirm || IsDangerous;

    public bool IsManualConfirmation => Entry.Confirm && !IsDangerous;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private double _cardWidth = 300;

    [ObservableProperty]
    private double _cardHeight = 164;

    [ObservableProperty]
    private bool _isCompactLayout;

    [ObservableProperty]
    private bool _isMicroLayout;

    [ObservableProperty]
    private bool _isDragPreviewTarget;

    [ObservableProperty]
    private bool _isDragPreviewSource;

    [ObservableProperty]
    private bool _isDragInsertBefore;

    [ObservableProperty]
    private bool _isDragInsertAfter;

    public bool ShowDirectory => _owner.IsDetailedListMode || !IsMicroLayout;

    public bool ShowCommand => _owner.IsDetailedListMode || !IsCompactLayout;

    public bool ShowUpdatedLabel => _owner.IsDetailedListMode || !IsMicroLayout;

    public double TitleFontSize => _owner.IsDetailedListMode ? 17 : IsMicroLayout ? 15 : IsCompactLayout ? 16 : 18;

    partial void OnIsSelectedChanged(bool value)
    {
        _owner.UpdateSelection(this, value);
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
        var micro = CardHeight <= 96 || CardWidth <= 216;
        var compact = micro || CardHeight <= 124 || CardWidth <= 248;

        var changed = false;
        if (micro != IsMicroLayout)
        {
            IsMicroLayout = micro;
            changed = true;
        }

        if (compact != IsCompactLayout)
        {
            IsCompactLayout = compact;
            changed = true;
        }

        if (!changed)
            return;

        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(ShowUpdatedLabel));
        OnPropertyChanged(nameof(TitleFontSize));
    }

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Directory));
        OnPropertyChanged(nameof(Command));
        OnPropertyChanged(nameof(IsDangerous));
        OnPropertyChanged(nameof(RequiresConfirmation));
        OnPropertyChanged(nameof(IsManualConfirmation));
        OnPropertyChanged(nameof(UpdatedLabel));
        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(ShowUpdatedLabel));
        OnPropertyChanged(nameof(TitleFontSize));
    }

    public void RefreshLayoutPresentation()
    {
        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(ShowUpdatedLabel));
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
