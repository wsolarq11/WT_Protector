# Persistence — Core Layer

> Single-file JSON store with atomic writes, backup rotation, and LF line endings.

---

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

### Serializer Options

```csharp
internal static readonly JsonSerializerOptions Options = new()
{
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    WriteIndented = false,
    MaxDepth = 64,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
```

**Key decisions:**
- `AllowTrailingCommas = true` → user-friendly config editing.
- `UnmappedMemberHandling = Skip` → forward-compatible: old binaries load configs with new fields. SchemaVersion provides version gating instead.
- `UnsafeRelaxedJsonEscaping` → Chinese characters stay readable in JSON.

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

**Forward compatibility:** Old `launcher.json` files with `settings.schemaVersion` baked in still load cleanly because `UnmappedMemberHandling = Skip` discards unknown properties during deserialization. The legacy field silently disappears on the next save.

**Decision:** Schema-First migration (Corvus.JsonSchema source generator) was rejected — C# records ARE the schema for app-internal persistence. Adding a JSON Schema file would create a second source of truth and increase drift risk, not decrease it. Corvus is correct for API contracts / externally-authored config, not for sole-author sole-reader app persistence.

### SchemaMigrationRegistry: deferred until v3 bump (2026-07-09, task 07-09-schema-migration-registry)

A versioned `SchemaMigrationRegistry` (EF Core / Rails style) was considered and **deferred**. The current migration surface is exactly ONE path: `MigrateFromLegacyFiles` (one-time, three-file → single-file, already executed on all real installations). There is no v3 schema bump planned.

**Why deferred:**
- Registry requires at least 2 migration paths to justify the abstraction (else it's an "interface with one implementation").
- Forward-compatible field additions (`UnmappedMemberHandling = Skip` + default values) handle most schema growth without any migration code.
- The 60-line static `MigrateFromLegacyFiles` is well-tested, self-documenting, and isolated.

**When to revisit:**
1. v3 schema bump involves breaking structural change (not just field additions), OR
2. Two parallel migration paths coexist (e.g., legacy + v2 → v3).

Until then, hand-rolled one-shot migration is correct for this codebase.

`LauncherStoreFile` is a `sealed record` with init-only properties — mutations produce a new instance via `with { }`.

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
