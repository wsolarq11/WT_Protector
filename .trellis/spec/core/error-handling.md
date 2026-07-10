# Error Handling — Core Layer

> How errors are represented, validated, and surfaced across layers.

---

## Error Representation

### Exception-based, with structured messages

The project uses **exceptions for failures** and **structured Chinese-language status text** for user-facing messages. There is no custom `Result<T, E>` type — failures throw, success returns.

**Source:** `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` (status text classes)

### Re-audit confirmation (2026-07-09, task 07-09-result-types-migration)

The 2026-07-09 schema-driven-architecture audit re-examined whether the exception flow should be migrated to `Result<T, StoreError>`. **Decision: keep exceptions.** The 2026-07-06 decision still holds.

**Why exceptions remain correct for this codebase:**
1. **Modern .NET practice (2025) confirms the split**: Result pattern for EXPECTED business-domain errors (validation, not-found, permission); exceptions for UNEXPECTED technical errors (I/O failure, contract violation, deserialization). Single-user launcher persistence failures are UNEXPECTED.
2. **No bug to cut**: the test suite (count evolves; run `dotnet test -c Release` for current numbers) passes, and the audit cited no specific bug caused by exception flow. "Exception-based control flow" was a misread — the 20+ `catch` sites are error boundaries, not control-flow branches.
3. **Cost > benefit**: ~50 throw sites + ~20 catch sites + ~50 tests would have to change to introduce a single-implementation `StoreError` abstraction (ponytail: no abstraction with one implementation).
4. **Layered discipline already in place**: value-object `Parse` (Parse-Don't-Validate), `ExceptionFilters.IsRecoverable` (fatal/recoverable classifier), `ExceptionMessageFormatter.ForUser` (UI sanitization), `LauncherPersistenceStatus.*` (typed status factories).

**When to revisit:** only if a high-frequency EXPECTED error path emerges (e.g., batch validation API, plugin dynamic loading). Until then, exceptions are correct.

**External references (2025 consensus):**
- [Microsoft Learn — Best practices for exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [Redgate Simple Talk — The Result Pattern in ASP.NET Core](https://www.red-gate.com/simple-talk/development/dotnet-development/the-result-pattern-in-asp-net-core-minimal-apis)

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

## Single-File Atomic Save (2026-07-09)

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs`

The store is a **single file** (`config/launcher.json`) owning entries + presets + settings + defaultPresetId together. The previous three-file stage-all-then-commit protocol (config.json + presets.json + settings.json) is gone — there is nothing to coordinate across files.

`LauncherJson.Save` is the single atomic write path:
1. Write JSON to `launcher.json.tmp-{Guid:N}`.
2. `File.Move(temp, launcher.json, overwrite: true)` — atomic on the same NTFS volume.
3. Rotate `.bak` → `.bak.1` → `.bak.2` → delete (copy-then-rotate, not rotate-then-copy).
4. `finally` block cleans up the temp file (wrapped in its own try/catch so cleanup failure cannot replace the original exception).

ViewModels call `LauncherJson.Save` after every mutation. If save throws, the in-memory ViewModel state has already been updated — callers capture a `LauncherEntryMutationSnapshot` BEFORE the mutation and restore from it on save failure.

**When snapshot-rollback is still appropriate:** Single mutations that modify in-memory ViewModel state before the save. The snapshot restores the ViewModel, not the files (the files are protected by the atomic write inside `LauncherJson.Save`).

---

## Exception Catching Discipline

**Source:** `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` (mutation command catch blocks)

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

### Catch Filter Convention (Gate-Enforced since `266aa6f`)

**Scope / Trigger:** Any `catch` clause in `src/avalonia-launcher/src/**/*.cs`. The `tools/verify.ps1` check `no-bare-catch-exception` rejects any `catch (Exception)` or `catch (Exception ex)` whose source line lacks a `when` filter. This is the architectural enforcement of the bare-catch invariant.

**Signatures:**

```csharp
// Narrow by exception type — preferred when the caller can enumerate the escapes
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
{
    // handle
}

// Orchestration-level — when escapes are not enumerable, narrow via classifier
catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
{
    ErrorLog.Write(ex, "<Scope>.<Action>");
    StatusText = "<user-facing 简体中文 message>";
}
```

**Contracts:**

| Field | Type | Constraint |
|-------|------|-----------|
| `ExceptionFilters.IsRecoverable(ex)` | `bool` | Returns `false` for `OutOfMemoryException`, `StackOverflowException`, `AccessViolationException`, `SEHException` (after unwrapping `AggregateException` / `TypeInitializationException`, max 16 levels). DEBUG builds rethrow `NullReferenceException` / `IndexOutOfRangeException` / `ArgumentNullException` as `InvalidOperationException` so logic bugs crash during development. |
| `ErrorLog.Write(ex, context)` | `void` | Writes to `%LOCALAPPDATA%/WTLauncher/errors.log`. Never throws. Internal narrow catches (`IOException \| UnauthorizedAccessException \| DirectoryNotFoundException`). Concurrent writes dropped via `Interlocked.Exchange`, not queued. |
| Scope string | `string` | `"<Component>.<Action>"` shape — e.g. `"DiscoverAgents.Scan"`, `"DiscoverAgents.Dialog"`. |

**Validation & Error Matrix:**

| Condition | Behavior |
|-----------|----------|
| Recoverable exception thrown inside try | `when` returns `true` → catch engages → status text set → state cleared in `finally` |
| Unrecoverable exception (OOM / SEH / AccessViolation) thrown | `when` returns `false` → catch skipped → exception propagates and crashes the process — intentional |
| Logic bug (NRE / IndexOutOfBounds / ArgNull) in DEBUG | `IsRecoverable` itself throws `InvalidOperationException` → forced crash during development |
| Logic bug in RELEASE | `IsRecoverable` returns `true` → catch engages → error logged → production stays up |

**Good / Base / Bad Cases:**

- **Good** — orchestration catch with classifier + log + status:
  ```csharp
  catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
  {
      ErrorLog.Write(ex, "DiscoverAgents.Scan");
      StatusText = "扫描过程中发生错误";
  }
  ```
- **Base** — specific exception filter when types are enumerable:
  ```csharp
  catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
  {
      ErrorLog.Write(ex, "LauncherJson.Save");
      // ...
  }
  ```
- **Bad** — bare catch (gate rejects at `tools/verify.ps1`):
  ```csharp
  catch (Exception) { /* swallow */ }      // ❌ gate fails
  catch (Exception ex) { ... }             // ❌ gate fails
  ```

**Tests Required:**

- **Recoverable-path test** — inject a recoverable exception through the seam (Func / mock / test hook), assert status text + state-cleared contract. Reference: `DiscoverAgentsViewModelTests.ScanAsync_WhenScanFuncThrowsRecoverable_SurfacesErrorAndClearsScanning` (`InvalidOperationException` injection → `StatusText == "扫描过程中发生错误"` + `IsScanning == false`).
- **Unrecoverable-path test** — NOT REQUIRED. C# language semantics of `when (false)` guarantee the catch is skipped; `ExceptionFilters.IsRecoverable` predicate behavior is independently verified. Adding a test that throws a reserved runtime exception (`OutOfMemoryException` / `SEHException` / `AccessViolationException`) triggers `CA2201`, enforced as error via `-warnaserror` — analyzer theater for a behavior the language already guarantees.

**Wrong vs Correct:**

```csharp
// WRONG — gate rejects; swallows OOM/SEH behind status string
catch (Exception)
{
    StatusText = "扫描过程中发生错误";
}

// CORRECT — narrowed via classifier; unrecoverable exceptions propagate
catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
{
    ErrorLog.Write(ex, "DiscoverAgents.Scan");
    StatusText = "扫描过程中发生错误";
}
```

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

- ❌ **Bare `catch (Exception)`** — gate-enforced via `tools/verify.ps1` `no-bare-catch-exception` check (since `266aa6f`). Filter to specific types or use `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))`.
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
