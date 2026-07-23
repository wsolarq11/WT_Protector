# Serialization / Versioning Strategies for .NET 10

## Comparison Table

| Strategy | Forward Compat | Backward Compat | Code Change | Native AOT | Complexity | Test Burden |
|---|---|---|---|---|---|---|
| **1. STJ Custom Converter + Version Dispatch** | Yes (by design) | Yes (by design) | Medium (one converter) | Yes (source-gen) | Medium | High (per-version fixtures) |
| **2. STJ Polymorphic Deserialization** | Partial | Partial | Low (attributes) | Yes (source-gen) | Low | Medium |
| **3. JSON Schema + Validation** | Yes (schema-driven) | Yes (schema-driven) | High (schema files + validator) | Partial (needs runtime) | High | High |
| **4. Protobuf / FlatBuffers** | Yes (field-based) | Yes (field-based) | Very High (build step, format change) | Yes | Very High | Very High |
| **5. STJ Source Generator + Manual Migration** | Yes | Yes | High (per-version contexts) | Yes | Medium-High | High |
| **6. Hybrid: Loose Read + Strict Write** | Yes | Yes | Low (options change) | Yes (source-gen) | Low | Medium |

---

## 1. STJ Custom Converter + Version Dispatch

### How it works

A single `JsonConverter<LauncherStoreFile>` reads `schemaVersion` from the raw JSON first (using `Utf8JsonReader` in a sequential pass), then dispatches to a version-specific deserializer. Each version gets its own C# record type and a mapping function to the current schema.

```csharp
public sealed class LauncherStoreFileConverter : JsonConverter<LauncherStoreFile>
{
    public override LauncherStoreFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Snapshot position for re-parse
        var doc = JsonDocument.ParseValue(ref reader);
        var version = doc.RootElement.GetProperty("schemaVersion").GetInt32();

        return version switch
        {
            1 => MigrateFromV1(doc.RootElement),
            2 => DeserializeV2(doc.RootElement, options),
            > 2 => throw new InvalidDataException(
                $"Schema version {version} is from a newer app version. Upgrade WT Launcher."),
            _ => throw new InvalidDataException($"Unknown schema version: {version}")
        };
    }

    public override void Write(Utf8JsonWriter writer, LauncherStoreFile value, JsonSerializerOptions options)
    {
        // Always write current version (v2 today)
        JsonSerializer.Serialize(writer, value, options);
    }
}
```

### Pros
- Full control over version dispatch and migration logic.
- Can handle arbitrary schema transformations (rename fields, split/merge types, change types).
- Single registration point in `LauncherJsonContext`.
- Works with source-generated resolvers (non-issue if converter is registered explicitly).

### Cons
- Custom converter bypasses source-generated contract for `LauncherStoreFile` -- must use `JsonSerializerOptions.Default` or a second context for the inner deserialization, which breaks Native AOT if the fallback context isn't also source-generated.
- Manual `Utf8JsonReader` navigation is error-prone; must handle `JsonDocument` correctly.
- The "inner deserialize" problem: calling `JsonSerializer.Deserialize<T>()` inside a converter for `T` causes infinite recursion unless you strip the converter from options or use a different context instance.

### WT Launcher fit

**Good fit** for major schema version bumps (v1 -> v2), but over-engineered for additive changes within a version. The main friction is the inner-deserialize loop with source-generated contexts.

### Key reference

