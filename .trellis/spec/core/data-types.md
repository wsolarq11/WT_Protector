# Data Types — Core Layer

> Records, enums, design tokens — the immutable-shaped data that defines the system.

---

## Record vs Class Decision

**Rule: immutable-shaped → `sealed record`; mutable entities → `sealed class`.**

- **Use `sealed record`** when ALL properties are `{ get; }` or `{ get; init; }` — the type is a pure data carrier, never mutated after construction. Records provide value equality, `with` cloning, and auto-generated `ToString()`.
- **Use `sealed class`** when ANY property has `{ get; set; }` — the type is mutated after construction (e.g., JSON deserialization into mutable state). Classes use explicit `Clone()` methods for deep copying.

### Record Pattern (immutable data carriers)

**Source:** `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherBatchLaunchReport.cs`

```csharp
public sealed record LauncherBatchLaunchReport
{
    public LauncherBatchLaunchReport(...)
    {
        // validate + assign get-only properties
    }

    public int RequestedCount { get; }
    public int SkippedCount { get; }
    public string? LastCommandLine { get; }
}
```

### Class Pattern (mutable persisted entities)

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs`

```csharp
public sealed class LaunchEntry
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; set; } = "";

    [JsonPropertyName("directory")]
    [JsonRequired]
    public string Directory { get; set; } = "";

    [JsonPropertyName("command")]
    [JsonRequired]
    public string Command { get; set; } = "";

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; set; } = "";

    [JsonPropertyName("launchedAtUtc")]
    public string LaunchedAtUtc { get; set; } = "";

    public LaunchEntry Clone() => new()
    {
        Id = Id,
        Name = Name,
        Directory = Directory,
        Command = Command,
        Selected = Selected,
        UpdatedAtUtc = UpdatedAtUtc,
        LaunchedAtUtc = LaunchedAtUtc,
    };
}
```

### Rules

1. **Immutable data → `sealed record`** — get-only properties, value equality, `with` cloning.
2. **Mutable data → `sealed class`** — get/set properties, explicit `Clone()` method, reference equality.
3. **`[JsonRequired]` on all non-optional fields** — ensures schema enforcement on deserialization.
4. **`[JsonPropertyName]` on every property** — explicit JSON contract, no implicit camelCase surprises.
5. **String defaults `= ""`** — never `null` for JSON fields (prevents null-reference cascades).
6. **No behaviour methods on data carriers** — keep them as pure data. Logic goes in static helpers.
7. **Every mutable class provides `Clone()`** — returns a deep copy, used by snapshot/rollback infrastructure.

---

## Enum Pattern

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherEntryViewMode.cs`

```csharp
public enum LauncherEntryViewMode
{
    Cards,
    Details,
    Names,
}

public static class LauncherEntryViewModes
{
    public const string CardsId = "cards";
    public const string DetailsId = "details";
    public const string NamesId = "names";

    public static string NormalizeId(string? entryViewMode)
    {
        var normalized = (entryViewMode ?? string.Empty).Trim();
        return normalized switch
        {
            CardsId => CardsId,
            DetailsId => DetailsId,
            NamesId => NamesId,
            _ => throw new InvalidDataException($"Unknown launcher entry view mode: {normalized}."),
        };
    }

    public static string ToId(LauncherEntryViewMode mode) => mode switch
    {
        LauncherEntryViewMode.Cards => CardsId,
        LauncherEntryViewMode.Details => DetailsId,
        LauncherEntryViewMode.Names => NamesId,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown launcher entry view mode."),
    };
}
```

### Rules

1. **Enum + static helper class pairing** — the enum is the code contract; the helper owns serialization/deserialization.
2. **String IDs as `const`** — never inline magic strings (e.g., `"cards"`).
3. **`NormalizeId()` for deserialization** — validates input, throws on unknown values.
4. **`ToId()` for serialization** — exhaustive switch, throws on unknown enum members.

---

## Design Tokens Pattern

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LauncherDesignTokens.cs`

```csharp
public static class LauncherDesignTokens
{
    public const string PrimaryColor = "#2563EB";
    public const double CardBorderRadius = 13;
    public const double CardPadding = 13;
    public const double WindowDefaultWidth = 1180;
    // ... all colors, spacing, sizing
}
```

### Rules

1. **All design constants live in `LauncherDesignTokens`** — single source of truth.
2. **Tokens are `const`** — compile-time constants, no runtime mutation.
3. **Injected at runtime via `App.axaml.cs`**, not hardcoded in XAML.
4. **Never duplicate a token value in XAML** or code-behind — always reference the Core constant.

---

## Cloning Pattern

**Immutable records** use `with { }` for shallow clones:

```csharp
var clone = report with { };
```

**Mutable classes** use explicit `Clone()` methods:

```csharp
// Deep clone for snapshot/rollback
var entries = sourceEntries.Select(e => e.Clone()).ToList();
var presetClone = LauncherPresetFiles.Clone(presetFile);
```

**Source:** `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryMutationSnapshot.cs`

---

## Forbidden Patterns

- ❌ **Mutable properties without `[JsonPropertyName]`** — breaks JSON contract visibility.
- ❌ **`null` defaults for JSON fields** — always use `""`, `[]`, or `false`.
- ❌ **Logic methods on record types** — records are data carriers; put logic in static helpers.
- ❌ **Magic strings for identifiers** — use `const` fields from the paired helper class.
- ❌ **Avalonia or CommunityToolkit types in Core** — zero UI dependencies in Layer 0.
- ❌ **`Dictionary<string, object>` or `dynamic`** — always define a typed record or class.
