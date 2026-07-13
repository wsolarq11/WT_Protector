# MainWindowViewModel ownership audit

Date: 2026-07-13

## Extracted owners

| Workflow | Owner | ViewModel responsibility retained |
| --- | --- | --- |
| Entry dialogs, validation, draft creation and clone IDs | `IEntryService` / `EntryService` | apply returned entry to observable state, CQ-02 transaction, notifications |
| Preset dialogs, CRUD, eligibility, reference count and selection preview | `IPresetService` / `PresetService` | selected-preset binding, apply returned immutable file, transaction, status projection |
| Filter and sort rules | `LauncherEntryQuery` in Core | invoke rule, incrementally sync `ObservableCollection`, update counts/groups |
| Search-history normalize/dedupe/cap | `LauncherSearchHistory` in Core | persist returned settings value and raise binding notification |
| Single/batch plan, preflight and execution | injected `LauncherLaunchWorkflow` | busy/cancellation state, dialog routing, receipt/status mapping, launch-count persistence |
| Selection snapshot/restore | `LauncherSelectionMutationSnapshot` | mutate entry bindable flags, transaction, command notifications |
| Preset selection comparison | `LauncherPresetSelectionPreview` via `PresetService` | choose current preset and expose binding text |
| Drag reorder state machine | `LauncherDragReorderWorkflow` | pointer-facing orchestration and observable presentation |

## Retained direct calls and reasons

- `LauncherEntryQuery.FilterAndSort` remains a one-line Core invocation in `ApplyFilter`; a `LayoutService` wrapper would add no contract. `SyncVisibleEntries` stays because it is specifically an Avalonia `ObservableCollection` container-preservation algorithm.
- `LauncherLaunchWorkflow` calls stay in single/batch commands because it is already the injected workflow service. Wrapping it in `LaunchService` would duplicate the same API before CQ-06 defines typed results.
- `SetSelection` and `ReplaceSelection` stay in the ViewModel because their work is changing bindable `LauncherEntryViewModel.IsSelected`, capturing/restoring UI snapshots and notifying generated commands. The independent comparison rule is already owned by `LauncherPresetSelectionPreview`.
- `LauncherDragReorderWorkflow` is constructed after settings normalization because it requires the canonical observable entry collection and save callback. It is an internal state machine, not a hidden alternate aggregate or disk owner.

## Forbidden empty extractions

- No service that only forwards `LauncherEntryQuery.FilterAndSort`.
- No service accepting `MainWindowViewModel` or a callback bag.
- No second mutable entries/settings/presets aggregate.
- No mediator, event bus, repository or new dependency.

## Executable evidence

- `ArchitectureRules` R7–R10 prevent Entry/Preset feature services from referencing Views or `System.IO.File`.
- `PresetServiceTests` cover dialog normalization/failure, immutable CRUD, eligibility, reference count and selection-preview contracts.
- `LauncherSearchHistoryTests` cover normalize, dedupe, no-op identity and maximum size.
- Existing ViewModel tests cover complete state rollback, selection notifications, filtering, single/batch launch safety and status/receipt behavior.
