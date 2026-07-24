# UI Architecture Patterns for Avalonia 12 + .NET 10

**Task:** 07-23-arch-deep-research  
**Author:** Deep Research agent  
**Date:** 2026-07-23  

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Comparison Table](#comparison-table)
3. [Pattern Deep Dives](#pattern-deep-dives)
   - [1. CommunityToolkit.Mvvm (Full Pattern)](#1-communitytoolkitmvvm-full-pattern)
   - [2. Fluxor / Redux](#2-fluxor--redux)
   - [3. ReactiveUI](#3-reactiveui)
   - [4. MVVM + MediatR](#4-mvvm--mediatr)
   - [5. Functional / Elmish Architecture (Avalonia.FuncUI)](#5-functional--elmish-architecture-avaloniafuncui)
   - [6. Split ViewModel Pattern](#6-split-viewmodel-pattern)
4. [Recommended Architecture for WT Launcher](#recommended-architecture-for-wt-launcher)
   - [Decision: Keep CommunityToolkit.Mvvm, Kill Fluxor, Formalize Split-VM](#decision-keep-communitytoolkitmvvm-kill-fluxor-formalize-split-vm)
   - [Rationale](#rationale)
   - [What to do with Fluxor](#what-to-do-with-fluxor)
   - [Concrete Architecture Diagram](#concrete-architecture-diagram)
   - [Migration Path: Phase 1-3](#migration-path-phase-1-3)

---

## Executive Summary

WT Launcher's `MainWindowViewModel` is ~2200 lines across 14 partial class files, using CommunityToolkit.Mvvm (`ObservableObject`, `[RelayCommand]`, `[ObservableProperty]`). There is also a **dead Fluxor skeleton** -- registered in DI, `LayoutState`/`LayoutReducers`/`LauncherState`/`LauncherReducers`/`LauncherEffects` all exist, but **zero ViewModel code references Fluxor**. The actual state management is a custom `AppStore` class using `SemaphoreSlim` + event subscription, which acts as a mini-Redux already.

After researching six patterns against the codebase's real constraints (3 view modes, drag-reorder, session snapshots, 5-layer architecture, ~400 tests), the recommendation is:

**Keep CommunityToolkit.Mvvm, kill Fluxor cleanly, formalize the existing partial-class Split-VM pattern with strict boundaries.**

No pattern offers enough ROI to justify replacing CommunityToolkit.Mvvm. The pressing problem is not the MVVM framework but the lack of **state management strategy** (two competing systems: Fluxor dead skeleton + AppStore custom store). The fix is to eliminate one (Fluxor) and harden the other (AppStore + Split ViewModel).

---

## Comparison Table

| Criteria | CommunityToolkit.Mvvm | Fluxor / Redux | ReactiveUI | MVVM + MediatR | Elmish (FuncUI) | Split VM |
|----------|-----------------------|----------------|------------|----------------|-----------------|----------|
| **Current investment** | Heavy (entire VM layer) | Medium (skeleton exists) | None | None | None | Already used (partial classes) |
| **Learning curve** | Low | Medium-High | High | Medium | High (F#) | Low |
| **Boilerplate** | Low (source generators) | High (actions, reducers, effects, state) | Medium (observables) | Medium (handlers, pipeline) | Medium (Msg/Dispatch) | Low |
| **Testability** | Good | Excellent (pure reducers) | Good | Excellent | Excellent (pure functions) | Good |
| **Scalability to 2000+ lines** | Medium (needs structure) | High | Medium-High | High | High | High (natural split) |
| **Avalonia ecosystem fit** | Primary recommendation | Unknown (no Avalonia samples) | First-class | No special support | F# only | Framework-agnostic |
| **Drag-reorder complexity** | Manual | Works (dispatch actions) | Rx composition works | Overkill | Cumbersome in F# | Natural per concern |
| **Dead-code risk** | None | Already dead | N/A | N/A | N/A | None |
| **Performance overhead** | None (source gen) | Action object allocation | Subscription overhead | Handler resolution | Diff computation | None |
| **Community size** | Very large (Microsoft) | Medium (Blazor-focused) | Large (Avalonia + Xamarin) | Very large (ASP.NET) | Small (F# niche) | Universal |
| **.NET 10 readiness** | Yes (v8.4+) | Yes (v6.10) | Yes (Avalonia.ReactiveUI 12.x) | Yes (v12.x) | FuncUI 1.6+ | N/A |

---

## Pattern Deep Dives

### 1. CommunityToolkit.Mvvm (Full Pattern)

**What it provides:**
- `ObservableObject` base class with `SetProperty` and `INotifyPropertyChanged`
- `[ObservableProperty]` source generator for field-to-property auto-generation
- `[RelayCommand]` source generator for `ICommand` wrappers
- `IMessenger` for loosely-coupled communication (WeakReferenceMessenger, StrongReferenceMessenger)
- `ObservableValidator` for validation

**Strengths:**
- Already deeply invested (entire ViewModel layer uses it)
- Zero runtime overhead -- all code is generated at compile time
- No additional NuGet dependencies beyond what's already present
- `IMessenger` can replace the need for Fluxor cross-cutting notifications
- Source generators produce clean, debuggable code
- No view-contamination (pure POCO/partial classes)

**Weaknesses:**
- No built-in state management pattern -- you must invent or layer one
- `IMessenger` is fire-and-forget (no middleware, no replay, no devtools)
- No concept of "reducers" or "pure state transformations" -- mutation is imperative
- ObservableObject doesn't help with structured concurrency or cancellation
- Source generators require `partial class` (already done)

**Scaling potential for ~2200 lines:**
CommunityToolkit.Mvvm itself scales fine -- the problem is structural, not framework-related. The partial-class split VM is the right approach; what's missing is a **clear contract** between partial files. Without a contract, partials become just "separate files with shared mutable state."

**Fitness for WT Launcher:**
High. The toolkit is not the bottleneck. The 2200-line VM problem is solvable within CommunityToolkit.Mvvm by:
1. Formalizing the existing partial-file boundaries
2. Moving more logic to `LauncherEntryDrafts`, `LayoutManager`, `PresetManager`, `StatusPresenter`
3. Using `IMessenger` for cross-cutting communication (status bar updates, selection sync)

---

### 2. Fluxor / Redux

**What it provides:**
- Single immutable state tree (`[FeatureState]` records)
- Pure reducer functions (`[ReducerMethod]` static methods)
- Side-effect handlers (`[EffectMethod]`)
- Unidirectional data flow: View -> Dispatch(Action) -> Reducer -> New State -> View re-render
- DevTools integration (Blazor-focused)

**Current state in WT Launcher:**
- `LayoutState` (11 fields: entries, filter, view mode, sort mode, group key, presets, container size, scroll offsets)
- `LayoutActions` (8 action records)
- `LayoutReducers` (8 pure reducer methods)
- `LauncherState` (store file, loading, error, dirty flag)
- `LauncherActions` (6 action records)
- `LauncherReducers` (8 reducer methods)
- `LauncherEffects` (2 effects: load + save)
- Fluxor registered in `App.axaml.cs` DI
- `Fluxor.Blazor.Web` package referenced (not needed for desktop)
- **ZERO usage from any ViewModel** -- dead code

**Strengths:**
- Predictable state changes (pure reducers are easy to test and debug)
- Action log provides free audit trail
- Undo/redo becomes trivial (action replay)
- Forces separation of mutation from side effects (reducers vs. effects)
- Works well with immutable data structures

**Weaknesses for WT Launcher:**
- **Massive boilerplate**: Each feature needs State + Actions + Reducers (optionally Effects). WT Launcher's 10+ command categories would mean 30+ files minimum.
- **No Avalonia-specific support**: Fluxor targets Blazor. There is no `Fluxor.Avalonia` package. No examples exist for Avalonia integration.
- **Overkill for single-window app**: Redux shines in multi-page/multi-component apps with shared state. WT Launcher is one window with 3 view modes.
- **Immutable collections overhead**: `ImmutableList<LaunchEntry>` + `with` expressions allocate on every dispatch. For 200+ entries, this matters on every keystroke filter.
- **Deadlock via GetAwaiter().GetResult()**: The current initialization in `App.axaml.cs` calls `fluxorStore.InitializeAsync().GetAwaiter().GetResult()` -- a known deadlock antipattern.
- **Competition with AppStore**: The custom `AppStore` already does what Fluxor would do (dispatch -> mutate -> save -> notify). Two state systems is worse than one.
- **`Fluxor.Blazor.Web` dependency is wrong**: A desktop app should not reference a Blazor Web package.

**Verdict: KILL IT. Remove Fluxor entirely.** The codebase already has a working state management system (`AppStore`), and Fluxor provides zero incremental value while adding a second conflicting system.

---

### 3. ReactiveUI

**What it provides:**
- `ReactiveObject` base (INotifyPropertyChanged + IObservable)
- `ReactiveCommand` (ICommand + IObservable)
- `WhenActivated` for view lifecycle management
- `WhenAnyValue` for observable property composition
- `ReactiveUserControl` / `ReactiveWindow` base classes
- `RoutedViewHost` for navigation/routing
- `Bind`, `OneWayBind`, `BindCommand` for code-behind bindings
- Observable-as-ViewModel-property support (Avalonia-specific)

**Strengths:**
- First-class Avalonia support (`Avalonia.ReactiveUI` NuGet)
- Declarative reactive pipelines reduce callback spaghetti
- `WhenActivated` solves lifecycle/disposal cleanly
- `ReactiveCommand` has built-in `IsExecuting` for loading states
- Mature ecosystem (Xamarin Forms, Avalonia, WPF for 10+ years)

**Weaknesses for WT Launcher:**
- **Cognitive overhead**: Reactive programming with `IObservable<T>` chain debugging is notoriously hard. The `ReactiveUI not a cup of tea worth drinking` Reddit thread (2024-12, 4 upvotes, 17 comments) captures this: experienced developers report high complexity.
- **Two competing systems**: Introducing ReactiveUI would create a third state management approach alongside AppStore and Fluxor.
- **ViewModel base class conflict**: CommunityToolkit.Mvvm uses `ObservableObject`; ReactiveUI uses `ReactiveObject`. They are incompatible. Would need to pick one for the VM layer.
- **Overlapping with Avalonia's built-in observable bindings**: Avalonia 12 supports binding `IObservable<T>` directly, reducing ReactiveUI's value proposition.
- **Migration cost**: Rewriting ~2200 lines of VM code from `ObservableObject` to `ReactiveObject` is high risk with low reward.
- **No drag-reorder benefit**: Reactive pipelines don't significantly simplify drag-reorder state machines.

**Verdict: Do not adopt.** The cost of migration outweighs the benefits. CommunityToolkit.Mvvm + the existing observable binding support in Avalonia 12 already covers 95% of what ReactiveUI would provide, without the learning curve.

---

### 4. MVVM + MediatR

**What it provides:**
- `IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>` for CQRS
- `INotification` / `INotificationHandler` for events
- `IPipelineBehavior<TRequest, TResponse>` for cross-cutting concerns (logging, validation, authorization)
- In-process mediator pattern (no service bus)

**Strengths:**
- Clean separation of "what the user wants to do" (command) from "how it's done" (handler)
- Pipeline behaviors provide centralized exception handling, logging, validation
- Well-understood in the .NET ecosystem (80M+ NuGet downloads)
- Each handler is independently testable
- Works with any MVVM framework

**Weaknesses for WT Launcher:**
- **Extra abstraction layer that adds no clear value**: WT Launcher does not have multiple command sources (no web API, no background services). The ViewModel already knows what to do.
- **Handler explosion**: Each command type (AddEntry, DeleteEntry, LaunchEntry, ReorderEntries, etc.) needs a separate handler class. This is 20+ handler files for WT Launcher.
- **No state management**: MediatR handles commands but doesn't manage state. Still need AppStore or equivalent.
- **Return type ceremony**: MediatR handlers must return values through `IRequest<TResponse>`, adding generic complexity for void operations.
- **Assembly scanning overhead**: MediatR uses reflection at startup to register handlers.
- **Desktop app ≠ web app**: MediatR's pipeline behaviors solve cross-cutting concerns that in desktop apps are better handled by the existing `AppStore.Dispatch` rollback pattern.

**Fitness for WT Launcher:**
Low. The command/query separation is already handled by `MainWindowViewModel` methods. Adding MediatR would wrap each `[RelayCommand]` method in a handler class, increasing file count without improving testability (VM methods are already testable through the ViewModel).

**Verdict: Do not adopt.** Overkill for a single-window desktop app that already has a working command pattern via `[RelayCommand]`.

---

### 5. Functional / Elmish Architecture (Avalonia.FuncUI)

**What it provides:**
- Model-View-Update (MVU) pattern from Elm
- Single immutable `Model` (state)
- `View` function that renders Model -> UI tree
- `Update` function that takes (Model, Msg) -> Model
- `Cmd<Msg>` for side effects
- Avalonia.FuncUI packages: core + Elmish integration + F# tooling

**Strengths:**
- Pure functional state management (no mutation, no surprises)
- Time-travel debugging (replay messages)
- Everything is a function -- maximum testability
- Concise F# syntax for UI trees
- Strong type safety with discriminated unions for Msg

**Weaknesses for WT Launcher:**
- **F# only**: Avalonia.FuncUI targets F#. WT Launcher is C#. A language switch is not acceptable.
- **Performance**: The Game of Life benchmark (Avalonia.FuncUI + Elmish, 200x200 grid = 40,000 elements) showed performance issues with large lists due to full diff computation. WT Launcher renders 200+ entries with drag overlays.
- **No C# equivalent**: There is no C#-friendly Elmish library for Avalonia. `Avalonia.Redux` exists but is a niche project (1 contributor, no community).
- **Entire codebase rewrite**: All ~400 tests, all ViewModel code, all XAML would need to be rewritten.
- **Team skillset**: F# knowledge is not assumed. Training/hiring overhead.
- **Drag-reorder in FP**: State machines + drag previews are harder to express in pure functional style.

**Verdict: Do not adopt.** The F# requirement alone disqualifies this for a C# codebase. Even in C#, the MVU pattern has no production-ready Avalonia library.

---

### 6. Split ViewModel Pattern

**What it provides:**
- Multiple focused ViewModels instead of one monolithic VM
- Each sub-VM owns a slice of state and behavior
- Parent VM (or composition root) coordinates sub-VMs
- Data binding sub-VM properties through ContentControl/DataTemplates

**Current state in WT Launcher:**
- MainWindowViewModel is already split into 14 partial class files by concern: `Batch`, `CommandManagement`, `DirectoryManagement`, `Discovery`, `Drag`, `Entries`, `Grouping`, `Layout`, `Presets`, `Reorder`
- Delegate objects exist: `StatusPresenter`, `LayoutManager`, `PresetManager`, `LaunchOrchestrator`, `EntryListViewModel`
- The partial class boundary is **physical** (separate files) but not **logical** (all partials share the same `this`, same private fields, same mutable state)

**Strengths:**
- Already partially implemented -- formalizing is cheaper than replacing
- Each concern is isolated in its own file
- Easy to understand: each file has a clear name (`MainWindowViewModel.Drag.cs`)
- No new dependencies required
- Can be incrementally hardened without breaking changes

**Weaknesses of the CURRENT implementation:**
- **Partial classes share `this`**: All 14 files access the same private fields. There is no encapsulation -- `MainWindowViewModel.Drag.cs` can freely modify fields that `MainWindowViewModel.Presets.cs` owns. This is "organized spaghetti," not true separation.
- **No interface boundary**: Sub-VMs don't have contracts. A `IDragViewModel` or `IPresetViewModel` interface doesn't exist.
- **Delegate objects leak**: `_layoutManager`, `_presetManager`, `_statusPresenter` are `internal` fields directly accessed by all partials.
- **Property forwarding noise**: ~30 lines in `MainWindowViewModel.cs` are just forwarding properties to `_layoutManager`, `_presetManager`, `_statusPresenter`.

**True Split VM (what should be):**
```
MainWindowViewModel (orchestrator only)
  ├── ILayoutViewModel (view mode, sort, filter, group)
  ├── IPresetViewModel (preset selection, apply)
  ├── IEntryListViewModel (entry list, selection, keyboard focus)
  ├── IDragViewModel (drag-reorder session, ghost, insert indicators)
  ├── ILaunchViewModel (launch command, confirmation)
  └── IStatusViewModel (status bar text, tone, counts)
```

Each sub-VM is a standalone `ObservableObject` with its own private state. The parent `MainWindowViewModel` creates them and delegates commands/properties. The View binds to `MainWindowViewModel` which exposes sub-VMs as properties.

**Fitness for WT Launcher:**
High. This is the path of least resistance with maximum benefit. The codebase already uses delegate objects (`LayoutManager`, `PresetManager`, `StatusPresenter`, `LaunchOrchestrator`) -- these need to be elevated from "helper classes" to proper sub-ViewModels with their own `[ObservableProperty]` fields and `[RelayCommand]` methods.

---

## Recommended Architecture for WT Launcher

### Decision: Keep CommunityToolkit.Mvvm, Kill Fluxor, Formalize Split-VM

| Component | Decision | Rationale |
|-----------|----------|-----------|
| **MVVM Framework** | Keep CommunityToolkit.Mvvm | Already invested; source generators save boilerplate; no viable alternative offers enough improvement to justify rewrite |
| **State Management** | Keep AppStore (custom Redux-like) | Already works; SemaphoreSlim + rollback pattern is sound; single writer with atomic save |
| **Fluxor** | Remove entirely | Dead code with zero consumers; competing with AppStore; Blazor.Web dependency wrong for desktop; adds complexity without benefit |
| **ViewModel Structure** | Formalize Split-VM | Already partially done (14 partial files); elevate delegate objects to proper sub-ViewModels with contracts |
| **Cross-cutting** | Use IMessenger | Already part of CommunityToolkit.Mvvm; zero new dependencies; status updates, selection sync, and notifications fit this pattern |

### Rationale

**Why not Fluxor:**
Fluxor is already dead in this codebase. The skeleton exists but nothing consumes it. Reviving it would mean:
1. Replacing `AppStore` with Fluxor Store (or running both -- worse)
2. Converting all `[RelayCommand]` methods to `Dispatch(action)` calls
3. Writing reducers for every state mutation (10+ files)
4. Writing effects for every side effect (launch, save, discover, etc.)
5. Converting `LauncherEntryViewModel` (observable wrapper) to immutable records
6. Rewriting all XAML bindings to work with Fluxor state observations
7. Losing the existing rollback-on-failure pattern (`AppStore` does this automatically; Fluxor effects require manual error handling)

The only benefit Fluxor would bring -- action logging + devtools -- does not justify this cost for a single-window launcher.

**Why not ReactiveUI:**
ReactiveUI would create a **third** state management approach (AppStore + Fluxor skeleton + ReactiveUI). The conflict between `ObservableObject` and `ReactiveObject` means one framework dominates. ReactiveUI's learning curve is well-documented (high), and Avalonia 12 already supports `IObservable<T>` bindings natively, reducing ReactiveUI's unique value.

**Why formalize Split-VM:**
The 2200-line `MainWindowViewModel` is the single biggest maintainability problem. The current partial-class approach is "better than one file" but still allows cross-file coupling. Converting delegate objects (`LayoutManager`, `PresetManager`, `StatusPresenter`, `LaunchOrchestrator`) from "helper classes with internal state" to "proper ObservableObject sub-ViewModels with interfaces" gives:
- True encapsulation (sub-VMs can't access each other's private state)
- Interface-based contracts (testable, swappable)
- Clear ownership (each sub-VM has a single responsibility)
- Progressive migration (one sub-VM at a time, no big bang)

**Why keep AppStore:**
`AppStore` is well-designed for this app:
- `SemaphoreSlim` protects against concurrent access (R8 fix)
- Rollback on failure (R4 mitigation)
- `Dispatch`/`DispatchAsync`/`DispatchErrorOr` covers sync and async paths
- Event-based subscription (`Subscribe`) allows ViewModel to react to external changes
- No external dependencies (pure C#)

The main improvements needed for AppStore are:
1. `CancellationToken` propagation (add to `DispatchAsync`)
2. Remove the legacy `Mutate`/`MutateAsync` aliases (dead code)
3. Consider replacing `event Action<LauncherStoreFile>` with `IMessenger` for loose coupling

### What to do with Fluxor

**Remove it. The exact steps:**

1. Remove packages from `Directory.Packages.props`:
   - `Fluxor` v6.10.0
   - `Fluxor.Blazor.Web` v6.10.0
2. Remove `PackageReference` from `WTLauncher.App.csproj`
3. Delete `Infrastructure/Store/` directory (4 files):
   - `LauncherActions.cs`
   - `LauncherEffects.cs`
   - `LauncherReducers.cs`
   - `LauncherState.cs`
4. Delete `Features/` directory (9 files + 3 subdirectories):
   - `LayoutState.cs`
   - `LayoutActions.cs`
   - `LayoutReducers.cs`
   - `ServiceResult.cs`
   - `Dialogs/IDialogService.cs`
   - `Entries/EntryListViewModel.cs`, `EntryService.cs`, `IEntryCardHost.cs`, `IEntryListViewModel.cs`, `IEntryService.cs`, `EntryDeleteApproval.cs`
   - `Presets/IPresetService.cs`, `PresetMutation.cs`, `PresetService.cs`
   - `Status/LauncherStatusContent.cs`, `Status/LauncherStatusTone.cs`
5. Remove Fluxor initialization block from `App.axaml.cs` (~20 lines)
6. Verify no remaining `using Fluxor;` or `Fluxor.*` references

**Note on `Features/` overlap:** Some files under `Features/` are not strictly Fluxor and may have live consumers (e.g., `IDialogService`, `IEntryService`, `EntryListViewModel`). Audit each file before deletion. Files with `using Fluxor;` are Fluxor-specific. Files without `using Fluxor;` may need to be moved to `Infrastructure/` or `ViewModels/`.

### Concrete Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│                    View (MainWindow.axaml)           │
│   ┌──────────┐ ┌──────────┐ ┌──────────┐            │
│   │ Cards    │ │ Details  │ │ Names    │            │
│   │ View     │ │ View     │ │ View     │            │
│   └────┬─────┘ └────┬─────┘ └────┬─────┘            │
│        │            │            │                   │
│   ┌────┴────────────┴────────────┴────┐              │
│   │      ContentControl + DataTemplates │              │
│   └─────────────────────────────────────┘              │
└──────────────────────┬──────────────────────────────┘
                       │ Data binding
┌──────────────────────┴──────────────────────────────┐
│           MainWindowViewModel (orchestrator)        │
│  Uses CommunityToolkit.Mvvm ObservableObject        │
│                                                     │
│  ┌──────────────────────────────────────────────┐   │
│  │ Sub-ViewModels (each is an ObservableObject)  │   │
│  │                                              │   │
│  │  IEntryListVM ─── LauncherEntryVM[]          │   │
│  │       (entries, selection, keyboard)         │   │
│  │                                              │   │
│  │  ILayoutVM ───── LayoutManager               │   │
│  │       (view mode, sort, filter, group)       │   │
│  │                                              │   │
│  │  IPresetVM ───── PresetManager               │   │
│  │       (presets, selection, apply)            │   │
│  │                                              │   │
│  │  IDragVM ─────── DragReorderSession          │   │
│  │       (ghost, insert hints, session state)   │   │
│  │                                              │   │
│  │  ILaunchVM ───── LaunchOrchestrator          │   │
│  │       (launch workflow, confirmation)        │   │
│  │                                              │   │
│  │  IStatusVM ───── StatusPresenter             │   │
│  │       (status bar, counts, tone)             │   │
│  └──────────────────────────────────────────────┘   │
│                                                     │
│  ┌──────────────────────────────────────────────┐   │
│  │ IMessenger (CommunityToolkit.Mvvm)            │   │
│  │  - StatusMessage (sub-VM -> status bar)      │   │
│  │  - SelectionChanged (entryVM -> batch ops)    │   │
│  │  - StoreChanged (AppStore -> all VMs)         │   │
│  └──────────────────────────────────────────────┘   │
└──────────────────────┬──────────────────────────────┘
                       │ Dispatch / Subscribe
┌──────────────────────┴──────────────────────────────┐
│              AppStore (custom Redux-like)            │
│  - Single LauncherStoreFile state                    │
│  - SemaphoreSlim guard                              │
│  - Dispatch(Func<T, T>) pattern                     │
│  - Rollback on failure                              │
│  - File.Replace atomic write                       │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────┐
│              Core (WTLauncher.Core)                  │
│  - LauncherStore (JSON schema, load/save)            │
│  - LauncherJson (atomic temp-file write)             │
│  - LaunchEntry, LauncherPreset, LauncherStoreFile    │
│  - LaunchArgumentBuilder (wt.exe quoting)            │
│  - LauncherDesignTokens (single source for design)   │
│  - LauncherEntryMutationSnapshot (rollback data)     │
└─────────────────────────────────────────────────────┘
```

**Data flow for a typical command (e.g., delete entry):**

```
User clicks Delete
  → MainWindowViewModel.DeleteCommand [RelayCommand]
    → orchestrator validates (checks entry exists)
    → orchestrator creates LauncherEntryMutationSnapshot (rollback data)
    → orchestrator calls AppStore.Dispatch(state => {
        // pure reducer
        return state with { Entries = state.Entries.Remove(entry) };
      })
    → AppStore saves atomically (temp + File.Replace)
    → On success: AppStore notifies subscribers (IMessenger)
    → On failure: AppStore restores previous state, throws
    → Orchestrator catches, restores from snapshot, shows error
    → Sub-ViewModels receive StoreChanged message, update ObservableProperties
    → View auto-updates via bindings
```

### Migration Path: Phase 1-3

#### Phase 1: Clean Up Dead Code (1-2 days)

**Goal:** Remove Fluxor, consolidate state management.

| Step | Files | Impact |
|------|-------|--------|
| Remove Fluxor NuGet packages | `Directory.Packages.props`, `.csproj` | Low |
| Delete `Infrastructure/Store/` | 4 files | Low (zero consumers after audit) |
| Audit `Features/` files, move non-Fluxor files | 9 files | Medium (some files may have live consumers) |
| Remove Fluxor init block | `App.axaml.cs` (~20 lines) | Low |
| Remove `using Fluxor;` directives | Search across solution | Low |
| Remove `Fluxor.Blazor.Web` package reference | `.csproj` | Low |
| Verify gate passes | Run `verify.ps1` | Gate requirement |

**Exit criteria:** Zero `Fluxor` references in the solution. All tests pass.

#### Phase 2: Harden AppStore + Introduce IMessenger (2-3 days)

**Goal:** Make AppStore the single, authoritative state management system. Introduce IMessenger for decoupled notifications.

| Step | Files | Impact |
|------|-------|--------|
| Add `CancellationToken` parameter to `DispatchAsync` | `AppStore.cs` | Low (no external callers currently pass CTS) |
| Remove legacy `Mutate`/`MutateAsync`/`MutateErrorOr` aliases | `AppStore.cs` | Low (find-replace to Dispatch) |
| Replace `event Action<LauncherStoreFile>` with `IMessenger` | `AppStore.cs` | Medium (change subscribe pattern) |
| Create store notification messages (`StoreChangedMessage`) | New file in `Messages/` | Low |
| Update ViewModel subscription to use `WeakReferenceMessenger` | `MainWindowViewModel.cs` | Medium (change Subscribe call) |
| Update tests for new subscription pattern | Test files | Medium |

**Exit criteria:** AppStore has no dead code. IMessenger replaces event-based subscription. All tests pass.

#### Phase 3: Formalize Split-VM with Interfaces (3-5 days)

**Goal:** Convert delegate objects to proper sub-ViewModels with interface contracts.

**Order of decomposition (pick the easiest first):**

| Sub-VM | Current state | Work | Risk |
|--------|--------------|------|------|
| IStatusViewModel | `StatusPresenter` exists, no interface | Create interface, move status-related [ObservableProperty] from MainWindowViewModel | Low (status is read-only forwarding) |
| ILayoutViewModel | `LayoutManager` exists, VM forwards 30+ properties | Create interface, move layout properties to LayoutManager as [ObservableProperty] | Low (layout is already delegated) |
| IPresetViewModel | `PresetManager` exists, VM forwards presets | Create interface, move preset [RelayCommand] methods | Medium (preset commands touch entries) |
| ILaunchViewModel | `LaunchOrchestrator` exists | Create interface, move launch [RelayCommand] methods | Medium (launch has confirmation logic) |
| IDragViewModel | Drag-reorder code in partial files | Extract to standalone ViewModel with drag session state | High (drag state machine is complex) |
| IEntryListViewModel | Partial files + `EntryListViewModel` | Formalize the already-extracted interface | Low (interface already exists) |

**Key patterns for the split:**
1. Each sub-VM is a standalone `ObservableObject partial class`
2. Each sub-VM has an interface (for testability)
3. Sub-VMs communicate via `IMessenger`, not direct method calls
4. `MainWindowViewModel` creates sub-VMs in its constructor and binds to them
5. XAML binds to `MainWindowViewModel.SubVM.Property` using compiled bindings

**Example XAML binding after split:**
```xml
<!-- Before: flat property on MainWindowViewModel -->
<TextBlock Text="{Binding StatusText}" />

<!-- After: property on sub-VM exposed through MainWindowViewModel -->
<TextBlock Text="{Binding Status.Text}" />
```

**Exit criteria:** Each concern in the ViewModel layer has its own ViewModel class with an interface. MainWindowViewModel is reduced to orchestration only. All tests pass.

**Future (Phase 4+):**
- Consider replacing `LauncherEntryViewModel` with a more lightweight model wrapper if performance profiling shows allocation pressure
- Add structured concurrency with `CancellationToken` throughout all async paths
- Consider `AsyncLock` (from Nito.AsyncEx) as an alternative to `SemaphoreSlim` if wait/async patterns become more complex
