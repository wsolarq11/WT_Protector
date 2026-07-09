# Settings state single-source-of-truth refactor

## Goal

Eliminate the dual/triple source-of-truth for `LauncherSettings` across `MainWindowViewModel` and its workflow/snapshot collaborators, and route every save through `BuildStoreFile()` so there is exactly one canonical assembler.

## Background (from 2026-07-09 second-pass audit #4 + deepsearch)

`MainWindowViewModel` and its partials currently maintain **three** `_settings` mirrors:

| Field | Owner | Purpose |
|-------|-------|---------|
| `_settings` | `MainWindowViewModel` (main) | Authoritative settings state |
| `_settings` | `LauncherDragReorderWorkflow` | Working state during drag reorder; exposed via `.Settings` and synced back via `_settings = _dragReorderWorkflow.Settings` at 5 call sites in `MainWindowViewModel.Drag.cs` |
| `_settings` | `LauncherEntryMutationSnapshot` | Snapshot/rollback state for entry add/clone/edit/delete |

And **three** save paths:

| Path | Used by | Routes through `BuildStoreFile`? |
|------|---------|--------------------------------|
| `SaveConfig() → BuildStoreFile() → SaveStoreFile()` | Most mutations | yes |
| `SaveSettingsChange(snapshot) → BuildStoreFile() → SaveStoreFile()` | Layout/entries mutations with rollback | yes |
| **`SaveSettingsToStore(settings)`** | `LauncherDragReorderWorkflow` drag-commit callback | NO — hand-assembles 4 fields; does NOT update `_settings` field |

The third path is the audit's identified bug surface: it accepts an external `settings` parameter, persists it, but never updates the main `_settings` field. Caller syncs back via the 5 `_settings = _dragReorderWorkflow.Settings` sites — manual coordination that exists only because the canonical save path wasn't used.

## Requirements

### R1 (must) — Eliminate SaveStoreFile duplication
- `SaveSettingsToStore(settings)` must route through `BuildStoreFile()` after mutating `_settings`.
- No hand-assembly of `LauncherStoreFile` anywhere except `BuildStoreFile()`.

### R2 (must) — Single _settings field at any time
- After R1, evaluate whether `LauncherDragReorderWorkflow._settings` and `LauncherEntryMutationSnapshot._settings` can be replaced with collaborator-boundary accessors (`Func<LauncherSettings>` getter + `Action<LauncherSettings>` commit) instead of mirrored fields.
- **Decision criterion**: only collapse a mirror if the sync points (e.g. the 5 `_settings = _dragReorderWorkflow.Settings` sites) can be removed without changing observable behavior. If collapsing requires touching >5 unrelated call sites, defer to a follow-up task.

### R3 (must) — No regression
- All existing tests pass (453 baseline + 2 added 2026-07-09 = 455 expected).
- Add at least one pin test: `SaveSettingsToStore_UsesBuildStoreFile_NotHandAssembly`.
- `tools/verify.ps1` 17/17 PASS.

### R4 (must) — No scope creep
- Do NOT touch unrelated `_settings with { ... }` mutation sites (24 across partials).
- Do NOT refactor entry/preset workflow state machines.
- Do NOT introduce DI container or `Microsoft.Extensions.DependencyInjection` (second-pass audit #5 explicitly deferred).

## Acceptance Criteria

- [ ] `SaveSettingsToStore` body calls `BuildStoreFile()` (or is removed entirely if R2 collapses the path)
- [ ] No hand-assembly of `LauncherStoreFile` outside `BuildStoreFile()`
- [ ] `_settings` field count across `MainWindowViewModel` + `LauncherDragReorderWorkflow` + `LauncherEntryMutationSnapshot` is reduced OR each retained field has a documented reason (e.g. workflow rollback semantics)
- [ ] 455/455 tests passing
- [ ] `tools/verify.ps1` 17/17 PASS
- [ ] No ponytail-violating speculative abstractions introduced

## Out of scope (deferred to future tasks)

- DI container introduction (second-pass audit #5)
- CompiledBindings enablement (second-pass audit #3)
- `LauncherEntryVisibility → LauncherEntryQuery` rename (115 refs, misc-cleanup)
- Sync File I/O async-ification (second-pass audit #6, no latency issue at current scale)
