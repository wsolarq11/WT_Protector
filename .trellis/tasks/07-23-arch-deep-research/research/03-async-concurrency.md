# Async / Concurrency Patterns for .NET 10 Desktop Apps

Research date: 2026-07-23
Context: WT Launcher -- .NET 10 Avalonia desktop app, win-x64 self-contained (Native AOT compatible).

---

## Current Codebase Concurrency Issues (Discovered During Scan)

| Location | Issue | Risk |
|----------|-------|------|
| `App.axaml.cs:154` -- `fluxorStore.InitializeAsync().GetAwaiter().GetResult()` | Sync-over-async at startup | Deadlock when Avalonia UI thread context is captured |
| `AgentDiscoveryScanner.cs:116` -- `Parallel.ForEachAsync(…).GetAwaiter().GetResult()` | Sync-over-async blocking the caller thread | Thread-pool starvation if caller is UI thread or constrained |
| `AgentDiscoveryScanner.cs:79` -- `(entry, _) =>` | CancellationToken discarded as `_` | Caller-provided cancellation silently ignored |
| `AppStore.cs:21` -- `SemaphoreSlim(1,1)` | Manual lock management, sync `_lock.Wait()` blocks thread | Correct but no timeout; blocks UI thread on sync path |
| `LauncherJson.Save()` (no lock) | Save has no SemaphoreSlim/lock | Multi-thread write race when called outside AppStore |
| `MemoizedSelector.cs` -- plain fields, no volatile/Interlocked | Struct-tear risk for value-tuple inputs; unsynchronized cache | Stale reads on concurrent access |
| `ServiceContractTests.cs:101` (known gap) | Async methods lack CancellationToken parameter | No cancellation propagation through service layer |

---

## Comparison Table

| Pattern | Thread Safety Model | Async-Aware | Best For | Complexity | .NET 10 Status |
|---------|--------------------|-------------|----------|------------|----------------|
| **AsyncLock (Nito.AsyncEx)** | Mutual exclusion | Yes -- `await lock.LockAsync()` | Serializing async file I/O, single-writer | Low (drop-in) | External NuGet; ~3k stars |
| **SemaphoreSlim** | Counting semaphore | Yes -- `WaitAsync()` | Rate limiting, resource pool | Low (built-in) | No changes in .NET 10 |
| **System.Threading.Channels** | Producer/consumer queue | Yes -- `await WriteAsync`/`ReadAsync` | Pipeline processing, UI event queue | Medium | Built-in (inbox) |
| **System.Threading.Tasks.Dataflow** | Block-based pipeline | Yes -- `await SendAsync`/`ReceiveAsync` | Complex multi-stage pipelines | High (heavy) | Built-in (NuGet) |
| **Immutable Collections** | No locks needed; swap reference atomically | N/A | State snapshots, read-dominated paths | Low-Medium | `System.Collections.Immutable` shipped |
| **lock (Monitor)** | Mutual exclusion | **No** -- blocks thread, no async | Brief CPU-only critical sections | Very Low | No changes |
| **ReaderWriterLockSlim** | Multiple readers / single writer | **No** -- blocks threads | Read-heavy but needs write exclusion | Medium | No changes |

---

## Detailed Research

### 1. AsyncLock / Nito.AsyncEx

**What it is:** `AsyncLock` from Stephen Cleary's Nito.AsyncEx library provides an `await`-compatible mutual exclusion primitive. Internally it wraps a `SemaphoreSlim(1,1)` but provides correct re-entrancy semantics and `IAsyncDisposable` for `await using` patterns.

```csharp
private readonly AsyncLock _mutex = new();

public async Task SaveAsync(LauncherStoreFile state, string path)
{
    using (await _mutex.LockAsync())
    {
        await File.WriteAllTextAsync(path, Serialize(state));
    }
}
```

**Key findings:**
- The `LockAsync()` returns `IDisposable` -- release happens when the `using` scope exits.
- Also offers `Lock()` (synchronous) for mixed sync/async scenarios.
- Nito.AsyncEx.Coordination package provides `AsyncReaderWriterLock` for read/write scenarios.
- Nito.AsyncEx 5.x is available for .NET 10.
- Stephen Cleary's blog (2012) remains the canonical "Don't Block on Async Code" reference: `GetAwaiter().GetResult()` + `SynchronizationContext` = deadlock.

