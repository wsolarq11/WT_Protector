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

### Record Pattern (immutable persisted entities)

**Source:** `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs`

```csharp
public sealed record LaunchEntry
{
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; } = "";

    [JsonPropertyName("directory")]
    [JsonRequired]
    public string Directory { get; init; } = "";

    [JsonPropertyName("command")]
    [JsonRequired]
    public string Command { get; init; } = "";

    [JsonPropertyName("selected")]
    public bool Selected { get; init; }

    [JsonPropertyName("updatedAtUtc")]
    public string UpdatedAtUtc { get; init; } = "";

    [JsonPropertyName("launchedAtUtc")]
    public string LaunchedAtUtc { get; init; } = "";
}
```

Mutations produce a new instance via `with { }`:
```csharp
entry = entry with { Selected = true, UpdatedAtUtc = timestamp };
```

### Class Pattern (mutable persisted entities)

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
5. **Add only stable semantic tokens with a production consumer** — repeated typography levels such as `FontSizeSmall`, `FontSizeBody`, and `FontSizeEmphasis` qualify; one-off margins, widths, heights, and grid ratios do not.
6. **Keep framework shapes out of Core** — `LauncherDesignTokens` owns strings/doubles; `App.InjectDesignTokens()` converts them to `Brush`, `CornerRadius`, `Thickness`, or `BoxShadows` resources.

```csharp
// Core authority
public const double FontSizeBody = 14;

// App composition root
Resources["FontSizeBody"] = LauncherDesignTokens.FontSizeBody;
```

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

**Source (historical):** `LauncherEntryMutationSnapshot.cs` was deleted in 2026-07-21 Unified Store Architecture refactoring. Clone semantics moved to `AppStore.Mutate()` internal rollback + `RestoreVmStateFromStore()`.

---

## Forbidden Patterns

- ❌ **Mutable properties without `[JsonPropertyName]`** — breaks JSON contract visibility.
- ❌ **`null` defaults for JSON fields** — always use `""`, `[]`, or `false`.
- ❌ **Logic methods on record types** — records are data carriers; put logic in static helpers.
- ❌ **Magic strings for identifiers** — use `const` fields from the paired helper class.
- ❌ **Avalonia or CommunityToolkit types in Core** — zero UI dependencies in Layer 0.
- ❌ **`Dictionary<string, object>` or `dynamic`** — always define a typed record or class.
- ❌ **`Substring(0, N)` or `[..N]` without surrogate-pair boundary check** — truncation mid-surrogate produces illegal UTF-16. Always use `char.IsHighSurrogate(span[^1])` guard before truncating.
- ❌ **`char.IsControl()` without also checking `UnicodeCategory.Format`** — BiDi overrides (U+202A-U+202E), zero-width spaces (U+200B), and LTR/RTL marks pass through `IsControl`. Use `char.GetUnicodeCategory(ch) is UnicodeCategory.Control or UnicodeCategory.Format` for complete unsafe-character stripping.
- ❌ **Missing Unicode normalization on IDs and cross-reference keys** — NFC "café" and NFD "café" must be treated as the same ID. Apply `Normalize(NormalizationForm.FormC)` to all ID-like fields before comparison/storage.

---

## String Safety — Unicode and Surrogate Pairs (2026-06-25)

### Safe Truncation Rule

Any code that truncates a string to a maximum length MUST check for surrogate pair boundaries. Truncating between a high surrogate (U+D800..U+DBFF) and its paired low surrogate (U+DC00..U+DFFF) produces an illegal UTF-16 string.

```csharp
// CORRECT: truncate at code-point boundary
if (value.Length > MaxLength)
{
    var span = value.AsSpan(0, MaxLength);
    if (char.IsHighSurrogate(span[^1]) && value.Length > MaxLength + 1)
        span = value.AsSpan(0, MaxLength + 1);
    value = span.ToString();
}
```

**Applies to:** `LauncherEntryIds.Normalize()`, `LauncherPresetIds.Normalize()`, and any future truncation.

### Unsafe Character Stripping Rule

Command-line argument strings (name, directory, command) MUST strip BOTH control characters (Cc category) AND format characters (Cf category). Format characters include:
- BiDi overrides (U+202A–U+202E) — can visually reverse paths in terminal output (CVE-2021-42578 class)
- Zero-width spaces (U+200B) — invisible but semantically significant
- LTR/RTL marks (U+200E/U+200F) — alter text direction in terminals

```csharp
// CORRECT: strip both categories
var cat = char.GetUnicodeCategory(ch);
if (cat is UnicodeCategory.Control or UnicodeCategory.Format)
    continue;
```

### Unicode Normalization for IDs

All cross-reference keys (entry IDs, preset IDs) MUST be normalized to FormC (canonical composition). Without normalization, NFC `"café"` (1 code point) and NFD `"café"` (2 code points) are treated as different IDs, silently breaking preset→entry references.

```csharp
var normalized = id.Trim().Normalize(NormalizationForm.FormC);
```

**Applies to:** `LauncherEntryIds.Normalize()` — `LauncherPresetIds.Normalize()` already applies FormKC, which is a superset of FormC.

### Percent-Sign Injection After FormKC

FormKC normalization converts fullwidth characters to ASCII. Fullwidth percent (U+FF05 `％`) normalizes to ASCII `%` (U+0025). If `Environment.ExpandEnvironmentVariables()` runs AFTER FormKC normalization, an attacker can inject environment-variable expansion through a fullwidth percent.

```csharp
// CORRECT: detect % appearing only after normalization
if (normalized.Contains('%') && !original.Contains('%', StringComparison.Ordinal))
    throw new InvalidOperationException("Directory contains characters that normalize to '%'");
```

