# Test Quality Standards

> What to test, coverage expectations, and quality gates.

---

## Testing Philosophy

**Test the contracts, not the implementation.** A test should break when behavior changes, not when you refactor.

Two kinds of tests in this project:
1. **Logic tests:** Pure functions with clear input → output (Core layer).
2. **Behavior tests:** ViewModel commands produce correct side effects + status text.

---

## What Must Be Tested

| Scenario | Required? |
|----------|-----------|
| New Core pure function | ✅ Must have tests |
| New persistence path | ✅ Must test success + rollback |
| New ViewModel command | ✅ Must test happy + cancel + error paths |
| New dialog window | ✅ Must test return values (accept, cancel, validation) |
| New drag-drop behavior | ✅ Must test decision logic + ghost layout |
| XAML-only styling change | ❌ Visual smoke only |
| Ctrl+F / keyboard shortcut | ❌ Manual verification unless logic is complex |

---

## What Tests Must Cover

### Persistence Tests

Every new persistence path must test:
1. **Normal save:** data round-trips correctly through `LauncherStore.Save` → `LauncherStore.Load`.
2. **Save failure:** `LauncherStore.Save` throws → caller's snapshot restore returns VM state to pre-mutation values (entries + settings + presets + custom order + selection).

Single-file atomic save means there is no partial-failure path — either all of `launcher.json` commits via `File.Replace` or none of it does. The rollback contract lives at the VM layer (`LauncherEntryMutationSnapshot`), not the store layer.

**Reference:** `LauncherEntryMutationSnapshotTests.cs`, `LauncherSelectionMutationSnapshotTests.cs`, `LauncherStoreLoadFailureTests.cs`

### ViewModel Command Tests

Every command (Add/Edit/Delete/Launch/Reorder) must test:
1. **Success:** command produces correct status text + state change.
2. **Cancel:** user cancels dialog → status text reflects cancellation.
3. **Dialog error:** dialog throws → status text reflects failure.
4. **Validation failure:** invalid input → status text reflects validation error.

**Reference:** `MainWindowViewModelTests.EntryTests.cs`, `MainWindowViewModelTests.DialogBoundaryTests.cs`

---

## Line Ending Contract

All persisted JSON uses LF (`\n`). `LauncherJson.NormalizeLineEndings` enforces it at write time inside `Save`, so every byte stream written through `LauncherStore.Save` is LF-normalized regardless of input. The repo-level `.gitattributes` LF policy is enforced by the `line-ending-policy` check in `tools/verify.ps1` (that check inspects `.gitattributes`, not `launcher.json` bytes). No per-test fixture method is needed.

---

## Quality Gates

### Local Gate (before commit)

```powershell
dotnet test -c Release
```

### Full Gate (before claiming "done")

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

This runs: restore → test → publish → GUI smoke (Windows only).

For visual-token changes, the gate must additionally prove:

- `WTLauncher.App/**/*.cs|*.axaml` has zero raw hex colors (`app-raw-hex-zero`).
- A temporary `#RRGGBB` fixture is rejected by the same scanner.
- `VisualTokenContractTests` pins Core token ownership, App resource injection, production consumers, and removal of repeated XAML `12/14/16` font literals.

For main-window action-hierarchy changes, a structural contract must pin which actions remain directly visible, which existing commands moved into labeled menus, the automation names of menu triggers, and the card-level “launch + more” boundary. Published GUI smoke remains required to catch XAML parsing or resource failures.

For dialog-consistency changes, `DialogConsistencyContractTests` must pin:

- All three dialog tokens (`DialogTitleFontSize`, `DialogOuterPadding`, `DialogFooterTopMargin`) exist in Core and are injected in `App.axaml.cs`.
- `LauncherDialogText.Title` consumes `LauncherDesignTokens.DialogTitleFontSize`; `CommandManagementWindow.axaml` consumes `{StaticResource DialogTitleFontSize}`, `{StaticResource DialogOuterPadding}`, and `{StaticResource DialogFooterMargin}`.
- All 10 programmatic dialogs use `LauncherDesignTokens.DialogOuterPadding` for outer padding and `LauncherDesignTokens.DialogFooterTopMargin` for footer top margin — no hardcoded literals (12/14/16/18/24).
- `ConfirmLaunchWindow` uses `Footer(0, ...)` because its parent Grid `RowSpacing=14` provides the spacing.
- Destructive dialogs (`ConfirmEntryDeleteWindow`, `ConfirmPresetDeleteWindow`) have `Cancel(isDefault: true)` and initial focus on Cancel; Delete is `Destructive(isDefault: false)` and not focused.

---

## Test Count Expectations

| When | Expectation |
|------|-------------|
| Adding a Core helper | +1 test file minimum |
| Adding a ViewModel command | +1 test scenario in existing file (or new area file) |
| Adding a persistable data type | +1 persistence test (round-trip + failure) |
| Bug fix | +1 regression test that fails before the fix |

---

## Forbidden Patterns

- ❌ **Tests without assertions** — every test must have at least one `Assert`.
- ❌ **Passing tests that don't test what the name says.**
- ❌ **`Thread.Sleep()` or `Task.Delay()`** — use fixed clock injection.
- ❌ **Tests that depend on test execution order** — each test creates its own temp directory.
- ❌ **Skipping tests with `[Fact(Skip = "...")]`** — fix or delete, don't accumulate skipped tests.
