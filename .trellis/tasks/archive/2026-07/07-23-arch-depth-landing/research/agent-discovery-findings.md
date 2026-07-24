# Research: Agent Discovery Subsystem Ultrareview Findings

- **Query**: Deep code review of the Agent Discovery subsystem (17 source/test files)
- **Scope**: Internal
- **Date**: 2026-07-23

## Files Reviewed

### Source Files

| # | File Path | Description |
|---|---|---|
| 1 | `src/avalonia-launcher/src/WTLauncher.Core/AgentCatalog.cs` | Static catalog of 8 known AI agent CLIs |
| 2 | `src/avalonia-launcher/src/WTLauncher.Core/AgentCatalogEntry.cs` | Immutable record for one catalog row |
| 3 | `src/avalonia-launcher/src/WTLauncher.Core/AgentCategory.cs` | Enum: TerminalNative / IDEExtension / DesktopApp |
| 4 | `src/avalonia-launcher/src/WTLauncher.Core/AgentBrandMatcher.cs` | Pure function: matches entry command string to catalog entry |
| 5 | `src/avalonia-launcher/src/WTLauncher.Core/AgentDiscoveryScanner.cs` | PATH scanner: resolves binaries, detects versions via process spawn |
| 6 | `src/avalonia-launcher/src/WTLauncher.Core/AgentDiscoveryResult.cs` | Result record: ImmutableArray<DiscoveredAgent> + failures |
| 7 | `src/avalonia-launcher/src/WTLauncher.Core/DiscoveredAgent.cs` | One probe result: catalog ref, resolved path, version string |
| 8 | `src/avalonia-launcher/src/WTLauncher.App/ViewModels/AgentRecommendationFactory.cs` | Maps DiscoveredAgent to EntryDraft |
| 9 | `src/avalonia-launcher/src/WTLauncher.App/ViewModels/DiscoverAgentsViewModel.cs` | Dialog VM: owns rows, selection, mode switching, scan orchestration |
| 10 | `src/avalonia-launcher/src/WTLauncher.App/ViewModels/DiscoveryDialogResult.cs` | Result discriminated union: DiscoveredAgentsSelection / ExistingDirectorySelection |
| 11 | `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs` | Code-behind: manual property subscriptions, programmatic UI build |
| 12 | `src/avalonia-launcher/src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml` | XAML layout for the discovery dialog |

### Test Files

| # | File Path | Description |
|---|---|---|
| 13 | `src/avalonia-launcher/tests/WTLauncher.Tests/AgentBrandMatcherTests.cs` | 4 test methods matching/rejection patterns |
| 14 | `src/avalonia-launcher/tests/WTLauncher.Tests/AgentCatalogTests.cs` | 7 tests: non-empty, unique exec names, required fields, brand colors |
| 15 | `src/avalonia-launcher/tests/WTLauncher.Tests/AgentDiscoveryScannerTests.cs` | 7 tests: resolve, scan, parallel spawn, version detect timeout |
| 16 | `src/avalonia-launcher/tests/WTLauncher.Tests/AgentRecommendationFactoryTests.cs` | 4 tests: mapping, stable ID, recommended args, blank directory |
| 17 | `src/avalonia-launcher/tests/WTLauncher.Tests/DiscoverAgentsViewModelTests.cs` | 3 tests: scan success/empty/error states |
| 18 | `src/avalonia-launcher/tests/WTLauncher.Tests/DiscoverAgentsViewModelDirectoryModeTests.cs` | 5 tests: sorting, selection, mode switching |
| 19 | `src/avalonia-launcher/tests/WTLauncher.Tests/MainWindowViewModelTests.DiscoveryTests.cs` | 6 integration tests: cancel, add, rollback, directory mode |

### Context Files Read

| # | File Path | Relevance |
|---|---|---|
| 20 | `src/avalonia-launcher/src/WTLauncher.Core/LauncherErrors.cs` (in `ErrorOrExtensions.cs`) | `IsRecoverable` canonical implementation |
| 21 | `src/avalonia-launcher/src/WTLauncher.App/ViewModels/ExceptionFilters.cs` | DEBUG guard over `LauncherErrors.IsRecoverable` |
| 22 | `src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.Discovery.cs` | Integration: orchestrates discovery dialog + create entries + save |
| 23 | `src/avalonia-launcher/src/WTLauncher.App/Views/DialogService.cs` | View-layer dialog factory; creates VM and window |
| 24 | `src/avalonia-launcher/src/WTLauncher.Core/DirectoryUsageAnalyzer.cs` | Directory dedup/analysis for existing-directory mode |
| 25 | `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryDrafts.cs` | EntryDraft record and validation |
| 26 | `src/avalonia-launcher/src/WTLauncher.Core/LauncherDesignTokens.cs` | Brand badge design constants |
| 27 | `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherStatusText.cs` | Status text factory methods |
| 28 | `src/avalonia-launcher/tests/WTLauncher.Tests/StubDialogService.cs` | Test stub for IDialogService |
| 29 | `.trellis/spec/views/dialogs.md` | Spec: discover dialog listed in dialog inventory |

