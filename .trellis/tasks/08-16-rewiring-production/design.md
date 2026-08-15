# Design: Rewiring Production Implementation

> Authoritative design source: `docs/wiring-rewiring-production-blueprint.md`. This file records the task-specific boundary and non-negotiable invariants.

## Boundaries

- MainWindowViewModel keeps bindable state, command surface, projection ownership, status and collection owners.
- Command bodies move into six workflows: Entry, Preset, Layout, Selection, Maintenance, Exchange.
- Workflows receive `ICommandExecutionContext` only; no `MainWindowViewModel` dependency and no callback bags.

## Core Contracts

```csharp
public interface IStatusSink
{
    void Apply(LauncherStatusContent status);
    void SetDryRunText(string text);
}

public interface ICommandExecutionContext : IProjectionSynchronizer
{
    StoreGrain Store { get; }
    IStatusSink Status { get; }
    string GetTimestampUtc();
}
```

`ILaunchCoordinatorContext : ICommandExecutionContext` keeps launch-specific members and drops duplicated timestamp/status members.

## Data Flow

Views -> MainWindowViewModel command surface -> Workflow -> ICommandExecutionContext -> StoreMutationSession/StoreGrain -> projection sync.

## Non-negotiable

- No DI container / service locator / global EventBus.
- No thin wrapper delegation (`no-thin-wrapper-delegation` gate).
- Save-first-then-commit stays in StoreGrain.
- Two selection write paths remain.
- External IO and launch side effects stay out of Views and out of MainWindowViewModel after Phase D.

## Target Files

- ViewModels/Commands/: six workflow classes.
- ViewModels/IStatusSink.cs, ViewModels/ICommandExecutionContext.cs.
- docs/wiring-map.md: port ledger.
- tools/verify.ps1: three new checks.
