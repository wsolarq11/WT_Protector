# Research: Add/edit/delete launcher buttons (Feature 10)

- **Query**: Audit add/edit/delete launcher button mutations, clone/snapshot handling, delete confirmation, preset/settings coordination, rollback, dialog boundary
- **Scope**: internal
- **Date**: 2026-06-10

## Findings

### Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Entries.cs` | Add/Edit/Delete/Clone entry mutations with snapshot/rollback |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` | ViewModel core, settings coordination, status text contracts |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryViewModel.cs` | Single entry VM: SnapshotEntry(), ReplaceEntryFromOwner(), commands |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryMutationSnapshot.cs` | Deep-copy snapshot for entries + preset + order rollback |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryMutationPersistence.cs` | Config -> Preset -> Settings save chain with rollback |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryDrafts.cs` | Draft normalization, validation, create from draft |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryDeleteTarget.cs` | Immutable delete target: snapshot id + name |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryReferences.cs` | Preset/settings entry id reference tracking |
| `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.EntryTests.cs` | 11 tests: add/edit/delete rollback, cancellation, snapshot protection, persistence, drag interaction |
| `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.DialogBoundaryTests.cs` | Null rejection for all dialog handler assignments |
| `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryMutationSnapshotTests.cs` | 3 tests: capture deep-copies, clones are fresh, public accessors are read-only |
| `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.CollectionContractTests.cs` | Read-only projections, snapshot-not-live-row |

### Code Patterns

#### Mutation Flow (all entry ops share the same pattern)

```
1. DiscardDragReorderPreview()       -- safety: uncommitted drag state
2. RequestEntryDraftAsync(snapshot)   -- dialog boundary: external UI
3. CreateEntryFromDraftOrReportValidation() -- validate inputs
4. CaptureEntryMutationSnapshot()     -- before mutation: deep copy state
5. mutate in-memory state             -- add/edit/delete to collections
6. SaveEntryCollectionChanges()       -- config -> preset -> settings chain
   -> on failure: RestoreEntryMutationSnapshot() (full in-memory rollback)
   -> persistence layer also restores config/preset files on disk
7. ApplyStatusText()                  -- user-visible result
```

#### Snapshot / Rollback (MainWindowViewModel.Entries.cs:243-261)

- `CaptureEntryMutationSnapshot()`: deep-copies entries, preset file, custom order, selected preset id (line 243-251)
- `RestoreEntryMutationSnapshot()`: full replacement of entries + settings + preset state + refresh (line 253-261)
- Public accessors on snapshot are read-only; clone methods return fresh mutable copies

#### Delete Confirmation (MainWindowViewModel.Entries.cs:160-197)

- `DeleteEntryAsync`: captures snapshot BEFORE confirmation (line 164), so dialog mutations don't corrupt the delete target
- `LauncherEntryDeleteTarget`: immutable record constructed from snapshot (line 163), rejects blank/null entry id/name at construction
- If user cancels: snapshot is restored (line 167) - early snapshot means memory is already protected even if dialog mutates the live view
- If dialog throws: `DeleteConfirmationFailed` status (line 188), no mutation occurs
- Status text uses snapshot's `EntryName`, not live entry, for deletion status (line 176)

#### Dialog Boundary (MainWindowViewModel.Entries.cs:14-24)

- `RequestEntryDraftAsync` and `ConfirmDeleteEntryAsync` are `Func` properties with null rejection (ArgumentNullException)
- Default implementations return null/false (null-safe if not wired)
- All dialog calls are wrapped in try/catch, converting exceptions to status text
- Test in `DialogBoundaryTests.cs` verifies all 6 dialog handler properties reject null assignment

#### Edit Entry (MainWindowViewModel.Entries.cs:124-158)

- Dialog receives `SnapshotEntry()` copy, not live mutable entry (line 130)
- `CreateEntryFromDraftOrReportValidation` validates + uses originalId to detect ID changes
- On ID rename: `LauncherEntryReferences.RenameEntryId` rewrites preset + order references (line 152)
- `savePresets: true` -- edit can change id/name which affects preset references