---

## Findings

### A. AgentBrandMatcher: Path-with-spaces bug (confidence: 95)

**File:** `AgentBrandMatcher.cs:22-23`

```csharp
var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
if (tokens.Length == 0) return false;
var token = tokens[0];
```

`TryMatch` splits the command string by whitespace to extract the first token. This is correct for simple cases like `"codex --flag"`, but **breaks when the executable path contains spaces**:

- Input: `"C:\Program Files\Claude CLI\claude.exe --version"`
- Token 0: `"C:\Program"` (space splits here)
- `Path.GetFileNameWithoutExtension("C:\\Program")` returns `"Program"`
- No catalog match on `"Program"`
- Result: `false` (missed match)

The fix requires proper command-line tokenization (handling quotes), not naive `Split(' ')`. An agent installed in a space-containing directory will not receive its brand badge.

Tests do not cover paths with spaces (`AgentBrandMatcherTests` only tests paths without spaces like `C:\path\codex.cmd`).

---

### B. AgentDiscoveryScanner: No CancellationToken propagation (confidence: 90)

**File:** `AgentDiscoveryScanner.cs:76-116`

```csharp
Parallel.ForEachAsync(
    entries,
    new ParallelOptions { MaxDegreeOfParallelism = 4 },
    (entry, _) =>    // <-- CancellationToken is discarded via _
    {
        // ... never checks ct.IsCancellationRequested
    })
```

The per-iteration lambda receives a `CancellationToken` parameter named `_`. It is never checked or passed to any sub-operation. Additionally, `Scan()` itself does not accept a `CancellationToken` parameter.

Consequences:
- If `ScanAsync` in the ViewModel is awaited and a cancellation signal arrives, in-flight processes continue to spawn and run until completion.
- When the dialog is closed during an active scan (via `Close(null)` or X button), the orphaned scan continues to run on the thread pool, consuming resources and spawning processes unnecessarily.
- `Parallel.ForEachAsync` will only stop scheduling new iterations if the token is canceled; in-flight iterations complete.

---

### C. ConcurrentBag non-deterministic display order (confidence: 85)

**File:** `AgentDiscoveryScanner.cs:71,119-120`

```csharp
var found = new ConcurrentBag<DiscoveredAgent>();
// ... parallel adds ...
return new AgentDiscoveryResult
{
    Found = found.ToImmutableArray(),
    // ...
};
```

`ConcurrentBag` does not preserve insertion order. The `ImmutableArray` created from `found.ToImmutableArray()` will have items in thread-scheduling-dependent order. Since `AgentCatalog.Entries` defines a deliberate display order (Claude first, then Codex, etc.), the scanner should preserve this order.

The agent discovery dialog displays row order based on `result.Found` order, meaning **agent display order varies between scans**. This is a UX consistency issue, not a correctness bug.

**Related test gap:** `DiscoverAgentsViewModelTests.ScanAsync_WhenScanSucceeds_PopulatesRowsAndUpdatesStatus` checks row order against the scan result order, but the scan result order is non-deterministic due to `ConcurrentBag`, making this assertion potentially flaky.

---

### D. Unhandled exception escapes from Parallel.ForEachAsync (confidence: 80)

**File:** `AgentDiscoveryScanner.cs:81-113`

The per-iteration try/catch catches: `IOException`, `UnauthorizedAccessException`, `Win32Exception`, `InvalidOperationException`. Any exception NOT in this list (e.g., `ArgumentException` from `Process.Start`, `NullReferenceException` from a logic bug, `NotSupportedException`) will:

1. Escape the try/catch
2. Cause `Parallel.ForEachAsync` to stop scheduling new iterations
3. Re-throw on the calling thread
4. Abort the scan with remaining agents unscanned

The `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` in the ViewModel layer at `DiscoverAgentsViewModel.cs:125` would then catch the aggregate, but some agents would be missing from results.

The scanner's own error handling in the parallel loop does not use `ExceptionFilters.IsRecoverable` — it uses a hardcoded list. `ExceptionFilters` is in the App layer and can't be referenced from Core. If this is intentional (Core layer has no dependency on App), a Core-level recoverable check or a broader catch is needed.

