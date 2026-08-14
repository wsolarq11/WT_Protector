# Research: Exception Patterns

- **Query**: Check all .cs files for exception handling patterns: bare catch, custom exceptions, exception messages
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### Bare `catch (Exception)` with when Filter (Custom Analyzer WT1004)

The project has a custom Roslyn analyzer (WT1004) that enforces all `catch (Exception)` blocks must have a `when` filter. Every `catch (Exception)` in the codebase complies:

**All catch (Exception) blocks use `when` filters** — examples:
- `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` — Views/ViewModels layer
- `catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))` — Core layer
- `catch (Exception ex) when (ex is IOException or UnauthorizedAccessException ...)` — Type-filtered

**No bare `catch (Exception)` without `when` found.** All pass the WT1004 analyzer.

### Narrow Exception Types (Non-Exception catches)

The codebase correctly uses narrow exception types for non-`Exception` catches:
- `catch (IOException ex)` (AgentDiscoveryScanner.cs:103)
- `catch (UnauthorizedAccessException ex)` (AgentDiscoveryScanner.cs:107)
- `catch (FileNotFoundException)` (FileLauncherJsonIO.RotateBackups.cs:217)
- `catch (DirectoryNotFoundException)` (FileLauncherJsonIO.RotateBackups.cs:218)
- `catch (InvalidOperationException)` (AgentDiscoveryScanner.cs:203)
- `catch (FormatException)` (EntryName.cs:32, EntryId.cs:80, etc.)
- `catch (AbandonedMutexException)` (Program.cs:83)

All narrow-type catches are correctly justified by context.

**No violations found.**

### Custom Exception Naming

The project has only one custom exception-related type:
- `AppError` (Errors/AppError.cs) — ErrorOr-based, not an Exception subclass
- `LauncherErrors` (ErrorOrExtensions.cs) — static error factory, not an Exception class

The codebase does not define any custom Exception subclasses. All validation errors use `FormatException`, `ArgumentException`, `InvalidOperationException`, or `InvalidDataException`. This is consistent with the project's use of ErrorOr for error handling rather than custom exceptions.

**No naming violations found** (no custom exceptions to check).

### Exception Messages Are Descriptive

Exception messages throughout the codebase include the offending value and expected shape when applicable:

| File | Line | Message |
|---|---|---|
| LaunchArgumentBuilder.cs | 62 | `$"Launch entry '{name}' has an empty command."` |
| LaunchArgumentBuilder.cs | 78 | `$"Launch command line exceeds Windows limit of {MaxCommandLineLength} characters."` |
| EntryId.cs | 64 | `"ID 仅包含无效字符。"` |
| EntryName.cs | 24 | `"名称不能为空。"` |
| LauncherPresetValidator.cs | Various | Include offending values |

**No violations found.**

### Result: Throw / ErrorOr Return Consistency

Methods consistently use one of two patterns:
1. **Throw** for programmer errors (invalid arguments, corrupted state)
2. **Return ErrorOr** for recoverable domain failures (IO errors, validation)

This separation is clear and consistent throughout the codebase.

**No violations found.**

## Summary

| Category | Violations |
|---|---|
| Bare `catch (Exception)` without `when` | 0 violations |
| Narrow exception type catches | 0 violations |
| Custom exception naming | N/A (no custom exceptions) |
| Exception message descriptiveness | 0 violations |
| Throw vs ErrorOr consistency | 0 violations |
