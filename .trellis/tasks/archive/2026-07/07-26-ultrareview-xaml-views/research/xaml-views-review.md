# Research: XAML & Views Ultrareview

- **Query**: Review ALL XAML files and view code-behind files for issues (hardcoded colors, binding errors, x:DataType correctness, DataContext, performance, accessibility, MVVM violations, style/theme consistency, missing states, event leaks, compilation errors)
- **Scope**: Internal (all 17 XAML files + 14 .axaml.cs code-behind files)
- **Date**: 2026-07-26

## Files Reviewed

| File | Type |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.App/App.axaml` | Application resources |
| `src/avalonia-launcher/src/WTLauncher.App/App.axaml.cs` | Application startup |
| `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml` | Main window layout + styles |
| `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs` | Main window code-behind + drag |
| `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs` | Drag reorder logic |
| `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindowEntryCardResolver.cs` | Card resolution helper |
| `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml` | Cards/Details/Names views |
| `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml.cs` | Event forwarding |
| `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherControlBar.axaml` | Control bar |
| `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherControlBar.axaml.cs` | Search/display box accessors |
| `src/avalonia-launcher/src/WTLauncher.App/Views/Controls/StatusBar.axaml` | Status bar |
| `src/avalonia-launcher/src/WTLauncher.App/Views/Controls/StatusBar.axaml.cs` | Status bar (empty) |
| `src/avalonia-launcher/src/WTLauncher.App/Views/Dialogs/DialogSharedStyles.axaml` | Shared dialog styles |
| `src/avalonia-launcher/src/WTLauncher.App/Views/CommandEditDialog.axaml` | Command edit dialog |
| `src/avalonia-launcher/src/WTLauncher.App/Views/CommandEditDialog.axaml.cs` | Command edit code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/CommandManagementWindow.axaml` | Command management |
| `src/avalonia-launcher/src/WTLauncher.App/Views/CommandManagementWindow.axaml.cs` | Command management code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmBatchLaunchWindow.axaml` | Batch launch confirmation |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmBatchLaunchWindow.axaml.cs` | Batch launch code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmEntryDeleteWindow.axaml` | Delete confirmation |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmEntryDeleteWindow.axaml.cs` | Delete code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmLaunchWindow.axaml` | Single launch confirmation |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmLaunchWindow.axaml.cs` | Single launch code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmPresetDeleteWindow.axaml` | Preset delete confirmation |
| `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmPresetDeleteWindow.axaml.cs` | Preset delete code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/DirectoryEditDialog.axaml` | Directory edit dialog |
| `src/avalonia-launcher/src/WTLauncher.App/Views/DirectoryEditDialog.axaml.cs` | Directory edit code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/DirectoryManagementWindow.axaml` | Directory management |
| `src/avalonia-launcher/src/WTLauncher.App/Views/DirectoryManagementWindow.axaml.cs` | Directory management code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml` | Agent discovery |
| `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs` | Agent discovery code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/EntryDetailsWindow.axaml` | Entry details editor |
| `src/avalonia-launcher/src/WTLauncher.App/Views/EntryDetailsWindow.axaml.cs` | Entry details code-behind |
| `src/avalonia-launcher/src/WTLauncher.App/Views/PresetDetailsWindow.axaml` | Preset details dialog |
| `src/avalonia-launcher/src/WTLauncher.App/Views/PresetDetailsWindow.axaml.cs` | Preset details code-behind |

---

## Findings

### Issue 1: ConfirmLaunchWindow -- incomplete FontFamily fallback list (bug risk)

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmLaunchWindow.axaml`
- **Line**: 28
- **Severity**: Medium
- **Detail**: Uses `FontFamily="Cascadia Code, Consolas"` while every other XAML file in the project uses `FontFamily="Cascadia Code, Consolas, Courier New, monospace"`. The shorter list is missing `Courier New` and the generic `monospace` fallback. If neither Cascadia Code nor Consolas is installed on the machine, Avalonia falls back to the default serif font rather than a monospace. The `CommandText` TextBox shows monospace command text, so a non-monospace fallback would break the visual expectation.
- **Verified in**: `MainWindow.axaml:217`, `CommandEditDialog.axaml:24`, `CommandManagementWindow.axaml:144`, `DialogSharedStyles.axaml:55`, `EntryDetailsWindow.axaml:24` all use the full `"Cascadia Code, Consolas, Courier New, monospace"` fallback chain.