**Relevance to WT Launcher:**
- Replace the manual `SemaphoreSlim` in `AppStore` with `AsyncLock` for clearer semantics.
- Add an `AsyncLock` to `LauncherJson.Save` for write serialization independent of AppStore's lock.
- Avoid Nito.AsyncEx if Native AOT trimming is a concern -- the library hasn't been AOT-audited. Prefer a hand-rolled `SemaphoreSlim`-based `AsyncLock` (see Recommended Architecture).

### 2. System.Threading.Channels

**What it is:** A thread-safe producer/consumer queue in the `System.Threading.Channels` namespace (inbox in .NET 10 -- no NuGet needed). Supports bounded (backpressure) and unbounded modes.

```csharp
var channel = Channel.CreateUnbounded<ScanRequest>();

// Producer
await channel.Writer.WriteAsync(request);

// Consumer (background loop)
await foreach (var item in channel.Reader.ReadAllAsync(ct))
{
    await ProcessAsync(item);
}
```

**Key findings:**
- `BoundedChannel` with `BoundedChannelFullMode.DropOldest` provides automatic backpressure.
- `ChannelReader.ReadAllAsync()` returns `IAsyncEnumerable<T>` -- natural fit for `await foreach`.
- Single-reader, single-writer can use `UnboundedChannelOptions` with `SingleReader = true, SingleWriter = true` for lock-free perf.
- Channels are allocation-friendly -- `ValueTask` returns avoid allocations when slots are available.

**Relevance to WT Launcher:**
- Agent discovery scan pipeline: producer emits CLI names, consumer spawns processes. This would avoid blocking the scan's caller.
- UI event queue: channel could serialize AppStore mutations from multiple sources.
- **Not recommended** for the simple write-serialization case -- overkill when `AsyncLock` suffices.
- **Recommended** for the agent discovery pipeline: replace `Parallel.ForEachAsync` with a Channel-based pipeline that streams results to the UI as they arrive.

### 3. System.Threading.Tasks.Dataflow

**What it is:** TPL Dataflow (`ActionBlock`, `TransformBlock`, `BroadcastBlock`, etc.) provides higher-level building blocks for dataflow networks. Available via NuGet `System.Threading.Tasks.Dataflow`.

```csharp
var options = new ExecutionDataflowBlockOptions
{
    MaxDegreeOfParallelism = 4,
    CancellationToken = ct,
};

var scanBlock = new TransformBlock<AgentCatalogEntry, DiscoveredAgent>(
    async entry => await ScanAgentAsync(entry, ct), options);

var outputBlock = new ActionBlock<DiscoveredAgent>(
    agent => results.Add(agent));

scanBlock.LinkTo(outputBlock, new DataflowLinkOptions { PropagateCompletion = true });
```

**Key findings:**
- `ActionBlock<T>` -- simple fire-and-forget consumer with `MaxDegreeOfParallelism`.
- `TransformBlock<TIn, TOut>` -- pipeline stage: input in, output out.
- `BroadcastBlock<T>` -- fan-out to multiple linked blocks.
- Dataflow blocks support bounded capacity, cancellation, and completion propagation.
- TPL Dataflow has been stable since .NET Core 3.1 -- no major changes in .NET 10.
- Considered "the best Microsoft library nobody knows about" (Reddit consensus).

**Relevance to WT Launcher:**
- Over-engineered for current needs. Channels + manual loops are simpler and achieve the same result.
- Could be useful if the batch launch pipeline grows complex (preflight -> confirm -> launch -> record -> notify).
- **Ponytail verdict:** Not justified for the current 8-entry catalog and simple batch-launch workflow. Stick with Channels or direct calls.

### 4. Immutable Collections + Optimistic Concurrency

**What it is:** `System.Collections.Immutable` provides immutable variants of `List<T>`, `Dictionary<K,V>`, etc. Each mutation returns a new collection instance. Paired with `Volatile.Write` / `Interlocked.CompareExchange` for lock-free state swaps.

```csharp
private ImmutableList<LaunchEntry> _entries = ImmutableList<LaunchEntry>.Empty;

public void AddEntry(LaunchEntry entry)
{
    // Volatile read ensures we see latest, Interlocked ensures atomic swap
    ImmutableList<LaunchEntry> snapshot;
    do
    {
        snapshot = _entries;
    }
    while (Interlocked.CompareExchange(ref _entries, snapshot.Add(entry), snapshot) != snapshot);
}
```

