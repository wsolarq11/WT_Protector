# Quality Guidelines — Core Layer

> Code standards, testing requirements, and forbidden patterns for WTLauncher.Core.

---

## Invariants (Must Never Break)

1. **Core never references Avalonia** — verified by `dotnet test`.
2. **Core never references CommunityToolkit.Mvvm** — verified by `dotnet test`.
3. **All persisted JSON uses LF (`\n`) line endings** — verified by `verify.ps1`.
4. **`config.json` entries always have `id`, `name`, `directory`, `command` non-empty** — enforced by `LauncherConfigStore`.
5. **One source per state** — view mode, sort mode, drag session, card density each have exactly ONE writable owner.

**Source:** `src/avalonia-launcher/README.md` — "Key invariants" section.

---

## Single Source of Truth

Per `DESIGN.md` token ownership rules:

| State | Single Owner | Read-only Projection |
|-------|-------------|---------------------|
| View mode | `_selectedDisplayMode` | `IsCardsViewMode` / `IsDetailedListMode` / `IsNamesViewMode` |
| Sort mode | `_selectedSortMode` | `IsCustomSortMode` |
| Card density | `LauncherEntryLayoutDensity` | `IsCompactLayout` / `IsMicroLayout` |
| Drag cues | `DragCueState` | `IsDragPreviewTarget` / `IsDragPreviewSource` / `IsDragInsertBefore` / `IsDragInsertAfter` |
| Drag session | `DragReorderSession?` | no loose fields allowed |
| Selection | `IsSelected` (user) / `SetSelectionFromOwner` (batch) | no global bulk flag |

**Rule:** Read-only projections must be computed properties, not stored fields. Never store a second copy of state.

---

## Testing Requirements

**Test framework:** xUnit
**Test project:** `tests/WTLauncher.Tests/` — 71 test files (approximately 410 `[Fact]`/`[Theory]` attributes)
**Run gate:** `dotnet test -c Release` (556 tests, updated 2026-06-23)

### Test Categories

| Category | Example files | Count |
|----------|--------------|-------|
| Core unit tests | `LaunchArgumentBuilderTests.cs`, `LauncherConfigStoreTests.*.cs`, `LauncherEntryLayoutCalculatorTests.cs` | ~25 |
| ViewModel tests | `MainWindowViewModelTests.*.cs` (12 partial files) | ~12 |
| View logic tests | `MainWindowDragGhostFactoryTests.cs`, `MainWindowXamlContractTests.cs` | ~15 |
| Contract/integration tests | `RepositoryConfigContractTests.*.cs`, `StatusTextContractTests.cs`, `DialogLayoutContractTests.cs` | ~8 |
| Persistence tests | `LauncherConfigMutationPersistenceTests.cs`, `LauncherEntryMutationPersistenceTests.cs` | ~10 |

### Test Patterns

1. **One test file per production class.**
2. **Shared test fixtures in `LauncherPersistenceTestFixtures.cs`, `TestRepositoryPaths.cs`, `TempDirectory.cs`.**
3. **Factory methods for test data** — e.g., `PresetFile()`, `Entry()` in test shared files.
4. **Tests verify both success and failure paths** — every persistence test has rollback scenarios.

---

## Forbidden Patterns

- ❌ **`dynamic` or `Dictionary<string, object>`** — always define typed records.
- ❌ **`#pragma warning disable`** without a comment explaining why.
- ❌ **Public mutable fields** — use properties.
- ❌ **Static mutable state** — all static classes are stateless helpers.
- ❌ **Direct `File.WriteAllText` outside `LauncherJson`** — always use the persistence wrapper.
- ❌ **Adding a NuGet reference to Core** — Core must stay dependency-free (beyond BCL).
  // ponytail: LiteDB reference (`LiteDB` NuGet) is an intentional exception for the IStore persistence abstraction. If adding another dependency, challenge it against the same bar: is this the minimum viable dependency for a cross-layer concern that cannot be implemented via BCL alone?
