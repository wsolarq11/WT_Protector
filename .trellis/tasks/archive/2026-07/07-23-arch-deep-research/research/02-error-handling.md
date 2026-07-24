# Error / Exception Handling Strategies for .NET 10 Desktop Apps

## Research Scope

Evaluating error handling patterns for **WT Launcher** -- a .NET 10 Win64 Avalonia desktop application with file I/O, JSON deserialization, process launch, and UI thread concerns. The codebase has 5 architectural layers (Core through Views), and any chosen strategy must work uniformly across all of them.

## Current State (Baseline)

| Metric | Value |
|--------|-------|
| `catch (Exception)` sites with `when` filter | ~65 |
| `catch { }` (bare, no type) | 2 (both narrow-scoped: `AgentDiscoveryScanner.cs:198`, `DirectoryPath.cs:43`) |
| `ExceptionBoundary` static methods defined | 4 |
| `ExceptionBoundary` callers | **0** (dead code) |
| `ErrorOr` usage | Partial (Core: `LoadErrorOr`/`SaveErrorOr`/`Try` helpers; App: `AppStore.DispatchErrorOr`, `LauncherEffects`) |
| `IsRecoverable` classifiers | 2 overlapping implementations (`ExceptionFilters.IsRecoverable` with DEBUG guard vs `LauncherErrors.IsRecoverable` without) |
| Catch filter strategies used | narrow-by-type (e.g. `when (ex is IOException or UnauthorizedAccessException)`) AND classifier-based (`when (ExceptionFilters.IsRecoverable(ex))`) -- inconsistently mixed |

---

## Comparison Table

