# Research: Comprehensive Violations Summary

- **Query**: Consolidated summary of all coding standard violations found
- **Scope**: Internal (all categories)
- **Date**: 2026-07-26

## All Violations Found

### Category 1: Naming Conventions

**No violations found.** All naming rules per .editorconfig are consistently followed:
- Private fields: `_camelCase` (100% compliance)
- Interfaces: `I` prefix (100%)
- Const fields: PascalCase (100%)
- Public members: PascalCase (100%)

### Category 2: File Structure

**1 violation:** Multiple classes in MainWindow.axaml.cs

| File | Line | Issue |
|---|---|---|
| `src/.../Views/MainWindow.axaml.cs` | 405 | `MainWindowEntryClasses` — 5th class in file |
| `src/.../Views/MainWindow.axaml.cs` | 412 | `GridExtensions` — 6th class in file |
| `src/.../Views/MainWindow.axaml.cs` | 429 | `MainWindowEntryRowDoubleTap` — 7th class in file |
| `src/.../Views/MainWindow.axaml.cs` | 446 | `MainWindowEntryCardContextMenu` — 8th class in file |
| `src/.../Views/MainWindow.axaml.cs` | 464 | `MainWindowVisualTree` — 9th class in file |

Total: 9 classes in one file (MainWindow + 5 static classes + GridExtensions + 2 public classes).

### Category 3: Modifier Order

**No violations found.** All modifiers follow the correct ordering per .editorconfig.

### Category 4: Bracing

**Style observation (50+ instances):** Single-line `if` statements without braces are pervasive throughout the codebase. This is flagged by IDE0011 at `suggestion` level (not build-blocking per .editorconfig). Key representative examples:

| File | Line | Code |
|---|---|---|
| `App.axaml.cs` | 102-103 | `if (_isInitialized) return;` |
| `AgentDiscoveryScanner.cs` | 218-219 | `if (firstLine is null) return null;` |
| `Program.cs` | 29-30 | `if (ex is not null) ErrorLog.Write(ex, ...)` |
| `EntryName.cs` | 23-24 | `if (normalized.Length == 0) throw ...` |
| `EntryId.cs` | 63-64 | `if (trimmed.Length == 0) throw ...` |
| `EntryId.cs` | 162-163 | `if (!allowed) throw ...` |
| `ErrorOrExtensions.cs` | 29-30 | `if (ex is AggregateException agg) ex = ...;` |
| `LaunchArgumentBuilder.cs` | 59-60 | `if (name.Length == 0) throw ...` |
| `LauncherCommandTemplateResolver.cs` | 23-24, 30-31 | `if (templates is null) return command;` |
| `PresetService.cs` | 58-59 | `if (presetName is null) return ...` |
| `DiscoverAgentsViewModel.cs` | 97-98 | `if (IsScanning) return;` |

### Category 5: String Comparison

**2 violations:** Direct string `==` comparison without `StringComparison` (functionally Ordinal in .NET 5+, but inconsistent with explicit pattern used elsewhere):

| File | Line | Code |
|---|---|---|
| `MainWindow.axaml.cs` | 105 | `if (w.WindowState == "Maximized")` |
| `MainWindow.axaml.cs` | 131 | `if (mode == "acrylic")` |

### Category 6: Null Checking

**1 violation:** Inconsistent null-checking pattern (manual `?? throw` instead of `ArgumentNullException.ThrowIfNull`):

| File | Lines | Issue |
|---|---|---|
| `EntryListViewModel.cs` | 73-76 | 4 lines use `?? throw new ArgumentNullException(nameof(...))` instead of `ArgumentNullException.ThrowIfNull(...)` |

### Category 7: Exception Patterns

**No violations found.** All `catch (Exception)` blocks use `when` filters. No bare catches. Exception messages are descriptive.

### Category 8: Task/Async Patterns

**No violations found.** No `Task.Result` or `Task.Wait()`. `async void` limited to event handlers. ConfigureAwait(true) consistent.

### Category 9: Thread Safety

**No violations found.** No `lock(this)` or `lock(typeof(T))`. Volatile correctly used on `_hasCached`. SemaphoreSlim correctly used for async-compatible synchronization.

### Category 10: Disposable Pattern

**3 violations:** Missing `GC.SuppressFinalize(this)` in IDisposable implementations:

| File | Line | Class |
|---|---|---|
| `EntryProjection.cs` | 96 | `EntryProjection` |
| `FileLauncherJsonIO.cs` | 222 | `FileLauncherJsonIO` |
| `StoreGrain.cs` | ~115 | `Subscription` (nested class) |

These missing `SuppressFinalize` calls are technically harmless (no finalizers exist), but create inconsistency with ViewModelBase and DisposableAction which do call it.

## Summary Count

| Severity | Count | Categories |
|---|---|---|
| **High (structurally incorrect)** | 0 | — |
| **Medium (violates documented rule)** | 6 | File structure (1), String comparison (2), Null checking (1), Disposable (3) |
| **Low (style, suggestion-level)** | 50+ | Single-line if without braces (IDE0011 suggestion) |