**Applies to:** `LaunchArgumentBuilder.Build()` — directory path processing.

---

## Accepted Design Decisions

These patterns may appear suboptimal to automated reviewers but are intentional architectural choices:

### String Timestamps (Not DateTimeOffset)

`LaunchEntry.UpdatedAtUtc` and `LaunchEntry.LaunchedAtUtc` are typed as `string`, not `DateTimeOffset`.

**Why:** JSON config files are human-readable and user-editable. ISO 8601 strings (`"2026-06-23T12:00:00Z"`) are universally parseable. Switching to `DateTimeOffset` with a custom converter would:
1. Break all existing user config files (format change)
2. Add serialization complexity without user benefit
3. Require migration logic for zero functional gain

The `ParseLaunchedAt`/`ParseUpdatedAt` helpers in `LauncherEntryVisibility` are the single validation point. String timestamps are validated at parse time, not at rest.

### String Color Tokens (Not Typed Color/ARGB)

`LauncherDesignTokens` stores colors as `const string` hex values (e.g., `"#2563EB"`).

**Why:** Avalonia XAML resource dictionaries consume colors via `Brush.Parse()`. The const strings are directly assignable to `StaticResource` in XAML. Switching to a typed `Color`/`SolidColorBrush` would:
1. Break XAML `StaticResource` references (XAML cannot consume C# constants of non-string types)
2. Require runtime conversion in `App.axaml.cs` (already done — the string→Brush conversion happens ONCE at startup)
3. Add a dependency on Avalonia.Media in Core layer (violates zero-UI-dependency invariant)

### Enum + String ID Duality

Every enum has a paired static helper class with `const string` IDs and `NormalizeId()`/`ToId()` methods.

**Why:** JSON persistence uses string identifiers for forward compatibility. If a future version adds a new enum member, old config files with the string ID still deserialize correctly (the string is unrecognized → `InvalidDataException` with a clear message). If we serialized enum ordinals, adding a member would silently shift all values. See `LauncherEntryViewMode` + `LauncherEntryViewModes` for the canonical example.

### Immutable Collections in Records (Not Mutable `List<T>`)

Record properties holding collections use `ImmutableList<T>` and `ImmutableDictionary<K,V>` — not `List<T>` or `Dictionary<K,V>`.

**Why:** `sealed record` + `{ get; init; }` only protects the property reference from reassignment — the collection content remains mutable. `list.Clear(); list.AddRange(...)` silently mutates an "immutable" record. Immutable collections make the immutability contract enforceable at the type level. `System.Collections.Immutable` is built into .NET 8+ shared framework — zero additional NuGet dependency.

**Migration rule:** When changing collection APIs from mutating (`void`) to immutable (returning new instance), every call site MUST consume the return value. C# does not warn on discarded return values. Audit all callers manually or use `[MustUseReturnValue]` if available. See `LauncherEntryOrder.SyncWithEntries` and `LauncherEntryOrder.MoveRelative` for the canonical pattern.

### Record Reference Sharing Across Components

When Component A holds a reference to Component B's `sealed record` instance, and Component B replaces that instance via `with { }`, Component A's copy becomes stale. This is NOT a bug in the record pattern — it's the expected behavior of immutable types. The fix: either (a) always access through a getter that reads the latest instance, or (b) synchronize the reference after every mutation.

See `LauncherDragReorderWorkflow._settings` for the canonical pitfall — the workflow held a frozen copy from construction time, and `DiscardDragReorderPreview` overwrote the ViewModel's updated copy with the stale one.

---

## Value Object Pattern — Parse, Don't Validate

### Pattern

```csharp
// Value object: construction IS validation. Private constructor + TryParse.
[JsonConverter(typeof(EntryIdJsonConverter))]
public readonly record struct EntryId
{
    public string Value { get; }
    private EntryId(string value) => Value = value;

    public static Result<EntryId, IdError> TryParse(string? raw) { /* ... */ }

    public static implicit operator string(EntryId id) => id.Value;
}
```

### Rules

1. **`readonly record struct`** — zero heap allocation, value semantics.
2. **Private constructor** — only `TryParse` can create instances.
3. **Result return** — success vs failure enforced at compile time.
4. **JsonConverter per type** — transparent string serialization in JSON.
5. **Implicit conversion to string** — drops into existing string APIs without ceremony.
6. **All normalization in constructor** — no normalization happens after construction.
7. **Security checks on raw input** — compare raw vs normalized before accepting (%-injection, etc.).

### Existing Value Objects

| Type | Validation | Eliminates |
|------|-----------|------------|
| `EntryId` | FormC, control strip, 256 truncation, reserved names, path separators | `LauncherEntryIds` |
| `EntryName` | FormKC, unpaired surrogate strip | `LauncherEntryText` |
| `EntryDirectory` | FormKC, raw %-injection detection, UNC/DOS rejection | `LaunchArgumentBuilder` directory logic |
| `PresetId` | FormC, lowercase, alphanumeric filter | `LauncherPresetIds` |
| `PresetName` | FormKC, 256-char limit | `LauncherPresetFiles` name validation |
| `ShellCommand` | Shell path resolution, fallback to `pwsh` | Hardcoded `pwsh` prefix |

### Migration Pattern

When introducing a value object for existing `string` properties:

1. Create value object with `TryParse`
2. Mark old static class methods `[Obsolete]`, delegate to value object
3. Add `JsonConverter` for transparent serialization
4. Suppress CS0618 in `.csproj` with documented reason
5. Migrate call sites gradually (compiler warnings guide)
6. Delete old methods in cleanup phase
