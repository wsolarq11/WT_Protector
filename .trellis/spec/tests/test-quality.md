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

**Source:** `LauncherPersistenceTestFixtures.cs`

```csharp
private static void AssertUsesRepositoryLineEndings(string text)
{
    Assert.EndsWith("\n", text, StringComparison.Ordinal);
    Assert.DoesNotContain("\r", text);
}
```

Every test that writes a JSON file asserts LF line endings.

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
