# Persistence — Core Layer

> Single-file JSON store with atomic writes, backup rotation, and LF line endings.

---

## Wiring Contract Update (2026-08-15)

- `StoreMutationSession` is the single transaction primitive for multi-step ViewModel mutation commits: `Commit` saves once, and `Dispose` restores in-memory state when the caller did not commit.
- `StoreGrain.ReloadFromDisk` fires `Reloaded`; `MainWindowViewModel` performs one `SynchronizeProjections(ProjectionSyncMode.FullReload)` pass while suppressing intermediate `EntriesChanged` syncs.
- Canonical store writes still go through `StoreGrain` → atomic `LauncherJson.Save`. External import/export reads/writes are wrapped by `IConfigExchangeService`.
- Launch history saves use `LaunchSaveResult` so the caller can distinguish “save failed before launch” from “launch happened but history save failed.”

## JSON Persistence Engine

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs`

### Atomic Write Pattern

```csharp
// temp file → optional backup rotation → atomic replace
var tempPath = fullPath + $".tmp-{Guid.NewGuid():N}";
File.WriteAllText(tempPath, json + "\n");

if (backup && File.Exists(fullPath))
{
    // Copy current → bakTempPath, rotate old backups, move bakTempPath → .bak.
    // Copy-then-rotate order: if crash occurs mid-rotation, bakTempPath is
    // still on disk and the original .bak is preserved.
    var bakTempPath = fullPath + $".bak-tmp-{Guid.NewGuid():N}";
    File.Copy(fullPath, bakTempPath, overwrite: true);
    RotateBackups(fullPath);
    File.Move(bakTempPath, fullPath + ".bak", overwrite: true);
}

// Atomic commit on NTFS:
if (File.Exists(fullPath))
    File.Replace(tempPath, fullPath, null);   // MOVEFILE_REPLACE_EXISTING
else
    File.Move(tempPath, fullPath);
```

**Rules:**
1. **Always write to a temp file first** — never directly overwrite the real file.
2. **Temp file names include `Guid.NewGuid():N`** — prevents collisions.
3. **`File.Replace` when target exists** (atomic on same NTFS volume); `File.Move` only for first-save when target doesn't exist.
4. **`finally`-style cleanup is handled by `CleanupStaleTempFiles`** at app startup — temp files orphaned by a crash are cleaned next launch.

### Backup Rotation

Keeps up to 3 generations: `.bak` → `.bak.1` → `.bak.2` → delete.

**Source:** `LauncherJson.RotateBackups(fullPath, maxBackups = 3)`

**Order:** copy current file to `bakTempPath` → rotate existing `.bak*` files → atomically `File.Move(bakTempPath, fullPath + ".bak")`. This avoids the window where `.bak` is missing mid-rotation.

### Line Ending Normalization

All persisted JSON uses LF (`\n`) — matches `.gitattributes` `* text=auto`.

```csharp
private static string NormalizeLineEndings(string text) =>
    text.ReplaceLineEndings(RepositoryLineEnding);
```

### Native AOT: Source-Generated JsonSerializerContext (2026-07-12)

For Native AOT publishing (`-p:PublishAot=true`), `System.Text.Json` uses a **source-generated** `JsonSerializerContext` instead of reflection-based serializers. Every type that passes through `LauncherJson.Load<T>` / `Save<T>` must be registered.

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherJsonContext.cs`

```csharp
[JsonSourceGenerationOptions(
    WriteIndented = false,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    MaxDepth = 64)]
[JsonSerializable(typeof(LauncherStoreFile))]
[JsonSerializable(typeof(LauncherSettings))]
[JsonSerializable(typeof(LaunchEntry))]
[JsonSerializable(typeof(LauncherPreset))]
[JsonSerializable(typeof(List<LaunchEntry>))]
[JsonSerializable(typeof(List<LauncherPreset>))]
[JsonSerializable(typeof(EntryName))]
[JsonSerializable(typeof(EntryId))]
[JsonSerializable(typeof(DirectoryPath))]
[JsonSerializable(typeof(LauncherStore.LegacyPresetDocument))]
internal sealed partial class LauncherJsonContext : JsonSerializerContext
{
}
```

