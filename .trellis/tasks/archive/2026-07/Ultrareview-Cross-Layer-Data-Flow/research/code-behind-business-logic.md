# Research: Code-Behind Business Logic Violations

- **Query**: Find all `.axaml.cs` (code-behind) files in Views layer that contain business logic violations per the rule "code-behind = windowing only (drag hit-test, dialog injection, keyboard nav)".
- **Scope**: internal
- **Date**: 2026-07-26

## Findings

### Files Examined (15 total)

All 15 `.axaml.cs` code-behind files in `src/avalonia-launcher/src/WTLauncher.App/Views/` were read and analyzed.

### VIOLATION 1: CommandEditDialog.axaml.cs

**File**: `src/avalonia-launcher/src/WTLauncher.App/Views/CommandEditDialog.axaml.cs`
**Lines**: 48-69 (`OnOkClicked` method)

**Violation type**: Business validation logic in code-behind.

**Details**:
- Line 51-55: Empty/null value validation (`ShowError("值不能为空。")`)
- Line 59-63: Reserved key collision check (`_reservedKeys.Contains(v1)`) returning business error
- This is business logic ("a command template key must be unique") enforced in the code-behind, not via ViewModel

The `_reservedKeys` field (line 25) is built from template keys passed through the constructor. The uniqueness check should be delegated to a ViewModel or service layer.

### VIOLATION 2: EntryDetailsWindow.axaml.cs

**File**: `src/avalonia-launcher/src/WTLauncher.App/Views/EntryDetailsWindow.axaml.cs`
**Lines**: 77-115 (`SetupTemplatePreview` method), 121-146 (`Submit` method)

**Violation type**: Business logic (template resolution, entry validation) in code-behind.

**Details**:
- Line 77-115: `SetupTemplatePreview()` -- resolves `$key` template references against `_commandTemplates` dictionary. The `CommandBox.TextChanged` event handler (lines 87-101) contains business logic: checking if a command starts with `$`, looking up the key in the dictionary, and generating preview text. This is command-template resolution logic (business domain) in code-behind.
- Lines 91-93: Template key lookup and branch logic -- business decision, not UI formatting
- Lines 121-146 (`Submit`): Calls `LauncherEntryDrafts.TryNormalizeRequiredDraft()` (Core type) and handles `InvalidDataException`. While using Core types is allowed, the orchestration and error handling is business logic.

**Note**: Some of this logic involves Core types used for validation (EntryId.Normalize, EntryName.Normalize). While View layers can technically call Core types, the amount of orchestration logic here exceeds "windowing only."

### VIOLATION 3 (acknowledged debt): PresetDetailsWindow.axaml.cs

**File**: `src/avalonia-launcher/src/WTLauncher.App/Views/PresetDetailsWindow.axaml.cs`
**Lines**: 43-63 (`Submit` method)

**Violation type**: Deliberate duplicate business validation in code-behind.

**Details**:
- Line 46-52: Empty name validation (`"请输入启动组名称。"`)
- Line 54-60: Existing name collision check (`"该名称已存在，请使用其他名称。"`)

Comment at line 42 reads:
```
// ponytail: intentional duplicate of ViewModel validation.
// This UI-layer check gives instant visual feedback (red error text + focus)
// before the save-path delegates to ViewModel validation. Keep both.
```

This is a known, intentional violation of the "code-behind = windowing only" rule. The justification is "instant visual feedback" before delegate to ViewModel. This constitutes technical debt and a violation of DRY (the same validation exists in the ViewModel/preset service layer).

### CONCERN (not violation): DiscoverAgentsWindow.axaml.cs

**File**: `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs`
**Total**: 340 lines

**Concern type**: Excessive programmatic UI construction and business-conditional rendering.

**Details**:
- This is the largest code-behind file (340 lines)
- `BuildRowCard()` method (lines 177-339) constructs a complete visual tree programmatically (checkboxes, text blocks, borders, badges, buttons) with business-conditional logic (line 282: `if (row.IsAlreadyAdded)`, line 286: `if (!string.IsNullOrEmpty(row.HomePageUrl))`)
- Comment at line 19: "ponytail: bindings done via direct PropertyChanged subscriptions rather than WhenAnyValue/ReactiveUI."
- Lines 149-152: Button content logic (`AddButton.Content = _viewModel.IsExistingDirectoryMode ? "创建启动项" : ...`) -- business conditional in view code

