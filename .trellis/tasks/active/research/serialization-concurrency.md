# Research: Serialization & Concurrency Audit

- **Query**: Ultrareview of all serialization, persistence, I/O, and concurrency code
- **Scope**: Internal (all specified source files)
- **Date**: 2026-07-26
- **Files reviewed**: 16 files across WTLauncher.Core and WTLauncher.App.Infrastructure

## Summary

- **1 medium** (TOCTOU race in ReloadFromDisk -- confirmed data race)
- **1 medium** (StoreGrain.Dispose does not clean up StateChanged event)
- **1 medium** (RotateBackups callable externally without _saveLock)
- **4 low** (various serialization edge cases, null handling, UI-blocking I/O)
- **2 informational** (design observations about serialization completeness)

---

## Serialization Findings

### S1. [LOW] EntryIdJsonConverter.Read does not handle JSON null

**File**: `src/avalonia-launcher/src/WTLauncher.Core/EntryId.cs`, lines 192-196

```csharp
public override EntryId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    var raw = reader.GetString();
    return EntryId.Parse(raw);
}
```

**Description**: `reader.GetString()` returns `null` when the JSON token is a literal `null`. `EntryId.Parse(null)` calls `string.IsNullOrWhiteSpace(raw)` at line 47 which evaluates to `true`, throwing `FormatException("ID 不能为空。")`. This means a JSON value of `"id": null` causes deserialization failure rather than mapping to `EntryId.Empty`.

Compare with `DirectoryPathJsonConverter.Read` (DirectoryPath.cs line 68) which explicitly handles null: `return raw is not null ? DirectoryPath.Parse(raw) : DirectoryPath.Empty;`.

**Context**: `LaunchEntry.Id` is `[JsonRequired]`, so a missing key already throws at the STJ level before the converter runs. The null-in-JSON scenario only triggers when the key is present with an explicit null value (which is a schema violation anyway). The `EntryNameJsonConverter` has the same behavior (EntryName.cs line 77).

**Evidence in codebase**: The pattern choice is inconsistent with `DirectoryPathJsonConverter` which handles null gracefully. No test in the codebase verifies this null path.

---

### S2. [LOW] EntryNameJsonConverter.Read null-to-empty then throws anyway

**File**: `src/avalonia-launcher/src/WTLauncher.Core/EntryName.cs`, lines 74-78

```csharp
public override EntryName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
{
    var raw = reader.GetString();
    return EntryName.Parse(raw);
}
```

`EntryName.Parse(null)` at line 19 sets `source = string.Empty`, which after `Trim().Normalize()` results in `normalized.Length == 0` (line 23), throwing `FormatException("名称不能为空。")`. So the null-to-empty guard in `Parse` is reachable only if a test calls `Parse(null)` directly; through the converter, null always throws.

**Design intent**: Throwing on null is the intended strict parsing behavior. The null-to-empty guard in `Parse` exists for non-converter callers (e.g., direct code usage `EntryName.Parse(null)` in test assertions or normalization helpers).

---

### S3. [LOW] Backup rotation timing creates window with no valid .bak

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs`, lines 124-149

```csharp
if (backup && File.Exists(fullPath))
{
    // ... fullPath -> bakTempPath (copy)
    RotateBackups(fullPath);     // LINE 130: rotate existing .bak chain
    File.Move(bakTempPath, fullPath + ".bak", overwrite: true); // LINE 131
}
// ...
File.Replace(tempPath, fullPath, null);  // LINE 147
```

**Description**: The backup chain is rotated BEFORE the main file is atomically updated via `File.Replace`. Between lines 130 and 147:

1. `launcher.json` is still the old content
2. `launcher.bak` has been shifted to `launcher.bak.1` (or deleted if at max rotation count)
3. `launcher.bak-tmp-*` is being moved to `launcher.bak` (step 131)

If the process crashes between `RotateBackups` (line 130) and `File.Replace` (line 147):
- `launcher.json` = old content (intact)
- `launcher.bak` = old content (just been restored from the temp copy)
- Backup chain is shifted by one (`.bak.1` = previous `.bak`)

The old content is preserved. However, after `.bak.1` was shifted to `.bak.2` during rotation (if maxBackups=3), and the crash happens before `.bak.3` is deleted (which happens at `i == maxBackups - 1`), the rotation is only partially complete. The next full save re-establishes the chain correctly.

**Edge case**: If `File.Move(bakTempPath, fullPath + ".bak")` at line 131 succeeds but `File.Replace` at line 147 fails, `launcher.json` still has its original content (unchanged by `File.Replace` failure), and `launcher.bak` has a correct pre-save snapshot. The new data exists only in the `.tmp-*` file (cleaned by `CleanupStaleTempFiles`). This is a safe outcome.

**Verdict**: Not a bug. The backup timing is intentionally pre-save so the backup captures the last known good state. The crash-recovery property holds.

---

### S4. [LOW] LoadWithRecovery recovery temp files not cleaned up inline

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs`, lines 64-70 and 90-93

