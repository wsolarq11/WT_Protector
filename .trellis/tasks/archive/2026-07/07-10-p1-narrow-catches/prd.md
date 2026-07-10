# P1: Narrow 9 bare/wide catches

## Goal

Eliminate all bare-catch (`catch { }`) and unfiltered wide-catch (`catch (Exception ex)` without `when`)
sites that violate the project's hard rule: "catch (Exception) is forbidden. Narrow to the known exception types."

## Background

CLAUDE.md mandates narrow catches. Bare catches silently swallow real bugs (see pitfalls §6.12).
Audit identified 13 sites needing repair.

## Sites (verified via grep)

| # | File:line | Current | Required fix |
|---|-----------|---------|--------------|
| 1 | `LauncherJson.cs:157` | `catch { /* best-effort */ }` on `File.Delete(bakTempPath)` | narrow → IOException \| UnauthorizedAccessException \| DirectoryNotFoundException; log via Debug/Trace |
| 2 | `LauncherStore.cs:139` | `catch { /* use defaults */ }` legacy settings load | narrow → JsonException \| IOException \| FileNotFoundException; log |
| 3 | `LauncherStore.cs:156` | `catch { /* use empty presets */ }` legacy presets load | same as #2 |
| 4 | `LauncherStore.cs:181` | `catch { /* best-effort */ }` TryMoveToBak | narrow → IOException \| UnauthorizedAccessException |
| 5 | `MainWindowViewModel.cs:166` | `catch { /* best-effort */ }` LoadPresetFile retry | narrow → ExceptionFilters.IsRecoverable + ErrorLog |
| 6 | `MainWindowViewModel.cs:243` | `catch (Exception ex)` no filter, SaveStoreFile | add `when (ExceptionFilters.IsRecoverable(ex))`; rethrow unrecoverable |
| 7 | `MainWindowViewModel.Presets.cs:225` | `catch { storeFile = new LauncherStoreFile(); }` | narrow + ErrorLog |
| 8 | `MainWindow.axaml.cs:302` | `catch (Exception ex)` OnClosing | add `when (ExceptionFilters.IsRecoverable(ex))` |
| 9 | `MainWindow.Drag.cs:50,103,119,139` | 4 sites `catch (Exception ex)` | each add `when (ExceptionFilters.IsRecoverable(ex))` |
| 10 | `ErrorLog.cs:108` | `catch (Exception ex)` RotateIfNeeded | narrow → IOException \| UnauthorizedAccessException \| FileNotFoundException |
| 11 | `Program.cs:148-151` | `catch { }` DeleteStaleTempFiles | narrow → IOException \| UnauthorizedAccessException |
| 12 | `App.axaml.cs:117-121` | `catch { }` config-dir write-test | narrow → IOException \| UnauthorizedAccessException \| SecurityException \| DirectoryNotFoundException |
| 13 | `ErrorLog.cs:63,71` | nested `catch { }` for logging fallback | true trust boundary; narrow to IOException \| UnauthorizedAccessException; comment semantics |

## Strategy

- Each `catch { }` becomes a typed narrow catch. Use pattern matching `when (ex is X or Y or Z)`.
- Where current semantic is "best-effort fallback" (LauncherStore legacy loads), keep fallback behavior but log.
- Where current semantic is "log + recover" (VM/View layer), use `ExceptionFilters.IsRecoverable`.
- `ErrorLog` internal catches stay typed (last-resort logging) but narrow to IO types + commented.

## Acceptance criteria

- [ ] `grep -E "catch\s*(\{|\(\s*\))" src/` returns 0 unfiltered bare catches.
- [ ] `grep -E "catch\s*\(Exception(\s+ex)?\s*\)(?!\s*when)" src/` returns 0 unfiltered wide catches.
- [ ] `dotnet build -c Release` 0 warnings / 0 errors.
- [ ] `dotnet test -c Release` all 453 pass.
- [ ] No behavioral regression (recovery paths still trigger on corrupt JSON / missing file / locked store).

## Out of scope

- New exceptions, new error codes.
- Changing exception filter semantics in `ExceptionFilters.IsRecoverable`.
- Adding new tests for catch behavior (existing 453 tests already exercise recovery paths).
