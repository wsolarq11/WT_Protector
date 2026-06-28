export const meta = {
  name: 'finish-everything',
  description: 'Execute all remaining blocked items: atomic save, backup safety, test fixes, perf',
  phases: [
    { title: 'Atomic', detail: 'Fix save atomicity and backup rotation' },
    { title: 'Tests', detail: 'Fix test fixture and flaky assertions' },
    { title: 'Perf', detail: 'Fix sort tiebreaker perf' },
    { title: 'Verify', detail: 'Build + test all changes' },
  ],
}

// === ITEM 1: Fix LauncherJson atomicity and backup safety (C3/C4/C5/C6) ===
const saveFix = await agent(`Fix LauncherJson save atomicity and backup safety in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs.

THREE BUGS TO FIX:

BUG 1 (C3/C5 — save non-atomic, temp deleted after failed move):
Current: File.Move(tempPath, fullPath, overwrite:true) deletes target then renames. If crash between delete+rename, both files gone. Finally block then deletes temp. Fix: use File.Replace when target exists (it's atomic on NTFS — uses MoveFileEx with MOVEFILE_REPLACE_EXISTING). Only use File.Move for first-save (no target). On move failure, keep temp for recovery.

BUG 2 (C4 — backup rotation deletes .bak then recreates):
Current: RotateBackups() deletes old .bak, THEN copies current to .bak-temp, THEN renames .bak-temp → .bak. Window between rotate and new .bak = no backup exists. Fix: reverse the order — create new backup first, THEN rotate old backups. Use a copy-then-delete-oldest strategy.

BUG 3 (C6 — LoadWithRecovery skips missing temp silently):
Current: When primary file missing/corrupt AND .bak missing, LoadWithRecovery returns default. But corrupted file is still at path (not deleted). Fix: when both corrupt AND backup missing, keep the corrupt file and throw so caller knows something is wrong — don't silently return default.

Apply ALL three fixes. Read the file completely first. Run dotnet build and dotnet test at D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher after your changes. Report every changed line.`, { phase: 'Atomic', label: 'fix:save-atomicity' })

// === ITEM 2: Fix multi-file commit consistency (C9) ===
const commitFix = await agent(`Fix multi-file commit consistency in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryMutationPersistence.cs.

THE BUG: Multi-file save commit is non-atomic. Config committed first via CommitIfNeeded, then presets, then settings. If crash between commits, on-disk state is inconsistent (config new, presets old, settings old). The journal survives but replay skips already-committed files.

THE FIX: Add a pre-commit validation step: BEFORE any commit, verify ALL staged temp files exist and are valid JSON. If any fail, abort the entire commit, delete all staged files, clear the journal, return error. This makes the commit phase "all-or-nothing" — either all 3 files commit successfully, or none do (staged files remain for next attempt, journal preserved).

Read LauncherEntryMutationPersistence.cs completely, also read CommitJournal.cs. Apply changes. Run dotnet build and dotnet test. Report changes.`, { phase: 'Atomic', label: 'fix:multi-commit' })

// === ITEM 3: Fix C11 (test TempDirectory) ===
const c11Fix = await agent(`Fix the TempDirectory premature disposal bug in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/WTLauncher.Tests/LauncherDragReorderSessionTests.cs.

THE BUG: The StartSession helper (around line 96) uses "using var temp = new TempDirectory()" which disposes the temp dir before returning. The session and its ViewModel hold references to config/settings paths inside the now-deleted directory. While current tests don't touch those paths after construction, the stale references make any future test change silently fail on missing dirs.

THE FIX: Change "using var temp = new TempDirectory()" to "var temp = new TempDirectory()" and let the test class handle disposal (TempDirectory likely inherits IDisposable and handles cleanup). Or wrap it differently. Read the file first to understand the exact pattern. Also read TempDirectory.cs to understand the cleanup mechanism. Apply the minimal fix. Run dotnet test.`, { phase: 'Tests', label: 'fix:tempdir-leak' })

