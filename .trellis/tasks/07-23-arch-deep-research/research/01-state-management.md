# State Management Research: Candidates for WT Launcher

**Date:** 2026-07-23
**Context:** Avalonia 12 + .NET 10, win-x64 self-contained desktop app. Current codebase has a hybrid mess: custom `AppStore` (SemaphoreSlim-guarded state + event subscription) coexists with an initialized-but-dead Fluxor skeleton. Goal: find a SINGLE coherent strategy.

---

## Comparison Table

| Criterion | Fluxor | ReactiveUI | CommunityToolkit.Mvvm | Pure Functional / Immutable | MVVM + MediatR |
|---|---|---|---|---|---|
| **Type** | Redux-style (unidirectional) | Observable-based MVVM | Source-generated MVVM | Immutable record tree | CQRS / mediator |
| **Boilerplate** | High (Action + Reducer + Effect per operation) | Medium (ReactiveObject base, WhenAny observables) | Low ([ObservableProperty], [RelayCommand] source gen) | Low (record `with` expressions) | Medium-High (IRequest + IRequestHandler per op) |
| **Learning curve** | Medium (Redux concepts) | Medium-High (reactive programming) | Low (familiar MVVM) | Low-Medium (C# records) | Medium (CQRS concepts) |
| **Thread safety** | Built-in (dispatched actions) | Via Observable plumbing | Manual (you own the model) | Natural (immutable = no mutation) | Manual (handlers run wherever) |
| **Testability** | Excellent (pure reducers) | Good (observable assertions) | Good (standard ViewModel testing) | Excellent (pure functions) | Excellent (handler isolation) |
| **Avalonia maturity** | Experimental (Blazor-first library) | Native (Avalonia was built on ReactiveUI) | First-class (official Avalonia docs recommend it) | Framework-agnostic | Framework-agnostic |
| **NuGet maturity** | ~500 stars, single maintainer (mrpmorris) | Mature, wide ecosystem | Microsoft-maintained, part of .NET Foundation | None (language feature + stdlib) | Mature (Jimmy Bogard, 10+ years) |
| **Memory overhead** | Action objects + state snapshots | Per-subscription overhead | Minimal (source-generated) | Minimal (records are references) | Mediator pipeline allocations |
| **Current usage in codebase** | Dead skeleton (zero Reducers/Effects wired) | None | In use (ObservableObject, [RelayCommand]) | None | None |
| **WT Launcher fit** | Overkill for single-window desktop app | Tight coupling to ReactiveObject base class | **Already works, just needs AppStore replaced** | Natural alignment with current architecture | Adds indirection without clear benefit |

---

## 1. Fluxor

### Overview
Fluxor is a zero-boilerplate Flux/Redux library for .NET by mrpmorris. It implements the classic Redux triad: a single `IStore` holds application state; actions are dispatched through the store; pure `[ReducerMethod]` functions produce new state; `[EffectMethod]` handlers run side effects (I/O, navigation). State is typically a C# `record` with `ImmutableArray` collections.

### Pros
- **Single source of truth** -- the store is the only state holder. Eliminates split-brain outright.
- **Pure reducers** -- every state transition is a pure `(State, Action) -> State` function. Trivially testable; no mocking needed.
- **Strong temporal debugging** -- action log + state snapshots enable time-travel debugging (Redux DevTools integration exists).
- **Already imported** -- the package reference exists in the project. No new dependency approval needed.

### Cons
- **Blazor-first library.** Fluxor's documentation, examples, and community all target Blazor WebAssembly. Avalonia-specific guidance is absent. The DI registration pattern (`AddFluxor`) works with the generic host but the skeleton in `App.axaml.cs` disposes the service provider at method end, meaning state is lost across dispatches.
- **High ceremony per operation.** Every user action requires: (a) an Action record/class, (b) a Reducer method (often in a separate static class), and (c) optionally an Effect class. For a simple toggle like "expand card details", this is 3 files vs. 1 property setter.
- **Single maintainer risk.** The repository (mrpmorris/Fluxor) has ~500 stars and limited contributor activity. The issue tracker shows unanswered questions. Bus-factor is 1.
- **Memory churn.** Redux patterns generate many short-lived Action objects and state snapshots on every dispatch. For a 410-test suite at the gate, GC pressure is measurable.
- **Unnatural fit for Avalonia data binding.** Fluxor expects components to subscribe to `IStateSelection<TState, TValue>` and re-render on change. Avalonia's binding system works with `INotifyPropertyChanged` / `INotifyCollectionChanged`. Bridging these requires wrapper properties in every ViewModel, partially defeating the purpose.

### Ecosystem Maturity
**Low.** Single maintainer. Designed for Blazor, not desktop. No Avalonia-specific guidance, samples, or templates exist. The library is stable but not actively growing.

### WT Launcher Fit
**Poor.** The dead Fluxor skeleton in the codebase is evidence of a previous attempt that was abandoned mid-migration. The ceremony-per-operation cost is high for a launcher app where most "actions" are simple property toggles. The Blazor-centric design fights Avalonia's binding model. Continuing with Fluxor would require building infrastructure (Avalonia bridge, state selectors) that the current codebase does not have, extending the migration timeline with no clear payoff over simpler alternatives.

---

## 2. ReactiveUI / ReactiveObject

### Overview
ReactiveUI is the original MVVM framework for .NET reactive applications. It provides `ReactiveObject` (with `INotifyPropertyChanged` + `IObservable`), `WhenAnyValue` / `WhenAny` for composing observables, `ReactiveCommand` for async commands, and routing via `IScreen` / `IRoutableViewModel`. Avalonia was originally built on ReactiveUI -- the template `New Avalonia App` uses it by default.

### Pros
- **Native Avalonia integration.** ReactiveUI was Avalonia's MVVM layer from day one. Binding to `WhenAnyValue` expressions and `ReactiveCommand` works without adapters.
- **Powerful composition.** `WhenAnyValue(x => x.Foo, x => x.Bar, (f, b) => f + b)` creates reactive projections without manual `PropertyChanged` wiring.
- **Mature ecosystem.** ReactiveUI has 10+ years of development, extensive documentation, and community knowledge on StackOverflow.
- **Testability.** `ReactiveObject` instances can be tested with standard assertions plus `ObservableAsPropertyHelper` for output properties.

### Cons
- **Tight coupling to ReactiveObject base class.** Every ViewModel must inherit from `ReactiveObject`. This is a concrete base-class constraint, not an interface. Mixing with `ObservableObject` (CommunityToolkit) creates a dual-inheritance problem.
- **Reactive programming overhead.** Simple get/set properties require `[Reactive]` attribute or manual `RaiseAndSetIfChanged`. `WhenAnyValue` chains add complexity for straightforward data flows. Junior developers on the team will struggle.
- **Memory: subscription leaks.** `WhenAnyValue` and `ObservableAsPropertyHelper` create subscriptions. Failure to manage lifecycle (`WhenActivated`, `Dispose`) causes leaks. The codebase has no established pattern for this.
- **Source generators lagging.** CommunityToolkit.Mvvm's `[ObservableProperty]` source generator is more ergonomic than ReactiveUI's `[Reactive]` attribute. ReactiveUI's source generators are less mature.
- **Overlap with existing CommunityToolkit usage.** The codebase already uses `ObservableObject` and `[RelayCommand]` extensively. Introducing ReactiveUI would mean two competing MVVM frameworks in the same project -- a second split-brain.

### Ecosystem Maturity
**High.** ReactiveUI is mature, well-documented, and actively maintained. However, the community has been shifting toward CommunityToolkit.Mvvm in recent years as Microsoft's recommended approach.

### WT Launcher Fit
**Mixed.** ReactiveUI's native Avalonia integration is a genuine strength, but the codebase already has significant investment in CommunityToolkit.Mvvm. Introducing a second base-class requirement (`ReactiveObject`) alongside `ObservableObject` would create a confusing dual-framework situation. The reactive composition power (`WhenAnyValue`) is useful for complex projections (e.g., drag state validation), but most WT Launcher state is simple enough that `[ObservableProperty]` suffices. **Recommend: do not add ReactiveUI.** The cost of managing two MVVM frameworks outweighs the benefit.

---

## 3. CommunityToolkit.Mvvm + WeakReferenceMessenger

### Overview
CommunityToolkit.Mvvm is Microsoft's officially recommended MVVM toolkit, part of the .NET Foundation. It provides:
- `ObservableObject` base class (or `[ObservableObject]` on your own base)
- `[ObservableProperty]` source generator (auto-generates `INotifyPropertyChanged` properties)
- `[RelayCommand]` source generator (auto-generates `IRelayCommand` from methods)
- `WeakReferenceMessenger` / `StrongReferenceMessenger` for loosely coupled cross-VM communication
- `IMessenger` with token-based channels and `IRecipient<TMessage>` interface

The Avalonia documentation explicitly recommends CommunityToolkit.Mvvm as the MVVM approach.

### Pros
- **Already in use.** The codebase depends on `CommunityToolkit.Mvvm` and uses `ObservableObject`, `[ObservableProperty]`, and `[RelayCommand]` extensively. No new dependency needed.
- **Source-generated, zero boilerplate.** `[ObservableProperty] private int _count;` generates `public int Count { get; set; }` with full `INotifyPropertyChanged`. This is the most ergonomic option available.
- **Avalonia docs recommend it.** The official Avalonia MVVM how-to guide uses CommunityToolkit.Mvvm exclusively. XAML binding expectations align perfectly.
- **WeakReferenceMessenger avoids leaks.** Weak references mean recipients can be GC'd without explicit unsubscription, eliminating the subscription-leak problem that ReactiveUI has.
- **Low learning curve.** Standard MVVM patterns. Any .NET developer familiar with WPF or MAUI MVVM will be productive immediately.
- **Microsoft-maintained.** Part of the .NET Foundation, used in Microsoft Store and other first-party apps. Active maintenance, regular releases (8.4.2 as of 2026).
- **Source generators = compile-time.** No reflection overhead for property generation. The `[RelayCommand]` generator creates efficient `IRelayCommand` implementations.

### Cons
- **No built-in state management.** CommunityToolkit provides ViewModel infrastructure (property change notification, commands, messaging) but does NOT provide a centralized store. State management is left entirely to the developer.
- **Manual thread safety.** `ObservableProperty` generates property setters that raise `PropertyChanged` on the same thread. Cross-thread dispatch is the caller's responsibility. The current codebase handles this with `SynchronizationContext.Post` and `SemaphoreSlim`, but this pattern would need to be solidified.
- **Messenger is fire-and-forget.** `WeakReferenceMessenger` provides pub/sub but no guarantees about delivery order, handling, or error recovery. It is not a replacement for a store.
- **No built-in undo/rollback.** The codebase has `LauncherEntryMutationSnapshot` for rollback support. CommunityToolkit does not provide this -- it must remain custom.

### Ecosystem Maturity
**Very High.** Microsoft-maintained, part of the .NET Foundation, 1.5M+ NuGet downloads, active development, regular releases. The source generator approach is the modern .NET way.

### WT Launcher Fit
**Strong.** CommunityToolkit.Mvvm is already the ViewModel layer. The missing piece is a coherent state management layer to replace the `AppStore` + dead Fluxor hybrid. The natural approach:

1. **Keep CommunityToolkit.Mvvm** for ViewModel infrastructure (`[ObservableProperty]`, `[RelayCommand]`, `ObservableObject`).
2. **Build a thin, purpose-built state container** (or evolve the existing `AppStore`) that:
   - Owns all state in one place (single source of truth)
   - Uses `INotifyPropertyChanged` / `INotifyCollectionChanged` for ViewModel binding (no bridging needed)
   - Provides thread-safe read/write via a clear API
   - Supports the existing `LauncherEntryMutationSnapshot` rollback pattern
   - Uses `WeakReferenceMessenger` for cross-VM notifications (e.g., "data saved", "navigation requested")

This avoids introducing a second framework while solving the root problem: the current `AppStore` + dead Fluxor coexistence.

---

## 4. Pure Functional / Immutable Architecture

### Overview
All application state lives in a single immutable record tree. State transitions are pure functions: `(State, Event) -> State` using C# `with` expressions. The root record is replaced atomically. Views bind to a reactive wrapper that exposes the current state snapshot. This is the Elm architecture / Redux philosophy, but implemented with pure C# records rather than a library.

### Pros
- **Eliminates mutation bugs entirely.** No mutable state means no thread-safety bugs, no unexpected mutations, no "who changed my list" debugging.
- **C# record support is first-class.** The `with` expression (`state with { Entries = newEntries }`) was designed for this pattern. `ImmutableArray<T>` provides O(1) structural sharing for collections.
- **Natural fit with `LauncherJson` atomic save.** The temp-file + `File.Replace` pattern aligns with atomic state replacement.
- **Perfect testability.** Pure functions take input, return output. No mocks, no setup, no state leaks between tests.
- **No library dependency.** Uses C# language features and BCL types only (`record`, `ImmutableArray`, `with`). Zero NuGet risk.

### Cons
- **Performance on large state.** Every state transition creates a new root record. With structural sharing this is O(change) not O(total), but GC pressure increases with dispatch frequency.
- **Avalonia binding friction.** Avalonia's binding system is designed for mutable `INotifyPropertyChanged` objects. An immutable tree must be wrapped in an observable container (e.g., `IObservable<AppState>` with a `.Subscribe` that re-binds). This wrapping is boilerplate.
- **Collection updates are cumbersome.** Replacing one item in `ImmutableArray<LauncherEntry>` requires: `entries = entries.SetItem(index, entry with { Name = "new" })`. Compared to `entries[index].Name = "new"`, this is more code and requires knowing the index.
- **Coexistence with CommunityToolkit.Mvvm.** `[ObservableProperty]` generates mutable property setters. Using immutable state means either:
  - Avoiding `[ObservableProperty]` for state properties (losing source generation benefits), or
  - Using the immutable tree for persistent/canonical state and projecting into mutable ViewModel properties (two representations = drift risk).
- **No established desktop precedent.** This pattern is common in web frontends (Elm, Redux) but rare in .NET desktop apps. Team onboarding cost.

### Ecosystem Maturity
**Low as a pattern for .NET desktop.** The C# language features (records, `with`, `ImmutableArray`) are mature, but their use as a complete state management architecture for desktop apps has limited community adoption. No dedicated library or tooling support.

### WT Launcher Fit
**Interesting but high-risk.** The immutable record tree concept aligns naturally with the current `LauncherStoreFile` (a single JSON document) and the atomic save pattern. However, the friction with Avalonia data binding and the need to coexist with CommunityToolkit.Mvvm's mutable property generation make a full commitment costly. **Best use:** evolve the single JSON store concept toward a more structured immutable model *within* the Core layer, but keep a mutable ViewModel projection layer using CommunityToolkit.Mvvm. Do not go "all in" on immutable architecture at the UI layer.

---

## 5. MVVM + MediatR

### Overview
MediatR implements the mediator pattern for in-process messaging. Commands (write) and Queries (read) are dispatched through `IMediator` to registered handlers. Pipeline behaviors (logging, validation, retry) wrap around handler execution. In MVVM, ViewModels dispatch `IRequest` objects and receive results, keeping ViewModels thin and handlers testable in isolation.

### Pros
- **Strong separation of concerns.** ViewModels do not contain business logic -- they dispatch commands. Handlers own the logic. This enforces the existing 5-layer architecture rule (layers depend only on lower layers).
- **Pipeline behaviors for cross-cutting concerns.** Logging, validation, rollback (via `LauncherEntryMutationSnapshot`), and authorization can be implemented as MediatR pipeline behaviors, not scattered across command methods.
- **Highly testable.** A handler is a single class with a single `Handle` method. Testing requires only `IMediator` mock.
- **Mature library.** Jimmy Bogard's MediatR has 10+ years, 10K+ GitHub stars, extensive documentation, and wide adoption in the .NET ecosystem.
- **Fits the existing mutation pattern.** The current codebase has command methods (`AddEntryCommand`, `DeleteEntryCommand`, etc.) that validate -> mutate -> save. These map directly to MediatR `IRequest` handlers.

### Cons
- **Indirection for simple operations.** A "toggle view mode" operation requires: ViewModel dispatches `SetViewModeCommand` -> MediatR routes to `SetViewModeCommandHandler` -> handler validates -> returns result. For a single-line state change, this is ceremony without benefit.
- **Single-class-per-operation file explosion.** The gate already has 410 tests; adding 30+ `IRequest`/`IHandler` pairs adds at least 60 files. File navigation becomes harder.
- **No built-in state management.** MediatR is a messaging library, not a state container. It does not solve the split-brain problem. You still need a store.
- **No INotifyPropertyChanged integration.** MediatR handlers return values but do not provide property change notifications. Wiring handler results back to ViewModel properties is manual.
- **Overlap with existing abstractions.** The current codebase already has command methods on the VM. Adding MediatR would replace direct method calls with dispatched IRequest objects -- same logic, more files, more indirection.

### Ecosystem Maturity
**Very High.** MediatR is one of the most widely used .NET libraries. Mature, stable, well-documented. But its primary use case is ASP.NET Core / Web API, not desktop applications.

### WT Launcher Fit
**Poor to Moderate.** MediatR's separation-of-concerns benefit is real, but the cost (file count, indirection, no state management) is high for a single-window desktop launcher. The existing 5-layer architecture already provides separation without MediatR. Pipeline behaviors for logging and validation are attractive, but the same cross-cutting concerns can be implemented with `Interlocked` or `SemaphoreSlim` wrappers at the store level. **Recommend: do not adopt MediatR.** The existing ViewModel command methods with `[RelayCommand]` are sufficient.

---

## Fit for WT Launcher: Recommended Path

### The Core Problem

The current codebase has two state management mechanisms fighting each other:

1. **`AppStore`** (custom, SemaphoreSlim-guarded, event-subscription-based) -- used for some state
2. **Fluxor** (initialized but dead, zero wiring) -- imported but abandoned

This split-brain is the root cause of multiple bugs. The fix is **not** to add a fifth mechanism. The fix is to consolidate on one.

### Recommendation: CommunityToolkit.Mvvm + Thin Dedicated Store

| Layer | What | Why |
|---|---|---|
| **ViewModel infrastructure** | CommunityToolkit.Mvvm (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`) | Already in use, Microsoft-maintained, Avalonia-recommended, zero-boilerplate source generators |
| **State container** | A single purpose-built class (evolve `AppStore` or replace it) | Owns all state, exposes thread-safe read/write, raises `PropertyChanged`/`CollectionChanged` for direct binding |
| **Cross-VM communication** | `WeakReferenceMessenger` | Already included in CommunityToolkit.Mvvm, weak references prevent leaks, no new dependency |
| **Rollback / atomic save** | Keep `LauncherEntryMutationSnapshot` + `LauncherJson.Save` | Already works, pure Core-layer code, no framework changes needed |
| **Persistence** | Keep `LauncherStore` (schema validation) + `LauncherJson` (atomic write) | Already works, no changes needed |

### What to Remove

| Component | Action |
|---|---|
| `Fluxor` NuGet package | Remove (no Reducers, no Effects, dead skeleton) |
| Fluxor registration code in `App.axaml.cs` | Remove the dead `AddFluxor` and `serviceProvider.Dispose` call |
| `AppStore` | Refactor into a clean state container (or replace with a new one that owns ALL state, not just some) |

### What to Keep Strengthening

#### 1. Single State Container

The container should:
- Hold **all** application state (entries, presets, settings, UI mode flags, drag state, search history) as a single cohesive object graph
- Expose observable properties that ViewModels bind to directly
- Provide atomic `Update(Func<State, State>)` for batch mutations
- Use `SemaphoreSlim` for thread safety (the existing pattern is fine)
- Raise `PropertyChanged` and `CollectionChanged` from the same thread as the caller (SynchronizationContext dispatch is the ViewModel's responsibility, not the store's)

#### 2. WeakReferenceMessenger for Cross-Cutting Notifications

Messages for:
- `LauncherDataSaved` -- broadcast after successful `LauncherJson.Save`
- `SaveFailed` -- broadcast with exception details for rollback
- `NavigationRequested` -- view mode changes, preset switches
- `DragStateChanged` -- drag preview lifecycle

ViewModels implement `IRecipient<TMessage>` and the source generator produces the registration boilerplate. Weak references mean no dispose/shutdown lifecycle management needed.

#### 3. Keep the Existing 5-Layer Architecture

The architecture rules already enforce the right boundaries:
- **Core (Layer 0):** Pure data records, `LauncherJson`, `LauncherStore`, `LaunchArgumentBuilder`, `LauncherDesignTokens`
- **ViewModel / Commands (Layer 1-3):** Uses CommunityToolkit.Mvvm, dispatches through the store
- **Views (Layer 4):** XAML only, binds to ViewModel properties

No layer needs to know about Fluxor, ReactiveUI, or MediatR. The store is injected via DI at Layer 2 (Persistence) and consumed by Layer 3 (Orchestration).

### Why Not the Others

| Candidate | Why Not |
|---|---|
| **Fluxor** | Already proven a dead end (abandoned skeleton in codebase). Blazor-first library fights Avalonia binding. Single-maintainer risk. High ceremony for simple operations. |
| **ReactiveUI** | Would create a dual-framework problem (ReactiveObject + ObservableObject). Subscription lifecycle management is a new class of bug. CommunityToolkit.Mvvm already provides what we need. |
| **Pure Functional / Immutable** | Friction with Avalonia's mutable-binding model. Collection updates are verbose. No established .NET desktop precedent. Best used within Core layer, not across the whole stack. |
| **MediatR** | Addresses separation of concerns already handled by the 5-layer architecture. File explosion for simple operations. Does not solve the state management problem. Overkill for single-window desktop app. |

### Migration Path

```
Phase 1 (safe, no behavior change):
  - Remove Fluxor package and registration code
  - Verify gate passes (410+ tests)
  
Phase 2 (consolidate state):
  - Audit AppStore: what state does it own?
  - Audit ViewModel: what state is stored directly in VM properties?
  - Design single state record (or class) that unifies all state
  - Build new state container, inject alongside AppStore
  
Phase 3 (migrate consumers):
  - One by one, move state ownership from VMs to the new container
  - Replace event subscription with WeakReferenceMessenger where appropriate
  - Remove AppStore
  
Phase 4 (cleanup):
  - Verify no state is split between multiple owners
  - Remove any remaining Fluxor/AppStore references
  - Gate passes with 410+ tests
```

### Summary

The winning approach is **not** to adopt a new framework. It is to **complete the migration that was started but abandoned**: remove Fluxor, strengthen the role of CommunityToolkit.Mvvm (which is already the ViewModel layer), and build a thin, purpose-built state container that eliminates the current split-brain. The codebase's best assets -- the pure Core layer, atomic JSON save, `LauncherEntryMutationSnapshot` rollback, and the 5-layer architecture -- remain untouched or strengthened.
