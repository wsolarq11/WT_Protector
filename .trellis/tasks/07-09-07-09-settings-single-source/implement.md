# Implement: Settings state single-source-of-truth refactor

## Pre-flight

- [ ] Read this entire document
- [ ] Confirm `git status` clean
- [ ] Confirm `python ./.trellis/scripts/task.py current` shows this task

## Phase 1: Fix SaveStoreFile duplication (R1)

### Step 1.1: Update SaveSettingsToStore

File: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs`

Replace lines 248-258:

```csharp
// BEFORE
private string? SaveSettingsToStore(LauncherSettings settings)
{
    var storeFile = new LauncherStoreFile
    {
        Entries = _entries.Select(ToLaunchEntry).ToImmutableList(),
        Settings = settings,
        Presets = _presetFile.Presets,
        DefaultPresetId = _presetFile.DefaultPresetId,
    };
    return SaveStoreFile(storeFile);
}
```

With:

```csharp
// AFTER
// ponytail: mutate _settings first, then route through BuildStoreFile —
// eliminates the hand-assembly dual path that existed here pre-2026-07-09.
// The 5 `_settings = _dragReorderWorkflow.Settings` sites in Drag.cs remain
// (workflow still owns working/rollback state for non-commit paths) but become
// no-ops in the commit-success case.
private string? SaveSettingsToStore(LauncherSettings settings)
{
    _settings = settings;
    return SaveStoreFile(BuildStoreFile());
}
```

### Step 1.2: Build

```bash
cd src/avalonia-launcher
dotnet build WTLauncher.Avalonia.sln -c Release
```

Expected: Build succeeded, 0 errors.

### Step 1.3: Run tests

```bash
dotnet test tests/WTLauncher.Tests/WTLauncher.Tests.csproj -c Release --no-build
```

Expected: 453 passing (no regression). If failures, investigate before proceeding.

## Phase 2: Document why _settings mirror is NOT collapsed (R2)

### Step 2.1: Add ponytail comment to LauncherDragReorderWorkflow._settings

File: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherDragReorderWorkflow.cs`

Insert above line 8 (`private LauncherSettings _settings;`):

```csharp
// ponytail: this field is working state for drag reorder, NOT a duplicate of
// MainWindowViewModel._settings. Workflow mutates during Preview (before persist),
// rolls back via session.Snapshot on Cancel/commit-failure. Main VM syncs back
// via `_settings = _dragReorderWorkflow.Settings` (5 sites in Drag.cs) — this is
// the cost of collaborator-boundary design, NOT a bug. Verified by 7 equivalence
// tests in MainWindowViewModelTests.DragTests.cs.
private LauncherSettings _settings;
```

### Step 2.2: Add ponytail comment to LauncherEntryMutationSnapshot._settings

