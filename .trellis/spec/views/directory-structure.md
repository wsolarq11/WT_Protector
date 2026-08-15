# Directory Structure — Views Layer

> How View files are organized in `WTLauncher.App/Views/`.

---

## File Layout

`Views/` — 44 files total (24 C# + 20 AXAML). Counts are machine-verified against
`AGENTS.md` 结构基线 by gate check `structure-baseline-consistency`.

```
Views/
├── MainWindow.axaml / .axaml.cs      # Main window: XAML (three view modes) + code-behind
├── MainWindow.Drag.cs                # Drag-over / drop event handlers + ghost factory
├── MainWindowEntryCardResolver.cs    # Card visual tree resolution
├── LauncherControlBar.axaml(.cs)     # Header batch controls bar
├── LauncherEntriesView.axaml(.cs)    # Entries view host
├── FastWrapPanel.cs                  # Custom panel for name-wall layout
├── DialogService.cs                  # Dialog injection helper
├── LauncherDialogText.cs             # Shared dialog text factory
├── Dialogs/DialogSharedStyles.axaml  # Shared dialog styling
├── Controls/StatusBar.axaml(.cs)     # Status bar control
├── CommandEditDialog.axaml(.cs)      # Command template editor dialog
├── CommandManagementWindow.axaml(.cs)# Command management window
├── ConfirmBatchLaunchWindow.axaml(.cs)# Batch launch confirmation
├── ConfirmEntryDeleteWindow.axaml(.cs)# Entry delete confirmation
├── ConfirmLaunchWindow.axaml(.cs)    # Single launch confirmation
├── ConfirmPresetDeleteWindow.axaml(.cs)# Preset delete confirmation
├── DirectoryEditDialog.axaml(.cs)    # Directory edit dialog
├── DirectoryManagementWindow.axaml(.cs)# Directory management window
├── DiscoverAgentsWindow.axaml(.cs)   # Agent discovery window
├── EntryDetailsWindow.axaml(.cs)     # Add/edit entry dialog
├── ImportMigrationWindow.axaml(.cs)  # Import / migration window
├── PresetDetailsWindow.axaml(.cs)    # Save/update preset dialog
├── QuickLaunchWindow.axaml(.cs)      # Quick-launch window
├── RunHistoryWindow.axaml(.cs)       # Run-history window
└── UpdateCheckWindow.axaml(.cs)      # Update-check window
```

---

## File Ownership

| File | Ownership |
|------|-----------|
| `MainWindow.axaml` | XAML layout — three view modes, card templates, drag visual states |
| `MainWindow.axaml.cs` | Constructor, dialog injection, keyboard navigation, window lifecycle |
| `MainWindow.Drag.cs` | DragOver / Drop event handlers + ghost card factory |
| `MainWindowEntryCardResolver.cs` | Visual-tree hit-test resolution for entry cards |
| `LauncherControlBar.axaml(.cs)` | Header bar with batch controls / view-mode switching |
| `LauncherEntriesView.axaml(.cs)` | Entries list/card host |
| `*Window.axaml(.cs)` / `*Dialog.axaml(.cs)` | One window/dialog per user action — confirmation, editing, management |
| `LauncherDialogText.cs`, `DialogService.cs` | Shared dialog helpers — avoid duplication across dialog windows |
| `Controls/`, `Dialogs/` | Reusable controls and shared dialog styling |

---

## Naming Conventions

- **Main window + helpers:** `MainWindow*.cs` — prefix pattern for window-scoped helpers.
- **Dialog windows:** `Confirm*Window.cs`, `*DetailsWindow.cs`, `*EditDialog.axaml(.cs)`.
- **Shared helpers:** `LauncherDialog*.cs` — `Launcher` prefix for View-layer shared code.

---

## Where to Put New Code

| What you're adding | Where |
|-------------------|-------|
| New dialog window | New `*Window.axaml(.cs)` (or `*Dialog.axaml(.cs)`) + corresponding code-behind |
| Shared dialog logic | `LauncherDialogText.cs` or `DialogService.cs` |
| Drag visual behavior | `MainWindow.Drag.cs` |
| Keyboard shortcut | `MainWindow.axaml.cs` — `OnWindowKeyDown` |
| Reusable control | `Controls/` |
| XAML style/DataTemplate | `MainWindow.axaml` |