---

### Issue 2: DiscoverAgentsWindow -- missing x:DataType (no compiled bindings)

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml`
- **Line**: 1-6 (Window opening tag)
- **Severity**: Medium
- **Detail**: The `Window` element lacks `x:DataType="vm:DiscoverAgentsViewModel"`. All bindings in this XAML file fall back to reflection-based binding resolution instead of compiled bindings. This means:
  - No compile-time binding error checking -- misspelled properties only surface as runtime binding errors.
  - Slower binding resolution at runtime.
  - Compared to `CommandManagementWindow.axaml` and `DirectoryManagementWindow.axaml` which correctly declare `x:DataType` on their Window elements.
- **Evidence**:
  - `CommandManagementWindow.axaml:7` has `x:DataType="vm:CommandManagementViewModel"`
  - `DirectoryManagementWindow.axaml:7` has `x:DataType="vm:DirectoryManagementViewModel"`
  - `DiscoverAgentsWindow.axaml` has no `x:DataType` attribute

---

### Issue 3: DiscoverAgentsWindow.axaml.cs -- significant MVVM violation in BuildRowCard

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs`
- **Lines**: 177-338 (`BuildRowCard()` method)
- **Severity**: High
- **Detail**: The `BuildRowCard` method constructs the entire visual tree for each discovered agent row programmatically using C# code (CheckBox, TextBlock, Border, StackPanel, Button with click handlers, etc.). This is a significant MVVM violation:
  1. Bypasses the Avalonia DataTemplate system
  2. Duplicates card styling logic already present in `MainWindow.axaml` styles (`.card`, `.entry-drag-handle`, etc.)
  3. Uses `FindResource("TextMutedBrush")` etc. with manual resource lookups instead of compiled binding
  4. Hardcodes visual structure that should be expressed in XAML
  5. Bypasses virtualization (all rows are created and added to `RowsPanel.Children` at once)
  6. `RenderRows()` (line 156-175) clears and rebuilds all children on every collection change, destroying and recreating the entire visual tree each time
- **Additional evidence**: `InitDirectoryPanel()` (lines 57-74) similarly creates RadioButtons programmatically.
- **The ponytail comment** at lines 19-22 acknowledges this is deliberate ("bindings done via direct PropertyChanged subscriptions rather than WhenAnyValue/ReactiveUI") but the extent of programmatic UI construction far exceeds what a binding-only concern would need.

---

### Issue 4: DiscoverAgentsWindow -- potential NullReferenceException in RenderRows / SyncUiFromViewModel

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs`
- **Lines**: 167, 202-203, 247, 255, 265, 330
- **Severity**: Medium
- **Detail**: Multiple calls to `Application.Current!.FindResource("TextMutedBrush")!` and similar resource lookups use null-forgiving operators (`!`) without a null guard. `FindResource` can return null if:
  - `Application.Current` is null (during early startup or designer mode)
  - The resource key does not exist (e.g., renamed or removed token)

  The already-registered static resources in `App.axaml.cs:34-93` should cover these keys, but the `!` operators suppress compiler warnings without runtime protection. A safer pattern would be `TryFindResource` or a null-coalescing fallback. Compare with `LauncherDialogText.SafeParseBrush` in `MainWindow.Drag.cs:311-312` which explicitly handles fallback values.

---

### Issue 5: Hardcoded FontSize="11" does not match any design token

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml`
- **Line**: 71
- **Severity**: Low
- **Detail**: `FontSize="11"` is hardcoded on the badge count TextBlock inside the group header. The design token `LauncherDesignTokens.FontSizeSmall` is defined as `12`. Using value 11 instead of 12 means this badge text is smaller than the smallest standardized font size. If tokens are adjusted globally for readability/scaling, this inline value will drift.