File: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryMutationSnapshot.cs`

Insert above line 10 (`private readonly LauncherSettings _settings;`):

```csharp
// ponytail: snapshot field — immutable capture of settings at mutation start,
// used to restore on rollback. NOT a parallel live truth.
private readonly LauncherSettings _settings;
```

### Step 2.3: Build + test (no behavior change expected)

```bash
dotnet build WTLauncher.Avalonia.sln -c Release
dotnet test tests/WTLauncher.Tests/WTLauncher.Tests.csproj -c Release --no-build
```

Expected: 453 passing.

## Phase 3: Pin test (R3)

### Step 3.1: Decide test file location

Option A: Add to `tests/WTLauncher.Tests/LauncherStoreLoadFailureTests.cs` (existing file, related concerns)
Option B: Create new `tests/WTLauncher.Tests/MainWindowViewModelSettingsSingleSourceTests.cs`

**Decision: Option A** — keep related tests together (ponytail: fewest files possible).

### Step 3.2: Add pin test

Append to `LauncherStoreLoadFailureTests.cs`:

```csharp
[Fact]
public void SaveSettingsToStore_RoutesThroughBuildStoreFile_NotHandAssembly()
{
    // Verifies the dual-path fix from 2026-07-09: SaveSettingsToStore mutates
    // _settings then routes through BuildStoreFile (canonical assembler).
    // Indirect test through CommitDragReorder — exercises the full callback path.
    
    // Arrange: VM with at least 2 entries + custom sort
    using var temp = new TempDirectory();
    var storePath = Path.Combine(temp.Path, "launcher.json");
    var initialEntries = new List<LaunchEntry>
    {
        new() { Id = EntryId.Parse("e1") with { }, /* required fields */ },
        new() { Id = EntryId.Parse("e2") with { }, /* required fields */ },
    };
    // ... construct LauncherStoreFile, save, load VM
    
    // Act: simulate drag commit via PreviewMoveEntry + CommitDragReorder
    
    // Assert:
    // - File on disk has settings.CustomEntryOrder matching post-drag order
    // - vm.Settings equals the post-drag order (was synced during save)
}
```

**Note**: The actual test implementation may need helper construction. If construction is too involved, fall back to a simpler structural test:

```csharp
[Fact]
public void MainWindowViewModel_SaveSettingsToStore_DoesNotHandAssembleStoreFile()
{
    // Source-level invariant: grep MainWindowViewModel.cs for "new LauncherStoreFile"
    // should find exactly ONE occurrence (in BuildStoreFile). If found elsewhere,
    // the dual path has regressed.
    var sourcePath = Path.Combine(
        RepoPaths.AppProjectRoot, "ViewModels", "MainWindowViewModel.cs");
    var source = File.ReadAllText(sourcePath);
    var count = Regex.Matches(source, @"new\s+LauncherStoreFile\s*\{").Count;
    Assert.True(count <= 1,
        $"MainWindowViewModel.cs should have at most 1 'new LauncherStoreFile {{' (in BuildStoreFile); found {count}. Dual path may have regressed.");
}
```

**Decision: Use the structural test** — direct, fast, regression-proof, doesn't require complex VM construction. Matches the verify-negative-check pattern from `feedback_verify_negative_check.md`.

### Step 3.3: Build + test

```bash
dotnet build WTLauncher.Avalonia.sln -c Release
dotnet test tests/WTLauncher.Tests/WTLauncher.Tests.csproj -c Release --no-build
```

Expected: 454 passing (453 + 1 new pin test).

## Phase 4: Verify gate + spec update

### Step 4.1: Run verify.ps1

```bash
powershell -ExecutionPolicy Bypass -File tools/verify.ps1
```

Expected: 17/17 PASS.

### Step 4.2: Spec update

If any non-obvious lesson emerged, add to `.trellis/spec/guides/pitfalls.md` or `.trellis/spec/core/persistence.md`. Likely candidate: a note about "collaborator workflow holding working state is NOT a dual-truth bug — it's the cost of rollback semantics".

### Step 4.3: Commit

Single commit per phase or one combined commit:

```
fix(settings): route SaveSettingsToStore through BuildStoreFile (R5 dual-path)

- SaveSettingsToStore now mutates _settings then calls BuildStoreFile
- Eliminates hand-assembled LauncherStoreFile dual path
- Add ponytail: comments documenting why workflow _settings mirror exists
  (working state for rollback, NOT parallel live truth)
- Add structural pin test preventing dual-path regression

Ref: second-pass audit #4 (2026-07-09 architecture review)
```

## Validation summary (acceptance criteria check)

- [ ] `SaveSettingsToStore` body calls `BuildStoreFile()` ✅
- [ ] No hand-assembly of `LauncherStoreFile` outside `BuildStoreFile()` ✅ (verified by pin test)
- [ ] `_settings` field count: 1 main VM + 1 workflow (justified) + 1 snapshot (justified) — documented via ponytail: comments ✅
- [ ] 454/454 tests passing (453 + 1 pin)
- [ ] `tools/verify.ps1` 17/17 PASS
- [ ] No speculative abstractions introduced ✅