---

### E. File.Exists on slow network shares blocks threads (confidence: 75)

**File:** `AgentDiscoveryScanner.cs:154`

```csharp
if (File.Exists(candidate))
    return candidate;
```

`File.Exists` is a synchronous call with no timeout. If a PATH entry points to a slow or unavailable network share (`\\slow-server\share\tools`), `File.Exists` can block for seconds or minutes. With `MaxDegreeOfParallelism = 4`, four such blocking calls could consume all parallel slots.

For each of the 8 catalog entries, the scanner probes every PATH directory until a match is found. A single slow network share could delay the entire scan considerably.

---

### F. Scanner hits catalog entries, not process PATH (confidence: 85)

**File:** `AgentDiscoveryScanner.cs:42-46`

```csharp
var machine = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
var user = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
return string.IsNullOrEmpty(user) ? machine : $"{machine};{user}";
```

`BuildEffectivePath()` reads Machine + User environment variables from registry. It does NOT include the process-level PATH (which can be modified by profile scripts, `.bashrc`, or launcher init). This is intentional per the ponytail comment (the explorer.exe snapshot issue on Windows), but it means:

- Agents installed via tools that modify shell profile PATH (not registry) won't be detected.
- A user who runs the launcher from a terminal with a modified PATH won't see those additions reflected.
- The scanner can report "agent not found" even when the agent is available at the command line.

---

### G. Process launch race: deleted-between-resolve-and-detect (confidence: 70)

**File:** `AgentDiscoveryScanner.cs:83-96 and 167-218`

There's a TOCTOU race between `TryResolveOnPath` (checks `File.Exists`) and `TryDetectVersion` (calls `Process.Start`). If the binary is deleted in between, `Process.Start` could throw:

- `FileNotFoundException` (inherits from `IOException`) -- caught by Scan's catch(IOException). **Safe.**
- `Win32Exception` (ERROR_FILE_NOT_FOUND = 2) -- caught by Scan's catch(Win32Exception). **Safe.**
- However: `InvalidOperationException` if the process file is invalid -- caught. **Safe.**

The catch coverage is sufficient for this specific race.

---

### H. DiscoverAgentsWindow Rows collection event handler leak (confidence: 70)

**File:** `DiscoverAgentsWindow.axaml.cs:45`

```csharp
_viewModel.Rows.CollectionChanged += (_, _) => RenderRows();
```

This subscribes an anonymous lambda to the ViewModel's `Rows.CollectionChanged` event. The lambda captures the Window instance. The subscription is never unsubscribed.

Since the dialog is a short-lived modal (created once per discovery session, closed after use), this is not a production leak — once the Window is unreachable, it will be GC'd. But the strong reference chain (ViewModel -> Window, kept alive by the event handler) means the Window outlives its natural scope until the ViewModel is also collected. In practice, both are eligible for GC together after the dialog closes.

---

### I. `ScanAsync` guard has narrow TOCTOU race (confidence: 65)

**File:** `DiscoverAgentsViewModel.cs:97-100`

```csharp
if (IsScanning)  // <-- read
    return;
IsScanning = true;  // <-- write (non-atomic with check)
```

Two concurrent calls from different threads could both pass the `IsScanning` check before either sets it to true. In practice, `ScanAsync` is only called from the UI thread (via `[RelayCommand]` button click and `Opened` event), so Avalonia's single-threaded UI model prevents reentrancy. The race is only a concern if a future refactoring calls `ScanAsync` from a background thread.

---

### J. DiscoverAgentsWindow.axaml uses DialogSharedStyles (confidence: 70)

**File:** `DiscoverAgentsWindow.axaml:13`

```xml
<StyleInclude Source="/Views/Dialogs/DialogSharedStyles.axaml"/>
```

This XAML file references a shared dialog styles file. The `DiscoverAgentsWindow.axaml.cs` code-behind does NOT reference any of the shared dialog helpers (`LauncherDialogButtons`, `LauncherDialogText`). Instead, it hardcodes button content strings (Chinese text) directly. This is inconsistent with the spec in `.trellis/spec/views/dialogs.md` which recommends shared button layout/text helpers. However, the spec says this is for "similar button layouts" — since the discover dialog has unique buttons (scan, add selected), this may be a deliberate choice.

---

### K. No per-item scan timeout for TryResolveOnPath (confidence: 60)

**File:** `AgentDiscoveryScanner.cs:129-160`