// === ITEM 4: Fix H14 (flaky 300ms) ===
const h14Fix = await agent(`Fix the flaky 300ms WaitForExit assertion in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/WTLauncher.Tests/LauncherProcessTests.cs.

THE BUG (line ~58): Assert.False(process.WaitForExit(300)) — on slow CI, 300ms may not be enough for Process.Start to even launch. The "ping 127.0.0.1 -n 60" process should run ~60 seconds, but the assertion is about whether the process is STILL running, not about the ping duration.

THE FIX: Replace the 300ms wait with checking process.HasExited immediately (the process CAN'T have exited yet — it was just started). If process.HasExited is true, the test can assert that the exit code is non-zero (cmd.exe failed to run ping). Use Assert.False(process.HasExited, "process should not have exited immediately") — no timeout, just an instant check. Read the file, apply, run dotnet test.`, { phase: 'Tests', label: 'fix:flaky-wait' })

// === ITEM 5: Fix H15 (exception.Message in tests) - fix the 5 worst ===
const h15Fix = await agent(`Fix exception.Message format assertions in tests. These are brittle across .NET versions.

Read these files and fix the Message-dependent assertions:
1. D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryDeleteTargetTests.cs (line ~29)
2. D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/WTLauncher.Tests/LauncherDragReorderSessionTests.cs (line ~65)
3. D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/WTLauncher.Tests/LauncherStartupArgumentsTests.cs (lines ~43, 62, 79)
4. D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowDragEntryTransferTests.cs (lines ~19, 27)
5. D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.DialogBoundaryTests.cs (line ~24)

THE BUG: Assert.Equal("Entry id is required. (Parameter 'Id')", exception.Message) depends on .NET's exact Message format which can change between runtime versions.

THE FIX: Replace with Assert.Contains for the meaningful part, AND add a check on exception.ParamName where applicable:
  Assert.Contains("Entry id is required", exception.Message);
  Assert.Equal("Id", exception.ParamName);  // stable across .NET versions

Or use Assert.Matches with a regex that's version-independent.

Apply to ALL the files listed above. Run dotnet test to verify.`, { phase: 'Tests', label: 'fix:exception-msg' })

// === ITEM 6: Sort tiebreaker perf ===
const sortPerf = await agent(`Fix sort tiebreaker performance in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryVisibility.cs.

THE BUG: In CompareEntries, Recent/Updated sort modes call ResolveName() in the tiebreaker path (line ~156-157), which calls RequireText(entry.Name, "name"), which runs LauncherEntryText.Normalize on every comparison. List.Sort is O(n log n), so this runs Normalize O(n log n) times. The Alphabetical sort mode correctly builds a nameCache (O(n) normalizations) and passes it.

THE FIX: Build a nameCache in the Recent and Updated sort loops (same pattern as Alphabetical at lines ~94-98) and pass it to CompareEntries. The loop already iterates entries to build a timeCache — just add nameCache in the same pass.

Read the file, apply the one-line addition. Run dotnet build and dotnet test. Report the changed line.`, { phase: 'Perf', label: 'fix:sort-tiebreaker' })

phase('Verify')
// Final verification: build + full test suite
const finalCheck = await agent(
  `Verify ALL changes are correct. Run: cd D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher && dotnet build && dotnet test --no-build. Report total passed/failed/skipped counts and any warnings. If any tests fail, report the full failure details.`,
  { phase: 'Verify', label: 'verify:all' }
)

return {
  save: saveFix?.slice(0, 1000),
  commit: commitFix?.slice(0, 1000),
  c11: c11Fix?.slice(0, 500),
  h14: h14Fix?.slice(0, 500),
  h15: h15Fix?.slice(0, 500),
  perf: sortPerf?.slice(0, 500),
  final: finalCheck?.slice(0, 1000),
}
