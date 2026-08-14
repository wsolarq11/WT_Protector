# Research: Test Coverage Audit

- **Query**: Systematic review of all ~89 test files (~1950 assertions) for quality, correctness, edge cases, and gaps
- **Scope**: Internal
- **Date**: 2026-07-26

## Methodology

Read every `.cs` file in `src/avalonia-launcher/tests/WTLauncher.Tests/` (excluding `obj/` and `bin/` generated files). Counted 89 actual test source files with ~1952 `Assert.*` calls across them. Also performed targeted grep searches for timing dependencies, environment-variable dependencies, and `DateTime.Now` usage.

## Findings Summary

### Test Structure

- All test classes are `sealed` -- good practice, prevents unintended inheritance.
- Namespace is uniform `WTLauncher.Tests`.
- `MainWindowViewModelTests` is a `partial class` split across ~25 files, sharing helpers through `Shared.cs`.
- Global usings: System, System.Collections.Generic, System.IO, System.Linq, System.Net.Http, System.Threading, System.Threading.Tasks, Xunit.
- Test parallelization is **globally disabled** (`[assembly: CollectionBehavior(DisableTestParallelization = true)]` in AssemblyInfo.cs). This is presumably because most tests touch the filesystem via `TempDirectory`/`LauncherStore.Save`/`LauncherJson.Load`.
- Uses **xUnit v3** (packages `xunit.v3`, `TngTech.ArchUnitNET.xUnitV3`).

### No Timing-Dependent Flakiness

- Zero occurrences of `Thread.Sleep`, `Task.Delay`, or `await Task.Delay`.
- All timestamp-dependent tests use `DateTimeOffset.UtcNow` (never `DateTime.Now`, `DateTimeOffset.Now`).
- Tests that need controlled timestamps inject `Func<DateTimeOffset>` parameters to override the clock (`utcNow` parameter in `CreateViewModel`).

### Environment Dependencies

- `Environment.CurrentDirectory`: Used in PBTs (`CsCheckPropertyTests`, `LauncherJsonSerializerPbtTests`) as a valid existing directory for generated entries. Acceptable since PBT input is random and needs a real directory.
- `Environment.SystemDirectory`: Used once in `LauncherJsonContextRegistrationTests` roundtrip test.
- `Environment.CurrentDirectory` in `LauncherPersistenceTestFixtures.RootDrive`: extracts the drive letter for construction of test paths.
- No `Environment.GetEnvironmentVariable`, `Environment.UserName` found.

### Edge Case Coverage (Impressive)

The test suite covers a wide range of edge cases:

| Category | Examples |
|---|---|
| Null input | `null` strings rejected with `ArgumentNullException` on IDs, names, filter text, command templates |
| Empty/whitespace | Empty and whitespace strings rejected for entry IDs, names, sort modes, view modes, confirmation labels |
| Unicode normalization | Cyrillic homoglyph detection, fullwidth percent injection (fullwidth `%` U+FF05), surrogate pair handling at 256-char boundary, FormKC normalization for accented characters |
| Boundary values | 256-char truncation for EntryId and PresetId, surrogate pair preservation at boundary, 32767-char command-line limit |
| PowerShell injection | Base64 encoding verification for shell injection prevention, backtick escape test |
| File I/O errors | Missing files, corrupt JSON, locked files, exceeded 50MB limit, read-only file save failure, directory-instead-of-file save failure |
| Concurrency | Concurrent ErrorLog writes, StoreGrain dispatch serialization, SemaphoreSlim pattern |
| Drag reorder edge cases | Unknown source IDs, unknown target IDs, same-source-and-target no-op, non-custom-sort-mode rejection, filtered reorder equivalence |
| Save failure rollback | Settings file replaced with directory, config file replaced with directory, preset file replaced with directory |
| Snapshot isolation | Mutation of entry after passing to dialog/confirmation does not affect behavior; read-only collection wrappers throw on mutation |
| Template edge cases | Circular template references (A->B->A), null/empty template dictionaries |
| Zero-item edge cases | Empty preset, empty sort mode list, empty batch launch, empty search history, empty command analysis |