`TryResolveOnPath` walks directories sequentially with no timeout between `File.Exists` calls. While `TryDetectVersion` has a 2-second process timeout, the directory resolution phase has no timeout. If a PATH directory points to a disconnected network drive, the scanner could hang indefinitely at the resolution phase before even reaching the version detection.

This is partially mitigated by the `Parallel.ForEachAsync` thread pool context, but blocking thread pool threads is still undesirable.

---

### L. `BuildEffectivePath` produces Windows-style paths on non-Windows (confidence: 55)

**File:** `AgentDiscoveryScanner.cs:42-46`

The method hardcodes `';'` as the path separator and uses `EnvironmentVariableTarget.Machine`/`User` which are Windows-specific. On non-Windows runtimes (macOS, Linux), these environment variable targets don't exist and will return `null`. The fallback `pathOverride` parameter exists, but the default behavior would fail silently on non-Windows.

The project targets `win-x64 --self-contained` per CLAUDE.md, so this is not a current bug. But it's a portability concern if the project's target scope changes.

---

### M. No spec coverage for agent discovery invariants (confidence: 85)

The agent discovery subsystem has no dedicated spec file in `.trellis/spec/`. The dialog is listed in `.trellis/spec/views/dialogs.md` (single line entry in the dialog inventory), but there are no spec documents covering:

- Agent catalog maintenance conventions
- Brand matcher algorithm constraints
- Scanner timeout/resilience requirements
- Discovery result contract with MainWindowViewModel

The subsystem relies entirely on inline ponytail comments and test coverage for its invariants.

---

### N. `AgentRecommendationFactory.ToDraft` uses `agent.Catalog.ExecName` as directory fallback (confidence: 90)

**File:** `AgentRecommendationFactory.cs:25-27`

```csharp
Directory: string.IsNullOrWhiteSpace(defaultDirectory)
    ? agent.Catalog.ExecName  // Will fail validation; caller surfaces error
    : defaultDirectory,
```

When `defaultDirectory` is blank, the factory uses `agent.Catalog.ExecName` as the directory. This is a deliberate design choice (per the ponytail comment), but the resulting `EntryDraft` will fail validation with "按钮目录不能为空" (DirectoryRequired). The caller (`MainWindowViewModel.Discovery.cs:71`) calls `_entryService.CreateEntryFromDraft(...)` which validates and may produce a `ServiceFailure`. This is a valid error path, but the user will see an error message without understanding why the directory was empty.

---

### O. Agent catalog retired entry comment drift risk (confidence: 70)

**File:** `AgentCatalog.cs:63`

```csharp
// Retired by Google on 2026-05-19, replaced by Antigravity CLI.
// Kept in catalog because users may still have it installed.
ExecName = "gemini",
```

The "retired on" date is hardcoded. If the catalog is reviewed in 2027, this date becomes stale. The comment should either reference the commit that marked it retired (git blame) or use a relative description ("retired, kept for backward compatibility"). Minor documentation debt.

---

## Cross-cutting Analysis

### Exception Handling Patterns

| Layer | Pattern | Notes |
|---|---|---|
| AgentDiscoveryScanner (Core) | Narrow catch per-item: IoException, UnauthorizedAccessException, Win32Exception, InvalidOperationException | Misses non-listed exception types; unhandled exceptions escape the parallel loop |
| TryResolveOnPath | Silently catches ArgumentException, PathTooLongException | Correct — malformed PATH entries should not abort scan |
| TryDetectVersion | Minimal try/catch on ReadLine only; process spawn exceptions propagate to caller | Relies on caller's catch coverage |
| DiscoverAgentsViewModel.ScanAsync | `ExceptionFilters.IsRecoverable` | Broad filter (excludes only fatal OOM/AV/SEH); in DEBUG rethrows NRE/IOE/ANE |
| DiscoverAgentsWindow (View) | try/catch on browser launch; narrow exception types | Per spec: no bare `catch (Exception)` |

### Concurrency Safety

| Component | Threading Model | Issues |
|---|---|---|
| Scanner parallel loop | `Parallel.ForEachAsync` with MaxDOP=4 on thread pool | No cancellation propagation, slow sync I/O blocks thread pool threads |
| ConcurrentBag accumulation | Lock-free thread-safe | Non-deterministic ordering |
| ViewModel mutation | All on UI thread (after `await Task.Run` resumes on captured SynchronizationContext) | Correct — Avalonia SC ensures UI thread |
| Per-item catch exceptions | Accumulated in ConcurrentBag | Thread-safe, no ordering guarantees |
| Re-entrant scan guard | `IsScanning` bool check-then-set | TOCTOU race (mitigated by UI-thread-only invocation) |

