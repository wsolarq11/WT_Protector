# Design: Settings state single-source-of-truth refactor

## Scope decision (audit-hypothesis-verification)

The 2026-07-09 deepsearch + audit identified two related issues:

1. **SaveStoreFile duplication** (R1, real bug surface): `SaveSettingsToStore(settings)` hand-assembles `LauncherStoreFile` instead of routing through `BuildStoreFile()`. It also does NOT mutate `_settings` — caller must manually sync via `_settings = _dragReorderWorkflow.Settings`.

2. **`_settings` triple truth** (R2, hypothetical): three fields hold `LauncherSettings` (main VM + drag workflow + entry snapshot). May be collapsible to collaborator-boundary accessors.

### Per-source verdict (audit-driven-proposals-verify-each-source)

| Source | Verdict | Reasoning |
|--------|---------|-----------|
| R1 SaveStoreFile duplication | **FIX** | Real dual-path bug. Fix is 1-line: mutate `_settings = settings; return SaveStoreFile(BuildStoreFile());`. Eliminates hand-assembly. |
| R2 triple truth mirror collapse | **NO-ACTION (with reason)** | Workflow's `_settings` field is **working state for rollback semantics** — `Commit()` rolls back via `session.Snapshot.CustomEntryOrder` on save failure (workflow.cs:144,153,176). Main VM needs the rolled-back value synced via `_settings = _dragReorderWorkflow.Settings` (Drag.cs:30,38,120,135,147). 5 sync sites are the necessary cost of collaborator-boundary design. Collapsing would require either: (a) workflow not holding working state → breaks rollback; or (b) workflow holding snapshot only and main VM holding live state → but Preview mutates workflow `_settings` mid-flight (workflow.cs:106) before commit, main VM needs that preview visible in filter/sort. Collapsing would change observable behavior. **Defer.** |

This matches `feedback_godclass_composition_not_partials.md` lesson: partials (and collaborator workflows) are valid intermediate; composition is end-state but not at any cost.

## Design: R1 fix

### Current (bug)

```csharp
// MainWindowViewModel.cs:248-258
private string? SaveSettingsToStore(LauncherSettings settings)
{
    var storeFile = new LauncherStoreFile
    {
        Entries = _entries.Select(ToLaunchEntry).ToImmutableList(),
        Settings = settings,  // external param, not _settings field
        Presets = _presetFile.Presets,
        DefaultPresetId = _presetFile.DefaultPresetId,
    };
    return SaveStoreFile(storeFile);
}
```

### Fixed

```csharp
// ponytail: mutate _settings first, then route through BuildStoreFile —
// eliminates the hand-assembly dual path. The 5 `_settings = _dragReorderWorkflow.Settings`
// sites in Drag.cs remain (workflow still owns working/rollback state) but become
// no-ops in the commit-success case (assigning the same value).
private string? SaveSettingsToStore(LauncherSettings settings)
{
    _settings = settings;
    return SaveStoreFile(BuildStoreFile());
}
```

### Why not also remove the 5 sync sites?

Tried mentally — they handle non-commit paths:
- `BeginDragReorder` (Drag.cs:30): workflow mutates via `EnsureCustomOrderContainsEntries` without persisting
- `PreviewMoveEntry` (Drag.cs:38): workflow mutates `_settings` for preview only (not persisted)
- `CommitDragReorder` (Drag.cs:120): workflow may have rolled back on save failure
- `CancelDragReorder` (Drag.cs:135): workflow rolls back to snapshot
- `DiscardDragReorderPreview` (Drag.cs:147): workflow rolls back

All five carry non-persisted state or rollback state — they cannot be replaced by SaveSettingsToStore mutation. Keeping them is correct and documented.

## Design: R2 no-action documentation

Add a `ponytail:` comment near `LauncherDragReorderWorkflow._settings` (line 8) explaining why the mirror exists:

```csharp
// ponytail: this field is working state for drag reorder, NOT a duplicate of
// MainWindowViewModel._settings. Workflow mutates during Preview (before persist),
// rolls back via session.Snapshot on Cancel/commit-failure. Main VM syncs back
// via _settings = _dragReorderWorkflow.Settings (5 sites in Drag.cs) — this is
// the cost of collaborator-boundary design, NOT a bug. Verified by 7 equivalence
// tests in MainWindowViewModelTests.DragTests.cs.
private LauncherSettings _settings;
```

Similarly for `LauncherEntryMutationSnapshot._settings`:

```csharp
// ponytail: snapshot field — immutable capture of settings at mutation start,
// used to restore on rollback. NOT a parallel live truth.
private readonly LauncherSettings _settings;
```

## Design: R3 pin test

Add to `tests/WTLauncher.Tests/LauncherStoreLoadFailureTests.cs` (or a new file `MainWindowViewModelSettingsSingleSourceTests.cs`):

```csharp
[Fact]
public void SaveSettingsToStore_RoutesThroughBuildStoreFile_NotHandAssembly()
{
    // Verifies that after SaveSettingsToStore, the persisted file matches what
    // BuildStoreFile would produce with the same _settings — i.e. no drift.
    // Use reflection or InternalsVisibleTo to invoke private SaveSettingsToStore
    // via the drag-commit callback path, OR test indirectly through CommitDragReorder.
}
```

The test should verify:
1. After drag commit, the file on disk has `settings.CustomEntryOrder` matching the post-drag order.
2. `_settings` field equals the parameter passed to SaveSettingsToStore (was synced during save).
3. No LauncherStoreFile hand-assembly code path remains (could be a source-level grep assertion in verify.ps1).

## Risks

| Risk | Mitigation |
|------|-----------|
| Changing SaveSettingsToStore mutation order breaks drag-commit semantics | 7 existing drag equivalence tests cover this; add pin test for BuildStoreFile routing |
| Removing dual-path breaks tests that asserted hand-assembly | None exist — `MainWindowViewModelTests` test observable behavior, not internal assembly |
| Workflow `_settings` mirror collapse temptation | Documented as deferred — ponytail: comment makes the design intent explicit |

## Rollback

Single-commit revert if any test fails. No schema migration. No persisted-state change.
