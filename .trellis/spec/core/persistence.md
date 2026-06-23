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
- `UnmappedMemberHandling = Disallow` → catches schema drift early.
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
Parse JSON → Require each entry non-null → Require "selected" field exists
→ Normalize id (trim, non-empty) → Check duplicates → 
→ Normalize name/directory/command (trim, non-empty) → 
→ Return ReadOnlyList
```

**Source:** `LauncherConfigStore.NormalizeEntries()`, `RequireEntrySelectionFields()`

---

## Forbidden Patterns

- ❌ **Direct file overwrite** — always use temp file + atomic move.
- ❌ **Silently skipping missing `selected` field** — `RequireEntrySelectionFields()` throws.
- ❌ **CRLF in persisted JSON** — always LF, verified by `verify.ps1`.
- ❌ **Using `JsonSerializer.Serialize` directly** — always go through `LauncherJson.Save()`.