**Key rules:**
1. `[JsonSourceGenerationOptions]` on the context class --- NOT on `JsonSerializerOptions`. At runtime, source gen only reads options from the attribute, not from the options object.
2. All generic types used (e.g., `List<LaunchEntry>`) must have explicit `[JsonSerializable]` attributes --- `JsonSerializerContext` does not auto-generate for generic type arguments.
3. **new type -> add attribute** --- every new `LauncherJson.Load<T>` / `Save<T>` call with a type not yet registered will throw at runtime via `GetTypeInfo(typeof(T))` returning null.

**Loading pattern** (uses `JsonTypeInfo<T>` for AOT safety):
```csharp
var typeInfo = (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!;
return JsonSerializer.Deserialize(stream, typeInfo);
```

**Saving pattern** (AOT-safe `JsonSerializer.Serialize<T>(T, JsonTypeInfo<T>)`):
```csharp
var typeInfo = (JsonTypeInfo<T>)LauncherJsonContext.Default.GetTypeInfo(typeof(T))!;
var json = NormalizeLineEndings(JsonSerializer.Serialize(value, typeInfo));
```

### Serializer Options

Serialization options are set via `[JsonSourceGenerationOptions]` on `LauncherJsonContext`. Read options are lenient (forward-compatible), write options are strict via source-gen (only known fields written):

- `ReadCommentHandling = JsonCommentHandling.Skip` → silent forward-compat for newer config keys.
- `AllowTrailingCommas = true` → user-friendly config editing.
- `UnmappedMemberHandling` not set → defaults to `Skip` for reading, irrelevant for source-gen writes.
- `MaxDepth = 64` → nested structure limit.
- Source-gen overloads (`JsonSerializer.Deserialize(stream, jsonTypeInfo)` / `Serialize(value, jsonTypeInfo)`) are used instead of `JsonSerializerOptions`-based overloads for Native AOT safety.

### Loose Read / Strict Write (2026-07-23, Phase 1 Scheme B)

The serialization strategy uses **Loose Read + Strict Write**:

- **Read:** Unknown JSON keys are silently skipped. This enables forward compatibility — an older app version can load a config written by a newer version that added fields.
- **Write:** Source-gen only writes properties declared on the C# record. Unknown properties cannot be written, making the C# record the effective schema.

**Trade-off:** Silent forward-compatibility is preferred over fail-loud at load time. Developer mistakes (e.g., adding a record field without `[JsonPropertyName]`) are caught at write time, not read time. For the older approach (fail-loud on read), see "Schema-First Enforcement" in the history below.

### Historical: Schema-First `Disallow` (2026-07-10 — 2026-07-23)

Before Phase 1, `UnmappedMemberHandling = Disallow` was used, making the C# record the **single source of truth** for JSON structure — unknown keys threw `JsonException`. This was replaced with Loose Read for forward compatibility (Scheme B architecture decision).

---