| Candidate | Maturity | WT Launcher fit | Conviction |
|-----------|----------|-----------------|------------|
| **ErrorOr** (current) | NuGet stable, 500M+ downloads | Already imported, partially wired in Core + AppStore | HIGH -- leverage existing investment |
| **OneOf** | NuGet stable, 100M+ downloads | General-purpose DU, no error-specific semantics | MEDIUM -- adds another library, overlaps ErrorOr |
| **Exception filters** (C# 6+) | Language built-in | Already the primary pattern (65 sites) | HIGH -- keep as IO/exceptional boundary |
| **LanguageExt** | NuGet stable, heavy | Full FP framework, steep learning curve | LOW -- overkill for a single-user launcher |
| **Hybrid (ErrorOr + filters)** | Industry consensus pattern | Natural fit: ErrorOr for domain, filters for exceptional | **HIGHEST** -- aligns with Microsoft recommendations |
| **.NET 10 native features** | Preview | No new exception APIs in .NET 10 runtime | LOW -- no disruption |

---

## Detailed Analysis

### 1. ErrorOr (Already Imported)

**Overview:** ErrorOr is a Railway-oriented programming library for C# that provides a `ErrorOr<T>` discriminated union type representing either a success value of type `T` or a list of `Error` objects. It supports LINQ integration (`Then`, `Else`, `Switch`), implicit conversions, and ASP.NET Core integration.

**Pros:**
- Already imported and partially used in WT Launcher (Core: `LauncherStore.LoadErrorOr`, `LauncherJson.SaveErrorOr`, `ErrorOrExtensions.Try`/`TryAsync`; App: `AppStore.DispatchErrorOr`, `LauncherEffects`)
- Zero allocation for success paths (struct-based in recent versions)
- Explicit error contract at the type level -- callers must handle the error case (no silent swallowing)
- Railway-oriented combinators (`Then`, `Else`, `Finally`) enable sequential validation pipelines without nested try/catch
- `ErrorOr<Success>` for void-returning operations maps cleanly to command patterns
- Native AOT compatible (source-generated serialization not needed)
- Lightweight library with no transitive dependency chain

**Cons:**
- Partial adoption creates confusion: some methods return `ErrorOr`, others throw exceptions -- a caller never knows which to expect without checking the signature
- Inconsistent usage in WT Launcher: `AppStore.DispatchErrorOr` exists but `AppStore.Dispatch` (exception-based) is used more frequently
- `ErrorOrExtensions.Try`/`TryAsync` in `ErrorOrExtensions.cs` bridge to exceptions via `catch when (LauncherErrors.IsRecoverable(ex))` -- this is already a hybrid pattern but only 2 call sites
- ErrorOr lists can accumulate multiple errors, but most code only checks `.First().Description` -- the list capability is unused
- No built-in distinction between "recoverable" and "unrecoverable" -- that must be layered on top
- `Error` types are plain objects with `.Code` + `.Description` strings -- no strongly-typed error discrimination without additional pattern

**Codebase-specific observations:**
- `LauncherErrors` defines 4 error factories: `ConfigLoadFailed`, `ConfigSaveFailed`, `EntryNotFound`, `ValidationFailed`
- `ErrorOrExtensions.Try` is defined but only called from... *zero* production sites (only defined for potential use)
- `LauncherEffects` switches on `result` after `LoadErrorOr` -- this is the only real ErrorOr consumption path in production

**Ecosystem maturity:** NuGet stable since 2022, 500M+ downloads, actively maintained (latest release 2025), used extensively in ASP.NET Core community. Strong fit for request/response patterns.

---

### 2. OneOf

**Overview:** OneOf provides discriminated unions for C# via `OneOf<T0, T1, ... T8>` types. It is more general than ErrorOr -- it can represent any "one of N alternatives," not just success/failure. Includes `Switch`/`Match` exhaustive pattern matching and implicit conversions.

**Pros:**
- More general than ErrorOr -- can represent loading/loaded/error as a 3-state discriminated union
- Exhaustive matching via `.Switch()` and `.Match()` -- the compiler ensures all cases are handled
- No dependency on a specific "Error" type -- use any type for each case
- Implicit conversions reduce boilerplate for simple cases
- Can model "success with warnings" as `OneOf<T, Error, Warning>` -- more expressive than ErrorOr

**Cons:**
- C# generic type inference struggles with `OneOf` -- explicit type annotations often required
- No Railway-oriented combinators (no `Then`/`Else`/`Map`) -- the caller writes the switch logic manually each time
- Adds yet another NuGet dependency alongside ErrorOr -- two DU libraries in one project is confusing
- `OneOf<T, ErrorOr.Error>` would be redundant -- just use `ErrorOr<T>` which already has this shape
- Longer type signatures: `OneOf<LauncherStoreFile, NotFound, ConfigLoadError>` vs `ErrorOr<LauncherStoreFile>`
- ErrorOr already satisfies the use case -- OneOf adds generality without solving an unmet problem in this codebase

**Ecosystem maturity:** NuGet stable since 2019, 100M+ downloads, well-established. Used in ASP.NET Core, Blazor, and desktop apps. Recently (2026-05) .NET 11 preview announced native union types -- long-term, OneOf may become obsolete once the language ships union types natively.

**Fit for WT Launcher:** LOW -- ErrorOr already covers the discriminated-union-for-domain-errors use case. Adding OneOf would introduce two competing DU libraries with no clear boundary between them. The one scenario where OneOf excels (exhaustive matching of complex states) does not arise in the current codebase -- command handlers follow a straightforward success/failure pattern.

---

### 3. Exception Filters + Thrown Expressions (C# 6+)

**Overview:** C# 6 introduced `catch` filters (`catch (Exception ex) when (condition)`) and `throw` expressions. These allow precise exception catching without stack frame corruption (filters run on the original faulting frame) and conditional catch logic without nested try/catch.

**Pros:**
- Language built-in -- zero dependencies, no NuGet overhead, fully Native AOT compatible
- WT Launcher already uses this pattern extensively (~65 sites) -- the enforcement infrastructure is in place (gate script catches bare `catch (Exception)`)
- Filters can distinguish recoverable from unrecoverable at the catch boundary without catching then rethrowing
- `when` clause runs before the catch block enters -- the stack trace is never corrupted and the exception is not caught if the filter returns false
- Can combine type-check and predicate: `when (ex is IOException or UnauthorizedAccessException && !IsTransient(ex))`
- `throw` expressions allow null-check + throw in expression context: `result ?? throw new InvalidDataException(...)`

**Cons:**
- Exceptions are still invisible at the type level -- a method signature does not declare what exceptions it can throw
- No compiler enforcement -- callers can forget to catch and the exception propagates to the top-level handler
- Bare `catch (Exception)` is a well-known antipattern that the gate must enforce (already done)
- `Exception`-based flow control in expected error paths (e.g., "entry not found") is an antipattern -- exceptions should be for exceptional situations
- Debugging is harder -- the catch site may be far from the throw site with no intermediate context
- Filters can themselves throw exceptions inside the `when` clause (though this is a runtime bug, not a design flaw)

**Fit for WT Launcher:** HIGH as the _exceptional boundary mechanism_. The codebase's current `catch when (ExceptionFilters.IsRecoverable(ex))` pattern is architecturally correct for unexpected I/O, security, and runtime failures. The problem is not the filter mechanism itself -- it's the **inconsistent mix** of two filter strategies (type-list and classifier) and the **absence of ErrorOr in the domain layer**.

---

### 4. LanguageExt / LaYumba.Functional

**Overview:** LanguageExt is a comprehensive functional programming base class library for C#. It provides `Either<L, R>` (the canonical Right-biased discriminated union), `Option<T>`, `Try<T>`, immutable collections, and monadic extensions. LaYumba.Functional is a lighter library focused on `Either` and `Exceptional` types.

**Pros:**
- `Either<Error, T>` is the functional standard -- well-defined semantics, extensive ecosystem knowledge
- Railway-oriented combinators (`Map`, `Bind`, `Match`, `BiMap`) enable error-pipeline composition
- `Try<T>` monad captures exceptions lazily -- the exception is not thrown until the Try is evaluated
- Immutable collections and functional utilities complement the error handling (reduce null-reference surface)
- LaYumba's `Exceptional<T>` explicitly represents "success or exception" as a discriminated union

**Cons:**
- LanguageExt is a _massive_ dependency (500+ KB compiled) -- it is a full base class library replacement, not just an error handling library
- Steep learning curve for team members unfamiliar with functional C# -- monads, functors, applicatives
- Mixed-paradigm friction: WT Launcher is not a functional codebase. It uses mutable state (`AppStore._state`), imperative loops, and side-effectful operations. The functional error handling types would clash with the dominant imperative style
- `LanguageExt.Core 5.0` has been in beta since 2024 -- the stable version (4.4.x) targets .NET 8 and may not have full .NET 10 Native AOT support
- LaYumba.Functional has minimal updates since 2022 -- maintenance concern
- The `Try<T>` monad captures exceptions that _can_ happen but _shouldn't_ -- it blurs the distinction between expected and exceptional errors
- Would add cognitive overhead disproportionate to the benefit for a single-user launcher app

**Ecosystem maturity:** LanguageExt is well-established in the C# functional community but niche in mainstream .NET. Production usage is concentrated in fintech, healthcare, and other domains with strong correctness requirements. For a desktop launcher with ~410 tests and straightforward CRUD operations, the library's power is far beyond what the problem domain requires.

**Fit for WT Launcher:** VERY LOW. The formalism-to-problem-size ratio is inappropriate. The 07-09 architecture audit already confirmed "keep exceptions" for this codebase, and LanguageExt would be a larger version of the ErrorOr migration that was already rejected.

---

### 5. Hybrid -- ErrorOr for Domain + Exception Filters for Exceptional

**Overview:** The industry consensus pattern (Microsoft Learn, Milan Jovanovic, Vladimir Khorikov, Khalid Abuhakmeh) distinguishes two categories:

| Category | Example | Mechanism |
|----------|---------|-----------|
| Expected/Domain errors | Entry not found, validation failed, preset in use | ErrorOr (or similar result type) -- visible in method signature, caller must handle |
| Unexpected/Exceptional errors | Disk full, file locked, OOM, SEH, AccessViolation | Exception filters -- propagate out of domain, caught at boundary |

**Pros:**
- Clear semantic boundary: "I knew this could fail" (ErrorOr) vs "I didn't expect this" (Exception)
- Method signatures communicate failure modes -- `ErrorOr<LaunchEntry>` tells the caller "this can fail with a known error"
- No need for `LauncherEntry?` where null means "not found" -- `ErrorOr<LauncherEntry>` with `Error.NotFound` is explicit
- Exception filters remain the boundary guard for truly exceptional failures -- OOM/SEH still crash the process
- Gradual migration possible -- individual methods switch to ErrorOr one at a time without a flag-day refactor
- The 07-09 architecture audit left the door open: "revisit if a high-frequency EXPECTED error path emerges"

**Cons:**
- Two error handling mechanisms in one codebase -- developers must learn both and choose correctly
- ErrorOr propagation through async/await can be verbose (`.Then()` chains instead of natural try/catch)
- Interop boundary between ErrorOr-returning methods and exception-throwing methods requires bridging code (the `ErrorOrExtensions.Try` pattern)
- Tests must cover two failure modes per method -- the ErrorOr error path and the exception path
- The current ErrorOr usage in WT Launcher is already hybrid (uses `catch when` inside `LoadErrorOr` to convert exceptions to ErrorOr) -- this pattern would become the universal standard

**WT Launcher-specific mapping:**

| Operation | Domain error? | Exceptional error? | Current mechanism | Target |
|-----------|--------------|-------------------|-------------------|--------|
| Entry validation | Yes | No | `InvalidDataException` | ErrorOr |
| Config load (JSON parse) | Yes | No | `catch when (JsonException)` -> ErrorOr | ErrorOr (already partial) |
| Config save (atomic write) | Borderline | Yes | `catch when (IOException \| UnauthorizedAccessException)` | Keep exception (Disk full is not domain logic) |
| Process launch | Yes (file not found) | Yes (access denied) | `catch when (ExceptionFilters.IsRecoverable)` | ErrorOr for expected, filter for unexpected |
| UI thread exceptions | No | Yes | `Dispatcher.UIThread.UnhandledException` | Keep exception (UI framework boundary) |
| OOM/SEH | No | Yes | `IsRecoverable` returns false | Keep exception (process crash) |

**Edge cases:**
- **Disk full during save**: This is exceptional (the app cannot reasonably handle "no space"), but the user deserves a status message. Current pattern (exception caught at boundary, status text set) is correct -- no migration needed.
- **JSON parse of user-edited config**: This is semi-expected (users can corrupt the file by hand-editing). Current `LoadWithRecovery` + backup fallback is excellent -- the hybrid model preserves this.
- **Process launch where exe is deleted between scan and launch**: This is domain-relevant but time-sensitive. `ErrorOr<ProcessResult>` returning `Error.NotFound("wt.exe")` would be cleaner than letting `Win32Exception` propagate to a catch filter.

---

### 6. .NET 10 / C# 14 Specific Features

**What's new in .NET 10:**
- No new exception handling APIs in the runtime (`System.Runtime.ExceptionServices` unchanged)
- No new `catch` syntax or `try` expression forms in C# 14
- No changes to `Exception` base class, `AggregateException`, or related types

**What's new in C# 14 (relevant to error handling):**
- `field` keyword for auto-property backing fields -- reduces boilerplate but does not affect error handling
- Null-conditional assignment: `mayBeNull?.Property = value` -- reduces null-check boilerplate, tangentially reduces NRE risk
- Partial constructors -- no error handling impact
- Extension members -- could be used to add `Match`/`Switch` to ErrorOr, but ErrorOr already provides these

**What's new in the runtime:**
- Native AOT improvements (ILC trimmer, faster startup) -- relevant for deployment, but no new exception semantics
- AVX10.2 support -- irrelevant
- JIT inlining improvements -- slight perf improvement for exception filter evaluation, but not architecturally significant

**Impact on this decision:** NONE. .NET 10 introduces no new exception handling features that would change the architectural trade-offs.

**One important external development:** .NET 11 preview (May 2026) announced native discriminated union types (`union` keyword). If this ships, ErrorOr and OneOf become interim solutions. However:
- .NET 11 is not yet released (expected Nov 2026)
- WT Launcher targets .NET 10 (current LTS-equivalent)
- Native unions would likely replace ErrorOr at the type level (`union StoreResult = StoreFile | ConfigError`) but the patterns (Switch, Match, railway combinators) would remain the same
- Migration from ErrorOr to native unions would be a mechanical search-and-replace for most call sites
- **Recommendation:** Do not wait for .NET 11. Adopt the best available pattern now; native unions will make it cleaner later.

---

## Recommended: Hybrid ErrorOr + Narrow Exception Filters

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    App Layer (Views/VM)                   │
│                                                          │
│  catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))
│  {                                                        │
│      ErrorLog.Write(ex, context);                         │
│      statusText = Sanitize(ex);                           │
│  }                                                        │
│                                                          │
│  This is the LAST RESORT boundary.                        │
│  All domain-expected errors are handled before here.      │
└──────────────────────────┬──────────────────────────────┘
                           │ ErrorOr<T> flows up
                           ▼