- ❌ **Silent null-to-empty coercion without validation** — bad data should throw, not silently normalize.
- ❌ **`catch (Exception)` without narrowing** — only catch specific types you can actually handle. In ViewModel command handlers, use `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` — the shared filter in `ExceptionFilters.cs` centralizes the fatal-exception exclusion. For UI delegate calls (dialog confirmations), bare `catch (Exception)` is acceptable because the delegate boundary is unknown. See [pitfalls §2.3](../guides/pitfalls.md#23-kiss-合并后-catch-过宽).
- ❌ **Snapshot captured before `await` in mutation methods** — snapshot must be captured AFTER async dialog confirmation, not before. Capturing before creates a window where concurrent operations can be lost on rollback. (Added 2026-06-23 ultrareview B6)
- ❌ **Hardcoded counts in documentation** ("13 条规则", "55 类") — always say "持续更新". See [pitfalls §3.1](../guides/pitfalls.md#31-不要在文档里硬编码n-条n-类).
- ❌ **Redundant backing fields on positional records** — if a positional record uses `{ get; init; }` with validation, use auto-properties with init-assigned defaults (`public string X { get; init; } = RequireText(X, nameof(X));`), NOT manual backing fields that duplicate the validation. (Added 2026-06-23 Read Every Struct H2)
- ❌ **Unused `const` tokens in `LauncherDesignTokens`** — every token must have at least one consumer in `src/` (excluding tests). Unused tokens are dead code and must be removed. Verify with `grep -r <TokenName> src/ --include='*.cs' --include='*.axaml'`. (Added 2026-06-23 Read Every Struct)
- ❌ **Event handlers with zero subscriptions** — a `private void On*` method in code-behind that has no `+=` subscription and no XAML event wiring is dead code. Remove it. (Added 2026-06-23 Read Every Struct H27)
- ❌ **Mutable data carriers without compile-time mutation guard** — `{ get; set; }` on types shared across ViewModels allows invisible shared-mutation bugs (e.g., `_entry.Selected = true` in one ViewModel silently diverges from another's copy). All shared data carriers must use `{ get; init; }` + `sealed record` — mutations produce a new instance via `with { }`, making every state change explicit and traceable. (Added 2026-06-23 — LaunchEntry immutability hardening)
- ❌ **`[JsonIgnore]` on SchemaVersion fields** — prevents the version number from being written to and read from JSON, making `if (settings.SchemaVersion != Expected)` into dead code. Use `[JsonPropertyName]` without `[JsonRequired]` to allow backward compatibility (missing field → default). (Added 2026-06-25 UltraReview R1)
- ❌ **Store.Load only recovering `.bak` on missing files** — must also recover when the file exists but is corrupt (`JsonException`/`InvalidDataException`). Wrap the deserialize+validate path in try-catch; on failure, attempt `.bak` recovery before throwing. (Added 2026-06-25 UltraReview R3)
- ❌ **Inconsistent Normalize behavior across ID types** — `LauncherEntryIds.Normalize` and `LauncherPresetIds.Normalize` must use the same truncation/case/exception strategy. Normalize functions should sanitize (truncate, trim, strip control chars) without throwing; validation is the caller's job. (Added 2026-06-25 UltraReview R10)
- ❌ **Ignoring `SaveConfig()` / `Save*()` return values** — all persistence methods return `bool` indicating success. Callers must check the return value. A discarded `false` means silent data loss: in-memory state is updated but disk write failed. (Added 2026-06-25 UltraReview R6)
- ❌ **Static mutable collections** — `static List<T>` / `static Dictionary<K,V>` with `Clear()`+`Add()` is thread-unsafe global state. Return tuples or immutable collections instead. (Added 2026-06-25 UltraReview R14)
- ❌ **Asymmetric failure strategies in batch operations** — `break` vs `continue` in the same workflow class. All phases of a batch operation must use the same failure strategy. If they differ by design, document the reason explicitly. (Added 2026-06-25 UltraReview R9)
- ❌ **Opaque internal marker comments** (`// H4`, `// L32`, `// M11`) — these reference external issue trackers and are meaningless in standalone code. Replace with descriptive comments explaining what and why. (Added 2026-06-25 UltraReview R30)
- ❌ **No Unicode normalization in text-processing functions** — `Normalize()`/`Sanitize()` methods must include `string.Normalize(NormalizationForm.FormKC)` to handle fullwidth/halfwidth and composed/decomposed character equivalence. Without it, search and sort produce inconsistent results. (Added 2026-06-25 UltraReview R26)
- ❌ **No command-line length check before Process.Start** — `CreateProcessW` has a 32767-character hard limit. Argument builders must validate final string length and throw a clear error before reaching Process.Start. (Added 2026-06-25 UltraReview R27)
- ❌ **`defaultValue` bypassing validation in Store.Load** — `LoadWithRecovery`'s `defaultValue` parameter must pass through the same `Validate()`/`Normalize()` pipeline as the loaded data. Returning an unvalidated `new()` instance means downstream code may encounter unvalidated state. (Added 2026-06-26 UltraReview H3)
- ❌ **IStore bypass** — when a persistence-layer refactoring introduces an `IStore` abstraction (`_store.Load/Save`), every existing direct call to `LauncherStore.Load/Save` must be migrated. Leaking one direct call creates a silent data-split between the primary store (e.g. LiteDB) and the bypassed legacy path (JSON). After migration: `grep -n "LauncherStore\.\\(Load\\|Save\\)(" src/` must return zero hits in the App layer. (Added 2026-06-30 UltraReview C1/C2)
- ❌ **Exception→user message using blacklist (deny-list) pattern** — `ForUser()` must use whitelist (allow-list): known-safe types pass through; everything else gets a generic message. A blacklist (`FileNotFoundException → safe, _ → raw`) leaks internal details for unmapped types. (Added 2026-06-26 UltraReview H8)
- ❌ **JSON smoke/fixture configs missing `[JsonRequired]` fields** — when a `[JsonRequired]` attribute is added to any JSON model, all test fixtures (test factories, `verify.ps1` smoke, README examples) must be updated to include the new field. Missing fields cause `JsonException` at startup only in published builds. (Added 2026-06-26 UltraReview smoke fix)

## Architecture Invariants (Compile-Time Enforcement)

These are not conventions — they are physically enforced by the build system.

### 1. Null Safety
```xml
<!-- WTLauncher.Core.csproj -->
<Nullable>enable</Nullable>
<WarningsAsErrors>nullable</WarningsAsErrors>
```
Core layer treats nullable warnings as compile errors. NullReferenceException is physically impossible in the data layer.

### 2. Immutable Data Flow
```csharp
// All shared data carriers use init-only properties + sealed record
public sealed record LaunchEntry
{
    public string Id { get; init; } = "";
    // ...
}
// Mutation = new instance — explicit, traceable, compiler-enforced
entry = entry with { Selected = true };
```
The compiler rejects `entry.Selected = true` — shared-mutation bugs are physically impossible.

### 3. Immutable Workflow Mutation via `ref`
When a static workflow method needs to modify an immutable record's scalar properties (while the `List<T>` contents remain mutable), use `ref` parameters:
```csharp
public static LauncherPreset AddPreset(ref LauncherPresetFile presetFile, ...)
{
    presetFile.Presets.Add(preset);          // List<T> is mutable
    presetFile = presetFile with { DefaultPresetId = preset.Id }; // scalar → with { }
    return preset;
}
```
The `ref` keyword makes the replacement explicit at every call site (`ref _presetFile`).

### 4. CI Headless Detection
```powershell
$isCi = $env:CI -eq 'true'
if ($isWindowsPlatform -and -not $isCi) { ... }  # GUI smoke skipped in CI
```
GitHub Actions sets `CI=true`. Use this to skip checks requiring a display (GUI smoke) or user-specific paths (config directory existence). The receipt reports `'skipped-ci-headless'` for audit trail.

---

## Cross-Reference

- [Pitfalls — 跨会话踩坑实录](../guides/pitfalls.md) — 50+ 真实 session 取证的坑与预防规则。涉及编码风格、工具使用、调试顺序的元教训。

---

## Code Review Checklist

- [ ] Does new Core code reference Avalonia or CommunityToolkit? (If yes — reject.)
- [ ] Are all new JSON fields decorated with `[JsonPropertyName]` and `[JsonRequired]`?
- [ ] Are string defaults `""` (not `null`)?
- [ ] Does every enum have a paired static helper class with `NormalizeId()` / `ToId()`?
- [ ] Are new test fixtures added to the appropriate shared file?
- [ ] Do new persistence paths use atomic temp-file writes?
- [ ] Are Chinese status texts in the appropriate static class?
- [ ] **Unicode safety**: Does any code truncate strings? → Must check surrogate pair boundaries via `char.IsHighSurrogate`.
- [ ] **Unicode safety**: Does any code strip "unsafe" characters? → Must strip BOTH `UnicodeCategory.Control` AND `UnicodeCategory.Format`.
- [ ] **Unicode safety**: Are cross-reference keys (IDs) normalized? → `Normalize(NormalizationForm.FormC)` before comparison.
- [ ] **Percent injection**: Does `ExpandEnvironmentVariables` run AFTER FormKC normalization? → Check for fullwidth `%` (U+FF05 → U+0025).
- [ ] **UNC paths**: Does any code accept user-controlled directory paths? → Reject UNC paths (`\\` prefix).
- [ ] **PATH hijack**: Does any code resolve executables? → Never fall back to bare names resolved via `PATH`.
- [ ] **Test assertions**: Are any `Assert.Equal(X, X)` self-comparisons? → Replace with concrete expected values.
- [ ] **Test assertions**: Do JSON property checks verify BOTH missing AND extra fields? → Use bidirectional set comparison.
- [ ] **Multi-file saves**: Do they touch 2+ config files? → Must use stage-all-then-commit, not sequential-save-with-rollback.
- [ ] **Backup recovery**: Is the retry OUTSIDE the try/catch? → Inside = infinite loop when backup is also corrupt.
- [ ] **Schema version**: Are `!=` checks replaced with directional `<` and `>` checks? → Old reject + new upgrade message.
- [ ] **`[JsonRequired]` propagation**: When adding `[JsonRequired]` to any model property, did you search and update ALL JSON fixtures? → `verify.ps1` smoke config, test factories, README examples, `config-lib.ps1` defaults. (Added 2026-06-26 UltraReview smoke fix)
- [ ] **Stacked bugs**: When fixing a bug that unblocks a verify gate, did the SAME gate surface a SECOND bug? → The diagnostic onion pattern: the first bug masked the second. After fixing one gate failure, re-run the FULL gate — don't assume a clean pass. (Added 2026-06-26 UltraReview TimeSpan + schemaVersion double-bug)
- [ ] **Atomic save**: `File.Replace` (backed by `MoveFileEx` with `MOVEFILE_REPLACE_EXISTING`, atomic on the same NTFS volume) when target exists; `File.Move` only for first-save. `File.Move(overwrite:true)` is NOT atomic. (Added 2026-06-28)
- [ ] **Backup rotation order**: Copy-then-rotate, not rotate-then-copy. If rotation runs first and crashes, .bak is missing. (Added 2026-06-28)
- [ ] **Cross-layer validation**: Same check in two layers MUST use same semantics (case-sensitivity, boundary: `> N` vs `>= N`). Share constants or methods. (Added 2026-06-28)
- [ ] **Exception unwrap loop**: Must have max-iteration guard (`for i<16`) — null InnerException causes infinite loop. (Added 2026-06-28)
- [ ] **NaN/Infinity guard**: All layout arithmetic must guard with `double.IsFinite`/`double.IsNaN` — NaN propagates through all ops. (Added 2026-06-28)
- [ ] **Single-file over multi-file**: When config needs cross-section consistency, merge sections into one file + `File.Replace`. Do NOT implement journal/pre-commit/multi-file-commit protocols unless sections genuinely cannot live in one file. (Added 2026-06-28 single-file architecture refactoring)<｜end▁of▁thinking｜>