## Single-File Store (since 2026-07-08)

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherStore.cs`, `LauncherStoreFile.cs`

The store is a **single file** `config/launcher.json` owning five sections together:

```json
{
  "schemaVersion": 2,
  "settings":     { "schemaVersion": 2, "lastFilterText": "", ... },
  "entries":      [ { "id": "...", "name": "...", "directory": "...", "command": "...", ... } ],
  "presets":      [ { "id": "...", "name": "...", "entryIds": [...] } ],
  "defaultPresetId": "core-projects"
}
```

| Section | Type | Owner |
|---------|------|-------|
| `schemaVersion` | `int` (currently `2`) | `LauncherStore.ExpectedStoreSchemaVersion` |
| `settings` | `LauncherSettings` | window state, sort/filter, command templates, search history |
| `entries` | `ImmutableList<LaunchEntry>` | the workspace buttons |
| `presets` | `ImmutableList<LauncherPreset>` | saved selection groups |
| `defaultPresetId` | `string` | which preset is active on startup |

### Single Source of Truth: SchemaVersion (2026-07-09, task 07-09-schema-first-persistence)

`schemaVersion` lives **only** at the `LauncherStoreFile` top level. `LauncherSettings` does NOT have its own `SchemaVersion` field — historical dual-versioning (top-level `=2` + nested `=1`) created drift bugs (pitfalls §6.1).

**Why single source:** The whole file is one atomic unit (single-file persistence). Versioning the settings section separately from entries/presets has no meaning — they all migrate together. The previous `LauncherSettings.SchemaVersion = 1` was a dead field never validated, and 4 mutation sites reset it on every save — confusing and bug-prone.

**Forward compatibility:** Loose Read (2026-07-23, Phase 1). Unknown JSON keys are silently skipped at load time. The legacy `settings.schemaVersion` key is no longer rejected — it is silently dropped and naturally removed on the next save (since source-gen writes only known fields). Pinned by `LauncherStoreLoadFailureTests.LauncherSettings_LegacySchemaVersionInJson_LooseReadSkipsUnknownKey`.

**Decision:** Schema-First migration (Corvus.JsonSchema source generator) was rejected — C# records ARE the schema for app-internal persistence. Adding a JSON Schema file would create a second source of truth and increase drift risk, not decrease it. Corvus is correct for API contracts / externally-authored config, not for sole-author sole-reader app persistence.

### SchemaMigrationRegistry: deferred until v3 bump (2026-07-09, task 07-09-schema-migration-registry)

A versioned `SchemaMigrationRegistry` (EF Core / Rails style) was considered and **deferred**. The current migration surface is exactly ONE path: `MigrateFromLegacyFiles` (one-time, three-file → single-file, already executed on all real installations). There is no v3 schema bump planned.

**Why deferred:**
- Registry requires at least 2 migration paths to justify the abstraction (else it's an "interface with one implementation").
- Field additions inside an existing record are now **breaking changes** under `UnmappedMemberHandling = Disallow` (2026-07-10): removing or renaming a `[JsonPropertyName]` fails loudly on the next load of any pre-change file. This is the intended schema-first contract — it does not eliminate migration work, it just surfaces it immediately instead of letting it drift silently. The current migration surface is still one path, and now it cannot grow unnoticed.
- The 60-line static `MigrateFromLegacyFiles` is well-tested, self-documenting, and isolated.

**When to revisit:**
1. v3 schema bump involves breaking structural change (not just field additions), OR
2. Two parallel migration paths coexist (e.g., legacy + v2 → v3).

Until then, hand-rolled one-shot migration is correct for this codebase.

`LauncherStoreFile` is a `sealed record` with init-only properties — mutations produce a new instance via `with { }`.

### Backup Rotation and Recovery (2026-07-24, Phase 3 triple-cut-arch)

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs`

`RotateBackups` uses copy-then-rotate order to avoid crash windows:
1. Copy current file to `bak-temp-{Guid}`
2. Rotate chain: `.bak` → `.bak.1`, `.bak.1` → `.bak.2`, delete oldest
3. Move `bak-temp-{Guid}` → `.bak`

**Recovery chain:** `LoadWithRecovery` searches `.bak` → `.bak.1` → `.bak.2` in order (up to `DefaultMaxBackups`), returning the first valid backup found.

**Crash safety:** `CleanupStaleTempFiles` acquires `SaveLock` so it cannot delete an in-progress save's temp file. The static `SaveLock` serializes all saves across all AppStore instances.

**Invariants:**
- `RotateBackups` never creates more than `maxBackups` managed backup files
- `CleanupStaleTempFiles` only deletes files matching `\.(tmp|stage|bak-tmp|recovery)-[0-9a-f]{32}$` — never unrelated files

### Compile-Time Write Path Enforcement (2026-07-24, Phase 2 triple-cut-arch)

**Source:** `WTLauncher.Analyzers/DispatchMustBeOnlyWritePathAnalyzer.cs`

Rule WT1003 (compile-time error): `LauncherStore.Save` must only be called from `StoreGrain.Dispatch` or `StoreGrain.DispatchErrorOr`. Any direct call in production code is rejected.

**Why:** `TrySaveWindowBounds` (H2 bug) was writing directly to disk via `LauncherStore.Save`, bypassing the store grain. This caused persistent state desync: the grain state never saw the window bounds change, so the next Dispatch overwrote them. Test infrastructure (test fixture setup) suppresses this rule via `NoWarn`.

### Read-Side State Single-Sourcing (2026-07-24, Phase 1 triple-cut-arch)

**Source:** `MainWindowViewModel.cs`

