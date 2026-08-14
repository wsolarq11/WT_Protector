# Research: File Structure

- **Query**: Check file structure rules: one class per file, file name matches class name, namespace matches folder path, no misplaced partial classes
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### One Class Per File (except small related types)

**V violations found: `MainWindow.axaml.cs` contains 6 classes/types.**

The file `src/avalonia-launcher/src/WTLauncher.App/Views/MainWindow.axaml.cs` defines:
1. `MainWindow` (line 17) — partial Window class
2. `MainWindowEntryClasses` (line 405) — public static class
3. `GridExtensions` (line 412) — internal static class
4. `MainWindowEntryRowDoubleTap` (line 429) — public static class
5. `MainWindowEntryCardContextMenu` (line 446) — public static class
6. `MainWindowVisualTree` (line 464) — internal static class

While `MainWindowEntryClasses`, `GridExtensions`, `MainWindowEntryRowDoubleTap`, `MainWindowEntryCardContextMenu`, and `MainWindowVisualTree` are small helper types, having 5 extra classes in one file violates the "one class per file" guideline, especially since the file is 477 lines long (the longest in the project).

**Minor finding: `DiscoverAgentsViewModel.cs` contains 2 classes:**
- `DiscoverAgentsViewModel` (line 20)
- `DiscoveredAgentRow` (line 203)

`DiscoveredAgentRow` is a small (26-line) related type, which is acceptable per the "except small related types" exception.

### File Name Matches Class Name

All file names match their primary class/record/struct names. The partial class files (e.g., `MainWindowViewModel.Batch.cs`, `MainWindowViewModel.Drag.cs`) correctly use dotted naming to indicate they extend the same class.

**No violations found.**

### Namespace Matches Folder Path

| Directory | Namespace | Status |
|---|---|---|
| `src/WTLauncher.Core/` | `WTLauncher.Core` | Correct |
| `src/WTLauncher.App/ViewModels/` | `WTLauncher.App.ViewModels` | Correct |
| `src/WTLauncher.App/Views/` | `WTLauncher.App.Views` | Correct |
| `src/WTLauncher.App/Views/Controls/` | `WTLauncher.App.Views.Controls` | Correct |
| `src/WTLauncher.App/Infrastructure/` | `WTLauncher.App.Infrastructure` | Correct |
| `src/WTLauncher.App/Features/Entries/` | `WTLauncher.App.Features.Entries` | Correct |
| `src/WTLauncher.App/Features/Presets/` | `WTLauncher.App.Features.Presets` | Correct |
| `src/WTLauncher.App/Features/Dialogs/` | `WTLauncher.App.Features.Dialogs` | Correct |
| `src/WTLauncher.App/Features/Status/` | `WTLauncher.App.Features.Status` | Correct |
| `src/WTLauncher.App/Platform/` | `WTLauncher.App.Platform` | Correct |
| `src/WTLauncher.Analyzers/` | `WTLauncher.Analyzers` | Correct |
| `tests/WTLauncher.Tests/` | `WTLauncher.Tests` | Correct |
| `src/WTLauncher.Core/Errors/` | `WTLauncher.Core` (intentional — AppError.cs) | Correct |

**No violations found.**

### File-Scoped vs Block-Scoped Namespaces

The .editorconfig enforces `csharp_style_namespace_declarations = file_scoped:warning`. Every source file correctly uses file-scoped namespaces (i.e., `namespace X.Y;` without braces).

**No violations found.**

### Partial Class Files — Location Check

All partial class files are correctly located adjacent to their primary declaration file within the same directory. For example:
- `MainWindowViewModel.cs` + `MainWindowViewModel.Batch.cs` + `MainWindowViewModel.Drag.cs` — all in `ViewModels/`
- `MainWindow.axaml.cs` + `MainWindow.Drag.cs` — both in `Views/`

**No misplaced partial class files found.**

## Summary

| Category | Violations |
|---|---|
| One class per file | 1 violation (6 classes in MainWindow.axaml.cs) |
| File name matches class name | 0 violations |
| Namespace matches folder | 0 violations |
| File-scoped namespaces | 0 violations |
| Partial class locations | 0 violations |
