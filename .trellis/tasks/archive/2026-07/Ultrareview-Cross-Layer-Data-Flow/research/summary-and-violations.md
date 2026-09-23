# Research: Ultrareview Cross-Layer Data Flow -- Summary and Violations

- **Query**: Comprehensive cross-layer data flow audit across 5-layer architecture
- **Scope**: internal
- **Date**: 2026-07-26

## Executive Summary

Out of ~130 source files examined across all 5 layers:

- **2 hard violations** found (code-behind business logic)
- **1 acknowledged debt** (duplicate validation, documented as ponytail)
- **1 concern** (heavy programmatic UI in code-behind)
- **All other rules**: Clean

## Violations

### Violation 1: CommandEditDialog.axaml.cs -- Business Validation in Code-Behind

| Field | Detail |
|---|---|
| **File** | `src/avalonia-launcher/src/WTLauncher.App/Views/CommandEditDialog.axaml.cs` |
| **Lines** | 48-69 |
| **Rule** | "code-behind = windowing only (drag hit-test, dialog injection, keyboard nav)" -- CLAUDE.md |
| **Evidence** | Lines 59-63: checks if template key is reserved via `_reservedKeys.Contains(v1)`. Lines 51-54: empty value validation with `ShowError()`. Both are business validation rules implemented in code-behind. |
| **Severity** | Medium -- the dialog is small (78 lines) and the validation is localized, but it breaks the architectural boundary. |

### Violation 2: EntryDetailsWindow.axaml.cs -- Template Resolution and Validation in Code-Behind

| Field | Detail |
|---|---|
| **File** | `src/avalonia-launcher/src/WTLauncher.App/Views/EntryDetailsWindow.axaml.cs` |
| **Lines** | 77-115 (SetupTemplatePreview), 121-146 (Submit) |
| **Rule** | "code-behind = windowing only" -- CLAUDE.md |
| **Evidence** | `SetupTemplatePreview()` (line 77-115) resolves `$key` template references and generates preview text inline with CommandBox.TextChanged handler. `Submit()` (line 121-146) calls `LauncherEntryDrafts.TryNormalizeRequiredDraft()` directly from code-behind, bypassing the `IEntryService` layer. |
| **Severity** | Medium -- similar validation exists in the service layer, but the template preview logic is duplicated in code-behind. |

## Acknowledged Debt

### Debt 1: PresetDetailsWindow.axaml.cs -- Duplicate Validation

| Field | Detail |
|---|---|
| **File** | `src/avalonia-launcher/src/WTLauncher.App/Views/PresetDetailsWindow.axaml.cs` |
| **Lines** | 43-63 |
| **Rule** | DRY + "code-behind = windowing only" |
| **Evidence** | Comment at line 42: "ponytail: intentional duplicate of ViewModel validation." Empty name check and existing name collision check duplicated in code-behind. Intentionally kept for "instant visual feedback." |
| **Severity** | Low (intentional, documented) |

## Concerns

### Concern 1: DiscoverAgentsWindow.axaml.cs -- Heavy Programmatic UI

| Field | Detail |
|---|---|
| **File** | `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs` |
| **Total** | 340 lines |
| **Evidence** | Creates entire visual tree programmatically (checkboxes, text blocks, borders, buttons) with business-conditional rendering. Comment at line 19 notes this doesn't use standard MVVM binding due to project constraints. |
| **Severity** | Low -- documented exception, but the file is 3x larger than any other code-behind and is hard to maintain. |

## Areas Verified Clean

| Rule | Files/Paths Examined | Result |
|---|---|---|
| Core layer no Avalonia references | 40+ files in WTLauncher.Core | CLEAN |
| Core layer no CommunityToolkit.Mvvm references | 40+ files in WTLauncher.Core | CLEAN |
| Code-behind no business logic (15 files) | All 15 .axaml.cs files | 2 violations + 1 debt |
| ViewModel no UI manipulation | All ViewModels/*.cs | CLEAN |
| No bare `catch (Exception)` | Entire src/ directory | CLEAN (analyzer enforces) |
| No direct JSON file I/O outside LauncherStore | Entire App layer | CLEAN |
| Interface boundaries not bypassed | All port/adapter interfaces | CLEAN |
| Single source of truth for projections | LayoutManager, Drag, Layout classes | CLEAN |
| Selection two write paths | MainWindowViewModel.Batch.cs | CLEAN |
| Drag state single owner | LauncherDragReorderWorkflow | CLEAN |
| Refresh discards drag preview | MainWindowViewModel.Refresh() | CLEAN |
| Atomic config write | FileLauncherJsonIO.Save() | CLEAN |
| No obsolete per-entry confirm key | LaunchEntry record | CLEAN |

## Files Written

| File | Description |
|---|---|
| `.trellis/tasks/Ultrareview-Cross-Layer-Data-Flow/research/layer-dependency-audit.md` | Core layer dependency check -- CLEAN |
| `.trellis/tasks/Ultrareview-Cross-Layer-Data-Flow/research/code-behind-business-logic.md` | Code-behind business logic violations -- 2 found |
| `.trellis/tasks/Ultrareview-Cross-Layer-Data-Flow/research/mvvm-and-interface-boundaries.md` | MVVM pattern and interface boundaries -- CLEAN |
| `.trellis/tasks/Ultrareview-Cross-Layer-Data-Flow/research/cross-layer-invariants.md` | All cross-layer invariants -- all CLEAN |
| `.trellis/tasks/Ultrareview-Cross-Layer-Data-Flow/research/data-flow-trace.md` | Add Entry data flow trace -- 1 concern (code-behind calls Core directly) |
| `.trellis/tasks/Ultrareview-Cross-Layer-Data-Flow/research/summary-and-violations.md` | This summary file |