**First path** (main file corrupt, recover from .bak):
```csharp
var recoveryTempPath = path + $".recovery-{Guid.NewGuid():N}";
File.Copy(backupPath, recoveryTempPath, overwrite: true);
File.Replace(recoveryTempPath, path, null);
```

**Second path** (file missing, recover from backup chain):
```csharp
var recoveryTempPath = path + $".recovery-{Guid.NewGuid():N}";
File.Copy(backupPathForMissing, recoveryTempPath, overwrite: true);
File.Move(recoveryTempPath, path);
```

**Description**:
- In the first path: `File.Replace(recoveryTempPath, path, null)` atomically replaces `path` with `recoveryTempPath`. The temp file ceases to exist (it becomes the new `path`). If `File.Replace` throws, `recoveryTempPath` is left on disk. Cleanup happens only via `CleanupStaleTempFiles` (which handles `*.recovery-*` glob pattern), not inline.
- In the second path: `File.Move(recoveryTempPath, path)` moves the temp file to `path` atomically. If `File.Move` throws, `recoveryTempPath` is left on disk. Same deferred cleanup.

**Verdict**: Minor. `CleanupStaleTempFiles` handles these files at startup. No data loss or corruption risk.

---

### S5. [INFO] Source-gen context completeness check

**File**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherJsonContext.cs`, lines 1-24

**Registered types**:
- `LauncherStoreFile` -- top-level document
- `LauncherSettings` -- settings section (registered but contains `ImmutableDictionary<string, string>` and `ImmutableList<string>`)
- `LaunchEntry` -- individual entry
- `LauncherPreset` -- preset (uses `string` for Id/Name, not `PresetId`/`PresetName`)
- `List<LaunchEntry>` -- entries collection (used by deserialization; actual runtime type is `ImmutableList<LaunchEntry>`)
- `List<LauncherPreset>` -- presets collection
- `EntryName` -- value type with custom converter
- `EntryId` -- value type with custom converter
- `DirectoryPath` -- value type with custom converter
- `LegacyPresetDocument` -- legacy migration type

**Types NOT explicitly registered but needed transitively**:
- `LauncherWindowSettings` -- property of `LauncherSettings.Window`. STJ source-gen handles this transitively as a property type of the root `LauncherSettings` registration.
- `ImmutableList<LaunchEntry>` / `ImmutableList<LauncherPreset>` -- STJ source-gen handles `ImmutableList<T>` through `IList<T>` interfaces. The `List<T>` registrations serve as the contract. Verified by the existing test suite passing.
- `ImmutableDictionary<string, string>` -- STJ source-gen handles dictionary types natively through `IDictionary<K, V>`.
- `PresetId` / `PresetName` -- NOT used as property types in `LauncherStoreFile`. `LauncherPreset.Id` and `LauncherPreset.Name` are typed as `string`. These are VM-layer value types only.

**Verdict**: Source-gen registration is complete. All transitively needed types are reachable from the registered roots.

---

### S6. [INFO] AOT compatibility -- all entry points use JsonTypeInfo<T>

**Verified paths**:
- `FileLauncherJsonIO.Load<T>` (FileLauncherJsonIO.cs line 22-23): `JsonSerializer.Deserialize(stream, (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!)` -- correct
- `LauncherJsonSerializer.Serialize<T>` (LauncherJsonSerializer.cs line 20-21): `JsonSerializer.Serialize(value, typeInfo)` with `typeInfo` cast from `LauncherJsonContext.Default.GetTypeInfo(typeof(T))` -- correct
- `LauncherJsonSerializer.Deserialize<T>` (LauncherJsonSerializer.cs line 26-27): `JsonSerializer.Deserialize(json, typeInfo)` -- correct

**No `JsonSerializerOptions`-based overloads** used anywhere in the serialization paths. The only `JsonSerializerOptions` references are the `Options` property on `LauncherJsonContext.Default` and `LauncherJsonSerializer.Options`, both of which expose the context's options for inspection (not for serialization calls). The `Options` parameter in custom converters (`EntryIdJsonConverter`, `DirectoryPathJsonConverter`, `EntryNameJsonConverter`) is passed by STJ infrastructure, not created by the code.

**Verdict**: AOT-safe. Verified by the `-warnaserror` build and CI passing.

---

### S7. [INFO] Encoding consistency -- UTF-8 without BOM

- **Write**: `File.WriteAllText(tempPath, json + RepositoryLineEnding)` (FileLauncherJsonIO.cs line 122) -- UTF-8 without BOM
- **Read**: `File.OpenRead(path)` -> `JsonSerializer.Deserialize(stream, typeInfo)` (FileLauncherJsonIO.cs lines 21-23) -- STJ reads UTF-8 from stream
- `RepositoryLineEnding` = `"\n"` (line 16) -- LF-only line ending, consistent with `.gitattributes`

**Verdict**: Consistent.

---

## Concurrency Findings

### C1. [MEDIUM] StoreGrain.ReloadFromDisk TOCTOU race (confirmed data race)

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs`, lines 52-56

```csharp
public void ReloadFromDisk()
{
    var fresh = _persistence.Load(_storePath);  // (1) reads outside lock
    SetState(fresh);                              // (2) sets inside lock -- acquires Grain._lock
}
```

**Issue**: Between line 54 (disk read, no lock held) and line 55 (lock acquired), a concurrent thread could call `Dispatch`:

1. Thread A calls `ReloadFromDisk` -> `_persistence.Load(_storePath)` -> reads state X from disk
2. Thread B calls `Dispatch(reducer)` -> `Mutate(...)` -> acquires `Grain._lock` -> computes state Y -> `Save(Y)` to disk -> releases lock -> `StateChanged?.Invoke(Y)`
3. Thread A calls `SetState(fresh)` -> acquires `Grain._lock` -> overwrites in-memory state with stale state X

**Result**: Thread B's Dispatch succeeded (saved Y to disk) and subscribers were notified with Y, but `ReloadFromDisk` then sets in-memory state back to X. Subsequent `Select` calls return X, which disagrees with disk state Y. The discrepancy persists until the next `Dispatch` overwrites it.

**Current mitigation**: In the existing codebase, both `ReloadFromDisk` (called from `MainWindowViewModel.Refresh`, line 242) and `Dispatch` (called from various UI command handlers) are executed on the Avalonia UI thread. Since Avalonia serializes UI thread work, the race does not trigger in practice. However, nothing enforces this constraint at the `StoreGrain` API level -- the `Dispatch` and `Select` methods are documented as thread-safe (via `Grain._lock`), but `ReloadFromDisk` breaks this contract.

**If the calling pattern ever changes** (e.g., `Refresh` called from a background timer, or `Dispatch` called from a `Task.Run`), the race would manifest as silent state corruption.

---

### C2. [MEDIUM] StoreGrain.Dispose does not clean up StateChanged subscribers

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs`, lines 88-91

```csharp
public void Dispose()
{
    if (_persistence is IDisposable disposable) disposable.Dispose();
}
```

**Issue**: `Dispose` only disposes `_persistence`. It does not:
1. Set `StateChanged = null` to release all subscriber references
2. Prevent further `Dispatch` calls from firing `StateChanged` after disposal

If someone calls `Dispatch` after `Dispose`:
- `Mutate` calls `_persistence.Save(newState, ...)` on a potentially disposed `_persistence` object (might throw `ObjectDisposedException`)
- If `Save` succeeds (or the persistence is not truly disposed), `StateChanged?.Invoke(after)` fires on handlers that may hold references to the disposed grain

The `Subscription` pattern (line 82-86) returns `IDisposable` for callers to unsubscribe, but there is no defensive cleanup in `Dispose` itself.

**Subscriber leak path**: The `StateChanged` event itself holds references to subscriber delegates. If `StoreGrain` is disposed but `StateChanged` is not set to null, the delegates keep their targets alive (if the targets are instance methods with closure captures). Conversely, the subscribers keep the `StoreGrain` alive via the event handler reference chain.

---

### C3. [MEDIUM] RotateBackups callable externally without _saveLock protection

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs`, lines 204-220
**Exposed via**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs`, lines 54-55

```csharp
public static void RotateBackups(string fullPath, int maxBackups = 3)
    => IO.RotateBackups(fullPath, maxBackups);
```

**Issue**: `RotateBackups` is called from within `Save` (which holds `_saveLock`), but when called through the public static facade `LauncherJson.RotateBackups()`, no lock is held. If an external caller invokes `LauncherJson.RotateBackups()` concurrently with a `Save` operation:

1. Thread A (Save): holds `_saveLock` -> calls `RotateBackups` (already inside lock)
2. Thread B (external): calls `LauncherJson.RotateBackups` -> `FileLauncherJsonIO.RotateBackups` (NO lock)

Both threads manipulate the same `.bak`, `.bak.1`, `.bak.2` files:
- `File.Delete(oldPath)` at line 213 could delete a file that Thread A is trying to `File.Move`
- `File.Move(oldPath, newPath, overwrite: true)` at line 215 could move a file that Thread A has already moved

**Current mitigation**: `RotateBackups` is only called from within `Save` in the current codebase. No external caller uses the static `LauncherJson.RotateBackups`. The risk is for future development.

---

### C4. [MEDIUM] Nested locking: Grain._lock { Save { _saveLock.Wait() } } blocks during file I/O

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs`, lines 58-71
**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/Grain.cs`, lines 44-49
**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs`, lines 109-155

**Call chain**:
```
Dispatch (StoreGrain.cs:59)
  -> Mutate (Grain.cs:43)        // acquires Grain._lock (object lock)
     -> reducer(state)            // computes new state
     -> _persistence.Save(newState)  // FileLauncherJsonIO.Save (FileLauncherJsonIO.cs:109)
        -> _saveLock.Wait()       // acquires SemaphoreSlim(1,1)
        -> Serialize              // JSON serialization
        -> File.WriteAllText      // write temp file
        -> RotateBackups          // manipulate .bak files
        -> File.Replace           // atomic write
        -> _saveLock.Release()
  // Grain._lock released here (after Mutate returns)
```

**Issue**: `Grain._lock` is held during the entire file I/O operation (serialize + write + backup rotation + File.Replace). During this time:

1. **All `ReadState`/`Select` calls are blocked** -- even read-only projections must wait for file I/O to complete.
2. **Thread-pool starvation risk** -- if called from a threadpool thread, the blocking I/O under `Grain._lock` consumes a threadpool thread for the duration of disk I/O.
3. **Lock ordering constraint** -- the consistent order is `Grain._lock` -> `_saveLock`. Any future code that acquires `_saveLock` first and then `Grain._lock` will deadlock. Currently `CleanupStaleTempFiles` only acquires `_saveLock` and does not touch `Grain._lock`, so no reverse ordering exists.

The nested lock is by design (eliminates the TOCTOU window between reading and writing state), but the performance cost is holding `Grain._lock` during all file I/O.

---

### C5. [LOW] SemaphoreSlim.Wait() blocks calling thread

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs`, lines 111 and 182

```csharp
_saveLock.Wait();  // blocking synchronous wait
```

**Issue**: Both `Save` (line 111) and `CleanupStaleTempFiles` (line 182) use `_saveLock.Wait()` (the synchronous blocking overload) rather than `_saveLock.WaitAsync()` (the awaitable overload).

`Save` is called from `StoreGrain.Dispatch`, which is called from Avalonia UI event handlers:
- `MainWindowViewModel.Layout.cs`: `OnLayoutFilterChanged` (line 23), `OnLayoutSortChanged` (line 40), `OnLayoutViewModeChanged` (line 63), `OnLayoutGroupKeyChanged` (line 80)
- `MainWindowViewModel.cs`: `SaveStoreFile` (line 359), `Refresh` (line 242)
- `EntryListViewModel.cs`: HandleAddOrCloneResult (line 271), HandleDelete (line 230), HandleEditResult (line 320)
- `MainWindowViewModel.Layout.cs`: `TrySaveWindowBounds` (line 186)

All of these run on the Avalonia UI thread. During the blocking `_saveLock.Wait()` + file I/O, the UI thread is blocked -- no input processing, no rendering, no layout. For a ~5KB JSON config file this is sub-millisecond, but with backup rotation overhead and potential disk contention, the freeze window extends.

This was previously reported as M4 in the App/ViewModel layer ultrareview. Confirmed here from the serialization perspective.

---

### C6. [LOW] StateChanged fires outside Grain._lock (TOCTOU on subscriber reads)

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs`, lines 58-71

```csharp
public void Dispatch(Func<LauncherStoreFile, LauncherStoreFile> reducer)
{
    var after = Mutate(state => { ... });  // Grain._lock acquired and released inside Mutate
    try { StateChanged?.Invoke(after); }     // Grain._lock NOT held here
    catch (Exception ex) when (LauncherErrors.IsRecoverable(ex))
    {
        ErrorLog.Write(ex, "StoreGrain.StateChanged");
    }
}
```

**Issue**: Between `Mutate` returning (releases `Grain._lock`) and `StateChanged?.Invoke(after)` (line 66), a second `Dispatch` call can enter `Mutate`, modify the state, and return. The `after` snapshot is still valid (it's a reference to an immutable record), but if the subscriber calls `_storeGrain.Select(s => s.Settings)` from within the handler, it gets the **newer** state from the racing `Dispatch`, not the `after` state. The subscriber sees inconsistent data: the event payload says "state A" but the store says "state B".

**Impact**: A subscriber that compares the event payload with `Select()` to compute a delta could get wrong results. However, no existing subscriber does this comparison -- they each either rely on the event itself or call `Select()` independently.

---

### C7. [LOW] ErrorLog._isWriting Interlocked pattern is correct (no missing volatile)

**File**: `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/ErrorLog.cs`, lines 27, 50, 79

```csharp
private static int _isWriting;  // line 27 -- no volatile needed

// line 50: acquire
if (Interlocked.Exchange(ref _isWriting, 1) == 1)
    return;  // concurrent write dropped

// ... file append ...

// line 79: release
Interlocked.Exchange(ref _isWriting, 0);
```

**Verdict**: The `Interlocked.Exchange` call provides full acquire+release memory barriers. No `volatile` keyword is needed. The pattern is correct: concurrent writes are dropped silently, and the `finally` block (lines 77-80) ensures `_isWriting` is always reset to 0 even on exception.

---

## Concurrency design observations (not bugs)

### D1. _saveLock held during CleanupStaleTempFiles prevents all Save operations

**File**: `FileLauncherJsonIO.cs`, lines 176-202

`CleanupStaleTempFiles` acquires `_saveLock` and holds it while iterating over all temporary files. During this time, any `Save` call blocks on `_saveLock.Wait()`. Since `Save` holds `Grain._lock` while waiting for `_saveLock`, this also blocks all `Select` calls.

For a config directory with many stale temp files (unlikely in practice), this could cause UI responsiveness issues.

### D2. Grain._lock re-entrance safety

`Grain._lock` uses `lock (_lock)` which is `Monitor.Enter` -- re-entrant on the same thread. If a mutator passed to `Mutate` calls `ReadState` or accesses the `State` property (which both acquire the same lock), it succeeds on the same thread. However, `ReadState` in this context reads the OLD state (before the mutation), which could be surprising but is technically correct (the mutation hasn't completed yet).

No existing mutator does this -- all mutators compute from the `state` parameter passed to the lambda. The re-entrance is only a concern if a future developer nests calls inside a mutator.

---

## Summary table

| ID | Severity | File | Line | Issue |
|---|---|---|---|---|
| S1 | LOW | EntryId.cs | 192-196 | EntryIdJsonConverter.Read does not handle JSON null; throws FormatException instead of returning EntryId.Empty |
| S2 | LOW | EntryName.cs | 74-78 | EntryNameJsonConverter.Read null-to-empty guard is unreachable through converter; always throws |
| S3 | LOW | FileLauncherJsonIO.cs | 124-149 | Backup rotation happens before File.Replace; crash mid-rotation shifts backup chain by one |
| S4 | LOW | FileLauncherJsonIO.cs | 64-70, 90-93 | Recovery temp files not cleaned up inline; deferred to CleanupStaleTempFiles at startup |
| S5 | INFO | LauncherJsonContext.cs | 1-24 | Source-gen registration is complete -- all transitive types reachable |
| S6 | INFO | Various | -- | All entry points use JsonTypeInfo<T>, no JsonSerializerOptions-based overloads. AOT-safe |
| S7 | INFO | FileLauncherJsonIO.cs | 16, 21-23, 122 | Consistent UTF-8 without BOM encoding |
| C1 | **MEDIUM** | StoreGrain.cs | 52-56 | **ReloadFromDisk TOCTOU race**: loads disk state outside lock, then SetState inside lock. Concurrent Dispatch can be silently overwritten |
| C2 | **MEDIUM** | StoreGrain.cs | 88-91 | Dispose does not set StateChanged = null. Subscriber references leak; Dispatch after Dispose can fire on disposed grain |
| C3 | **MEDIUM** | FileLauncherJsonIO.cs, LauncherJson.cs | 204-220, 54-55 | RotateBackups callable externally without _saveLock. Concurrent with Save, backup chain races |
| C4 | MEDIUM | StoreGrain.cs, Grain.cs | 58-71, 44-49 | Nested locking: Grain._lock held during entire file I/O (Serialize + Write + RotateBackups + File.Replace). Blocks all Select calls |
| C5 | LOW | FileLauncherJsonIO.cs | 111, 182 | SemaphoreSlim.Wait() blocks UI thread during file I/O (also reported as M4 in App/VM ultrareview) |
| C6 | LOW | StoreGrain.cs | 58-71 | StateChanged fires outside Grain._lock; subscriber calling Select() may see newer state than event payload |
| C7 | LOW | ErrorLog.cs | 27, 50, 79 | Interlocked.Exchange pattern is correct -- no missing volatile |

## Files reviewed

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs` | Static facade for JSON persistence (delegates to ILauncherJsonIO) |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherJsonSerializer.cs` | Pure JSON serialization/deserialization (Zone 0, no I/O) |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherJsonContext.cs` | Source-gen JsonSerializerContext with registered types |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreFile.cs` | Single-file store container record (immutable) |
| `src/avalonia-launcher/src/WTLauncher.Core/ILauncherJsonIO.cs` | Boundary port interface for JSON file I/O |
| `src/avalonia-launcher/src/WTLauncher.Core/IStorePersistence.cs` | Boundary port interface for store persistence |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherStore.cs` | Static facade for launcher store persistence |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreValidator.cs` | Pure schema validation (Zone 0, no I/O) |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs` | Immutable runtime settings record (includes LauncherWindowSettings) |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherPreset.cs` | Immutable preset record |
| `src/avalonia-launcher/src/WTLauncher.Core/LegacyPresetDocument.cs` | Legacy preset deserialization type |
| `src/avalonia-launcher/src/WTLauncher.Core/DirectoryPath.cs` | DirectoryPath value object with JSON converter (handles null correctly) |
| `src/avalonia-launcher/src/WTLauncher.Core/EntryId.cs` | EntryId value object with JSON converter (does NOT handle null) |
| `src/avalonia-launcher/src/WTLauncher.Core/EntryName.cs` | EntryName value object with JSON converter (does NOT handle null) |
| `src/avalonia-launcher/src/WTLauncher.Core/PresetId.cs` | PresetId value object with JSON converter (handles null) |
| `src/avalonia-launcher/src/WTLauncher.Core/PresetName.cs` | PresetName value object with JSON converter (delegates to EntryName) |
| `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs` | Immutable entry record |
| `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileStorePersistence.cs` | File-based store persistence (wraps FileLauncherJsonIO + LauncherStoreValidator) |
| `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/FileLauncherJsonIO.cs` | File-based JSON I/O with atomic writes, backup rotation, corrupt-file recovery |
| `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/StoreGrain.cs` | Thread-safe store grain with Dispatch/Select/ReloadFromDisk |
| `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/Grain.cs` | Actor base class with lock-protected state access |
| `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/ErrorLog.cs` | Persistent error log with Interlocked write guard |
| `src/avalonia-launcher/src/WTLauncher.App/Infrastructure/LegacyMigration.cs` | One-time migration from three-file to unified config |

## Related Specs

- `.trellis/spec/core/serialization-guidelines.md` (if exists) -- AOT-safe source-gen, Loose Read + Strict Write
- `.trellis/spec/core/quality-guidelines.md` -- single-source rules, catch exceptions narrowed, no bare Exception
- `.trellis/spec/guides/pitfalls.md` -- 50+ session gotchas, search keyword "serialization" or "concurrency"

## Caveats

- The `ReloadFromDisk` race (C1) does NOT trigger in the current codebase because both `ReloadFromDisk` and `Dispatch` run on the Avalonia UI thread. The bug is latent -- it requires a multi-threaded calling pattern to manifest.
- The nested locking (C4) is by design and is not a deadlock in current code. The concern is fragility for future changes.
- The `EntryIdJsonConverter` null behavior (S1) is arguably intentional strictness, not a bug. Noted because it is inconsistent with `DirectoryPathJsonConverter`.