`CurrentSettings` property reads directly from `StoreGrain.Select(s => s.Settings)`, eliminating `_settings` field drift:

```csharp
internal LauncherSettings CurrentSettings => _storeGrain.Select(s => s.Settings);
```

**Transition:** `_settings` field retained as working copy during construction and DragWorkflow sync. Future elimination requires DragWorkflow to read/write through StoreGrain directly.

### Single Canonical Assembler: BuildStoreFile (2026-07-09, task 07-09-settings-single-source)

`MainWindowViewModel.BuildStoreFile()` is the **only** method that hand-assembles a `LauncherStoreFile` for persistence. Every save path must route through it.

**Why single assembler:** Pre-2026-07-09 there were two — `BuildStoreFile()` and `SaveSettingsToStore(settings)` — and the latter hand-assembled the same 4 fields (`Entries` / `Settings` / `Presets` / `DefaultPresetId`) independently. The dual path meant any field-addition or re-ordering had to be mirrored in both sites, creating drift risk.

**Pin gate:** `scripts/verify.ts` check `settings-single-source-buildstorefile` regex-counts the unique hand-assembly signature in `MainWindowViewModel.cs` and asserts exactly 1 match. Regression triggers a verify failure.

**Collaborator working state is NOT dual truth:** `LauncherDragReorderWorkflow._settings` and the store grain state are sometimes mistaken for dual-truth — they are not. The workflow mutates `_settings` during Preview before persist; `StoreGrain` holds the single source and leaves memory unchanged on save failure (save-first-then-commit). Main VM syncs via `_settings = _dragReorderWorkflow.Settings` (5 sites in `Drag.cs`) and `_settings = _storeGrain.Select(s => s.Settings)` for store reads. This manual sync is the cost of collaborator-boundary design, NOT a bug. Verified by 7 equivalence tests in `MainWindowViewModelTests.DragTests.cs`.



### Why Settings Is Nested, Not Split (audit 2026-07-09)

The 2026-07-09 schema-driven-architecture audit re-examined whether `settings` should be split out of `launcher.json` into a separate file (concern S13). **Decision: keep nested.**

**Why nested is correct:**
1. **UI/business state already separated at the data-structure level** — `LauncherSettings` contains only UI/presentation state (lastFilterText, sortMode, entryViewMode, window bounds, command templates, search history, customEntryOrder, requireConfirmationForLaunches). Business state (entries, presets, defaultPresetId) lives at the top level. The nested design does not mix concerns.
2. **Single-file atomicity beats multi-file** — splitting into two files introduces a cross-file consistency window: settings.json writes succeed while entries.json fails (or vice versa). The current `LauncherJson.Save` writes one file atomically via temp→`File.Replace`→`.bak` rotation. Splitting would require a two-phase commit, which is overkill for a single-user launcher.
3. **No real problem to solve** — 19 entries, 2 presets, 9 UI settings run cleanly in one file. No user-visible bug, no test pressure, no perf constraint driving a split.

**When to revisit:** only if a real workload emerges that needs independent settings writes (e.g., window bounds written on every resize while entries are unchanged, causing excessive entry-array serialization). Until then, single-file wins.

### API

```csharp
public static class LauncherStore
{
    public const int ExpectedStoreSchemaVersion = 2;
    public const string StoreFileName = "launcher.json";
    public const string ConfigDirectoryName = "config";

    public static LauncherStoreFile Load(string storePath);
    public static void Save(LauncherStoreFile store, string storePath, bool backup = true);
    public static string ResolveStorePath(string? explicitPath, string? currentDirectory = null, string? appBaseDirectory = null);
    public static LauncherStoreFile? MigrateFromLegacyFiles(string configDirectory);
}
```

### Path Resolution Strategy (`ResolveStorePath`)

```
1. Explicit CLI path → use directly if it points at config/launcher.json
2. Search upward from current directory → find config/launcher.json + README.md
3. Search upward from app base directory → find config/launcher.json + README.md
4. Fallback: build config/launcher.json under the first non-null candidate root
```

Repository marker (`README.md`) is the **git root signal**. Without it, upward search continues.

### Validation Pipeline

`LauncherStore.Load()` runs a strict pipeline:

```
ThrowIfStorePathMissing
  → MigrateFromLegacyFiles (if legacy config.json/presets.json/settings.json exist and launcher.json doesn't)
  → LoadWithRecovery(LauncherJson.LoadRequired<LauncherStoreFile>)
      → File size check (< 50 MB)
      → Deserialize → LauncherStoreFile
      → SchemaVersion > Expected → throw InvalidDataException("from a newer version")
```

Value-object validation (`EntryId.Parse`, `EntryName.Parse`, etc.) happens at use time, not in the store layer. JSON deserialization handles structural validity; value objects handle semantic validity.

### First-Run Bootstrap

When `launcher.json` is missing, `LoadWithRecovery` returns `new LauncherStoreFile()` (empty entries, default settings). First-run is a normal path, NOT an error. Throwing on missing file causes silent exit (no MainWindow, no error dialog).

### Backup Recovery

`LoadWithRecovery` runs two recovery paths:

1. **File exists but is corrupt** (`JsonException` / `InvalidDataException` / `IOException`):
   - Try `path + ".bak"`: atomic recovery via temp-file → `File.Replace`.
   - Retry `loadAndValidate(path)` — OUTSIDE the original try/catch to avoid infinite loop when backup is also corrupt.
   - Invoke `OnRecoveryFromBackup` hook (logged by App layer).
   - If recovery fails or no backup exists, re-throw the original exception.

