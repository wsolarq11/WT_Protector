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
**Test project:** `tests/WTLauncher.Tests/` — 73 test files  
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
- ❌ **Silent null-to-empty coercion without validation** — bad data should throw, not silently normalize.
- ❌ **`catch (Exception)` without narrowing** — only catch specific types you can actually handle. In ViewModel command handlers, use `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` — the shared filter in `ExceptionFilters.cs` centralizes the fatal-exception exclusion. For UI delegate calls (dialog confirmations), bare `catch (Exception)` is acceptable because the delegate boundary is unknown. See [pitfalls §2.3](../guides/pitfalls.md#23-kiss-合并后-catch-过宽).
- ❌ **Snapshot captured before `await` in mutation methods** — snapshot must be captured AFTER async dialog confirmation, not before. Capturing before creates a window where concurrent operations can be lost on rollback. (Added 2026-06-23 ultrareview B6)
- ❌ **Hardcoded counts in documentation** ("13 条规则", "55 类") — always say "持续更新". See [pitfalls §3.1](../guides/pitfalls.md#31-不要在文档里硬编码n-条n-类).
- ❌ **Redundant backing fields on positional records** — if a positional record uses `{ get; init; }` with validation, use auto-properties with init-assigned defaults (`public string X { get; init; } = RequireText(X, nameof(X));`), NOT manual backing fields that duplicate the validation. (Added 2026-06-23 Read Every Struct H2)
- ❌ **Unused `const` tokens in `LauncherDesignTokens`** — every token must have at least one consumer in `src/` (excluding tests). Unused tokens are dead code and must be removed. Verify with `grep -r <TokenName> src/ --include='*.cs' --include='*.axaml'`. (Added 2026-06-23 Read Every Struct)
- ❌ **Event handlers with zero subscriptions** — a `private void On*` method in code-behind that has no `+=` subscription and no XAML event wiring is dead code. Remove it. (Added 2026-06-23 Read Every Struct H27)
- ❌ **Mutable data carriers without compile-time mutation guard** — `{ get; set; }` on types shared across ViewModels allows invisible shared-mutation bugs (e.g., `_entry.Selected = true` in one ViewModel silently diverges from another's copy). All shared data carriers must use `{ get; init; }` + `sealed record` — mutations produce a new instance via `with { }`, making every state change explicit and traceable. (Added 2026-06-23 — LaunchEntry immutability hardening)

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
// All shared data carriers use init-only properties
public sealed record LaunchEntry
{
    public string Id { get; init; } = "";
    // ...
}
// Mutation = new instance — explicit, traceable, compiler-enforced
entry = entry with { Selected = true };
```
The compiler rejects `entry.Selected = true` — shared-mutation bugs are physically impossible.

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