┌─────────────────────────────────────────────────────────┐
│               Service/Orchestration Layer                 │
│                                                          │
│  ErrorOr<LauncherStoreFile> result = store.LoadErrorOr() │
│  return result.Then(Validate).Then(Normalize);            │
│                                                          │
│  Only exceptional IO/security: let exception propagate   │
└──────────────────────────┬──────────────────────────────┘
                           │ ErrorOr<T> flows up
                           ▼
┌─────────────────────────────────────────────────────────┐
│                  Core Layer                               │
│                                                          │
│  ErrorOr<T> Try<T>(action, errorFactory)                  │
│  LauncherErrors.{ConfigLoadFailed, ConfigSaveFailed, ...} │
│                                                          │
│  Pure domain validation returns ErrorOr<Validated>        │
│  Atomic IO operations throw, caught by Try wrappers       │
└─────────────────────────────────────────────────────────┘
```

### Rules

1. **Domain-expected failures use ErrorOr.** Validation errors, entry-not-found, preset-in-use, config migration warnings. The method signature declares `ErrorOr<T>` so the caller knows failure is possible.

2. **Exceptional failures use exception filters.** Disk full, file locked by another process, permission denied, OOM, SEH, AccessViolation. Caught at the boundary (ViewModel command handler or App.OnFrameworkInitializationCompleted) with `catch when (ExceptionFilters.IsRecoverable(ex))`.

3. **ErrorOrExtensions.Try/TryAsync** bridge the gap -- wrap an IO-bound operation that can throw, and convert recoverable exceptions to ErrorOr at the bridge point. Already defined in `ErrorOrExtensions.cs` -- **use it instead of duplicating try/catch/convert patterns**.

4. **ExceptionBoundary is eliminated** (ponytail: 4 methods, zero callers since introduction). It wrapped the same `IsRecoverable` classifier that individual catch blocks already use. The bridge is ErrorOrExtensions.Try, not ExceptionBoundary.Run.

5. **Single `IsRecoverable` classifier.** `ExceptionFilters.IsRecoverable` (with DEBUG guard) is the sole implementation. `LauncherErrors.IsRecoverable` is removed. `ExceptionBoundary` (which uses `LauncherErrors.IsRecoverable` without the DEBUG guard) is removed. One source of truth.

6. **ErrorOr is not used in Views (Layer 4).** View code-behind catches exceptions only via the boundary filter. The VM (Layer 3) unwraps ErrorOr and converts to status text. Views do not call `.Switch` or `.Match` -- they assign `DataContext` and let bindings do the work.

### Migration Path

| Phase | Changes | Files affected |
|-------|---------|----------------|
| 1 | Consolidate `IsRecoverable` into single `ExceptionFilters.IsRecoverable` (with DEBUG guard). Remove `LauncherErrors.IsRecoverable`. Remove `ExceptionBoundary` class. | 3-4 |
| 2 | Fix the 2 bare `catch { }` sites to use narrow typed catches (DirectoryPath.cs, AgentDiscoveryScanner.cs) | 2 |
| 3 | Audit ~65 catch sites: classify each as "domain expected" (migrate to ErrorOr) or "exceptional" (keep filter, ensure consistent status text). Start with the 4 `ErrorOrExtensions.Try` call sites that could use the existing helpers. | 20-30 (gradual, per-method) |
| 4 | Eliminate `LauncherErrors.IsRecoverable` usage in `LauncherEffects.cs` and `ErrorOrExtensions.cs` -- bridge through `ExceptionFilters.IsRecoverable` instead | 2 |

### Relationship to the 5-Layer Architecture

```
Layer 4 (Views):          catch when (ExceptionFilters.IsRecoverable) -- boundary only
Layer 3 (Orchestration):  ErrorOr<T> return + catch when at outermost boundary
Layer 2 (Persistence):    ErrorOrExtensions.Try for recoverable IO, let exceptional propagate
Layer 1 (Commands):       ErrorOr<T> return, Switch/Match for status text
Layer 0 (Core):           ErrorOr<T> for validation + Try helpers; NO references to ExceptionFilters
```

**Critical constraint:** Layer 0 (Core) cannot reference `ExceptionFilters` (which lives in the App layer). This is already satisfied -- `ErrorOrExtensions.Try` uses `LauncherErrors.IsRecoverable`, which is in Core. When `LauncherErrors.IsRecoverable` is removed, `ErrorOrExtensions.Try` should remove its `when` filter entirely or accept a `Func<Exception, bool>` predicate parameter. The cleanest approach: **ErrorOrExtensions.Try in Core stays filter-free** (catches all Exception and converts). The recoverability filter is applied at the App boundary where `ExceptionFilters.IsRecoverable` lives.

### Verification

- **Gate check:** `tools/verify.ps1` already rejects bare `catch (Exception)`. Extend to also reject calls to `ExceptionBoundary.*` (dead code).
- **Architecture test (ArchUnitNET):** Add test ensuring `ErrorOr<T>` is the only return type for validation methods in Core. Exception-throwing validation (`InvalidDataException` for input) becomes an architecture violation.
- **Test coverage:** Each method migrating to ErrorOr gains a unit test for the error path (the success path should already be tested). Exception path tests remain for the boundary layer only.

### Risks

| Risk | Mitigation |
|------|------------|
| ErrorOr propagation through deep call chains becomes verbose | Accept succinctness trade-off. Use `Then`/`Else` combinators. Only layer boundaries need the ErrorOr type -- internals can unwrap early. |
| Two error mechanisms confuse new contributors | Document the rule clearly: "domain = ErrorOr, exceptional = exception". The ErrorOr vs exception choice is already present in the codebase -- the hybrid model just clarifies the boundary. |
| Partial migration leaves some methods on ErrorOr and some on exceptions | Accept incrementalism. The codebase already has this state. Phase 3 is gradual -- no flag-day required. |
| NativeAOT compatibility of ErrorOr | ErrorOr is NativeAOT-safe (no runtime code generation). Verified by the .NET 10 NativeAOT community. |

---

## Decision Summary

**Adopt: Hybrid (ErrorOr for domain errors + narrow exception filters for exceptional failures).**

- ErrorOr: Already imported, partially used, correct for domain-level expected errors (validation, not-found, conflict).
- Exception filters (catch when): Already the dominant pattern, correct for unexpected IO/security/runtime failures. Keep and consolidate around `ExceptionFilters.IsRecoverable` (single source of truth with DEBUG guard).
- Remove: `ExceptionBoundary` (dead code), `LauncherErrors.IsRecoverable` (duplicate classifier without DEBUG guard).
- Do NOT add: OneOf, LanguageExt, or any new dependency. The existing ErrorOr import plus built-in exception filters cover all use cases.
- Do NOT wait for: .NET 11 native unions. If they ship in 2026-11, migrate from ErrorOr at that point with a straightforward type transformation.