### Property-Based Testing

Four files use `CsCheck` for property-based testing:

| File | Properties Tested |
|---|---|
| `CsCheckPropertyTests.cs` | RotateBackups never exceeds max, Build produces valid plan for random input, Save/Load roundtrip, Dispatch consistency after random mutations |
| `LauncherJsonSerializerPbtTests.cs` | Serialize/Deserialize roundtrip restores original, normalized line endings |
| `LauncherStoreValidatorPbtTests.cs` | Valid schema passes, newer schema rejected, older schema rejected |
| `LauncherLayoutPbtTests.cs` | Ghost position non-negative, ghost dimensions finite across random inputs |
| `LauncherEntryOrderPbtTests.cs` | SyncWithEntries preserves all known IDs, MoveRelative preserves entries, MoveRelative same-source no-op, BuildIndex covers all inputs, NormalizeSortModeId idempotent |

### Architecture Rules

`ArchitectureRules.cs` contains 14 rules enforced via ArchUnitNET:

- R1-R3: Core layer isolation (no Avalonia, no CommunityToolkit.Mvvm, no App)
- R4: ViewModels must not depend on System.IO.File
- R5: Only LauncherJson + known readers may depend on System.IO.File in Core
- R6: Core must not reference App.ViewModels
- R7-R8: Service layers must not reference Views
- R9-R10: Service layers must not depend on System.IO.File
- R11: Infrastructure must not depend on ViewModels
- R12: Features.Dialogs must not reference Views
- R13: Core (minus scanners) must not reference System.IO.File
- R14: StoreGrain must not call LauncherStore directly

### Notable Patterns and Potential Issues

#### 1. ArchitectureRules R13 vs R5 Overlap (ArchitectureRules.cs:112, 210)

R5 excludes `LauncherJson`, `LaunchArgumentBuilder`, `LauncherStore`, and `AgentDiscoveryScanner`. R13 excludes only `LaunchArgumentBuilder` and `AgentDiscoveryScanner`. R13 is stricter -- if `LauncherStore` or `LauncherJson` directly depend on `System.IO.File` as a type (not just method call), R13 would fail. These files presumably delegate I/O through `ILauncherJsonIO` / `FileStorePersistence` abstractions so the type-level dependency is absent. But if they ever make direct `File.` calls, R13 would fail first while R5 would still pass. Having both rules is belt-and-suspenders but could be confusing on failure -- the developer might fix R13 without realizing why R5 also exists.

#### 2. ServiceContractTests Brittle String Matching (ServiceContractTests.cs:77-92)

The `ServiceConstructors_ValidateParameters` test searches source code for exact strings:

```csharp
if (!entrySource.Contains("dialog ?? throw new ArgumentNullException(nameof(dialog))"))
    violations.Add("EntryService(IDialogService dialog) missing null guard for 'dialog'");
```

This is extremely brittle. A code style change (different formatting, `ArgumentNullException.ThrowIfNull`, splitting across lines, `nameof` spelling) would break the test even though the constructor still validates. These tests are in ServiceContractTests.cs which does source-level analysis.

#### 3. ServiceContractTests Informational Threshold (ServiceContractTests.cs:137)

```csharp
Assert.True(violations.Count <= 20, msg);
```

This assertion always passes (0 <= 20) and is documented as a "known gap". It serves as an informational output via the failure message. While intentional, this is an unusual pattern -- it only surfaces issues if the count exceeds an arbitrary 20 threshold.

#### 4. CollectionContractTests Backup File Access (CollectionContractTests.cs:22)

```csharp
Assert.All(LauncherStore.Load(config + ".bak").Entries, entry => Assert.False(entry.Selected));
```

Directly accesses `config + ".bak"` assuming the backup naming convention. This assumes `LauncherStore.Save` generated a `.bak` file. If the save behavior changes (e.g., `.bak.0` naming instead of `.bak`), this test breaks silently. Currently it works because `Save(config, backup: true)` in `CreatePortableFixture` creates `config.bak`.

#### 5. GroupingTests No Verification on LaunchAll (GroupingTests.cs:127-133)