While each individual decision is about visual presentation, the volume (340 lines) and programmatic nature make this hard to maintain and review. The comment explicitly notes it is NOT following standard MVVM binding patterns.

### ACCEPTABLE (no violation): MainWindow.axaml.cs

**File**: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs`

**Lines 217-294** (`HandleKeyboardNavigation`): Contains keyboard navigation logic that directly invokes `entry.LaunchCommand.Execute()` (line 275) and `viewModel.FilterText = ""` (line 286). This is within the "keyboard nav" exception for code-behind -- it handles input routing, not business decisions about what the command does.

### ACCEPTABLE (no violation): Other code-behind files

- `LauncherEntriesView.axaml.cs` (39 lines) -- Pure event forwarding. CLEAN.
- `LauncherControlBar.axaml.cs` (25 lines) -- Pure event forwarding. CLEAN.
- `Controls/StatusBar.axaml.cs` (12 lines) -- Pure constructor. CLEAN.
- `ConfirmEntryDeleteWindow.axaml.cs` (24 lines) -- Sets display text only. CLEAN.
- `ConfirmPresetDeleteWindow.axaml.cs` (24 lines) -- Sets display text only. CLEAN.
- `ConfirmLaunchWindow.axaml.cs` (26 lines) -- Sets display text from ViewModel. CLEAN.
- `ConfirmBatchLaunchWindow.axaml.cs` (53 lines) -- Conditional UI (hasFailures) is presentation logic from ViewModel data. ACCEPTABLE.
- `DirectoryEditDialog.axaml.cs` (68 lines) -- Folder picker (view code) + basic empty validation. ACCEPTABLE.
- `CommandManagementWindow.axaml.cs` (158 lines) -- Dialog orchestration + event routing. ACCEPTABLE.
- `DirectoryManagementWindow.axaml.cs` (94 lines) -- Dialog orchestration + event routing. ACCEPTABLE.
- `MainWindow.Drag.cs` (393 lines) -- Drag ghost, overlay, insert indicators. Per DESIGN.md, drag visual management is explicitly a View responsibility. ACCEPTABLE.

### `MainWindowDragInsertDecision` static class (MainWindow.Drag.cs lines 373-392)

**File**: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs`
**Lines**: 373-392

This is a Views-layer calculation class for determining drag insert position (before/after) based on pointer position in card. It contains layout arithmetic (threshold-based decision using card width/height). This is view-level (visual positioning), not business logic. ACCEPTABLE.

## Summary

| File | Lines | Severity | Rule Violated |
|---|---|---|---|
| `CommandEditDialog.axaml.cs` | 48-69 | VIOLATION | Code-behind=windowing only: business validation |
| `EntryDetailsWindow.axaml.cs` | 77-115, 121-146 | VIOLATION | Code-behind=windowing only: template/business logic |
| `PresetDetailsWindow.axaml.cs` | 43-63 | ACKNOWLEDGED DEBT | Duplicate validation (ponytail comment) |
| `DiscoverAgentsWindow.axaml.cs` | 149-152, 177-339 | CONCERN | Heavy programmatic UI + conditionals |
| `MainWindow.axaml.cs` | 217-294 | ACCEPTABLE | Keyboard navigation (allowed) |
| `MainWindow.Drag.cs` | 29-392 | ACCEPTABLE | Drag visual management (explicit View role) |
| All other code-behind | all | CLEAN | No business logic found |

## Caveats / Not Found

- The DiscoverAgentsWindow is noted as a concern but not flagged as a hard violation because:
  - The comment explicitly acknowledges the non-standard binding approach
  - The business decisions are wrapped in UI construction context
  - The ViewModel (DiscoverAgentsViewModel) owns the actual business state
- The PresetDetailsWindow's duplicate validation is explicitly documented and intentional
