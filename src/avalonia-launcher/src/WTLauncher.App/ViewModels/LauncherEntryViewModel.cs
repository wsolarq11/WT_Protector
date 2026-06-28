using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WTLauncher.App.Infrastructure;
using WTLauncher.Core;

namespace WTLauncher.App.ViewModels;

public sealed partial class LauncherEntryViewModel : ObservableObject
{
    private readonly MainWindowViewModel _owner;
    private LaunchEntry _entry;

    public LauncherEntryViewModel(LaunchEntry entry, MainWindowViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(owner);
        _entry = entry;
        _owner = owner;
        _isSelected = entry.Selected;
    }

    /// <remarks>Callers must ensure _isSelected and _entry.Selected are in sync before calling this method during selection mutations.</remarks>
    public LaunchEntry SnapshotEntry() => _entry with { };

    public string Id => _entry.Id;

    public string Name => _entry.Name;

    public string Directory => _entry.Directory;

    public string Command => _entry.Command;

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

    [ObservableProperty]
    private bool _isKeyboardFocused;

    internal void SetSelectionFromOwner(bool selected)
    {
        if (_entry.Selected != selected)
            _entry = _entry with { Selected = selected };
        SetProperty(ref _isSelected, selected, nameof(IsSelected));
    }

    /// <summary>
    /// <para><b>Fragile dual-update contract — read carefully before touching.</b></para>
    /// <para>
    /// This method is part of a two-phase selection mutation protocol between
    /// <see cref="MainWindowViewModel.UpdateSelection"/> and this ViewModel.
    /// The caller MUST have already updated <see cref="_isSelected"/> via
    /// <see cref="ObservableObject.SetProperty{T}"/> (which raises
    /// <c>PropertyChanged</c> for <see cref="IsSelected"/>) BEFORE calling
    /// this method. This method ONLY updates the backing <see cref="_entry"/>
    /// data (<c>LaunchEntry.Selected</c>) and returns the previous value so
    /// the caller can capture a restore snapshot.
    /// </para>
    /// <para>
    /// It deliberately does NOT touch <see cref="_isSelected"/> — doing so
    /// would re-trigger <see cref="IsSelected"/>'s setter, which would call
    /// <see cref="MainWindowViewModel.UpdateSelection"/> again, creating an
    /// infinite loop. This is why <see cref="SetSelectionFromOwner"/> exists
    /// as a separate method for the simpler "replace all" path that can safely
    /// update both fields at once.
    /// </para>
    /// </summary>
    /// <param name="selected">The new selection state (already applied to
    /// <see cref="_isSelected"/> by the caller).</param>
    /// <returns>The previous <c>_entry.Selected</c> value, used by the caller
    /// to construct a <see cref="LauncherSelectionMutationSnapshot"/>.</returns>
    internal bool ReplaceEntrySelectionFromOwner(bool selected)
    {
        var previousSelected = _entry.Selected;
        if (previousSelected != selected)
            _entry = _entry with { Selected = selected };
        return previousSelected;
    }

    /// <summary>
    /// Replaces backing entry data from the owner ViewModel (e.g. after edit).
    /// <see cref="_isSelected"/> is set directly (not via the property setter)
    /// to avoid re-triggering <see cref="MainWindowViewModel.UpdateSelection"/>,
    /// which would double-save during entry mutation rollback. The explicit
    /// <c>OnPropertyChanged</c> keeps Avalonia bindings in sync.
    /// </summary>
    internal void ReplaceEntryFromOwner(LaunchEntry updated, string updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(updated);
        _entry = updated with { UpdatedAtUtc = updatedAtUtc };
        _isSelected = updated.Selected;
        OnPropertyChanged(nameof(IsSelected));
        RefreshDisplay();
    }

    internal string RecordLaunchFromOwner(string launchedAtUtc)
    {
        if (launchedAtUtc.Length > 0 && !DateTimeOffset.TryParseExact(launchedAtUtc, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            throw new ArgumentException("Invalid ISO 8601 timestamp format (expected 'O' round-trip format).", nameof(launchedAtUtc));
        var previous = _entry.LaunchedAtUtc;
        _entry = _entry with { LaunchedAtUtc = launchedAtUtc };
        return previous;
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

    [RelayCommand]
    private Task CloneAsync() => _owner.CloneEntryAsync(this);

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        try { _owner.MoveEntryUp(this); }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex)) { ErrorLog.Write(ex, "MoveUp"); }
    }

    private bool CanMoveUp() => _owner.CanMoveEntryUp(this);

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        try { _owner.MoveEntryDown(this); }
        catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex)) { ErrorLog.Write(ex, "MoveDown"); }
    }

    private bool CanMoveDown() => _owner.CanMoveEntryDown(this);

    internal void RefreshReorderCommands()
    {
        MoveUpCommand.NotifyCanExecuteChanged();
        MoveDownCommand.NotifyCanExecuteChanged();
    }
}