2. **File missing**:
   - Try `path + ".bak"`: simple `File.Move` (target doesn't exist, atomicity not critical).
   - If no backup, return `defaultValue` (first-run bootstrap).

**Rationale:** The save path creates `.bak` files — the load path must consume them. Otherwise backup is dead code.

### Schema Version Validation

```csharp
public const int ExpectedStoreSchemaVersion = 2;

if (store.SchemaVersion > ExpectedStoreSchemaVersion)
    throw new InvalidDataException(
        $"Launcher store schema version {store.SchemaVersion} is from a newer version of the app. Please upgrade WT Launcher.");
```

**Migration contract:**
- Schema versions **above** expected → reject with upgrade message (file is newer than app → prevents silent data loss on downgrade).
- Schema versions **at or below** expected → accept. No downward migration code yet — when bumping `ExpectedStoreSchemaVersion`, add a migration path that reads the old version and transforms it to the new format BEFORE validation.

### Legacy Migration (one-time, F06)

`MigrateFromLegacyFiles(configDir)` runs once when `launcher.json` doesn't exist but `config.json` does:

1. Load legacy `config.json` (flat `List<LaunchEntry>`).
2. Load legacy `settings.json` if present (best-effort, default on failure).
3. Load legacy `presets.json` if present (best-effort, empty on failure).
4. Build `LauncherStoreFile` and save to `launcher.json` (with backup).
5. Rename legacy files to `.bak` (best-effort).

After migration, the legacy files are preserved as `.bak` and never read again.

## Atomic Write: `File.Replace` vs `File.Move`

`Save` uses **both**, depending on whether the target exists:

- **Target exists** → `File.Replace(temp, target, null)` — backed by `MoveFileEx(MOVEFILE_REPLACE_EXISTING)`, atomic on the same NTFS volume.
- **Target missing** (first save) → `File.Move(temp, target)` — `File.Replace` requires the target to already exist.

`File.Move(temp, target, overwrite: true)` is **NOT atomic** on Windows — it decomposes into delete+rename — and is not used.

## Concurrency: Save Lock (2026-07-23, Phase 2)

`LauncherJson.Save` is protected by a `private static SemaphoreSlim(1, 1)` to serialize concurrent writes:

```csharp
SaveLock.Wait();
try { /* temp file, backup rotation, atomic replace */ }
finally { SaveLock.Release(); }
```

**Why:** `StoreGrain` already serializes access through its `Grain<TState>` lock (read/mutate serialized). The Save lock is defense-in-depth against cross-instance or direct `LauncherStore.Save` calls from multiple threads.

## Stale Temp Cleanup

**Source:** `LauncherJson.CleanupStaleTempFiles(configDir)`

At startup (before any store load), enumerate `*.tmp-*`, `*.stage-*`, `*.bak-tmp-*`, `*.recovery-*` files matching the app's `.<kind>-<32-hex-chars>` naming convention and delete them. Safe to call every startup — failures are silently ignored. Catches orphaned files from crashed Save() or recovery operations.

## Forbidden Patterns

- ❌ **Direct file overwrite** — always use `LauncherJson.Save()` (temp + atomic replace).
- ❌ **`File.Move(overwrite: true)`** — NOT atomic on Windows. Use `File.Replace` when the target exists.
- ❌ **CRLF in persisted JSON** — always LF, verified by `verify.ps1`.
- ❌ **Using `JsonSerializer.Serialize` directly** — always go through `LauncherJson.Save()`.
- ❌ **Throwing on FileNotFound for Load** — return defaults for first-run bootstrap.
- ❌ **Adding serialized fields without `[JsonIgnore]` consideration** — every new field on a serialized record changes the JSON shape and breaks contract tests.
- ❌ **Backup rotation in rotate-then-copy order** — if rotation crashes mid-flight, `.bak` is missing. Use copy-then-rotate.

---

## Two-Layer Capped Collections

当集合字段有最大条目限制时，上限在**两层**执行：

1. **加载时（Store）**: `RequireXxx()` 在规范化加载后截断。防止手动编辑 JSON 绕过上限。
2. **写入时（ViewModel）**: 写入方法在保存前截断。防止运行时逻辑 bug 超过上限。

### 示例：SearchHistory（上限 20 条）

**加载时** — `LauncherStore` 在加载后截断:
```csharp
// 去重 → 去空 → 去空白 → 上限 20
if (normalized.Count > 20)
    normalized.RemoveRange(20, normalized.Count - 20);
```

**写入时** — `MainWindowViewModel.RecordSearchTerm()`:
```csharp
// 去重移到最前 → 上限 20
var newHistory = _settings.SearchHistory.RemoveAll(match).Insert(0, normalized);
if (newHistory.Count > 20)
    newHistory = newHistory.RemoveRange(20, newHistory.Count - 20);
```

**测试必须覆盖**: 两层都验证——VM 状态 + 文件持久化（round-trip via `LauncherStore.Load()`）。

**Source:** `LauncherStore.cs`, `MainWindowViewModel.cs`.

---

## Removed Abstractions (historical)

The following were removed during the 2026-07 single-file refactoring and ponytail audits. They are listed here to prevent re-introduction:

| Removed | Reason | Replaced By |
|---------|--------|-------------|
| `IStore` interface | One implementation, no polymorphism benefit | Direct `LauncherStore.Load/Save` calls |
| `JsonStore` wrapper | Thin wrapper over `LauncherStore` | Direct calls |
| `LiteDbStore` | LiteDB BSON mapper incompatible with init-only immutable records | `LauncherJson` (JSON) is the production store |
| `MigrationTool` (JSON→LiteDB) | LiteDB deferred indefinitely then deleted | Removed entirely |
| `NormalizeEntries` / `ValidateEntrySelectionFields` pipeline | Normalize pipeline removed in ponytail audit | Value-object `Parse` at use time |
| Multi-file stage-all-then-commit (config.json + presets.json + settings.json) | Three-file store collapsed into `launcher.json` | Single-file atomic save inside `LauncherJson.Save` |
| `LauncherPresetMutationSnapshot` (separate type) | Inlined — only one caller | Logic merged into `LauncherEntryMutationSnapshot` / `MainWindowViewModel` |
| `LauncherEntryMutationSnapshot` / `LauncherSelectionMutationSnapshot` | **Deleted** 2026-07-21 | Rollback via `AppStore.Dispatch` internal revert + `RestoreVmStateFromStore()` |
| `AppStore.Mutate` / `MutateAsync` / `MutateErrorOr` | **Deleted** 2026-07-23 (Phase 2) | Callers use `Dispatch` / `DispatchAsync` / `DispatchErrorOr` directly |
| `LooseOptions` / `StrictOptions` (JsonSerializerOptions fields) | **Deleted** 2026-07-23 (Phase 1) | `JsonTypeInfo<T>` from `LauncherJsonContext.Default` — AOT-safe overloads |
| `ExceptionFilters.UnwrapAggregates` | **Deleted** 2026-07-23 (Phase 2) | Inline unwrap in DEBUG guard; Core `IsRecoverable` handles production unwrap |
