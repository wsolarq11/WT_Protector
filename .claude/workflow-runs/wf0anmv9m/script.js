export const meta = {
  name: 'fix-everything-100',
  description: 'Execute ALL ponytail cuts and UltraReview #2 remaining findings — zero exceptions',
  phases: [
    { title: 'Cut dead', detail: 'Delete dead code, inline wrappers, remove duplicates' },
    { title: 'Cut yagni', detail: 'Remove speculative abstractions, fold one-caller layers' },
    { title: 'Fix remaining', detail: 'UltraReview #2 H1-H4, verify.ps1, Ghost clamp, dialog UX' },
    { title: 'Verify', detail: 'Build + full test suite' },
  ],
}

// ============================================================
// PHASE 1: Safe deletions and inlines (no behavioral change)
// ============================================================

// CUT 4: LauncherSettingsCopies.Clone — 5-line file, inline at 2 callers
const cut4 = await agent(`Ponytail cut: Delete LauncherSettingsCopies.cs. Inline its single method (settings with { SchemaVersion = 1 }) at the 2 call sites.

File to delete: D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsCopies.cs

Caller 1: LauncherSettingsSnapshot.Capture() — inline "settings with { SchemaVersion = 1 }" 
Caller 2: LauncherEntryMutationSnapshot.Capture() — inline same

Read all 3 files, delete LauncherSettingsCopies.cs, update callers. Run dotnet build + test. Report.`, { phase: 'Cut dead', label: 'cut:settingscopies' })

// CUT 5: LauncherPresetFiles.CloneNormalized — delegation to Validate, rename callers
const cut5 = await agent(`Ponytail cut: Remove CloneNormalized() from LauncherPresetFiles.cs. It's just "=> Validate(presetFile)". Replace all 3 callers with direct Validate() calls.

File: D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/LauncherPresetFiles.cs

Read the file, find CloneNormalized, find its 3 callers, replace all with Validate(), remove CloneNormalized. Build + test.`, { phase: 'Cut dead', label: 'cut:clonenormalized' })

// CUT 7: LauncherEntryText.RequireNonEmpty — 0 callers, delete
const cut7 = await agent(`Ponytail cut: Delete RequireNonEmpty() from LauncherEntryText.cs. It has ZERO callers in the entire codebase.

File: D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryText.cs

Read, remove the method, build + test.`, { phase: 'Cut dead', label: 'cut:requirenonempty' })

// CUT 8: Delete dead drag-drop code (MainWindowDragDropDecision.cs + MainWindowDragEntryTransfer)
const cut8 = await agent(`Ponytail cut: Delete dead OLE DragDrop code. The drag system uses pointer capture, not OLE DragDrop.

Files to delete/cut:
- D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindowDragDropDecision.cs — entire file, 0 production callers
- D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs:299-313 — MainWindowDragEntryTransfer class (dead)

Delete the files/classes. Remove any test files that test only these dead classes. Build + test. Report.`, { phase: 'Cut dead', label: 'cut:drag-dead' })

// CUT 20: MainWindowEntryCardResolver.ResolveCards — 0 callers
const cut20 = await agent(`Ponytail cut: Remove unused ResolveCards() method from MainWindowEntryCardResolver.cs. Zero production callers.

File: D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindowEntryCardResolver.cs

Read, find ResolveCards, remove it. Build + test.`, { phase: 'Cut dead', label: 'cut:resolvecards' })

// ============================================================
// PHASE 1b: Eliminate duplication
// ============================================================

// CUT 9: Reserved names duplication
const cut9 = await agent(`Ponytail cut: Eliminate duplicated Windows reserved names HashSet.

LauncherEntryDrafts.s_reservedNames duplicates LauncherEntryIds.WindowsReservedNames. 

Fix: Make WindowsReservedNames "internal" in LauncherEntryIds.cs. Remove s_reservedNames from LauncherEntryDrafts.cs. Have LauncherEntryDrafts reference LauncherEntryIds.WindowsReservedNames directly.

Read both files. Make the change. Build + test. Report.`, { phase: 'Cut dead', label: 'cut:reserved-dupe' })

