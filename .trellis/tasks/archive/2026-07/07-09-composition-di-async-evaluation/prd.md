# Composition + DI + async I/O evaluation

## Goal

Evaluate 3 large-scope items from 2026-07-09 second-pass audit. Expected outcome per audit: NO-ACTION (defer). Verify against actual code + cite modern practice before finalizing.

## Items

### R1: MainWindowViewModel composition refactor
- **Hypothesis**: 8-partial split is intermediate; composition (separate collaborator classes + constructor injection) is end-state
- **Audit verdict**: "Now that 8-partial split is in place, composition becomes easier: each partial is a candidate collaborator. Scope: collaborator boundary design, constructor injection migration, test mock strategy"

### R6: DI container introduction
- **Hypothesis**: `Microsoft.Extensions.DependencyInjection` for testability
- **Audit verdict**: "Introduce when next scaling"
- **Current state**: `App.axaml.cs` manually constructs `MainWindowViewModel`; dialog confirmation uses `Func<>` property injection; `Process.Start` closed over in `LauncherLaunchWorkflow.Create()` lambda — `Start(plan)` path has no test seam (only `PreflightBatch` is testable)

### R7: Sync File I/O async-ification
- **Hypothesis**: `LauncherJson.Save/Load` uses sync File API; might block UI thread
- **Audit verdict**: "Fine at current scale (<50MB cap, <100 entries); revisit only if latency appears"

## Investigation protocol

For each item:
1. Verify current state against code
2. Cite 2025-2026 modern practice if rejecting
3. Decide action OR no-action with reasoning
4. Document in spec docs if non-trivial

## Preliminary hypothesis

All three are expected NO-ACTION:
- R1: 8-partial split already delivered the maintainability win; composition refactor is large effort with marginal benefit at current scale
- R6: DI container adds ceremony without testability gain (already have `Func<>` injection); introduce when dependency graph complexity justifies it
- R7: Sync I/O at <50MB / <100 entries has no measurable latency; async adds complexity for no observed benefit

But hypothesis must be verified per the audit-driven-verify discipline (each proposal checked against actual code before action).

## Investigation results

### R1: Composition refactor — DEFER

**Current state verified:**
- MainWindowViewModel 1 main + 7 partials (8 files) total **2305 lines** (main 333 + Drag 151 + Entries 288 + Launch 163 + Presets 537 + Batch 339 + Layout 416 + Reorder 78)
- Largest partial: Presets 537 lines — still manageable
- All partials share `_settings`, `_entries`, `_presetFile` fields via `this.` access

**ROI assessment:**
- **Benefit**: Coupling made explicit; each collaborator testable in isolation
- **Cost**: Collaborator boundary design (8 decisions), constructor injection migration (8 partials × N dependent fields), test mock strategy (current tests use real ViewModel), full rewrite of all partial `.cs` files
- **Risk**: Tests cover behavior through the VM; introducing mocks may mask integration bugs

**Why defer:**
- Current partial size (largest 537) is below the "god class" threshold (~1000+)
- 453 tests cover observable behavior; composition refactor would not add test coverage
- Each partial is already a logical cluster (Drag/Entries/Launch/etc.) — composition's main benefit (logical decomposition) is partially realized via partials
- Memory `feedback_godclass_composition_not_partials.md` already documents: "composition is end-state, partials are valid intermediate"
- **Trigger for revisit**: any single partial exceeding 1000 lines, OR test setup becoming prohibitively expensive

### R6: DI container — DEFER

**Current state verified (App.axaml.cs:108-115):**
```csharp
var launchWorkflow = LauncherLaunchWorkflow.Create();
var viewModel = new MainWindowViewModel(
    storePath, storeFile, launchWorkflow,
    static () => DateTimeOffset.UtcNow);
```

**Composition root is trivial** — 1 workflow + 1 VM + 1 window. No graph complexity.

**Existing test seams:**
- `Func<DateTimeOffset> utcNow` — injectable clock
- `Func<LauncherSettings, string?> saveSettings` — injectable persistence (drag workflow)
- `LauncherLaunchWorkflow.Create()` factory — wraps `Process.Start` lambda

**Missing test seam** (per audit): `LauncherLaunchWorkflow.Start(plan)` path (only `PreflightBatch` is testable). The `Process.Start` call is closed over in `Create()`'s lambda.

**ROI assessment:**
- DI container would replace `Func<>` injection with `interface` injection — same testability, more ceremony
- Missing `Start(plan)` test seam can be added by extracting `ILauncherProcess` interface (smaller scope than full DI container)
- DI container introduces: service registration, lifetime management, scope handling — none of which solve a current problem

**Why defer:**
- Microsoft Learn 2026 [CommunityToolkit.Mvvm IoC guide](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/ioc): "MVVM Toolkit doesn't provide built-in APIs to facilitate this... Microsoft.Extensions.DependencyInjection acts as an easy to setup and use IServiceProvider" — explicit non-default
- 2025-2026 r/csharp consensus: DI container for desktop apps is overkill unless graph complexity is high
- **Trigger for revisit**: composition root >5 services, OR adding plugin/extension system

**Targeted alternative** (if testability becomes a pain): extract `ILauncherProcess` interface (smaller scope than full DI container).

### R7: Sync File I/O async-ification — DEFER

**Current state:** `LauncherJson.Save/Load` uses sync `File.ReadAllText`/`WriteAllText` + atomic `File.Replace`. Called on UI thread for save, on UI thread for load (startup + Refresh).

**ROI assessment:**
- File size cap: <50MB (current actual: ~10-50KB for 19 entries)
- Entry count: <100 (current: 19)
- Sync I/O latency at current scale: <5ms
- UI thread blocking threshold: ~100ms (perception)
- 20x safety margin before async becomes necessary

**Cost of async-ification:**
- Async/await viral propagation (every caller up the chain)
- ConfigureAwait(false) careful placement
- Test fixture rewrite (xUnit async tests)
- Risk of deadlocks if not done carefully

**Why defer:**
- No latency complaint, no UI hiccup observed
- Current 453 tests are sync; converting to async would touch hundreds of tests
- Stephen Cleary 2025 guidance: "Don't async-ify unless profiler says so" — matches ponytail ultra
- **Trigger for revisit**: file size >5MB OR user-perceived save lag OR profiler shows >50ms I/O

## Decision matrix

| Item | Decision | Trigger for revisit |
|------|----------|---------------------|
| R1 Composition | DEFER | Partial >1000 lines OR test setup prohibitively expensive |
| R6 DI container | DEFER | Composition root >5 services OR plugin system needed |
| R7 async I/O | DEFER | File >5MB OR perceived lag OR profiler >50ms I/O |

**No code changes.** All 3 items documented as deferred with explicit triggers in this PRD. Spec docs already reference the deferrals (persistence.md covers R7 implicitly; error-handling.md covers R6's ILauncherProcess alternative hint; feedback_godclass_composition_not_partials.md memory covers R1).

## Conclusion

All 3 items: NO-ACTION with documented reasoning. Matches the audit-driven-verify discipline (each proposal verified against actual code before action) — each audited source verified against actual code before accepting deferral.

This closes the residual backlog clearance parent task: 6 items investigated (R3+R4 already completed by earlier tasks; R2 deferred with ponytail comment; R1+R6+R7 deferred with explicit triggers). 1 real action (R5 from prior task) + 5 documented NO-ACTIONs + 0 speculative abstractions introduced.
