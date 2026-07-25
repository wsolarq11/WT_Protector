using Stateless;

namespace WTLauncher.App.ViewModels;

/// <summary>Top-level UI states for the launcher window.</summary>
public enum AppUIState
{
    Normal,
    Filtering,
    Dragging,
    DialogOpen,
    Saving
}

/// <summary>Triggers that drive UI state transitions.</summary>
public enum AppUITrigger
{
    StartFilter,
    EndFilter,
    BeginDrag,
    EndDrag,
    OpenDialog,
    CloseDialog,
    BeginSave,
    EndSave,
    Reload
}

/// <summary>
/// Zone 1: Formally validated UI state machine.
/// Every legal state transition is declared; any trigger fired in the wrong
/// state throws <see cref="InvalidOperationException"/> at runtime.
///
/// Reference: <see cref="LauncherDragReorderWorkflow"/> for the same pattern
/// applied to drag reorder.
/// </summary>
public sealed class AppStateMachine
{
    private readonly StateMachine<AppUIState, AppUITrigger> _machine;

    public AppUIState CurrentState => _machine.State;

    public AppStateMachine()
    {
        _machine = new StateMachine<AppUIState, AppUITrigger>(AppUIState.Normal);

        // ── Normal: the resting state ──
        _machine.Configure(AppUIState.Normal)
            .Permit(AppUITrigger.StartFilter, AppUIState.Filtering)
            .Permit(AppUITrigger.BeginDrag, AppUIState.Dragging)
            .Permit(AppUITrigger.OpenDialog, AppUIState.DialogOpen)
            .Permit(AppUITrigger.BeginSave, AppUIState.Saving)
            .PermitReentry(AppUITrigger.Reload);

        // ── Filtering: search box has focus/text ──
        _machine.Configure(AppUIState.Filtering)
            .Permit(AppUITrigger.EndFilter, AppUIState.Normal)
            .Permit(AppUITrigger.BeginDrag, AppUIState.Dragging)
            .Permit(AppUITrigger.OpenDialog, AppUIState.DialogOpen)
            .Permit(AppUITrigger.BeginSave, AppUIState.Saving);

        // ── Dragging: drag-reorder in progress ──
        // ponytail: commit/cancel are handled by LauncherDragReorderWorkflow.
        // The AppStateMachine only tracks the high-level "dragging is active" guard.
        _machine.Configure(AppUIState.Dragging)
            .Permit(AppUITrigger.EndDrag, AppUIState.Normal)
            .Permit(AppUITrigger.OpenDialog, AppUIState.DialogOpen)
            .Permit(AppUITrigger.BeginSave, AppUIState.Saving);

        // ── DialogOpen: modal dialog active ──
        _machine.Configure(AppUIState.DialogOpen)
            .Permit(AppUITrigger.CloseDialog, AppUIState.Normal)
            .Permit(AppUITrigger.BeginSave, AppUIState.Saving);

        // ── Saving: persist operation in flight ──
        _machine.Configure(AppUIState.Saving)
            .Permit(AppUITrigger.EndSave, AppUIState.Normal)
            .Permit(AppUITrigger.EndSave, AppUIState.Filtering)
            .Permit(AppUITrigger.EndSave, AppUIState.DialogOpen);
    }

    /// <summary>Check if a trigger can be fired without throwing.</summary>
    public bool CanFire(AppUITrigger trigger) => _machine.CanFire(trigger);

    /// <summary>Fire a trigger. Throws on invalid transition.</summary>
    public void Fire(AppUITrigger trigger) => _machine.Fire(trigger);

    /// <summary>Fire a trigger if the transition is valid. Returns true if fired.</summary>
    public bool FireIfPossible(AppUITrigger trigger)
    {
        if (!_machine.CanFire(trigger)) return false;
        _machine.Fire(trigger);
        return true;
    }

    /// <summary>True when the UI is in a state that blocks user interaction.</summary>
    public bool IsBlockingInteraction =>
        _machine.State is AppUIState.DialogOpen or AppUIState.Saving;
}