**Key findings:**
- No locks needed for readers -- they read `_entries` directly with `Volatile.Read`.
- Writers use CAS (compare-and-swap) loop -- retry on conflict.
- Memory overhead: each mutation allocates a new collection (structural sharing minimizes this, but it's non-zero).
- `ImmutableArray<T>` is a struct wrapping `T[]` -- zero overhead for fixed collections.
- .NET 10 added `FrozenSet<T>` / `FrozenDictionary<K,V>` for truly immutable read-optimized data.

**Relevance to WT Launcher:**
- The current codebase already uses `ImmutableList<LaunchEntry>` in `LauncherStoreFile` -- good start.
- `MemoizedSelector` could use `Volatile` reads/writes to fix the struct-tear issue without a lock.
- The AppStore state could be swapped atomically: `Interlocked.Exchange(ref _state, newState)` instead of SemaphoreSlim for reads. But writes still need serialization for file I/O.
- **Hybrid recommendation:** Use immutable collections for the in-memory state (already done). Use `AsyncLock` for write serialization (file I/O). Readers read the immutable reference without any lock.

### 5. SemaphoreSlim Best Practices

**What it is:** The built-in lightweight semaphore. `SemaphoreSlim(1,1)` = async-compatible mutex.

**Key findings:**
- `WaitAsync()` does **not** block the calling thread -- it queues the waiter. This is the key difference from `lock` / `Monitor.Enter`.
- **Always use `try/finally`** or the exception-safe pattern to release. C# 13+ `await using` with `SemaphoreSlim` requires a helper type.
- `Wait(Int32, CancellationToken)` overload exists for sync paths with timeouts -- protects against deadlocks.
- `CurrentCount` property is for diagnostics only -- do not use it for control flow.
- `Dispose()` must be called when the semaphore is no longer needed (`AppStore.Dispose` already does this -- good).
- Performance: `SemaphoreSlim` is ~10x faster than the classic `Semaphore` (kernel object).
- **Limitation:** No reentrancy. If code acquires the semaphore and then calls back into itself (directly or via an event), it deadlocks. AppStore dispatches `_stateChanged` *outside* the lock to avoid this.

**Relevance to WT Launcher:**
- The current `AppStore` usage is correct but could be improved:
  - `_lock.Wait()` (sync path in `Dispatch()` and `DispatchErrorOr()`) blocks the calling thread. If called from the UI thread, this is a hang risk.
  - Recommendation: always prefer `WaitAsync()` in a desktop app. Convert `Dispatch()` to use `WaitAsync()` and be `async Task`.
  - Add `CancellationToken` parameter to `WaitAsync()` calls.

### 6. CancellationToken Patterns for Desktop Apps

**Key findings:**
- **Cooperative cancellation** -- the `CancellationToken` signals intent; the operation must check it.
- `ThrowIfCancellationRequested()` at strategic points (loop iterations, between I/O operations).
- `Register(Action)` callback for non-task-based cleanup.
- `CancellationTokenSource.CreateLinkedTokenSource()` for combining multiple cancellation sources.
- **Desktop-specific:** Use `DispatcherTimer` or `PeriodicTimer` for polling, not `Task.Delay` with cancellation inside synchronous event handlers.
- **Avalonia-specific:** No framework-level CancellationToken propagation in MVVM. Must be manual.

**Systematic propagation pattern for layered desktop apps:**

```
Layer 0 (Core / pure logic)
  ↓ Takes CancellationToken from caller, passes it to I/O calls
  ↓ Does NOT create its own CTS
Layer 1 (Command mutations / VM)
  ↓ Creates CTS per operation (batch launch, scan, etc.)
  ↓ Passes Token down to Core
  ↓ Hooks UI events to call CTS.Cancel()
Layer 2 (Views / code-behind)
  ↓ Does NOT hold CTS references
  ↓ Triggers cancellation via VM commands
```

**Current state in WT Launcher:**
- `LaunchOrchestrator._batchCts` -- good pattern (per-operation CTS, disposed in finally).
- `LauncherLaunchWorkflow.StartBatch` accepts CancellationToken -- correct.
- Known gap: `AppStore.DispatchAsync` has no CancellationToken parameter.
- Known gap: Service interfaces lack CancellationToken.

### 7. .NET 10 Async Improvements

**Key findings:**

| Feature | Status | Impact on WT Launcher |
|---------|--------|----------------------|
| **Runtime-Async** (runtime-native async methods, no compiler state machine) | Preview in .NET 10, full in .NET 11 (Nov 2026) | Zero code change needed. Compile with `RuntimeAsync=true` on .NET 11 for free perf. |
| `Task.CompletedTask` / `ValueTask.CompletedTask` | Unchanged | Already using `ValueTask.CompletedTask` in scan paths -- good. |
| **C# 14 `ConfigureAwait(true)`** default | No change from C# 13 default | Same pattern as today. Use `ConfigureAwait(false)` in Core layer, `ConfigureAwait(true)` in VM layer. |
| `PeriodicTimer` | Available since .NET 6 | Useful for polling (config file watcher). |
| `ConcurrentDictionary` improvements | Minor internal perf tweaks | No action needed. |
| **FileHelpers** (new in .NET 10) | `File.ReadAllTextAsync` etc. optimized | Could replace sync `File.ReadAllText` in `LauncherJson`. |
| `FrozenDictionary`/`FrozenSet` | Available since .NET 8 | Use for immutable lookups in hot paths. |

**Runtime-Async detail (worth understanding for planning):**
- Currently, each `async` method generates a compiler state machine struct (~80-200 bytes IL).
- Runtime-Async moves suspension/resumption into the runtime JIT -- state machine is gone, replaced by native code continuations.
- Benefits: smaller IL, faster method invocation, better JIT optimization of async methods.
- Available in .NET 11 Preview 1 (Feb 2026), full in .NET 11 (Nov 2026).
- For WT Launcher: no action needed today. When upgrading to .NET 11, enable with `<RuntimeAsync>true</RuntimeAsync>` in csproj.

---

## Recommended Architecture for WT Launcher

### Overview

Replace the current mixed approach (manual `SemaphoreSlim` + sync-over-async + unsynchronized cache) with a three-layer concurrency strategy:

```
┌─────────────────────────────────────────────┐
│  Layer 2: View / Event Handlers              │
│  (runs on UI thread, triggers VM commands)   │
├─────────────────────────────────────────────┤
│  Layer 1: ViewModel (Orchestrator)           │
│  · Creates CTS per operation                 │
│  · Calls async Layer-0 methods               │
│  · Marshals result back to UI thread         │
├─────────────────────────────────────────────┤
│  Layer 0: Core (AppStore, LauncherJson, etc.)│
│  · AsyncLock for write serialization         │
│  · Immutable state snapshots for reads       │
│  · CancellationToken on all async methods    │
└─────────────────────────────────────────────┘
```

### 1. Eliminate Sync-Over-Async

**Problem:** `GetAwaiter().GetResult()` at startup and in scan path.

**Plan:**

**a) Startup (`App.axaml.cs`):**
- Current: `fluxorStore.InitializeAsync().GetAwaiter().GetResult()`
- Fix: Push Fluxor initialization into a startup Task that completes before the window is shown, using the Avalonia `OnFrameworkInitializationCompleted` event to defer window creation. If Fluxor init must be sync (because `desktop.MainWindow =` must be set in the same frame), use the `Task.Run(() => …).GetAwaiter().GetResult()` workaround to escape the UI SynchronizationContext:

```csharp
// Escape UI SynchronizationContext to avoid deadlock
Task.Run(() => fluxorStore.InitializeAsync()).GetAwaiter().GetResult();
```

Better yet, restructure so that MainWindow is set first (with a loading state), then Fluxor initializes asynchronously, then the loading state resolves.

**b) Agent discovery scan (`AgentDiscoveryScanner.Scan`):**
- Current: `Parallel.ForEachAsync(…).GetAwaiter().GetResult()`
- Fix: Make `Scan` return `Task<AgentDiscoveryResult>` (async). The caller already runs on a background thread or uses `await`. If the sync signature is required for backward compatibility, create an internal `ScanAsync` and have `Scan` call it via `Task.Run(…).GetAwaiter().GetResult()`.

### 2. Add AsyncLock to LauncherJson.Save

**Problem:** `LauncherJson.Save()` has no synchronization -- concurrent writes race.

**Fix:** Add a `private static readonly SemaphoreSlim _saveLock = new(1, 1)` inside `LauncherJson`:

```csharp
private static readonly SemaphoreSlim _saveLock = new(1, 1);

public static async Task SaveAsync<T>(T value, string path, bool backup, CancellationToken ct = default)
{
    await _saveLock.WaitAsync(ct).ConfigureAwait(false);
    try
    {
        // … existing implementation …
    }
    finally
    {
        _saveLock.Release();
    }
}
```

Keep the sync `Save()` method for backward compatibility, but have it call `SaveAsync(…).GetAwaiter().GetResult()` via `Task.Run` to avoid deadlock:

```csharp
public static void Save<T>(T value, string path, bool backup)
{
    Task.Run(() => SaveAsync(value, path, backup)).GetAwaiter().GetResult();
}
```

Or (cleaner): migrate all callers to the async overload and deprecate the sync one.

### 3. Fix MemoizedSelector Thread Safety

**Problem:** Struct-tear risk with value-tuple inputs, no synchronization.

**Fix -- option A (lightweight):** Add `Volatile` semantics to the cache fields:

```csharp
public class MemoizedSelector<TInput, TOutput> where TInput : notnull
{
    private TInput _lastInput = default!;
    private TOutput _lastOutput = default!;
    private volatile bool _hasCached;

    public TOutput Get(Func<TInput, TOutput> compute, TInput input)
    {
        if (_hasCached && EqualityComparer<TInput>.Default.Equals(_lastInput, input))
            return _lastOutput;

        _lastInput = input;
        _lastOutput = compute(input);
        _hasCached = true;
        return _lastOutput;
    }

    public void Invalidate()
    {
        _hasCached = false;
    }
}
```

**Fix -- option B (lock-free with Interlocked):** Use `Interlocked.CompareExchange` for the cache state, but this is over-engineered for a selector that's only called from the UI thread. Option A (volatile) suffices when callers are single-threaded but may see stale writes.

**Fix -- option C (documented single-thread contract):** Mark the class as `[System.ComponentModel.EditorBrowsable(Never)]` and document that it's not thread-safe. Since `LayoutManager` (the sole production caller) is bound to the UI thread, this is correct.

**Recommendation:** Option A -- the volatile bool is a one-line fix that prevents the JIT from caching `_hasCached` in a register.

### 4. CancellationToken Propagation Diagram

```
User clicks "Cancel" button
         │
         ▼
LaunchOrchestrator.CancelBatch()
         │
         ├── _batchCts.Cancel()       ← creates CancellationTokenSource
         │
         ▼
LauncherLaunchWorkflow.StartBatch(preflight, _batchCts.Token)
         │
         ├── loop: ct.ThrowIfCancellationRequested()
         │         at each iteration
         ▼
LauncherLaunchWorkflow.Start()    ← per-item launch (sync, no CT needed)
         │
         ▼
(Process.Start)
```

**Rules for systematic propagation:**

```
RULE 1: Every async method in Core layer gets CancellationToken as LAST parameter
        with default = default:
        
        Task<AgentDiscoveryResult> ScanAsync(
            IEnumerable<AgentCatalogEntry> catalog,
            CancellationToken ct = default,
            string? pathOverride = null);
        
RULE 2: ViewModel creates CTS, NOT the Core layer.
        Core layer only RECEIVES CancellationToken.
        
RULE 3: Dispose CTS in finally block (already done in LaunchOrchestrator -- good).
        
RULE 4: Do NOT pass UI-bound CancellationTokens to fire-and-forget operations.
        Operations that must complete (e.g., config save after launch) should
        use their own CTS or CancellationToken.None explicitly.
```

**Gap closure for AppStore:**

```csharp
public async Task DispatchAsync(
    Func<LauncherStoreFile, Task<LauncherStoreFile>> reducer,
    CancellationToken ct = default)
{
    await _lock.WaitAsync(ct).ConfigureAwait(false);
    // …
}
```

### 5. AppStore Lock Strategy (Revised)

**Current:** Single `SemaphoreSlim(1,1)` guards both reads and writes.

**Recommended:**

| Operation | Strategy | Rationale |
|-----------|----------|-----------|
| `Select<T>()` (sync, in-memory) | No lock | Reads the immutable reference atomically on 64-bit. The reference isn't changing during a read because only the UI thread reads it (single-writer assumption). |
| `SelectAsync<T>()` | Remove the lock | Same reasoning -- if reads are truly async-safe, the caller can read directly. |
| `Dispatch()` / `DispatchAsync()` | Keep SemaphoreSlim | Serializes the reduce-and-save sequence atomically. Add CancellationToken parameter. |
| `SaveConfig()` (via MWVM delegates) | Routes through AppStore.Dispatch | Already correct. No direct LauncherJson.Save call. |

### 6. Agent Discovery: Parallel.ForEachAsync with CancellationToken

**Fix the discarded `_` parameter:**

```csharp
Parallel.ForEachAsync(
    entries,
    new ParallelOptions
    {
        MaxDegreeOfParallelism = 4,
        CancellationToken = ct,    // <-- pass it here
    },
    (entry, ct) =>                 // <-- use it, not _
    {
        ct.ThrowIfCancellationRequested();
        // … existing logic …
    }).GetAwaiter().GetResult();
```

