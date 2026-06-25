# Persistence — Core Layer

> JSON file persistence with atomic writes, backup rotation, and LF line endings.

---

## JSON Persistence Engine

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs`

### Atomic Write Pattern

```csharp
// temp file → rotate backups → copy .bak → atomic move
var tempPath = fullPath + $".tmp-{Guid.NewGuid():N}";
File.WriteAllText(tempPath, json + "\n");
File.Move(tempPath, fullPath, overwrite: true);
// finally: cleanup temp
```

**Rules:**
1. **Always write to a temp file first** — never directly overwrite the real file.
2. **Temp file names include `Guid.NewGuid():N`** — prevents collisions.
3. **`File.Move` with `overwrite: true`** — atomic on the same volume.
4. **`finally` block cleans up temp** — even on exception.

### Backup Rotation

Keeps up to 3 generations: `.bak` → `.bak.1` → `.bak.2` → delete.

**Source:** `LauncherJson.RotateBackups()`

### Line Ending Normalization

All persisted JSON uses LF (`\n`) — matches `.gitattributes` `* text=auto`.

```csharp
private static string NormalizeLineEndings(string text)
{
    return text.Replace("\r\n", "\n").Replace("\r", "\n");
}
```

### Serializer Options

```csharp
private static readonly JsonSerializerOptions Options = new()
{
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
```

**Key decisions:**
- `AllowTrailingCommas = true` → user-friendly config editing.
- `UnmappedMemberHandling = Skip` → forward-compatible: old binaries load configs with new fields. SchemaVersion provides version gating instead.
- `UnsafeRelaxedJsonEscaping` → Chinese characters stay readable in JSON.

---

## Config File Resolution

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherConfigStore.cs`

### Path Resolution Strategy

```
1. Explicit CLI path → use directly
2. Search upward from current directory → find config/ + README.md
3. Search upward from app base directory → find config/ + README.md
4. Fallback: use current directory without README.md check
```

Repository marker (`README.md`) is the **git root signal**. Without it, upward search continues.

### Config Files

| File | Path | Source |
|------|------|--------|
| `config.json` | `{root}/config/config.json` | `LauncherConfigStore` |
| `presets.json` | `{root}/config/presets.json` | `LauncherPresetStore` |
| `settings.json` | `{root}/config/settings.json` | `LauncherSettingsStore` |

All three live in the same `config/` directory — resolved from `config.json` path as sibling files.

---

## Validation Pipeline

`LauncherConfigStore.Load()` runs a strict pipeline:

```
// Single-pass: deserialize directly from stream, then validate typed objects
Deserialize List<LaunchEntry?> → NormalizeEntries (null-check, dedup, field validation)
→ ValidateEntrySelectionFields (no-op: Selected is non-nullable bool, always present)
→ Return ReadOnlyList
```

**Source:** `LauncherConfigStore.NormalizeEntries()`, `ValidateEntrySelectionFields()`

---

## First-Run Bootstrap (2026-06-24)

**Source:** `LauncherConfigStore.Load()`, `LauncherSettingsStore.Load()`, `LauncherPresetStore.Load()`

When a config file is missing, the app MUST return sensible defaults, NOT throw:

```csharp
// Config: return empty list
if (!File.Exists(configPath))
    return new List<LaunchEntry>().AsReadOnly();

// Settings: return new instance with defaults
if (!File.Exists(settingsPath))
    return new LauncherSettings();

// Presets: return empty preset file
if (!File.Exists(presetPath))
    return new LauncherPresetFile();
```

**Rationale:** First-run is a normal path, not an error. Throwing `FileNotFoundException` on startup causes silent exit (no MainWindow, no error dialog).

## Backup Recovery (2026-06-24)

**Source:** all three `Load()` methods

Before returning defaults for a missing file, check for `.bak` recovery:

```csharp
if (!File.Exists(configPath))
{
    var backupPath = configPath + ".bak";
    if (File.Exists(backupPath))
    {
        File.Copy(backupPath, configPath, overwrite: false);
        // Falls through to normal load below
    }
    else
    {
        return /* defaults */;
    }
}
```

**Rationale:** The save path creates `.bak` files — the load path must consume them. Otherwise backup is dead code.

## Schema Version Validation (2026-06-24)

**Source:** `LauncherSettingsStore.Load()`, `LauncherPresetStore.Load()`

Every persisted type MUST validate its schema version on load:

```csharp
private const int ExpectedSchemaVersion = 1;

// After deserialization:
if (loaded.SchemaVersion != ExpectedSchemaVersion)
    throw new InvalidDataException(
        $"Schema version {loaded.SchemaVersion} is not supported. Expected {ExpectedSchemaVersion}.");
```

**Rationale:** `SchemaVersion` was written to disk but never read. Future schema evolution has zero detection/migration/rejection hook without this.

## Atomic Write: `File.Move` vs `File.Replace`

`File.Move(temp, target, overwrite: true)` is used instead of `File.Replace` because:
- `File.Replace` requires the target file to already exist → fails on first save
- For a single-process desktop app with temp-file+rename, `File.Move` is sufficient
- The temp file is GUID-named, eliminating collision risk

## Backup Recovery — Anti-Loop Pattern (2026-06-25)

**Source:** all three `Load()` methods

When both the main file AND its `.bak` are corrupt, the retry call MUST be placed OUTSIDE the try/catch block. Otherwise the backup recovery retry re-enters the same catch, finds `.bak` still exists, retries again → infinite loop.

```csharp
// CORRECT: retry outside try/catch
Exception? loadException = null;
try { return LoadAndValidate(path); }
catch (Exception ex) when (ex is JsonException or InvalidDataException or FileNotFoundException)
{ loadException = ex; }

if (loadException is not null)
{
    if (File.Exists(backupPath))
    {
        // Atomic recovery: temp-file → rename (never File.Copy directly)
        var recoveryTemp = path + $".recovery-{Guid.NewGuid():N}";
        File.Copy(backupPath, recoveryTemp, overwrite: true);
        File.Move(recoveryTemp, path, overwrite: true);
        return LoadAndValidate(path); // Outside try/catch — propagates if backup also corrupt
    }
    throw loadException;
}
```

**Rationale:** `File.Copy` is non-atomic — a crash mid-copy leaves a partially-written destination. Temp-file-then-rename guarantees the destination is either the complete old file or the complete new file.

## Multi-File Atomic Save — Stage-All-Then-Commit (2026-06-25)

**Source:** `LauncherEntryMutationPersistence.Save()`

When a single logical mutation touches config.json + presets.json + settings.json, all three MUST be committed atomically:

```csharp
// Stage phase: write all files to temp paths
var configTemp = configPath + $".stage-{Guid.NewGuid():N}";
var presetTemp = presetPath + $".stage-{Guid.NewGuid():N}";
var settingsTemp = settingsPath + $".stage-{Guid.NewGuid():N}";

LauncherJson.Save(configClone, configTemp, backup: false);
LauncherJson.Save(presetClone, presetTemp, backup: false);
LauncherJson.Save(settingsClone, settingsTemp, backup: false);

// Commit phase: atomic rename (same volume = atomic on NTFS)
File.Move(configTemp, configPath, overwrite: true);
File.Move(presetTemp, presetPath, overwrite: true);
File.Move(settingsTemp, settingsPath, overwrite: true);
```

**Key property:** If staging fails, originals are untouched. If a commit rename fails, already-committed files stay — but rename failures on the same NTFS volume are effectively impossible in production (all paths validated at startup, no cross-volume moves).

## Schema Migration Contract (2026-06-25)

**Source:** `LauncherPresetStore.Load()`, `LauncherSettingsStore.Load()`

```csharp
// Directional version check with distinct error messages
if (loaded.SchemaVersion < ExpectedSchemaVersion)
    throw new InvalidDataException($"Schema version {loaded.SchemaVersion} is too old.");
if (loaded.SchemaVersion > ExpectedSchemaVersion)
    throw new InvalidDataException($"Schema version {loaded.SchemaVersion} is from a newer version. Please upgrade.");
```

**Migration contract:**
- When bumping `ExpectedSchemaVersion`, add a migration path that reads the old version and transforms it to the new format BEFORE validation.
- Schema versions below expected → reject (app is newer than file → could migrate, but needs explicit migration code).
- Schema versions above expected → reject with upgrade message (file is newer than app → prevents silent data loss on downgrade).

## Forbidden Patterns

- ❌ **Direct file overwrite** — always use temp file + atomic move.
- ❌ **Silently skipping missing `selected` field** — `ValidateEntrySelectionFields()` exists as a no-op sanity checkpoint; the type system guarantees `Selected` is always a non-nullable `bool` after deserialization (defaults to `false` when absent from JSON).
- ❌ **CRLF in persisted JSON** — always LF, verified by `verify.ps1`.
- ❌ **Using `JsonSerializer.Serialize` directly** — always go through `LauncherJson.Save()`.
- ❌ **Throwing on FileNotFound for Load** — return defaults for first-run bootstrap.
- ❌ **Adding serialized fields without `[JsonIgnore]` consideration** — every new field on a serialized record changes the JSON shape and breaks contract tests.

---

## Two-Layer Capped Collections

当集合字段有最大条目限制时，上限在**两层**执行：

1. **加载时（Store）**: `RequireXxx()` 在规范化加载后截断。防止手动编辑 JSON 绕过上限。
2. **写入时（ViewModel）**: 写入方法在保存前截断。防止运行时逻辑 bug 超过上限。

### 示例：SearchHistory（上限 20 条）

**加载时** — `LauncherSettingsStore.RequireSearchHistory()`:
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

**测试必须覆盖**: 两层都验证——VM 状态 + 文件持久化（round-trip via `LauncherSettingsStore.Load()`）。

**Source:** `LauncherSettingsStore.cs` line 116, `MainWindowViewModel.Layout.cs` lines 234-242.