#### Clone Entry (MainWindowViewModel.Entries.cs:65-111)

- Creates a new `LaunchEntry` with generated unique ID + "(副本)" suffix + same dir/command (lines 69-76)
- `GenerateUniqueCloneId`: appends "-copy" and increments "-copy-2", "-copy-3" etc. (line 113-122)
- Uses the same dialog flow as Add
- Re-validates and saves as a new entry

#### Persistence Chain (LauncherEntryMutationPersistence.cs)

- Step 1: Save config.json
- Step 2: If savePresets, save presets.json; on failure, restore config.json from snapshot
- Step 3: Save settings.json; on failure, restore config.json + presets.json from snapshot
- Each restore is wrapped in its own try/catch (IOException/UnauthorizedAccessException only)
- Returns combined failure message with restore errors appended

### Test Coverage

#### EntryTests.cs (11 tests)
1. **AddEntryRollsBackInMemoryStateWhenConfigSaveFails** -- corrupted config path, memory restored
2. **AddEntryRollsBackConfigFileWhenSettingsSaveFailsAfterConfigSave** -- settings save fails, config file rolled back
3. **EditEntryRollsBackConfigFileWhenPresetSaveFailsAfterConfigSave** -- preset save fails, config file rolled back + memory restored
4. **AddEntryCancellationLeavesFilesAndOrderUnchanged** -- null draft = cancelled, drag preview discarded
5. **EntryDialogFailuresLeaveFilesAndReferencesUnchanged** -- dialog throws for add/edit/delete, nothing mutated
6. **AddEntryDuringDragPreviewDoesNotCommitPreviewOrder** -- ghost drag order not saved
7. **EditEntryCancellationLeavesFilesAndReferencesUnchanged** -- null draft preserves all state
8. **EditEntryDialogReceivesSnapshotEntry** -- dialog can mutate snapshot without affecting live entry
9. **EditEntryUsesSnapshotEntryWhenDialogReturnsDraft** -- mutated snapshot discarded, draft used
10. **EntryCommandsPersistPortableConfigAndReferences** -- full add/edit/delete cycle with preset + settings references
11. **DeleteEntryCancellationLeavesFilesAndReferencesUnchanged** -- false from confirmation preserves all state
12. **DeleteEntryConfirmationCancellationIgnoresMutatedEntrySnapshot** -- external mutates entry during confirmation, cancel uses snapshot
13. **DeleteEntryUsesConfirmationSnapshotForReferencesAndStatus** -- external mutates entry, deletion uses original snapshot for status + references

#### Snapshots: 3 tests
1. Capture deep-copies mutable entry/preset/order state
2. Clone methods return fresh mutable copies
3. Public accessors do not expose mutable rollback storage (read-only / NotSupportedException)

#### Dialog Boundary: 1 test
1. All 6 handler properties reject null assignment (ArgumentNullException)

#### Collection Contract: 1 test
1. EntryViewModels expose snapshot copies, not live rows

### Gaps

- **No dedicated clone entry tests**: The clone flow (CloneEntryAsync) has zero direct tests. It shares the dialog/validation/save path with Add, but the unique ID generation, copy suffix, and source snapshot extraction are not independently verified.
- **No test for GenerateUniqueCloneId collision**: The ID uniqueness logic (lines 113-122) is not directly tested, especially the edge case where "-copy", "-copy-2", ..., "-copy-N" all exist.
- **No test for clone + drag interaction**: Unlike Add/Edit/Delete which all test DiscardDragReorderPreview(), clone has no drag-preview test.

### External References

- (none -- internal feature)

### Related Specs

- `.trellis/spec/core/` -- core data structures
- Feature audit doc at `docs/reference/feature-reality-north-star-audit.md` (Feature id: `add-edit-delete-launcher-buttons`, verdict: `narrow-north-star`)

### Caveats / Not Found

- Clone command exists in LauncherEntryViewModel (line 116) but as `CloneAsync()` -- it calls `_owner.CloneEntryAsync(this)`. It is wired into the UI but has no independent test coverage.