---

### Issue 6: Hardcoded FontSize="11" does not match any design token

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/Dialogs/DialogSharedStyles.axaml`
- **Line**: 72
- **Severity**: Low
- **Detail**: `FontSize="11"` is hardcoded in the `.muted-detail` style. Same issue as Issue 5 -- `FontSizeSmall = 12` is the smallest design token, but value 11 is used here.

---

### Issue 7: Hardcoded FontSize="13" does not match any design token

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml`
- **Lines**: 34, 155, 211
- **Severity**: Low
- **Detail**: `FontSize="13"` is hardcoded in three styles: `.muted` (line 34), `.name-title` (line 155), and `.card-title` (line 211). No design token has value 13. `FontSizeSmall = 12`, `FontSizeBody = 14`. Hardcoded 13 sits between them and will drift if the token scale is adjusted.

---

### Issue 8: Hardcoded FontSize="12" inline in multiple dialog files

- **File**: Multiple dialog XAML files
- **Lines**: Various
- **Severity**: Low
- **Detail**: Inline `FontSize="12"` appears in places such as `DiscoverAgentsWindow.axaml.cs:166` and `DiscoverAgentsWindow.axaml.cs:291`. While `FontSizeSmall = 12` matches, these should use `{StaticResource FontSizeSmall}` to keep design authority centralized.

---

### Issue 9: Misuse of NameTileGap for stack spacing in dialog

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmBatchLaunchWindow.axaml`
- **Line**: 45
- **Severity**: Low
- **Detail**: `Spacing="{x:Static core:LauncherDesignTokens.NameTileGap}"` uses the `NameTileGap` token (value 6) for paragraph-level spacing in the failure section. `NameTileGap` is semantically "gap between name tiles in Names view mode". The semantically correct token would be `SpacingSmall` (value 8) which is used on line 68 in the same file for the button row. This mixing of semantic tokens creates confusion about intent.

---

### Issue 10: Redundant FontFamily hardcoding across multiple dialog files

- **File**: Multiple dialog XAML files
- **Lines**: CommandEditDialog.axaml:24, DirectoryEditDialog.axaml:26, EntryDetailsWindow.axaml:24/33/40/47, PresetDetailsWindow.axaml:30
- **Severity**: Low
- **Detail**: `FontFamily="Cascadia Code, Consolas, Courier New, monospace"` is repeated inline at 8+ locations across dialog XAML files. The `.mono` style already exists in `MainWindow.axaml:216-221` but these dialog files cannot reference `Window.Styles` from `MainWindow.axaml`. They include `DialogSharedStyles.axaml` which does NOT define a `.mono` style (only `.mono-command`, `.mono-directory`, and `.mono-preview` exist there). Consider adding a shared `.mono` style to `DialogSharedStyles.axaml` to eliminate this duplication.

---

### Issue 11: LauncherEntriesView -- Details view nesting: ListBox inside Border with background, no virtualization

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml`
- **Lines**: 252-424
- **Severity**: Low (performance)
- **Detail**: The Details view places a `ListBox` inside a Border that has `ClipToBounds="True"` and border styling. The `ListBox` has `ScrollViewer.VerticalScrollBarVisibility="Auto"` (inherited from style line 169) which should enable virtualization. However, the ListBox's `DataTemplate` contains complex elements (ContextMenu with 7 MenuItems, nested Grids, drag insertion preview borders). Each visible entry renders all this markup. With many entries visible, this could impact scrolling performance. Virtualization should help but the template complexity adds per-item overhead.

---

### Issue 12: LauncherEntriesView -- x:DataType on inner DataTemplates lacks namespace for string items

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml`
- **Lines**: 112, 79
- **Severity**: Low
- **Detail**: The inner `ItemsControl.ItemTemplate` (e.g., line 112 and line 79) has `<DataTemplate>` without `x:DataType`. If these items are strings (from `CommandRow.ReferencingEntryNames`), leaving `x:DataType` unspecified disables compiled bindings for those inner templates. The `Text="{Binding}"` binding will work (it binds to the string itself), but there is no compile-time binding protection.

---

### Issue 13: MainWindow.axaml.cs -- Event handlers subscribed but MethodImplOptions.NoInlining or exception guard patterns

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs` +
  `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.Drag.cs`