### Directory Path Normalization

| Component | Normalization | Notes |
|---|---|---|
| `BuildEffectivePath` | Join with `;` | No normalization applied to individual PATH entries |
| `TryResolveOnPath` | `Path.GetFullPath(Path.Combine(...))` | Normalizes directory separators, resolves `.`/`..` |
| `DirectoryUsageAnalyzer.Analyze` | `StringComparer.OrdinalIgnoreCase` | Correct for Windows case-insensitive paths |
| `DefaultNameFromDirectory` | `TrimEnd` separators + `GetFileName` | Correct; handles trailing slash |
| Discovery integration | `GetFileNameWithoutExtension(FirstCommandToken)` | Broken for paths with spaces (Finding A) |

### Test Coverage Gaps

| Area | Coverage | Gap |
|---|---|---|
| BrandMatcher paths with spaces | Not tested | Only tests simple paths (`C:\path\codex.cmd`) |
| Scanner cancellation | Not tested | No test for in-progress scan cancellation |
| Scanner ordering | `Scan_ParallelSpawn_FindsAllConcurrentEntries` verifies presence, not order | No assertion on display order stability |
| Dialog lifecycle: close during scan | Not tested | No test for closing dialog while scanner is running |
| ExceptionFilters.IsRecoverable edge cases | Not tested for scanner-specific exceptions | Missing tests for `Process.Start` failures |
| Scanner on empty/malformed PATH | `Scan_WithEmptyPathOverride_ReturnsEmptyResult` covers empty path | Missing: PATH with invalid Unicode entries, PATH with only network shares |
| `BuildEffectivePath` registry fallback | Not tested directly | No unit test for the registry-based PATH construction |
| EntryDraft duplicate ID collision | Not tested for discovery-generated IDs | `GenerateEntryId` uses `execName.Replace("-", "_")` — no test for collisions |

### Spec References

- `.trellis/spec/views/dialogs.md:20` — "Discover Agents" dialog entry in dialog inventory
- `.trellis/spec/guides/pitfalls.md` — references `07-10-discovery-scan-narrow-catch` (M12 entry about the original bare `catch (Exception)` issue)
- No dedicated spec for agent discovery in `.trellis/spec/core/` or `.trellis/spec/guides/`

### External References

- `.trellis/spec/guides/pitfalls.md:442` — M12: Spec invariant on narrow catch; history of the fix for the original bare `catch (Exception)` in `DiscoverAgentsViewModel.ScanAsync`

---

## Summary of Finding Confidence

| Finding | Confidence | Severity |
|---|---|---|
| A. BrandMatcher breaks on space-containing paths | 95 | Medium |
| B. No CancellationToken propagation | 90 | Medium |
| C. Non-deterministic display order | 85 | Low |
| D. Unhandled exception escapes parallel loop | 80 | Medium |
| E. File.Exists blocks on slow network shares | 75 | Low |
| F. Scanner misses process-level PATH | 85 | Low |
| G. TOCTOU resolve/detect race (caught) | 70 | Low |
| H. CollectionChanged event handler leak | 70 | Low |
| I. ScanAsync guard TOCTOU race | 65 | Low |
| J. Inconsistent dialog button styling pattern | 70 | Low |
| K. No timeout for directory resolution | 60 | Low |
| L. Windows-only PATH detection | 55 | Low |
| M. No spec coverage for discovery invariants | 85 | Low |
| N. Directory fallback produces silent validation error | 90 | Low |
| O. Stale retired date comment | 70 | Cosmetic |

### Severity Classification

- **High**: Causes incorrect behavior or data loss in real-world scenarios
- **Medium**: Causes incorrect behavior under specific conditions; violates spec invariants
- **Low**: Degraded UX, edge-case failures, maintainability debt
- **Cosmetic**: Documentation or style issues

---

## Caveats / Not Found

- No evidence of the original bare `catch (Exception)` mentioned in pitfalls.md M12 — it was already fixed in the current codebase (uses `ExceptionFilters.IsRecoverable` in the ViewModel layer).
- The scanner's error handling in the parallel loop (Core layer) does not use `ExceptionFilters.IsRecoverable` because it's in a different layer. This is a deliberate architectural constraint.
- No Obsolete attributes on the retired "gemini" catalog entry — the comment is the only indicator.
- The `ScanAsync` method in the ViewModel correctly runs the synchronous `Scan()` inside `Task.Run` (as documented in the comment about the cross-thread bug pre-fix at line 111).
