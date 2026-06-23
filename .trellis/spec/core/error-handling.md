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

**Source:** `LauncherConfigStore.cs`, `LauncherEntryDrafts.cs`

```csharp
private static string RequireEntryId(string? id, int index, HashSet<string> seenIds)
{
    var normalized = LauncherEntryIds.RequireEntryId(id, nameof(id));
    if (!seenIds.Add(normalized))
        throw new InvalidDataException($"Launcher config entry {index} id is duplicate: {normalized}.");
    return normalized;
}
```

**Rules:**
1. **Validate at the entry point** — `LauncherConfigStore.Load()` validates everything before ViewModel touch.
2. **Throw `InvalidDataException`** for malformed config/user input.
3. **Throw `ArgumentException` / `ArgumentNullException`** for programming errors.
4. **Include index and field name** in error messages for pinpoint debugging.

---

## Snapshot-Rollback Pattern

**Source:** `LauncherEntryMutationPersistence.cs`, `LauncherEntryMutationSnapshot.cs`

```
1. Capture snapshot (deep clone of entries + presets + settings)
2. Perform mutations
3. Save all files (config → presets → settings)
4. If any save fails:
   a. Restore config from snapshot
   b. Restore presets from snapshot (if they were affected)
   c. Return composite error message
```

```csharp
// Persistence returns null on success, error string on failure
public static string? Save(...)
{
    var configSaveFailure = LauncherConfigMutationPersistence.Save(entries, configPath);
    if (configSaveFailure is not null)
        return configSaveFailure;
    // ... cascade through presets, settings ...
    return null; // success
}
```

---

## Exception Catching Discipline

**Source:** `LauncherEntryMutationPersistence.RestoreSnapshotOnPersistenceFailure()`

```csharp
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
```

**Rule:** Catch **specific** exception types, not bare `Exception`. Use `when` filters for precision.

---

## Forbidden Patterns

- ❌ **Bare `catch (Exception)`** — always filter to specific types.
- ❌ **Swallowing exceptions silently** — always log/display to user.
- ❌ **Returning `null` for "not found" without documentation** — use exceptions for unexpected absence.
- ❌ **English status text** — all user-facing text in Chinese.
- ❌ **Inline string concatenation for status** — use the static text factory method.