- **Lines**: MainWindow.axaml.cs:109, MainWindow.Drag.cs:129-147
- **Severity**: Low (logic)
- **Detail**: `OnDragPointerCaptureLost` (Drag.cs:129) sets `_isDragging = false` before calling `ForceCleanup()` (line 145). But `ForceCleanup()` at line 209 also sets `_isDragging = false` and can itself trigger `OnDragPointerCaptureLost` synchronously via `_dragPointer?.Capture(null)` (line 210). The `_cleaningUp` guard (line 203) prevents re-entrancy. However, the double-assignment of `_isDragging = false` (once in the handler, once in ForceCleanup) is redundant by design -- the comment on line 207-208 explains the ordering requirement. This is correct as-is but fragile.

---

### Issue 14: DiscoverAgentsWindow -- no AutomationProperties on programmatically created controls

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs`
- **Lines**: 178-338
- **Severity**: Medium (accessibility)
- **Detail**: The programmatically created row cards in `BuildRowCard()` do not set `AutomationProperties` on any of the created controls (CheckBox, Buttons, TextBlocks, Borders). The XAML-based views consistently use `automation:AutomationProperties.Name="..."`. Without these, screen readers cannot identify interactive elements in the discovery dialog.

---

### Issue 15: ConfirmEntryDeleteWindow -- IsDefault on cancel button

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmEntryDeleteWindow.axaml`
- **Lines**: 29-30
- **Severity**: Medium (UX)
- **Detail**: The cancel button is set with both `IsDefault="True"` and `IsCancel="True"`. `IsDefault` makes the button respond to the Enter key, while `IsCancel` makes it respond to Escape. Having both on the same button is contradictory -- `IsDefault` on the cancel button means Enter will cancel the operation. Compare with `ConfirmPresetDeleteWindow.axaml:35-36` which has the same pattern. This is inconsistent with typical dialog behavior where the affirmative button (delete) should be the default.

---