```csharp
Exception? captured = null;
group.LaunchAllCommand.Execute(null);
Assert.Null(captured);
```

The `captured` variable is declared but never assigned by any `try/catch` -- it stays `null` forever. This test verifies that `Execute` does not throw synchronously (which it would if the command threw during `CanExecute` or `Execute`). But the actual side effect (launch batch) is not verified. The comment ("不验证启动副作用") acknowledges this. This is a known gap, not a bug.

#### 6. ErrorLog Year-Starting Assertion (ErrorLogTests.cs:138)

```csharp
Assert.All(lines, line => Assert.StartsWith("2", line));
```

This assumes all log lines start with year 2xxx. Currently correct (2026+), but would break in the year 3000+. This is a practical concern only at very long timescales.

#### 7. LauncherBatchLaunchReceiptTextTests Hardcoded Counting (multiple files)

Several receipt text tests use a `Count()` helper and hardcoded expected counts for how many times a string appears (e.g., `Assert.Equal(3, Count(receipt, LaunchArgumentBuilder.TerminalExecutable))`). These are behavior-verification tests, but the counting logic is somewhat fragile if receipt formatting changes.

#### 8. No CancellationToken on Service Async Methods (ServiceContractTests.cs:100-138)

This is a **documented gap** rather than an issue. The `ServiceInterfaces_AsyncMethods_Gap_NoCancellationToken` test lists which async service methods lack `CancellationToken` parameters. Per the ponytail comment, this is planned for migration.

#### 9. No High-Contention StoreGrain Stress Test

The `StoreGrain` uses `SemaphoreSlim` for serialization, but there is no test that dispatches hundreds of concurrent mutations to verify:
- No deadlocks
- No lost updates
- No stack overflows from reentrant dispatches
- Memory stability under high churn

The `CsCheckPropertyTests.Dispatch_MaintainsConsistentState` runs random sequential mutations but not concurrent ones.

#### 10. No Integration Tests for Full Startup -> Load -> Render -> Launch Cycle

The test suite is entirely unit-level. There are no integration tests that:
- Start the application process
- Verify the MainWindow renders
- Test actual process launch (StartBatch test delegates use `Action<LaunchPlan>` mocks)
- Test the drag ghost visual rendering

This is appropriate for unit tests, but integration coverage exists only at the verify.ps1 level (hidden GUI smoke test).

### Test Quality Assessment

| Criterion | Verdict |
|---|---|
| Tests have assertions | Pass -- every test verified has at least one `Assert.*` call |
| Fact/Theory appropriate | Pass -- parameterized tests use `[Theory]` with `[InlineData]`/`[MemberData]`, single-case tests use `[Fact]` |
| Tests pass for right reasons | Pass -- assertions verify specific behaviors, not vague properties |
| Flaky tests | Pass -- no timing dependencies, no shared mutable state between tests |
| Tests independent | Pass -- each test creates its own `TempDirectory` and fixture data |
| Edge case coverage | Pass -- null, empty, whitespace, boundary, unicode covered extensively |
| Error path coverage | Pass -- save failures, dialog failures, missing files, corrupt data, locked files |
| Concurrency testing | Partial -- ErrorLog has concurrent write test, StoreGrain has sequential mutation tests but no concurrent stress test |
| Architecture enforcement | Pass -- 14 ArchUnitNET rules, plus source-level contract tests for services and XAML |
| Read-only projection guarding | Excellent -- many tests verify `ISet<>`, `IList<>`, `IReadOnlyList<>` throw on mutation |
| Snapshot isolation | Excellent -- tests verify mutation of source data after capture does not propagate |

### Coverage Gaps (Minor)

1. **StoreGrain concurrent stress test** -- no high-concurrency test that pounds with parallel Dispatch calls.
2. **End-to-end launch integration** -- no test that actually starts a real process (mocked).
3. **AOT compatibility** -- `LauncherJsonContextRegistrationTests` verifies types are registered but does not verify Native AOT publishing succeeds.
4. **Localization** -- status text assertions use Chinese strings; no test verifies behavior when UI culture is non-Chinese.
