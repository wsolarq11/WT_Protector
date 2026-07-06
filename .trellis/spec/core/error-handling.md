# Error Handling — Core Layer

> How errors are represented, validated, and surfaced across layers.

---

## Error Representation

### Exception-based, with structured messages

The project uses **exceptions for failures** and **structured Chinese-language status text** for user-facing messages. There is no custom `Result<T, E>` type — failures throw, success returns.

**Source:** `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` (status text classes)

### Status Text Pattern

Every user-facing outcome has a static factory method in a dedicated text class:

```csharp
public static class LauncherEntryStatus
{
    public static string Added(string entryName) =>
        $"已新增按钮: {entryName}（已保存）";
    // ...
}
```

**Text classes in `MainWindowViewModel.cs`:**

| Class | Responsibility |
|-------|---------------|
| `LauncherMainWindowText` | Ready, loaded, placeholder messages |
| `LauncherPersistenceStatus` | Save/rollback failure messages |
| `LauncherEntryStatus` | Add/edit/delete outcomes |
| `LauncherPresetStatus` | Preset CRUD outcomes |
| `LauncherSelectionStatus` | Selection change outcomes |
| `LauncherSettingsStatus` | Settings change outcomes |
| `LauncherSingleLaunchStatus` | Single launch outcomes |
| `LauncherSingleLaunchReceiptText` | Single launch receipt formatting |
| `LauncherBatchLaunchPlanText` | Batch plan preview |
| `LauncherBatchLaunchReceiptText` | Batch launch receipt |

### Rules for Status Text

1. **All status text in Chinese (简体中文)** — matches target user language.
2. **Status text uses static methods with typed arguments** — never string interpolation at call sites.
3. **Always use `ArgumentNullException.ThrowIfNull()`** on method arguments.
4. **Status text does NOT contain PII or secrets** — file paths are safe (config paths are user-visible).

---

## Validation Pattern

### Input Validation

**Source:** `LauncherStore.cs`, `LauncherEntryDrafts.cs`

```csharp
private static string RequireEntryId(string? id, int index, HashSet<string> seenIds)
{
    var normalized = EntryId.RequireEntryId(id, nameof(id));
    if (!seenIds.Add(normalized))
        throw new InvalidDataException($"Launcher config entry {index} id is duplicate: {normalized}.");
    return normalized;
}
```

**Rules:**
1. **Validate at the boundary** — `EntryId.Parse()`, `EntryName.Parse()` etc. validate at the point of creation. JSON deserialization handles structural validity; value objects handle semantic validity. Do NOT re-validate in the store layer. (Normalize pipeline removed 2026-07-06.)
2. **Throw `InvalidDataException`** for malformed config/user input.
3. **Throw `ArgumentException` / `ArgumentNullException`** for programming errors.
4. **Include index and field name** in error messages for pinpoint debugging.

### Parse Pattern (replaces Result<T,E>, 2026-07-06)

Value objects use a `Parse(throw)` / `Normalize(catch)` pattern instead of the old `Result<T, E>`:

```csharp
// Parse — throws FormatException on invalid input
public static EntryId Parse(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        throw new FormatException("ID 不能为空。");
    // ... validation ...
    return new EntryId(trimmed);
}

// Normalize — returns empty on failure (no throw)
public static string Normalize(string? value)
{
    try { return Parse(value).Value; }
    catch (FormatException) { return string.Empty; }
}
```

**Rules:**
1. `Parse` throws `FormatException` for invalid input — never returns null or uses Result<T,E>.
2. `Normalize` catches `FormatException` and returns `string.Empty` on failure.
3. JSON converters call `Parse` directly (let exception propagate as `InvalidDataException`).
4. Callers that need "throw on invalid" use `Parse`; callers that need "default on invalid" use `Normalize` or wrap in try/catch.

---

## Snapshot-Rollback Pattern → Stage-All-Then-Commit (2026-06-25)

**Source:** `LauncherEntryMutationSnapshot.Save()`

The old snapshot-rollback pattern (save config → on failure, rollback config; save presets → on failure, rollback both) has been replaced with **stage-all-then-commit**:

```csharp
// Stage: write all files to temp paths
var configTemp = configPath + $".stage-{Guid.NewGuid():N}";
LauncherJson.Save(configClone, configTemp, backup: false);
// ... repeat for presets, settings ...

// Commit: atomically rename all staged files
File.Move(configTemp, configPath, overwrite: true);
// ... repeat for presets, settings ...
```

**Key properties:**
- If staging fails, originals are untouched (no rollback needed).
- `File.Move` on the same NTFS volume is atomic.
- All three files are either fully committed or not — no cross-file inconsistency window.

**When snapshot-rollback is still appropriate:** Single mutations that modify in-memory ViewModel state before the save. The snapshot restores the ViewModel, not the files.

---

## Exception Catching Discipline

**Source:** `LauncherEntryMutationSnapshot.RestoreSnapshotOnPersistenceFailure()`

```csharp
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
```

**Rule:** Catch **specific** exception types, not bare `Exception`. Use `when` filters for precision.

**Exception type selection guide (2026-06-24):**

| Exception | When to use |
|-----------|-------------|
| `ArgumentException` | Programming error — wrong argument passed by developer |
| `InvalidDataException` | Corrupted/malformed data from file or user input |
| `InvalidOperationException` | Object is in wrong state for the requested operation |
| `IOException` | File I/O failure (disk full, permission denied, locked file) |

**CRITICAL:** Exception types are public API. Changing them requires auditing all `Assert.Throws<T>` tests.

---

## Exception Message Sanitization (2026-06-24)

**Source:** `MainWindowViewModel.ExceptionMessageFormatter`

Exceptions displayed in the UI status bar MUST be sanitized to avoid leaking file paths:

```csharp
internal static class ExceptionMessageFormatter
{
    public static string ForUser(Exception exception) => exception switch
    {
        FileNotFoundException => "配置文件无法访问",
        DirectoryNotFoundException => "配置目录无法访问",
        UnauthorizedAccessException => "文件访问权限不足",
        IOException => "文件操作失败",
        _ => exception.Message,
    };
}
```

**Rules:**
1. All status text methods taking `Exception` MUST route through `ExceptionMessageFormatter.ForUser()`.
2. Never use raw `exception.Message` in `StatusText = $"...{exception.Message}"`.
3. `FileNotFoundException.Message` includes the full file path — always sanitize.
4. `LauncherPersistenceStatus.Failed()` is the reference implementation.

---

## Forbidden Patterns

- ❌ **Bare `catch (Exception)`** — always filter to specific types.
- ❌ **Swallowing exceptions silently** — always log/display to user.
- ❌ **Returning `null` for "not found" without documentation** — use exceptions for unexpected absence.
- ❌ **English status text** — all user-facing text in Chinese.
- ❌ **Inline string concatenation for status** — use the static text factory method.
- ❌ **Raw `exception.Message` in UI** — use `ExceptionMessageFormatter.ForUser()`.
- ❌ **Catching `Exception` without `ExceptionFilters.IsRecoverable(ex)` in UI callbacks** — OOM/StackOverflow must propagate.
- ❌ **`File.Delete` in `finally` block without its own try/catch** — a cleanup failure replaces the original exception. Always wrap cleanup in try/catch.
- ❌ **`Debug.WriteLine` without `Trace.WriteLine` for diagnostics in Core layer** — `Debug.WriteLine` is stripped in Release builds. Core can't reference `ErrorLog` (App layer). Always pair with `Trace.WriteLine` for Release-safe diagnostics.

## Finally-Block Safety (2026-06-25)

Cleanup code in `finally` blocks must never throw, because a `finally` exception **replaces** the original exception from the `try` block:

```csharp
// CORRECT: wrap cleanup in its own try/catch
finally
{
    try
    {
        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }
    catch
    {
        // Best-effort cleanup — don't replace the original exception.
    }
}
```

## Release Build Diagnostics (2026-06-25)

Core layer (no App dependency) uses `Trace.WriteLine` as the Release-safe fallback:

```csharp
var msg = $"[LauncherJson] Backup rotation failed: {ex.Message}";
Debug.WriteLine(msg);   // Debug builds only
Trace.WriteLine(msg);   // Release builds with trace listener
```

App layer uses `ErrorLog.Write(ex, context)` which works in all build configurations.
