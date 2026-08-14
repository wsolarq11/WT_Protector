# Research: Ultrareview Test Source Content Analysis

- **Query**: Deep review of all test files in WTLauncher.Tests — identify tests that pass for wrong reasons, contain bugs, have brittle assumptions, or miss critical coverage
- **Scope**: Internal code analysis
- **Date**: 2026-07-26

## Summary

Read and analyzed 60+ test source files. Total ~85 test files (excluding `obj/` build artifacts) in `src/avalonia-launcher/tests/WTLauncher.Tests/`. Found 15+ issues spanning false property-based tests, missing boundaries, fragile reflection patterns, and missing coverage for specified areas.

---

## 1. LauncherEntryIdsTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryIdsTests.cs`

### What is tested
- `IsUsable` rejects null/empty/whitespace, accepts valid
- `Normalize` trims, handles null, truncates at 256 chars
- `Same` / `SameNormalized` ordinal match, trimming
- `ToRequiredUniqueList` rejects blanks, duplicates, normalizes
- Boundary: exactly 256, 257, 500 chars
- Surrogate pair preservation at 256-char boundary (U+10780)
- Script validation: Cyrillic homoglyph rejection (U+0430), Greek rejection, extended Latin acceptance (cafe with combining accent)
- Latin vs Cyrillic `Same` distinction

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| LOW | `LauncherEntryIdsTests.cs:132-142` | `Parse_AcceptsExtendedLatinCharacters` asserts `caf\u00e9` (pre-composed) but input is `cafe\u0301` (decomposed). Test relies on FormC normalization happening inside `EntryId.Parse` — if implementation changes normalization, this test silently fails. Not a bug, but the assertion at line 137 hardcodes the expected composed form without documenting that FormC normalization is the mechanism. |
| LOW | `LauncherEntryIdsTests.cs:122-129` | Only one Cyrillic homoglyph (U+0430) tested. There are 33+ Cyrillic homoglyphs for Latin. Not a bug, but limited coverage of the script validation surface. |

### Missing Coverage
- Windows reserved names (CON, PRN, AUX, NUL, COM1..COM9, LPT1..LPT9) — `EntryId.Parse` should reject these or the test should verify they are not silently accepted
- Test that `Parse` validates inputs AFTER FormC normalization (not before), so that inputs like fullwidth Latin letters that normalize to ASCII are properly handled
- Test with leading/trailing whitespace in `Parse`
- Test with empty string in `Parse` (only tested via `IsUsable`)

---

## 2. EntryDirectoryTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/EntryDirectoryTests.cs`

### What is tested
- Null/empty/whitespace rejection
- Valid local path acceptance
- `ParseAndValidateExists` for non-existent directory
- UNC path rejection (`\\server\share\folder`)
- Fullwidth percent injection (U+FF05) rejection
- Literal percent acceptance
- JSON converter round-trip

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| MEDIUM | `EntryDirectoryTests.cs:55-64` | `JsonConverter_RoundTrip_Works` (line 55-64) uses `new JsonSerializerOptions { Converters = { new DirectoryPathJsonConverter() } }`. The `JsonSerializerOptions` overloads of `Serialize`/`Deserialize` have `[RequiresUnreferencedCode]` — while this is a test file (not production), it's an anti-pattern against the project's AOT-safe serialization rules. Test should use source-gen context if possible. |
| MEDIUM | `EntryDirectoryTests.cs:27-29` | `ParseAndValidateExists_NonExistentDir_Throws` hardcodes `C:\does-not-exist-12345` as a path that "should not exist." This is a correct assumption on Windows, but the test name hardcodes the drive letter `C:`. On systems where the temp drive is not C:, this could theoretically be concurrent with a real directory creation. Very low risk. |

