export const meta = {
  name: 'fix-architecture-bugs',
  description: 'Fix 4 remaining architecture bugs: FastWrapPanel, drag state, settings rollback, search perf',
  phases: [
    { title: 'Fix', detail: 'one agent per bug reading context and implementing the fix' },
    { title: 'Verify', detail: 'one agent per bug adversarially verifying the fix' },
    { title: 'Synthesize', detail: 'compile results and run tests' },
  ],
}

const BUGS = [
  {
    key: 'fastwrap-measure-arrange',
    label: 'FastWrapPanel mismatch',
    prompt: `Fix a Measure/Arrange wrap-condition mismatch in FastWrapPanel.cs at D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/FastWrapPanel.cs.

THE BUG: MeasureOverride (line ~55) wraps when:
  rowWidth + (rowWidth > 0 ? spacing : 0) + childWidth > width
But ArrangeOverride (line ~90) wraps when:
  x + childWidth > containerWidth

After a wrap, MeasureOverride resets rowWidth to the overflowing child width, while ArrangeOverride resets x to 0 then adds the childWidth. For items near the boundary, Measure can predict the child fits on a row while Arrange pushes it to the next row (or vice versa), causing calculated totalHeight to be wrong → bottom items get clipped.

THE FIX: Make both passes use the SAME wrap condition. Either:
A) Use "x + childWidth > containerWidth" in Measure too (trivially consistent, since x tracks the same position as rowWidth), OR
B) Replace both with a shared helper method

Do option A — the simplest fix. Read FastWrapPanel.cs completely, understand the code, apply the fix, then verify with dotnet build and dotnet test --no-build in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher.

Report what you changed, the exact lines, and the test results.`,
  },
  {
    key: 'search-normalize-perf',
    label: 'Search redundant normalize',
    prompt: `Fix redundant string.Normalize(FormKC) calls in the search/filter path at D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryVisibility.cs.

THE BUG: MatchesFilter() calls RequireText() for every entry's Name, Directory, and Command on every filter refresh. RequireText() runs LauncherEntryText.Normalize() (FormKC). But ALL entry fields were already FormKC-normalized at load time (LauncherConfigStore.RequireEntryText). For 200 entries × 3 fields = 600 redundant Normalize() calls per keystroke.

THE FIX: Replace the RequireText() calls in MatchesFilter (around lines 223-229) with a simple null/empty check that does NOT re-normalize. The data is already normalized — we just need to guard against null. Create a new simple helper or inline the check.

Read LauncherEntryVisibility.cs completely, also read LauncherEntryText.cs to understand Normalize(), then apply the fix. Verify with dotnet build and dotnet test in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher.

Report what you changed and the test results.`,
  },
  {
    key: 'settings-rollback',
    label: 'Settings partial rollback',
    prompt: `Fix incomplete settings rollback in entry mutation restore at D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Entries.cs.

THE BUG: RestoreEntryMutationSnapshot (around line 273) only restores CustomEntryOrder from the snapshot:
  _settings = _settings with { CustomEntryOrder = snapshot.GetCustomEntryOrder() };
Other settings fields (SortMode, LastFilterText, SearchHistory, CommandTemplates) are carried forward. While no entry CRUD operation modifies those today, if a future mutation changes e.g. SortMode, the rollback won't revert it.

THE FIX: Capture full LauncherSettings in LauncherEntryMutationSnapshot (at LauncherEntryMutationSnapshot.cs), store a clone alongside the existing fields, and restore from it in RestoreEntryMutationSnapshot. Read both files, read LauncherSettingsCopies.cs for Clone(), read how Capture is called in Entries.cs.

Apply the fix, verify with dotnet build and dotnet test in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher.

Report what you changed and the test results.`,
  },
  {
    key: 'drag-escape-state',
    label: 'Drag Escape state split',
    prompt: `Fix the drag state machine split between View and ViewModel when Escape key or other controls cancel the drag preview.

THE BUG: When the user presses Escape during a drag (MainWindow.axaml.cs ~line 249), it clears FilterText, which triggers OnFilterTextChanged → TryPersistSettingsChange → CaptureSettingsMutationSnapshot → DiscardDragReorderPreview → _dragReorderWorkflow.Cancel(). The ViewModel session is cancelled but the View state (_isDragging, _dragGhost, pointer capture, DisplayModeComboBox disabled) remains active. On the next PointerMoved, the drag silently resumes. Similarly, interacting with search/sort/preset controls during drag triggers DiscardDragReorderPreview silently without cleaning View state.

THE FIX: After calling viewModel.CancelDragReorder() or viewModel.CommitDragReorder() in FinishDragReorder (MainWindow.Drag.cs), ensure the View state is always cleaned. The simplest approach: extract a public method that can be called to force-sync View state whenever the ViewModel drag session is externally cancelled.

Read these files:
- D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs (the ForceCleanup, FinishDragReorder, HideDragGhost methods)
- D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs (the Escape handler, keyboard navigation)
- D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Drag.cs (CancelDragReorder, CommitDragReorder)

Apply the fix, verify with dotnet build and dotnet test in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher.

Report what you changed and the test results.`,
  },
]

const results = await pipeline(
  BUGS,
  bug => agent(bug.prompt, { label: `fix:${bug.key}`, phase: 'Fix' }),
  async (fixReport, bug) => {
    if (!fixReport) return { bug: bug.key, status: 'FAILED', reason: 'agent error' }
    // Second-stage: verify
    const verifyResult = await agent(
      `Adversarially verify this fix for bug "${bug.key}". Read the changed files from its report:\n\n${fixReport.slice(0, 8000)}\n\nYour job: try to find a case where this fix doesn't work, introduces a new bug, or breaks existing behavior. Check that all 595 tests still pass. Run dotnet build and dotnet test in D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher. If tests fail, report the failure. If no issues, confirm PASS.`,
      { label: `verify:${bug.key}`, phase: 'Verify' }
    )
    return { bug: bug.key, fix: fixReport.slice(0, 4000), verdict: verifyResult?.slice(0, 2000) || 'no verification' }
  }
)

phase('Synthesize')
const summary = results.filter(Boolean).map(r => `## ${r.bug}\n${r.verdict?.includes('PASS') ? '✅ PASS' : '⚠️ NEEDS ATTENTION'}\n---\n${r.fix}\n`).join('\n')
log(`All 4 bugs processed. Results:\n${results.filter(Boolean).length}/${BUGS.length} completed`)

return summary
