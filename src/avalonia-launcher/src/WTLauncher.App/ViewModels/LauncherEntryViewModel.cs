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

    internal void SetSelectionFromOwner(bool selected)
    {
        Entry.Selected = selected;
        SetProperty(ref _isSelected, selected, nameof(IsSelected));
    }

    internal bool ReplaceEntrySelectionFromOwner(bool selected)
    {
        var previousSelected = Entry.Selected;
        Entry.Selected = selected;
        return previousSelected;
    }

    internal void ReplaceEntryFromOwner(LaunchEntry updated, string updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(updated);
        Entry.Id = updated.Id;
        Entry.Name = updated.Name;
        Entry.Directory = updated.Directory;
        Entry.Command = updated.Command;
        Entry.UpdatedAtUtc = updatedAtUtc;
        SetSelectionFromOwner(updated.Selected);
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Directory));
        OnPropertyChanged(nameof(Command));
        RefreshLayoutPresentation();
    }

    public void RefreshLayoutPresentation()
    {
        OnPropertyChanged(nameof(ShowDirectory));
        OnPropertyChanged(nameof(ShowCommand));
        OnPropertyChanged(nameof(TitleFontSize));
    }

    [RelayCommand]
    private void DryRun() => _owner.ShowDryRun(this);

    [RelayCommand]
    private Task LaunchAsync() => _owner.LaunchAsync(this);

    [RelayCommand]
    private Task EditAsync() => _owner.EditEntryAsync(this);

    [RelayCommand]
    private Task DeleteAsync() => _owner.DeleteEntryAsync(this);
}