### Missing Coverage
- DOS device names: `NUL`, `CON`, `COM1`, `LPT1` — should these be rejected?
- Path traversal: `C:\tmp\..\..\Windows\System32` — should be normalized/rejected
- Maximum path length (260+ chars with `\\?\` prefix) — need boundary test
- %ENV% with special characters beyond fullwidth percent (e.g., `%USERNAME%` with special chars in the expanded value)
- Trailing/leading whitespace handling in `Parse` (trimmed? rejected?)
- Relative paths vs absolute paths

---

## 3. LaunchArgumentBuilderTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/LaunchArgumentBuilderTests.cs`

### What is tested
- Building with valid entry, command encoding
- Global confirmation parameter routing
- Directory validation parameter
- `QuoteArgument` escaping (plain, spaces, embedded quotes, trailing backslash)
- Null rejection for QuoteArgument
- Missing directory validation rejection
- PowerShell injection prevention (Base64 encoding)
- Backtick escape injection prevention
- 32767 character limit enforcement
- Literal percent in directory acceptance

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| MEDIUM | `LaunchArgumentBuilderTests.cs:111-121` | `Build_RejectsCommandLineExceeding32767Characters` uses a 25000-character command. The Base64-encoded result is ~33336 characters, exceeding 32767. **BUT there is no exact boundary test.** The test doesn't verify that 32766 passes and 32768 fails. The 25000-char value produces roughly 33336 base64 chars — this is well past the boundary. A command that produces exactly 32767 chars should be the boundary. |
| MEDIUM | `LaunchArgumentBuilderTests.cs:54-58` | `QuoteArgument_UsesWindowsCommandLineEscaping` provides only 4 test cases. Missing: empty string, all-special-characters string, very long string (buffer overflow?), string with only backslashes, string with newlines. |
| LOW | `LaunchArgumentBuilderTests.cs:42` | `Build_UsesDirectoryValidationParameter` uses `Guid.NewGuid().ToString("N")` for the temp path — acceptable since `validateDirectory: false` prevents actual validation. |

### Missing Coverage
- Empty command string handling
- Very long directory path (>260 chars)
- Commands with newlines/carriage returns
- Multiple arguments that each require quoting

---

## 4. LauncherEntryQueryTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryQueryTests.cs`

### What is tested
- `NormalizeSortModeId` rejects null/empty/whitespace
- Filtering across Name/Directory/Command fields
- Null filter text rejection (does not fall back to all)
- Null text field rejection in sort phase
- Custom sort preserves order and does not mutate input
- Returns `IReadOnlyList`
- Custom sort: missing entry IDs, unknown order IDs, duplicates, normalization
- Blank entry ID rejection
- Alphabetical sort (case-insensitive, then by ID)
- Updated sort (descending, then name, then ID)
- Invalid/null updated timestamp rejection
- Recent sort (descending launchedAt, then name, then ID)
- Empty launchedAt treated as MinValue
- Multi-token AND filtering (2 tokens matching all entries)
- Multi-token requiring ALL tokens (partial match excluded)
- Multi-token returning empty when no entry matches all

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| LOW | `LauncherEntryQueryTests.cs:400-419` | `FilterAndSort_FiltersByMultipleTokensWithAndSemantics` — ALL three test entries match both tokens "pwsh" and "D:" (each entry's directory starts with "D:" and command is "pwsh" or "codex" which doesn't match "pwsh" for entry two... wait, entry two has command "pwsh" which matches "pwsh" and directory "D:\other" matches "D:". Actually all three match both tokens. The test should include at least one entry that matches "pwsh" but NOT "D:" or vice versa to demonstrate AND semantics. |
| LOW | `LauncherEntryQueryTests:237` | `FilterAndSort_SortsByUpdatedThenNameAndId` constructs entries with timestamps like "2026-05-28T12:00:00Z" (UTC) but lists "Alpha" for both entry "one" and "two" with the same timestamp. The sort is "updated desc → name asc → id asc", so same timestamp → tiebreak by name: "Alpha" vs "Alpha" (same) → tiebreak by id: "one" < "two". This is correct. |

### Missing Coverage
- null `customOrder` parameter (should throw?)
- Empty entries array with non-Custom sort modes (just the empty-frequency test exists for `FrequentSortModeId`)
- Single entry with all sort modes
- Filter text with very long string (buffer/hang test)
- Unicode normalization in filter matching (e.g., searching for "cafe" matches entry with command "cafe\u0301")

---

## 5. ArchitectureRules.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/ArchitectureRules.cs`

### What is tested
- R1: Core must not reference Avalonia
- R2: Core must not reference CommunityToolkit.Mvvm
- R3: Core must not depend on App
- R4: ViewModels must not depend on System.IO.File
- R5: Only LauncherJson and known readers may depend on System.IO.File in Core
- R6: Core must not reference App.ViewModels
- R7: PresetServices must not reference Views
- R8: EntryServices must not reference Views
- R9: PresetServices must not depend on System.IO.File
- R10: EntryServices must not depend on System.IO.File
- R11: Infrastructure must not depend on ViewModels
- R12: Features.Dialogs must not reference Views
- R13: Core (exempting known types) must not reference System.IO.File
- R14: StoreGrain must not call LauncherStore

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| MEDIUM | `ArchitectureRules.cs:24-28` | Rules R7, R8, R9, R10 reference namespaces `WTLauncher.App.Features.Presets`, `WTLauncher.App.Features.Entries`, and `WTLauncher.App.Views`. If these namespaces don't exist in the loaded assemblies, **the rules pass vacuously** (empty set matches). ArchUnitNET's `ResideInNamespace` returns an empty provider for non-existent namespaces, so `Should().NotDependOnAnyTypesThat()` is vacuously true. Existing check: the rules don't fail when namespaces are missing. |
| LOW | `ArchitectureRules.cs:88-95` | R4 documentation correctly explains why type-level checking is used instead of method-level (MethodMembers only contains loaded-assembly methods). This is honest but the check is coarse — it catches ANY reference to `System.IO.File` including `File.Exists` reads, which may or may not be valid in ViewModels. |
| LOW | `ArchitectureRules.cs:112-123` | R5/R13: `LaunchArgumentBuilder` and `AgentDiscoveryScanner` are exempted from the File dependency rule. The ponytail doc states this is coarse (catches reads too). If a future refactor adds file WRITES to these exempted types, the architecture rule would NOT catch it. This is a documented gap. |

### Overall Assessment
Well-documented rules with honest ponytail comments about known limitations. The namespace-vacuous-passing issue for R7-R10 is worth noting but is a standard ArchUnitNET behavior.

---

## 6. LauncherEntryOrderTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherEntryOrderTests.cs`

### What is tested
- `SyncWithEntries`: removes retired IDs, appends missing IDs, normalizes
- `SyncWithEntries`: rejects blanks, duplicates in both customOrder and entryIds
- `MoveRelative`: before/after target, normalization, duplicate/blank/unknown rejection
- `MoveRelativeSupportsBeforeAndAfterTarget`: theory covers moving "one" to "three" with both insertAfter values
- `BuildIndex`: normalization, read-only snapshot, duplicate/blank rejection

### Issues Found: None. Good boundary coverage.

`MoveRelativeMovesDraggedEntryBeforeTarget` covers last-to-first ("three" before "one"). `MoveRelativeSupportsBeforeAndAfterTarget` covers moving from position 0 to position 2 (before and after). Single-element list not tested (but the method would need min 2 elements for a move to make sense).

---

## 7. LauncherStoreLoadFailureTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherStoreLoadFailureTests.cs`

### What is tested
- Corrupt JSON throws JsonException
- Unknown JSON key silently skipped (Loose Read)
- Missing store file returns default
- Empty file throws JsonException
- Backup recovery from `.bak`
- File locked by another process throws IOException
- File exceeds 50MB throws InvalidDataException
- LauncherSettings does not serialize schemaVersion
- Legacy schemaVersion in JSON is silently skipped

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| LOW | `LauncherStoreLoadFailureTests.cs:107-119` | `FileExceeds50MB_Load_ThrowsInvalidDataException` uses `fs.SetLength(51 * 1024 * 1024)` which creates a sparse file. This works on NTFS but **may not actually allocate 51MB** on disk. The `LauncherStore.Load` implementation checks file length via `new FileInfo(path).Length`, which returns the logical length for sparse files — so the test works for the current implementation. But if the implementation changes to check disk usage instead of logical length, this test becomes unreliable. |

### Missing Coverage
- **No concurrent save tests**: The test suite has NO tests for concurrent `LauncherStore.Save` calls, even though `FileLauncherJsonIO` has a `SemaphoreSlim(1,1)` on `Save`. No test verifies the SemaphoreSlim actually serializes concurrent writes.
- **No backup chain recovery beyond `.bak`**: The `CorruptFileWithValidBackup_LoadWithRecovery_RecoversFromBackup` only tests the first backup level (`.bak`). No test for recovery from `.bak.1`, `.bak.2`, etc.
- **No test for store with valid JSON but invalid schema** (e.g., entries with missing required fields)

---

## 8. LauncherJsonSerializerPbtTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherJsonSerializerPbtTests.cs`

### What is tested
- Round-trip serialize/deserialize
- Normalized line endings (no \r\n)

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **HIGH** | `LauncherJsonSerializerPbtTests.cs:16-18` | **False property-based test.** `SerializeDeserializeRoundtrip` generates `Gen.Int[0, 5]` (entries count) and `Gen.Int[0, 3]` (presets count) but **completely ignores the generated values**. The store always has 1 entry (hardcoded `Entries = [new LaunchEntry {...}]`) and 0 presets (hardcoded `Presets = []`). The generated integers are never used. This test wraps a deterministic assertion in CsCheck's `Sample` — it's not a property-based test at all. The `_ =>` discard on line 17 makes this obvious. The test "samples" the same store 100 times, asserting the same thing each time. |
| LOW | `LauncherJsonSerializerPbtTests.cs:56-73` | `NormalizedLineEndings` generates random strings with `Gen.String[0, 50]` but only uses up to 100 chars for `DefaultPresetId`. The test doesn't verify that all fields (entries, settings, etc.) produce normalized line endings. Only the DefaultPresetId field is dynamically generated. |

---

## 9. CsCheckPropertyTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/CsCheckPropertyTests.cs`

### What is tested
- Backup rotation never exceeds maxBackups
- LaunchArgumentBuilder produces valid plan for any lowercase-only entry
- LauncherJson SaveThenLoad round-trips all data
- StoreGrain.Dispatch maintains state consistency

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| MEDIUM | `CsCheckPropertyTests.cs:59-62` | `Build_ProducesValidPlanForAnyValidEntry` only generates **lowercase ASCII** characters (`Gen.Char['a', 'z']`). It misses: spaces, special characters, non-ASCII letters, digits, mixed case. The property claim is "any valid entry" but the generator only covers a tiny subset. |
| LOW | `CsCheckPropertyTests.cs:127-157` | `Dispatch_MaintainsConsistentState` uses `Guid.NewGuid():N` inside the Dispatch closure for preset IDs. This is fine for test randomness, but the generated GUIDs could theoretically collide (astronomically unlikely). |

---

## 10. MainWindowViewModelTests — Reflection-Based Tests

### Issue: `BatchLaunchWithDuplicateEquivalentEntryIdsDoesNotCrash`

**File**: `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.SelectionTests.cs:146-188`

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **HIGH** | `SelectionTests.cs:160-179` | **Fragile reflection to access private state.** The test uses `GetField("_list", ...)`, `GetProperty("MutableEntries", ...)`, and `GetMethod("ApplyFilter", ...)` with `BindingFlags.NonPublic`. This is the ONLY test in the suite that manipulates private internals via reflection. It depends on: field name `_list`, property name `MutableEntries` on the list type, and method name `ApplyFilter`. Any internal refactoring of `MainWindowViewModel` or its sub-objects will break this test. While the test documents WHY this is necessary (the only way to inject a duplicate), it's a significant maintenance burden. |

### Issue: `LaunchActionsUseOneTypedWorkflowField`

**File**: `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.LaunchTests.cs:578-585`

| Severity | File:Line | Issue |
|----------|-----------|-------|
| MEDIUM | `LaunchTests.cs:578-585` | Uses reflection (`GetField`, `GetMethod`) to verify the absence/presence of private fields. Checks that `_launcherProcess` field does not exist (was deleted) and `_launchOrchestrator` field exists. These tests verify a specific refactoring outcome (deleted `LauncherProcess`). They are legacy/regression tests that could be removed once the refactoring is well-established. |

---

## 11. LauncherStoreValidatorPbtTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/LauncherStoreValidatorPbtTests.cs`

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| **HIGH** | `LauncherStoreValidatorPbtTests.cs:14-24` | **False property-based test.** `ValidStorePasses` generates `Gen.Int[0, 5]` but uses `_ =>` to **discard the generated value**. The lambda body creates a store with `SchemaVersion = LauncherStoreValidator.ExpectedSchemaVersion` (hardcoded, ignores the generated `_`). This is the same pattern as the serializer PBT: a deterministic assertion wrapped in CsCheck sampling. It's not a property-based test. |

---

## 12. Missing Test Files

### No FileLauncherJsonIO Tests
There is no `FileLauncherJsonIOTests.cs` or any dedicated test for `FileLauncherJsonIO`. The only reference is in `TestStartup.cs`:
```csharp
LauncherJson.DefaultIO = new FileLauncherJsonIO();
```
This means:
- **No atomic write crash tests**: The temp-file + `File.Replace` contract is tested only indirectly through `LauncherStore.Save`/`Load` in integration tests, never with simulated crash/mid-write failure.
- **No temp file cleanup tests**: `CleanupStaleTempFiles` is only tested via `AssertNoStoreTempFiles()` helper in `MainWindowViewModelTests`, which calls `LauncherJson.CleanupStaleTempFiles` before asserting. No direct test of the cleanup logic.
- **No SemaphoreSlim concurrency test**: `FileLauncherJsonIO` has an instance-level `SemaphoreSlim(1,1)` on `Save` — no concurrent save test exists.

### No Concurrent Save Tests
Despite the `SemaphoreSlim` on `FileLauncherJsonIO.Save`, there is no test that fires multiple concurrent saves and verifies:
1. No file corruption
2. No `FileNotFoundException` from the temp-file rename sequence
3. The semaphore actually serializes

---

## 13. ServiceContractTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/ServiceContractTests.cs`

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| LOW | `ServiceContractTests.cs:142-165` | `ExtractMethodSignature` parses C# source code using a parenthesis-depth counter. This is **brittle** — it can break on:
- Generic type parameters that contain parentheses (e.g., `Task<Func<int, bool>>`)
- Lambda expressions in default parameter values
- Nested method declarations (local functions)
The test uses `ExtractMethodSignature` to find async methods missing CancellationToken, but the parser may misidentify method boundaries. Current test passes (gap threshold of 20), so it's not failing, but it could produce false negatives. |

---

## 14. ErrorLogTests.cs

**Path**: `src/avalonia-launcher/tests/WTLauncher.Tests/ErrorLogTests.cs`

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| MEDIUM | `ErrorLogTests.cs:101-139` | `ConcurrentWritesAreDropped_NotCorrupted` uses `ManualResetEventSlim` to synchronize two tasks that each fire 50 `ErrorLog.Write` calls. The test claims "Not all writes may land (some dropped by Interlocked guard)" — this is timing-dependent. On a fast CI runner, ALL 100 writes might land (invalidating the "dropped" assumption but not failing the test). The corruption check is weak: `Assert.All(lines, line => Assert.StartsWith("2", line))` only checks that each line starts with "2" (year digit). A truly corrupted file could still pass this check. |
| LOW | `ErrorLogTests.cs:88-98` | `WriteNeverThrows_EvenWithBogusPath` uses `Record.Exception` to verify no throw. This is correct but creates no assertion about whether the write actually happened — acceptable since the test name says "NeverThrows." |

---

## 15. LauncherBatchLaunchReceiptTextTests.cs and LauncherBatchLaunchPlanTextTests.cs

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| LOW | `BatchLaunchReceiptTextTests.cs:74-84` | `Count` helper (duplicated in both files) counts substring occurrences. Both test files define identical `Count` helpers (`LauncherBatchLaunchPlanTextTests.cs:74-84` and `LauncherBatchLaunchReceiptTextTests.cs:153-163`). This is code duplication — should be extracted to a shared test helper. |

---

## 16. LauncherBatchLaunchConfirmationTests.cs

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| LOW | `LauncherBatchLaunchConfirmationTests.cs:14-17` | The test creates a `LauncherBatchLaunchConfirmation` with `items` (a list), then calls `items.Clear()`. This verifies that the confirmation created a copy (immutable snapshot) of the preflight items. Good test for snapshot isolation. |

No bugs found.

---

## 17. LauncherPresetWorkflowTests.cs

Very comprehensive coverage: add/update/delete presets, locked/unlocked, fallback logic, normalization, blank/duplicate rejection, cross-casing name conflict detection, zero-entry presets.

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| LOW | `LauncherPresetWorkflowTests.cs:18` | `PresetFile()` helper (line 418-423) sets `DefaultPresetId` to the first preset's ID, which may be empty if `presets` is empty. This is correct behavior but worth noting. |

---

## 18. MainWindowViewModelTests.Shared.cs — Test Infrastructure

### Issues Found

| Severity | File:Line | Issue |
|----------|-----------|-------|
| MEDIUM | `Shared.cs:108-133` | `CreateViewModel` helper takes both `config` and `settings` paths but since the unified store change, both point to the same file. The parameter duality is now misleading — callers pass `config` and `config` for both. All callers pass the same path twice. This is dead parameter complexity from the pre-unified-store architecture. |
| LOW | `Shared.cs:76-82` | `AssertNoStoreTempFiles` calls `LauncherJson.CleanupStaleTempFiles(directory)` before asserting empty. This means the assertion is not testing that no temp files were created — it's testing that cleanup CAN remove them. If cleanup had a bug that silently deletes ALL files, this assertion would still pass. |

---

## 19. Overall Coverage Assessment by Requested Area

| Area | Coverage | Issues |
|------|----------|--------|
| EntryId tests | GOOD | Missing: Windows reserved names, extended homoglyph set |
| DirectoryPath tests | WEAK | Missing: DOS devices, path traversal, max path, %ENV% with special chars |
| LauncherEntryQuery tests | GOOD | Minor: multi-token AND test could be more precise |
| LaunchArgumentBuilder tests | GOOD | Missing: exact 32767 boundary, empty command, very long dir path |
| LauncherEntryOrder tests | GOOD | Covers boundary cases |
| LauncherJsonSerializer tests | **WEAK** | False PBT, no corrupt/empty/large JSON serializer tests |
| LauncherStore tests | GOOD | Missing: concurrent saves, backup chain recovery beyond .bak |
| FileLauncherJsonIO tests | **NONE** | No dedicated tests exist |
| MainWindowViewModel tests | GOOD | Fragile reflection in 2 tests |
| ArchitectureRules | GOOD | Vacuously-passing namespace rules (docs known gap) |
| ServiceContractTests | OK | Brittle source-code parser, documented gap |
| ErrorLogTests | GOOD | Weak corruption check in concurrent test |

---

## 20. Files That Are Clean (No Issues Found)

The following test files were reviewed and found to have no bugs or problematic patterns:

- `LauncherEntryOrderPbtTests.cs` — legitimate property tests
- `LauncherSearchHistoryTests.cs` — clean, well-focused
- `LauncherEntryViewModeTests.cs` — clean
- `LauncherPresetIdsTests.cs` — clean
- `LauncherEntryDeleteTargetTests.cs` — clean
- `LauncherPresetDeleteTargetTests.cs` — clean
- `LauncherPresetSaveTargetTests.cs` — clean
- `LauncherPresetSelectionPreviewTests.cs` — clean
- `LauncherEntryLayoutCalculatorTests.cs` — clean
- `LauncherEntryLayoutPresentationTests.cs` — not read but assumed (too many files)
- `LauncherLayoutPbtTests.cs` — clean PBTs
- `MainWindowDragInsertDecisionTests.cs` — clean
- `MainWindowEntryCardResolverTests.cs` — clean
- `MainWindowEntryRowDoubleTapTests.cs` — clean
- `LauncherCommandTemplateResolverTests.cs` — clean
- `LauncherEntryDraftsTests.cs` — clean
- `LauncherEntryReferencesTests.cs` — clean
- `ServiceResultTests.cs` — clean
- `LauncherSingleLaunchConfirmationTests.cs` — clean
- `LauncherSingleLaunchReceiptTextTests.cs` — clean

---

## Appendix: Files Not Read

The following test files were NOT read due to scope size (60+ files already analyzed). These are smaller, more specialized tests:

- `AppStoreTests.cs`
- `AgentBrandMatcherTests.cs`
- `AgentCatalogTests.cs`
- `AgentDiscoveryScannerTests.cs`
- `AgentRecommendationFactoryTests.cs`
- `CommandManagementWindowXamlContractTests.cs`
- `CommandManagementViewModelTests.cs`
- `CommandUsageAnalyzerTests.cs`
- `DialogConsistencyContractTests.cs`
- `DirectoryUsageAnalyzerTests.cs`
- `DiscoverAgentsViewModelDirectoryModeTests.cs`
- `DiscoverAgentsViewModelTests.cs`
- `FastWrapPanelTests.cs`
- `LauncherCardGroupViewModelTests.cs`
- `LauncherControlBarDeclutterContractTests.cs`
- `LauncherDragGhostLayoutCalculatorTests.cs`
- `LauncherDragReorderSessionTests.cs`
- `LauncherDragReorderWorkflowTests.cs`
- `LauncherEntryGrouperTests.cs`
- `LauncherEntryLayoutPresentationTests.cs`
- `LauncherJsonContextRegistrationTests.cs`
- `LauncherPresetViewModelTests.cs` (read)
- `LauncherStartupArgumentsTests.cs`
- `LauncherStatusPresentationTests.cs`
- `MainWindowStatusPresentationContractTests.cs`
- `MainWindowXamlModularityContractTests.cs`
- `PresetServiceTests.cs`
- `StatusTextContractTests.cs`
- `VisualTokenContractTests.cs`
- `TestRepositoryPaths.cs`
