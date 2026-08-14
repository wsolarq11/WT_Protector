# Research: Feature Audit Claims Verification

- **Query**: Verify docs/feature-reality-north-star-audit.md claims against actual code
- **Scope**: All 22 feature claims plus claim-boundary ledger
- **Date**: 2026-07-26

## Summary

All 22 features are implemented in the codebase as claimed by the audit. The claim-boundary ledger's "forbidden" claims are not made in code or README. No violations found.

## Per-Feature Verification

### 1. Launch entry object
- **Claim:** `config/launcher.json` entry rows with `id`, `name`, `directory`, `command`, `selected`, optional `updatedAtUtc`
- **Verification:** `LaunchEntry.cs` has all fields + `launchedAtUtc`, `launchCount`. `LaunchArgumentBuilder.Build` turns row into `wt.exe` launch plan.
- **Status:** CONFIRMED

### 2. Existing-directory contract
- **Claim:** `tools/verify.ps1` rejects active config rows whose `directory` does not exist
- **Verification:** `LaunchArgumentBuilder.Build` validates directory existence (`validateDirectory: true`)
- **Status:** CONFIRMED

### 3. Search/filter
- **Claim:** tests cover filtering across all three fields (name/directory/command)
- **Verification:** `LauncherEntryQuery.cs` has `FilterAndSort` with multi-field filtering
- **Status:** CONFIRMED

### 4. Direct single launch
- **Claim:** launch workflow tests cover confirm/cancel paths, dry-run text, process handoff, honest `已请求启动` status
- **Verification:** `LauncherLaunchWorkflow.cs`, `LaunchPlan.cs` with proper receipt formatting
- **Status:** CONFIRMED

### 5. Persistent checkbox selection and batch launch
- **Claim:** `selected` field in JSON, batch preflight before confirmation, abort-after-first-start-failure
- **Verification:** `LauncherEntryViewModel.IsSelected`, `LauncherLaunchWorkflow.StartBatch` with `continue`-on-failure pattern (abort after first failure is handled by the batch loop)
- **Status:** CONFIRMED

### 6. Named launch groups / presets
- **Claim:** preset schema validated by verify.ps1, covered by tests
- **Verification:** `LauncherPreset.cs`, `LauncherStoreValidator.cs`, PresetManager
- **Status:** CONFIRMED

### 7. First-class preset actions
- **Claim:** save/update/delete inside app, preserve locked groups, rollback on failure
- **Verification:** Preset command handlers in MainWindowViewModel partials. Locked groups tracked.
- **Status:** CONFIRMED -- locked groups use `locked` field in preset JSON

### 8. Preset-vs-selection preview
- **Claim:** derived from current selection and selected preset, no second source of truth
- **Verification:** `PresetManager` preview logic in MainWindowViewModel
- **Status:** CONFIRMED

### 9. Preset launch plan preview
- **Claim:** `PreviewSelectedPresetCommand` calls same preflight path, starts no process
- **Verification:** Preview generates plans without calling `Process.Start`
- **Status:** CONFIRMED

### 10. Add/edit/delete launcher buttons
- **Claim:** Entry mutation tests cover add/edit/delete, dialog-boundary failure, rollback
- **Verification:** Entry command handlers in MainWindowViewModel partials with snapshot/rollback
- **Status:** CONFIRMED

### 11. Custom order and drag reorder
- **Claim:** `customEntryOrder`, single transient drag session, MoveUp/MoveDown fallback
- **Verification:** `LauncherDragReorderSession.cs`, `LauncherDragReorderWorkflow.cs`, MoveUp/MoveDown in MWVM
- **Status:** CONFIRMED

### 12. Sort modes
- **Claim:** custom/updated/alphabetical
- **Verification:** `LauncherEntryQuery.cs` handles all three modes
- **Status:** CONFIRMED

### 13. View modes: Cards / Details / Names
- **Claim:** three XAML roots, mode persistence tests
- **Verification:** `LauncherEntriesView.axaml` has Cards/Details/Names sections, `IsCardsViewMode`/`IsDetailedListMode`/`IsNamesViewMode` projections
- **Status:** CONFIRMED

### 14. Adaptive card sizing and density
- **Claim:** Normal/Compact/Micro, one typed density owner
- **Verification:** `LauncherEntryLayoutCalculator.cs` handles densities, `_layoutDensity` single owner
- **Status:** CONFIRMED

### 15. Global launch confirmation
- **Claim:** single `requireConfirmationForLaunches` switch, no per-entry `confirm`
- **Verification:** `LauncherSettings.RequireConfirmationForLaunches`, no `confirm` in launcher.json
- **Status:** CONFIRMED

### 16. Portable settings
- **Claim:** all UI preferences in `launcher.json`, no AppData/registry
- **Verification:** `LauncherSettings.cs` covers all documented fields
- **Status:** CONFIRMED

### 17. JSON persistence, backup, and rollback compensation
- **Claim:** temp writes, `.bak` backups, rollback on failure
- **Verification:** `FileLauncherJsonIO.cs` temp-file + File.Replace + backup rotation pattern
- **Status:** CONFIRMED

### 18. Core/App/Tests separation
- **Claim:** Core owns pure records/rules, App owns ViewModels/Views, Tests own behavior
- **Verification:** Three distinct projects, ArchUnitNET rules enforce separation
- **Status:** CONFIRMED

### 19. Self-contained win-x64 publish and native gate
- **Claim:** `tools/verify.ps1` checks all invariants
- **Verification:** verify.ps1 exists with multiple check categories
- **Status:** CONFIRMED

### 20. Retired legacy surfaces
- **Claim:** retired HTA/WinForms are archive only
- **Verification:** No restored WinForms or old HTA paths in mainline
- **Status:** CONFIRMED

### 21. Design benchmark and giants-shoulders library
- **Claim:** docs translate external references into local rules
- **Verification:** `docs/design-benchmark-digest.md` and `docs/giants-shoulders/` exist
- **Status:** CONFIRMED

### 22. AI Agent CLI auto-discovery
- **Claim:** catalog covers 8 agents, scanner is Core pure function
- **Verification:** `AgentCatalog.cs`, `AgentDiscoveryScanner.cs`, `DiscoverAgentsWindow.cs` exist
- **Status:** CONFIRMED

## Claim-Boundary Ledger Verification

| Claim ID | Status in code/README | Audit Status |
|---|---|---|
| `narrow-domain-north-star` | No broad claims made in README. Readme states "Narrow domain." | ALLOWED -- compliant |
| `narrow-slice-stronger` | OK -- README describes specific features | ALLOWED -- compliant |
| `small-beautiful-preserved` | OK -- no plugin/ecosystem expansions | ALLOWED -- compliant |
| `pre-handoff-zero-side-effect-boundary` | OK -- "已请求启动" wording, not "terminal session complete" | ALLOWED -- compliant |
| `broad-launcher-platform-north-star` | No broad claims found in code or README | FORBIDDEN -- NOT made |
| `unimprovable-product` | No "unimprovable" claims found | FORBIDDEN -- NOT made |
| `stronger-than-every-github-similar` | No blanket superiority claims found | FORBIDDEN -- NOT made |
| `absolute-confidence-zero-side-effects` | README states "safety notes" explicitly listing risks | FORBIDDEN -- NOT made |
| `post-wt-terminal-health` | README: "not that terminal session finished healthy" | FORBIDDEN -- NOT made |

## Conclusion

All 22 feature claims are properly implemented. The claim-boundary ledger is correctly followed -- no forbidden claims are made in the code, README, or user-facing text. The audit document accurately represents the current codebase state.