But better: make `Scan` truly async:

```csharp
public static async Task<AgentDiscoveryResult> ScanAsync(
    IEnumerable<AgentCatalogEntry> catalog,
    CancellationToken ct = default,
    string? pathOverride = null)
{
    // … setup …
    await Parallel.ForEachAsync(entries,
        new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
        async (entry, innerCt) =>
        {
            innerCt.ThrowIfCancellationRequested();
            // … async scan logic with await …
        }).ConfigureAwait(false);
    return new AgentDiscoveryResult { … };
}
```

### 7. Summary of Migration Path

| Step | Change | Impact |
|------|--------|--------|
| 1 | Add `private static readonly SemaphoreSlim _saveLock` to `LauncherJson` | Thread-safe Save, no behavioral change |
| 2 | Make `AgentDiscoveryScanner.Scan()` return `Task<AgentDiscoveryResult>` | Update callers; existing tests use synchronous wrappers |
| 3 | Add `CancellationToken` to `AppStore.DispatchAsync` | Breaking change; update all callers (~5 sites) |
| 4 | Fix `MemoizedSelector._hasCached` to `volatile` | One-line, no behavioral change |
| 5 | Replace `fluxorStore.InitializeAsync().GetAwaiter().GetResult()` with `Task.Run(…).GetAwaiter().GetResult()` or async init | Eliminates startup deadlock risk |
| 6 | Mark `LauncherJson.Save()` as obsolete, migrate to `SaveAsync()` | Long-term; all callers go async |
| 7 | Evaluate Runtime-Async when upgrading to .NET 11 | Free performance; add `<RuntimeAsync>true</RuntimeAsync>` to csproj |

### 8. Concurrency Strategy Decision Matrix

| Scenario | Pattern | Why Not X |
|----------|---------|-----------|
| File I/O serialization (LauncherJson.Save) | `SemaphoreSlim(1,1)` with `WaitAsync` | `AsyncLock` adds an external dependency. `SemaphoreSlim` is built-in, the same mechanism, and AOT-safe. |
| Agent discovery pipeline | `Channel<AgentCatalogEntry> + await foreach` consumer loop | Dataflow is overkill for 8 catalog entries. Channels give backpressure + streaming naturally. |
| In-memory state (AppStore._state) | Immutable references + `Interlocked.CompareExchange` for writes | Lock-free reads; no contention on the UI thread. |
| Cache invalidation (MemoizedSelector) | `volatile bool` or `Interlocked.MemoryBarrier()` | Too simple for a lock. Just prevent JIT caching. |
| UI-bound cancellation (batch launch) | `CancellationTokenSource` owned by VM, disposed in `finally` | Already implemented correctly. Standard pattern. |
| Cross-layer CancellationToken | Last-parameter convention with `default` | Minimal boilerplate. No DI magic needed for a desktop app. |
| Startup sync-over-async | `Task.Run(() => …).GetAwaiter().GetResult()` | Escape the UI SynchronizationContext. Then plan for full async init. |

---

## Sources

- Stephen Cleary, "Don't Block on Async Code" (2012) -- https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
- Nito.AsyncEx GitHub -- https://github.com/stephencleary/AsyncEx
- Microsoft Learn, "Async coordination primitives" -- https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/async-coordination-primitives-advanced
- Microsoft Learn, "Channels" -- https://learn.microsoft.com/en-us/dotnet/core/extensions/channels
- Microsoft Learn, "TPL Dataflow" -- https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library
- Microsoft Learn, "Thread-safe collections" -- https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe
- Microsoft Learn, "What's new in .NET 10" -- https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview
- Microsoft Learn, "Synchronous wrappers for async methods" -- https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/synchronous-wrappers-for-asynchronous-methods
- Avalonia UI issue #17610 -- https://github.com/AvaloniaUI/Avalonia/issues/17610
- .NET Runtime-Async feature tracking -- https://github.com/dotnet/runtime/issues/109632
- "Why the Future of C# Is Faster: .NET 10+ Runtime-Async" -- https://medium.com/@schmidt.jeanbaptiste/why-the-future-of-c-is-faster-0949e30fe8bd
- Laurent Kempe, "Exploring .NET 11 Preview 1 Runtime Async" -- https://laurentkempe.com/2026/02/14/exploring-net-11-preview-1-runtime-async-a-dive-into-the-future-of-async-in-net