- [How to write custom converters - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to)
- [Stack Overflow: How to use default serialization in a custom STJ JsonConverter](https://stackoverflow.com/questions/65430420/how-to-use-default-serialization-in-a-custom-system-text-json-jsonconverter) -- covers the inner-deserialize recursion workaround.

---

## 2. STJ Polymorphic Deserialization (`[JsonDerivedType]`)

### How it works

Define a base `LauncherStoreFile` and each schema version as a derived type. STJ's `[JsonDerivedType]` attribute adds a `$type` discriminator to the JSON -- or you can use a custom type discriminator property.

```csharp
[JsonDerivedType(typeof(LauncherStoreFileV1), typeDiscriminator: "v1")]
[JsonDerivedType(typeof(LauncherStoreFileV2), typeDiscriminator: "v2")]
public abstract record LauncherStoreFileBase;

public sealed record LauncherStoreFileV1 : LauncherStoreFileBase { /* v1 fields */ }
public sealed record LauncherStoreFileV2 : LauncherStoreFileBase { /* v2 fields */ }
```

### Pros
- Minimal code: STJ handles the dispatch automatically.
- Well-supported in .NET 7+ and source-generated contexts.
- Clean separation of versioned types.

### Cons
- Forces a base type and inheritance hierarchy, which conflicts with the current sealed-record approach.
- The `$type` discriminator is a STJ-specific JSON artifact (not a clean `schemaVersion` integer). Can be customized, but adds complexity.
- Adding a new version requires a new derived type AND updating the base type's `[JsonDerivedType]` attributes (violates Open/Closed).
- Polymorphic deserialization with source generators in Native AOT has known limitations (see [dotnet/runtime #115218](https://github.com/dotnet/runtime/discussions/115218)).

### WT Launcher fit

**Poor fit.** The current schema uses a simple integer `schemaVersion` at the root, not a type discriminator. Forcing an inheritance hierarchy onto the store records would be a net negative. Also, the 5-layer architecture explicitly avoids inheritance in data records.

---

## 3. JSON Schema + Validation

### How it works

Maintain a JSON Schema file (`.json`) per schema version. Before deserialization, validate the raw JSON against the schema corresponding to its `schemaVersion`. Reject if it doesn't conform.

```csharp
// Using JsonSchema.NET or Corvus.JsonSchema
var schema = await JsonSchema.FromFileAsync($"schemas/v{schemaVersion}.json");
var validationResult = await schema.ValidateAsync(jsonString);
```

### Pros
- Schema-as-documentation: the schema file is the contract, human-readable.
- Can express complex constraints (required fields, type constraints, pattern matching) that C# records cannot encode.
- Independent of the C# type system -- can validate before deserialization, catching format errors earlier.

### Cons
- Adds a build-time or deploy-time artifact (`.json` schema files) that must stay in sync with C# records.
- Validation library dependency. The leading options (`Corvus.JsonSchema`, `JsonSchema.NET`, `JsonSchema.Net`) have varying levels of .NET 10 / Native AOT support.
- Two sources of truth: the schema file AND the C# record. If they drift, behavior is undefined.
- Runtime performance cost of validation + deserialization (two passes).
- Schema for `UnmappedMemberHandling.Disallow` equivalent (`additionalProperties: false`) must be explicitly set per schema level.

### WT Launcher fit

**Over-engineered** for a single-file config with one record type. The C# `record` with `[JsonRequired]` and `[JsonPropertyName]` already serves as the schema contract. Adding a separate JSON Schema file would create drift risk for little benefit, especially given the existing comment: "C# record types ARE the schema contract."

---

## 4. Protobuf / FlatBuffers

### How it works

Replace JSON with a binary serialization format that has schema evolution built into its design. Protobuf uses field numbers for wire compatibility; FlatBuffers uses forward-only offsets.

```protobuf
syntax = "proto3";
message LauncherStoreFile {
  int32 schema_version = 1;
  LauncherSettings settings = 2;
  repeated LaunchEntry entries = 3;
  // ...
}
```

### Pros
- Schema evolution is a first-class design goal. Adding fields, deprecating fields, and changing types are well-defined operations.
- Compact binary format (smaller files).
- Fast deserialization (especially FlatBuffers: zero-copy).
- Strongly typed generated code.

### Cons
- **Complete format change**: breaks every existing `launcher.json` file in the wild. Requires a one-time JSON-to-binary migration.
- **Build step required**: `.proto` / `.fbs` files must be compiled to C# via `protoc` / `flatc`. Adds CI complexity.
- **Not human-editable**: binary files cannot be inspected or modified with a text editor. This is a significant loss for a local config file that users may want to hand-edit or diff in version control.
- **Ecosystem friction**: Protobuf in .NET has multiple implementations (`Google.Protobuf`, `protobuf-net`, `Protobuf.NET`), none of which is the "canonical" choice for .NET 10.
- **AOT concerns**: The generated types may not be fully Native AOT compatible without trimming annotations.

### WT Launcher fit

**Rejected.** The config file (`config/launcher.json`) is designed to be human-readable, hand-editable, and tracked in version control. Switching to a binary format would violate these design goals. The project specifically uses JSON for this reason.

---

## 5. STJ Source Generator + Manual Migration Layer

### How it works

Define one source-generated context per schema version, each with its own set of record types. A migration service handles the `schemaVersion` dispatch at the `LauncherStore.Load` level (outside the JSON parser).

```csharp
// Separate source-gen contexts per version
[JsonSerializable(typeof(LauncherStoreFileV1))]
internal sealed partial class LauncherJsonV1Context : JsonSerializerContext { }

// Load dispatches by version
public static LauncherStoreFile LoadWithMigration(string path)
{
    var raw = File.ReadAllText(path);
    using var doc = JsonDocument.Parse(raw);
    var version = doc.RootElement.GetProperty("schemaVersion").GetInt32();

    return version switch
    {
        1 => MigrateV1ToV2(
            JsonSerializer.Deserialize<LauncherStoreFileV1>(raw, V1Options)!),
        2 => JsonSerializer.Deserialize<LauncherStoreFileV2>(raw, V2Options)!,
        > MaxVersion => throw ...
    };
}
```

### Pros
- Each versioned type is a clean, flat record matching its JSON -- no converters, no inheritance.
- Source-generated per version means Native AOT safe.
- Migration functions are explicit, testable, and auditable.

### Cons
- Proliferation of types: each version adds a record + a context class.
- Naming overhead: `LauncherStoreFileV1`, `LauncherStoreFileV2`, ..., each with full property declarations.
- If schema versions differ by only one field, you still need a full duplicate record type.
- Migration logic lives at the `LauncherStore` layer, not in `LauncherJson`, blurring the persistence/schema boundary.

### WT Launcher fit

**Acceptable** but heavy for the current use case (only 2 versions, likely low churn). The type duplication per version would add maintenance burden without a corresponding benefit until schema versions exceed 3+.

---

## 6. Hybrid: Loose Read + Strict Write

### How it works

Separate the **read** options (lenient: skip unknown fields, allow missing optional fields) from the **write** options (strict: always write current schema with all fields). This is the simplest approach and directly addresses the forward-compatibility problem.

```csharp
// Read options -- lenient for forward compat
internal static readonly JsonSerializerOptions LooseOptions = new()
{
    TypeInfoResolver = LauncherJsonContext.Default,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip, // skip unknown
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
};

// Write options -- strict, current schema only
internal static readonly JsonSerializerOptions StrictOptions = new()
{
    TypeInfoResolver = LauncherJsonContext.Default,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, // catch drift on write
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
```

On **read**: use `LooseOptions` to parse the JSON. New fields added by a future version are silently ignored (forward compat).
On **write**: always use `StrictOptions`, which writes only known fields. Disallow on write catches C#-side drift (accidentally adding a property without `[JsonPropertyName]`).

The `schemaVersion` check in `LauncherStore.Load` serves as the actual cross-version guard:

```csharp
if (store.SchemaVersion > ExpectedStoreSchemaVersion)
    throw new InvalidDataException("Newer version -- upgrade WT Launcher.");

if (store.SchemaVersion < MinimumSupportedSchemaVersion)
    throw new InvalidDataException("Config too old -- regenerate or migrate.");
```

### Pros
- **Lowest code change**: no converters, no new types, no schema files. Just two `JsonSerializerOptions` instances instead of one.
- **Native AOT safe**: uses the same source-generated context.
- **Forward compat**: old app silently ignores fields it does not understand. Config is still readable.
- **Backward compat**: new app reads old config (as long as all required fields exist).
- **No migration needed** for additive changes within a version.
- **`.NET 10 advantage`**: `JsonSerializerOptions.Strict` is available as a preset for writing.

### Cons
- Silent field drop on read: if a future version adds a critical field, the old app won't see it. But this is *correct behavior* -- the old app cannot meaningfully use future data.
- Schema version guard (`> ExpectedStoreSchemaVersion`) is a runtime check, not a compile-time guarantee.
- Does not automatically handle breaking changes (field renames, type changes). Those still require a migration function.

### WT Launcher fit

**Best fit.** The simplest solution that satisfies all constraints:

1. Forward compat: loosen read. Old app sees `schemaVersion > 2` and throws clear error, or if schema version is still 2 but new optional fields exist, they are silently ignored.
2. Backward compat: new app reads old v1/v2 config naturally (all existing fields are required, defaults for new fields).
3. Schema version lower bound: add `MinimumSupportedSchemaVersion = 1` check.
4. Migration path: for breaking changes, add a dedicated `MigrateV1ToV2` function called from `LauncherStore.Load`, outside the JSON parser.

---

## Recommended Strategy for WT Launcher

### Primary: Hybrid Loose Read + Strict Write (Strategy 6)

This is the recommended primary strategy because it eliminates the forward-compatibility problem with minimal code change, preserves the existing architecture, and requires no new dependencies.

#### Concrete `LauncherJson` changes

**1. Split `Options` into `LooseOptions` (read) and `StrictOptions` (write)**

```csharp
// LauncherJson.cs -- Replace the single Options with two

// Read: lenient. Silently skip unknown fields so an older app version
// can still load a config written by a newer version that added fields.
private static readonly JsonSerializerOptions LooseOptions = new()
{
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    MaxDepth = 64,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    TypeInfoResolver = LauncherJsonContext.Default,
};

// Write: strict. Catch unintended property drift (e.g. adding a field
// without [JsonPropertyName]) at write time, not silently at read time.
// Uses .NET 10's built-in Strict preset as a base.
private static readonly JsonSerializerOptions StrictOptions = new()
{
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    WriteIndented = false,
    MaxDepth = 64,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    TypeInfoResolver = LauncherJsonContext.Default,
};
```

**2. Update `Load<T>` to use `LooseOptions`**

```csharp
public static T? Load<T>(string path) where T : notnull
{
    using var stream = File.OpenRead(path);
    return JsonSerializer.Deserialize(stream, typeof(T), LooseOptions) is T result
        ? result
        : default;
}
```

**3. Update `Save<T>` to use `StrictOptions`**

```csharp
public static void Save<T>(T value, string path, bool backup)
{
    // ... directory creation ...
    var json = NormalizeLineEndings(JsonSerializer.Serialize(value, StrictOptions));
    // ... temp file + File.Replace ...
}
```

**4. Remove `[JsonUnmappedMemberHandling(Disallow)]` from `LauncherStoreFile`**

The record-level attribute forces `Disallow` even when `LooseOptions` is used, because STJ composes attribute-level and options-level settings with attribute winning. To make the split actually work, the record attribute must be removed -- the `Disallow` lives only on `StrictOptions`.

**5. Add lower-bound version check in `LauncherStore.LoadAndValidate`**

```csharp
// LauncherStore.cs
public const int MinimumSupportedSchemaVersion = 1;

// Inside LoadAndValidate:
if (store.SchemaVersion < MinimumSupportedSchemaVersion)
    throw new InvalidDataException(
        $"Launcher store schema version {store.SchemaVersion} is too old. " +
        $"Minimum supported version is {MinimumSupportedSchemaVersion}. Regenerate config or use an older app version.");
```

**6. Update `LauncherJsonContext` source gen attributes**

Remove the `UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow` from `[JsonSourceGenerationOptions]` on the context class. The disallow should be applied only through `StrictOptions` at runtime.

```csharp
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(LauncherStoreFile))]
// ... rest unchanged ...
internal sealed partial class LauncherJsonContext : JsonSerializerContext { }
```

#### Migration function for breaking changes

For cases within a major version that need field renames or type changes, add a dedicated static migration function in `LauncherStore`:

```csharp
private static LauncherStoreFile MigrateSchema(LauncherStoreFile store)
{
    // v1 -> v2: no migration needed (v1 was merged from legacy files)
    // Future: v2 -> v3 example:
    // if (store.SchemaVersion == 2) { ... transform ... }
    return store;
}
```

Call it from `LoadAndValidate` after deserialization:

```csharp
store = MigrateSchema(store);
```

#### Test plan additions

1. **Forward compatibility test**: serialize with a future schema (current type + extra unknown JSON property), verify old app loads it via `LooseOptions` without throwing.
2. **Backward compatibility test**: serialize with `LooseOptions`, verify re-load produces semantically identical object.
3. **Lower-bound rejection test**: set `schemaVersion = 0` in JSON, verify `InvalidDataException` is thrown.
4. **Upper-bound rejection test**: existing test (schemaVersion > ExpectedStoreSchemaVersion) already exists -- extend to test the error message.
5. **Strict write test**: verify writing with `StrictOptions` throws on unexpected properties (e.g., a property without `[JsonPropertyName]`).
6. **Migration test (when needed)**: given a v1 JSON fixture, verify `LoadAndValidate` + `MigrateSchema` produces a correct v2+ object.

### Secondary: Custom Converter for Major Version Breaks (Strategy 1)

Keep the Hybrid strategy as the default. If a future schema version requires a **breaking** change (e.g., renaming `Entries` to `Items`, changing a field type from string to int), then add a custom `JsonConverter<LauncherStoreFile>` specifically for that migration. The Hybrid strategy handles all non-breaking changes (add field, deprecate field) within a version.

This avoids the converter complexity for the common case while keeping it available for the rare breaking case.

### Rejected strategies for this codebase

| Strategy | Reason for rejection |
|---|---|
| **Polymorphic Deserialization** | Requires inheritance hierarchy, incompatible with sealed-record architecture, discriminator doesn't match existing `schemaVersion` field. |
| **JSON Schema + Validation** | Two sources of truth (schema file vs. C# records), dependency risk, performance cost. C# records ARE the schema contract. |
| **Protobuf / FlatBuffers** | Binary format is incompatible with human-readable, hand-editable, VCS-tracked config design. Build step adds CI burden. |
| **Full Manual Migration Layer** | Acceptable but over-engineered for expected low schema churn. Type proliferation with little benefit until 3+ versions exist. |

### Migration path from current state

| Step | What | Files touched | Risk |
|---|---|---|---|
| 1 | Split `Options` into `LooseOptions` / `StrictOptions` | `LauncherJson.cs` | Low |
| 2 | Remove `[JsonUnmappedMemberHandling(Disallow)]` from `LauncherStoreFile` | `LauncherStoreFile.cs` | Low -- still enforced on write |
| 3 | Remove `UnmappedMemberHandling` from `[JsonSourceGenerationOptions]` | `LauncherJsonContext.cs` | Low -- moved to `StrictOptions` |
| 4 | Add `MinimumSupportedSchemaVersion` check | `LauncherStore.cs` | Low |
| 5 | Update `RootType_RoundTripsSuccessfully` to use new options pattern | Test file | Low |
| 6 | Update `UnknownJsonKey_Load_RejectedByStrictSchema` to test writes | Test file | Low -- rename/reorient |
| 7 | Add forward-compat test (unknown field silently skipped on read) | New test | Low |
| 8 | Wire `MigrateSchema` call in `LoadAndValidate` (empty for now) | `LauncherStore.cs` | Low |
| 9 | Run full gate (`verify.ps1`) | -- | Low |

### Key design decisions summary

1. **Loosen reads, tighten writes** -- This is the core insight. `UnmappedMemberHandling.Disallow` is valuable for catching developer mistakes during writes (the time when schema drift matters), but harmful for reads where forward compatibility is needed.
2. **Schema version integer stays as the cross-version guard** -- `schemaVersion` at the root is the one field that MUST always be readable, by any version. Both loose and strict options MUST support reading `schemaVersion`.
3. **Migration functions live in `LauncherStore`, not `LauncherJson`** -- `LauncherJson` is the persistence engine (load bytes, save bytes). Schema logic belongs in the Store layer.
4. **No zero-version assumption** -- A `schemaVersion` of 0 or negative is rejected. The minimum is explicitly 1 (the initial unified format).
5. **Additive changes within a version need NO migration** -- Adding a new optional field to `LauncherStoreFile` is just adding a property with a default value. The loose reader skips it in old files; the strict writer includes it in new files. This is the common case and should be friction-free.
