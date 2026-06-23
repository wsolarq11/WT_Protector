# Directory Structure — Views Layer

> How View files are organized in `WTLauncher.App/Views/`.

---

## File Layout

```
Views/
├── MainWindow.axaml                 # Main window XAML (three view modes)
├── MainWindow.axaml.cs              # Code-behind: dialog injection, keyboard, drag wiring
├── MainWindow.Drag.cs               # Drag-over / drop event handlers
├── MainWindowDragDropDecision.cs     # Pure drag-drop decision logic
├── MainWindowDragGhostFactory.cs     # Ghost card creation during drag
├── MainWindowEntryCardResolver.cs    # Card visual tree resolution
├── FastWrapPanel.cs                  # Custom panel for name-wall layout
├── ConfirmLaunchWindow.cs            # Single launch confirmation dialog
├── ConfirmBatchLaunchWindow.cs       # Batch launch confirmation dialog
├── ConfirmEntryDeleteWindow.cs       # Entry delete confirmation dialog
├── ConfirmPresetDeleteWindow.cs      # Preset delete confirmation dialog
├── EntryDetailsWindow.cs             # Add/edit entry dialog
├── PresetDetailsWindow.cs            # Save/update preset dialog
├── LauncherDialogButtons.cs          # Shared dialog button factory
└── LauncherDialogText.cs             # Shared dialog text factory
```

---

## File Ownership

| File | Ownership |
|------|-----------|
| `MainWindow.axaml` | XAML layout — three view modes, card templates, drag visual states |
| `MainWindow.axaml.cs` | Constructor, dialog injection, keyboard navigation, window lifecycle |
| `MainWindow.Drag.cs` | DragOver / Drop event handlers |
| `MainWindowDrag*.cs` | Pure drag logic extracted from code-behind (testable without window) |
| `*Window.cs` (dialog) | One dialog per user action — confirmation, editing |
| `LauncherDialog*.cs` | Shared dialog helpers — avoid duplication across dialog windows |

---

## Naming Conventions

- **Main window + helpers:** `MainWindow*.cs` — prefix pattern for window-scoped helpers.
- **Dialog windows:** `Confirm*Window.cs`, `*DetailsWindow.cs`.
- **Shared helpers:** `LauncherDialog*.cs` — `Launcher` prefix for View-layer shared code.

---

## Where to Put New Code

| What you're adding | Where |
|-------------------|-------|
| New dialog window | New `*Window.cs` + corresponding `.axaml` |
| Shared dialog logic | `LauncherDialogButtons.cs` or `LauncherDialogText.cs` |
| Drag visual behavior | `MainWindowDrag*.cs` new file |
| Keyboard shortcut | `MainWindow.axaml.cs` — `OnWindowKeyDown` |
| XAML style/DataTemplate | `MainWindow.axaml` |