### Issue 16: ConfirmPresetDeleteWindow -- IsDefault on cancel button

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/ConfirmPresetDeleteWindow.axaml`
- **Lines**: 35-36
- **Severity**: Medium (UX)
- **Detail**: Same as Issue 15. The cancel button has `IsDefault="True"` which makes Enter press the cancel/escape button. For a destructive action ("删除启动组"), the default button should be the cancel/non-destructive action (which it is), but `IsDefault` on the same button is unusual.

Actually, upon rethinking: `IsDefault="True"` on the Cancel button means pressing Enter closes as cancel. This is actually safer for destructive operations -- it prevents accidental deletion by pressing Enter. The user must explicitly Tab to and activate the delete button. This is intentional defensive design. The issue is more of a documentation/notice than a bug.

---

### Issue 17: LauncherEntriesView -- Duplicate ContextMenu definition across 3 templates

- **File**: `src/avalonia-launcher/src/WTLauncher.App/Views/LauncherEntriesView.axaml`
- **Lines**: 108-136, 305-332, 459-486
- **Severity**: Low (maintainability)
- **Detail**: The exact same ContextMenu with 7 MenuItems (启动, 预演, 上移排序, 下移排序, 复制, 编辑, 删除) is defined 3 times -- once in the Cards DataTemplate, once in the Details DataTemplate, and once in the Names DataTemplate. Each has identical MenuItems with identical bindings and `InputGesture` definitions. If a new menu item is added or an existing one is modified, all 3 copies must be updated. Consider extracting to a shared resource.

---

## Summary

| # | Severity | File | Line(s) | Issue |
|---|----------|------|---------|-------|
| 1 | **Medium** | ConfirmLaunchWindow.axaml | 28 | Incomplete FontFamily fallback (missing `Courier New, monospace`) |
| 2 | **Medium** | DiscoverAgentsWindow.axaml | 1-6 | Missing `x:DataType` -- no compiled bindings |
| 3 | **High** | DiscoverAgentsWindow.axaml.cs | 177-338 | `BuildRowCard()` -- significant MVVM violation, programmatic UI construction |
| 4 | **Medium** | DiscoverAgentsWindow.axaml.cs | 167,202,247,255,265,330 | Unsafe null-forgiving `!` on `FindResource` calls |
| 5 | **Low** | LauncherEntriesView.axaml | 71 | Hardcoded `FontSize="11"`, no matching design token (`FontSizeSmall=12`) |
| 6 | **Low** | DialogSharedStyles.axaml | 72 | Hardcoded `FontSize="11"`, no matching design token |
| 7 | **Low** | MainWindow.axaml | 34,155,211 | Hardcoded `FontSize="13"`, no matching design token |
| 8 | **Low** | Multiple dialogs | Various | Inline `FontSize="12"` should use `{StaticResource FontSizeSmall}` |
| 9 | **Low** | ConfirmBatchLaunchWindow.axaml | 45 | Misuse of `NameTileGap` (6) for spacing instead of `SpacingSmall` (8) |
| 10 | **Low** | Multiple dialogs | Various | Redundant FontFamily inline, could be extracted to DialogSharedStyles |
| 11 | **Low** | LauncherEntriesView.axaml | 252-424 | Complex DataTemplate per item in ListBox, per-item overhead |
| 12 | **Low** | LauncherEntriesView.axaml | 112,79 | Inner DataTemplates missing `x:DataType` on string items |
| 13 | **Low** | MainWindow.Drag.cs | 129-147,201-226 | `_isDragging` double-set, guarded by `_cleaningUp` re-entrancy flag |
| 14 | **Medium** | DiscoverAgentsWindow.axaml.cs | 178-338 | Missing `AutomationProperties` on programmatically created controls |
| 15 | **Medium** | ConfirmEntryDeleteWindow.axaml | 29-30 | `IsDefault` + `IsCancel` on same button (defensive, intentional) |
| 16 | **Medium** | ConfirmPresetDeleteWindow.axaml | 35-36 | `IsDefault` + `IsCancel` on same button (defensive, intentional) |
| 17 | **Low** | LauncherEntriesView.axaml | 108-136,305-332,459-486 | 3x duplicate ContextMenu definitions |

## Items Checked and Found Clean

- **Hardcoded hex colors**: NONE found. All 17 XAML files consistently use `{StaticResource xxxBrush}` references. White-listed: `FontFamily` strings, `LauncherDesignTokens.cs` C# constant hex strings (not in XAML).
- **Binding errors**: NONE found. All binding targets verified against `MainWindowViewModel`, `LauncherCardGroupViewModel`, `LauncherEntryViewModel`, `CommandManagementViewModel`, `DirectoryManagementViewModel`, `CommandRow`, `TemplateRow`, `DirectoryRow` properties.
- **x:DataType correctness**: Correct on all typed ViewModels except `DiscoverAgentsWindow` (Issue 2) and inner templates (Issue 12).
- **DataContext**: Correctly set on all ViewModel-backed windows (`MainWindow`, `CommandManagementWindow`, `DirectoryManagementWindow`, `DiscoverAgentsWindow`). Code-behind-only dialogs set DataContext implicitly via Window inheritance.
- **Empty states**: Present in all 3 view modes (Cards, Details, Names). Dialog error states present via toggled `IsVisible` TextBlocks.
- **Event handler memory leaks**: NONE found. `MainWindow` properly unsubscribes in `OnClosing`. Dialog windows are short-lived and get GC'd on close. `DiscoverAgentsWindow` subscribes to ViewModel events but ViewModel lifetime matches window lifetime.
- **Style/theme consistency**: Good overall. `LauncherDesignTokens` is the single source of truth for colors, spacing, and sizing. Hardcoded FontSize values (Issues 5-8) are the main inconsistency.