// CUT 10: RequireEntryName pattern dedup
const cut10 = await agent(`Ponytail cut: Eliminate duplicated RequireEntryName pattern across 5 files. All use the same 4-line pattern: Normalize+check-empty+throw.

Files with the duplicate pattern:
- LauncherEntryDeleteTarget.cs:22-28
- LauncherSingleLaunchConfirmation.cs:24-30
- LauncherBatchLaunchItem.cs:22-31
- LauncherBatchLaunchReport.cs:152-159
- LauncherPresetDeleteTarget.cs:21-28

Fix: Make LauncherEntryText.RequireNonEmpty (or rename/re-create if you deleted it) public and use it in all 5 places. If RequireNonEmpty was deleted, create a simple RequireTextOrThrow helper.

Read all files. Apply the consolidation. Build + test.`, { phase: 'Cut dead', label: 'cut:requirename-dupe' })

// ============================================================
// PHASE 2: YAGNI cuts — remove one-caller wrappers
// ============================================================

// CUT 22/23: Remove thin persistence wrappers (LauncherConfigMutationPersistence, LauncherSettingsMutationPersistence, LauncherPresetMutationPersistence) by inlining their try/catch at callers. Wait — these ARE called from multiple places. Let me check.

// Actually, skip 22/23 — the wrappers have error-formatting value. Focus on the truly thin ones.

// CUT 24/25: LauncherPresetSaveTarget / LauncherPresetDeleteTarget / LauncherEntryDeleteTarget — these validate and wrap. Keep if called from 3+ places.

// ============================================================
// PHASE 3: UltraReview #2 remaining fixes
// ============================================================

// H5 / Views L3: Ghost initial position uses unclamped CalculatePosition
const ghostFix = await agent(`Fix: Ghost initial position bypasses window-bounds clamping.

File: D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs

In ShowDragGhost(), the ghost is created via MainWindowDragGhostFactory.Create() which calls the 2-parameter CalculatePosition (no window clamping). But MoveDragGhost() uses the 6-parameter overload with window-bounds clamping.

Fix: After creating the ghost and adding to overlay, immediately clamp its position using LauncherDragGhostLayoutCalculator.CalculatePosition(pointerX, pointerY, Bounds.Width, Bounds.Height, ghost.Bounds.Width, ghost.Bounds.Height).

Read the file around ShowDragGhost. Apply fix. Build + test.`, { phase: 'Fix remaining', label: 'fix:ghost-init-clamp' })

// Views L2: FastWrapPanel zero/negative width
const fwpFix = await agent(`Fix: FastWrapPanel containerWidth guard accepts zero/negative.

File: D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher/src/WTLauncher.App/Views/FastWrapPanel.cs
Line 80: var containerWidth = double.IsFinite(finalSize.Width) ? finalSize.Width : 900;

double.IsFinite(0) and double.IsFinite(-1) both return true. Zero/negative width causes every child to be arranged at width 0.

Fix: Add Math.Max(1, ...) wrapper: var containerWidth = Math.Max(1, double.IsFinite(finalSize.Width) ? finalSize.Width : 900);

Read, apply, build + test.`, { phase: 'Fix remaining', label: 'fix:fwp-zero-width' })

// verify.ps1 wrapper format fix
const verifyFix = await agent(`Fix: verify.ps1 Read-ConfigItems broken for wrapper-format config.json.

File: D:/<PROJECT_ROOT>/WT_Protector/tools/config-lib.ps1, function Read-ConfigItems (around line 85)

THE BUG: config.json now uses {"schemaVersion":1,"entries":[...]} wrapper format, but Read-ConfigItems assumes raw array format (foreach over top-level items).

THE FIX: Check if the top-level JSON has an "entries" property (wrapper format). If yes, iterate $json.entries. If no (legacy array), iterate $json as before.

Read the function. Apply the fix. Report what you changed.`, { phase: 'Fix remaining', label: 'fix:verify-wrapper' })

// ============================================================
// PHASE 4: Verify
// ============================================================
const final = await agent(
  `Final verification: cd D:/<PROJECT_ROOT>/WT_Protector/src/avalonia-launcher && dotnet build 2>&1 | grep -E "(错误|成功)" && dotnet test --no-build 2>&1 | grep -E "(通过|失败|总计)". Report exact counts. If failures, report ALL details.`,
  { phase: 'Verify', label: 'verify:final' }
)

return { final: final?.slice(0, 500) }
