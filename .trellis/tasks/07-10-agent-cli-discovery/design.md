# Design — AI Agent CLI Discovery Feature

Parent: `07-10-v2-agent-discovery-and-hygiene`
PRD: see `prd.md`

> Complex task: this feature adds ~730 lines of new code across all 5 layers. Per CLAUDE.md "net-additive for feature work" + ponytail "no abstraction without need" — we add code, do not refactor existing.

## Boundary

### Adds (new files)

| Layer | Path | Purpose |
|---|---|---|
| Core | `src/WTLauncher.Core/AgentCatalogEntry.cs` | Record: one known-agent row |
| Core | `src/WTLauncher.Core/AgentCatalog.cs` | Static readonly list of known agents |
| Core | `src/WTLauncher.Core/DiscoveredAgent.cs` | Record: catalog entry + resolved path + version |
| Core | `src/WTLauncher.Core/AgentDiscoveryScanner.cs` | Pure function: scan PATH, detect versions |
| Core | `src/WTLauncher.Core/AgentDiscoveryResult.cs` | Result wrapper (success / partial-failure with reasons) |
| VM | `src/WTLauncher.App/ViewModels/DiscoverAgentsViewModel.cs` | Dialog VM: selection state, progress |
| VM | `src/WTLauncher.App/ViewModels/MainWindowViewModel.Discovery.cs` | partial: `DiscoverAgentsCommand`, dialog injection |
| View | `src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml` | Recommendation dialog XAML |
| View | `src/WTLauncher.App/Views/DiscoverAgentsWindow.axaml.cs` | Code-behind (windowing only) |
| Tests | `tests/WTLauncher.Tests/AgentCatalogTests.cs` | Catalog integrity |
| Tests | `tests/WTLauncher.Tests/AgentDiscoveryScannerTests.cs` | Pure-function scan + version parsing |
| Tests | `tests/WTLauncher.Tests/DiscoverAgentsViewModelTests.cs` | Dialog state machine |
| Tests | `tests/WTLauncher.Tests/MainWindowViewModelTests.DiscoveryTests.cs` | End-to-end via mocked scanner |

### Touches (existing files, minimal edit)

| Path | Change | Why |
|---|---|---|
| `src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` | Add `partial` already exists; add `Discovery` partial sibling + `Func<DiscoverAgentsViewModel?>` injection in ctor | Dialog injection pattern matches existing `Func<...>` for entry/preset dialogs |
| `src/WTLauncher.App/Views/MainWindow.axaml` | Add "🔍 发现" button in header action row | Trigger surface |
| `src/WTLauncher.App/Views/MainWindow.axaml.cs` | Wire dialog `Func<>` injection | Same pattern as ConfirmLaunchWindow etc. |
| `docs/feature-reality-north-star-audit.md` | Add feature #24: AI Agent Discovery | Keeps audit honest |
| `AGENTS.md` | Mention discovery feature in repo map | Navigation |

### Does not touch

- `config/launcher.json` schema (still v2, still single-file, still atomic write via `LauncherJson.Save`)
- Existing `LauncherEntryDrafts`, `LauncherEntryMutationSnapshot`, `LauncherStore` — discovery reuses them as-is
- ArchUnitNET R1-R6 invariants
- 5-layer dependency direction
- Existing tests (only adds new test files)

## Data Contracts

### `AgentCatalogEntry` (Core, immutable record)

```csharp
namespace WTLauncher.Core;

/// <summary>One known AI agent CLI in the static catalog.</summary>
public sealed record AgentCatalogEntry
{
    public required string ExecName { get; init; }       // e.g. "claude"
    public required string DisplayName { get; init; }     // e.g. "Claude Code"
    public required string Vendor { get; init; }          // e.g. "Anthropic"
    public required string VersionArgs { get; init; }     // e.g. "--version"
    public string RecommendedArgs { get; init; } = "";    // e.g. "--enable goals"
    public required AgentCategory Category { get; init; } // TerminalNative / IDEExtension / DesktopApp
    public string HomePageUrl { get; init; } = "";
}

public enum AgentCategory { TerminalNative, IDEExtension, DesktopApp }
```

### `DiscoveredAgent` (Core)

```csharp
public sealed record DiscoveredAgent
{
    public required AgentCatalogEntry Catalog { get; init; }
    public required string ResolvedPath { get; init; }    // absolute path to binary
    public string Version { get; init; } = "";            // parsed version, "" on failure
}
```

### `AgentDiscoveryResult` (Core)

```csharp
public sealed record AgentDiscoveryResult
{
    public required ImmutableArray<DiscoveredAgent> Found { get; init; }
    public required ImmutableArray<AgentDiscoveryFailure> Failures { get; init; } // partial; scanner continues on per-agent error
}

public sealed record AgentDiscoveryFailure(string ExecName, string Reason);
```

## Data Flow

```
User clicks "🔍 发现"
  ↓
MainWindowViewModel.Discovery.DiscoverAgentsCommand
  ↓ injects
  Func<DiscoverAgentsViewModel>  (constructed with injected IAgentDiscoveryScanner)
  ↓
DiscoverAgentsViewModel.ScanAsync (background thread)
  ↓ calls
  AgentDiscoveryScanner.Scan(catalog, cancellationToken) → AgentDiscoveryResult
  ↓
DiscoverAgentsWindow opens, shows DiscoveredAgent list
  ↓
User checks agents, clicks "添加选中 (N)"
  ↓
For each selected → build LaunchEntryDraft via AgentRecommendationFactory.ToDraft(discovered)
  ↓ Reuses existing pipeline:
  LauncherEntryDrafts.Validate → MainWindowViewModel.AddEntryFromDraft
    → snapshot via LauncherEntryMutationSnapshot
    → mutate _entries
    → LauncherStore.Save (atomic)
    → rollback on failure
  ↓
Status: "已添加 N 个 AI Agent 启动项"
Dialog closes
```

## Layer Mapping (5-layer architecture respect)

| Layer | Owns | Forbidden |
|---|---|---|
| **0 Core** | `AgentCatalogEntry`, `DiscoveredAgent`, `AgentDiscoveryResult`, `AgentCatalog` (static), `AgentDiscoveryScanner` (pure), `AgentRecommendationFactory` (pure: DiscoveredAgent → draft shape) | Reference Avalonia / CommunityToolkit; spawn processes without cancellation; touch `config/launcher.json` |
| **1 VM command** | `DiscoverAgentsCommand` in `MainWindowViewModel.Discovery.cs` partial | Bypass existing add-entry pipeline |
| **2 VM persistence+rollback** | Reuses `LauncherEntryMutationSnapshot` + `LauncherStore.Save` | New persistence path |
| **3 VM orchestration** | `MainWindowViewModel` injects `Func<DiscoverAgentsViewModel>` through `MainWindow` ctor | Hold a second copy of entries state |
| **4 Views** | `DiscoverAgentsWindow.axaml` + `.cs` (windowing only); "🔍 发现" button | Business `if` statements about which agent is "safe" |

## Tradeoffs

### T1: Static catalog vs plugin marketplace
- **Choice**: Static `AgentCatalog` hardcoded list.
- **Why**: YAGNI; plugin marketplaces are a known scope-creep trap. The catalog file is small (~80 lines for 10 agents); adding agents is a 1-line edit.
- **Upgrade path**: if catalog grows past ~30 agents or external contribution needed, extract to embedded JSON resource loaded at startup.

### T2: Sync vs async scan
- **Choice**: Async with cancellation token, dialog shows progress.
- **Why**: PATH scan + multiple process spawns for `--version` can take 2-5s on cold cache. Sync would freeze UI.
- **Cost**: cancellation token threading, `ConfigureAwait` care; documented in `.editorconfig` (CA2007 already `none`).

### T3: Version detection vs skip
- **Choice**: Detect via `--version`, parse first line, 2s timeout per agent.
- **Why**: Version display in card gives user confidence ("yes this is the binary I think it is"). Cheap information.
- **Cost**: ~10 LOC per agent for parsing (regex for semver-like substring); failure → empty string (not fatal).

### T4: Recommendation dialog vs inline panel
- **Choice**: Modal dialog `DiscoverAgentsWindow`.
- **Why**: Existing pattern (ConfirmLaunchWindow, EntryDetailsWindow) — consistent UX. Inline panel would require main-window layout rework.
- **Cost**: dialog lifetime management matches existing pattern.

### T5: New `$key` command template injection vs raw commandline
- **Choice**: If catalog entry's `RecommendedArgs` references a `$key`, add a new entry to `settings.commandTemplates`; else write raw commandline.
- **Why**: Reuses existing template system instead of bypassing it.
- **Cost**: 1 extra template entry per agent with non-empty RecommendedArgs.

## Compatibility

- **launcher.json schema**: unchanged. New entries are regular `LaunchEntry` records.
- **Existing entries**: untouched. Discovery is additive.
- **Existing tests**: untouched. New tests live in new files.
- **ArchUnitNET R1-R6**: still enforced. `AgentDiscoveryScanner` lives in Core, has no UI deps.
- **verify.ps1 gate**: unchanged structurally; the `no-stale-three-file-refs` check applies because docs/feature-first-class-upgrade-audit.md is in activeDocs.

## Rollback Shape

Each new file is independently deletable. If the feature needs to be backed out:

1. Remove "🔍 发现" button from `MainWindow.axaml`
2. Revert `MainWindowViewModel.cs` ctor signature (drop the new `Func<>`)
3. `rm` the 13 new files listed in "Adds" above
4. `dotnet build` still passes (no dependencies on the new code from existing code)
5. `dotnet test` still passes (existing tests untouched)

No data migration needed — discovery never persisted state of its own.

## Test Strategy

| Test file | Coverage target | Pattern |
|---|---|---|
| `AgentCatalogTests` | Catalog integrity: no duplicate ExecName, all have VersionArgs, all have non-empty DisplayName/Vendor | `[Fact]` × 4-6 |
| `AgentDiscoveryScannerTests` | Synthetic PATH with fake executables in temp dir; version parsing edge cases (semver, leading `v`, multiline output, timeout) | `[Theory]` × 12-15 |
| `DiscoverAgentsViewModelTests` | Selection state, "已添加" dedup logic, "重新扫描" resets state | `[Fact]` × 5-7 |
| `MainWindowViewModelTests.DiscoveryTests` | End-to-end with mocked scanner: dialog opens → user selects N → N entries added → atomic save → rollback on failure | `[Fact]` × 4-6 |

Total: ~25-34 new tests.

## Performance / Concurrency

- Scan runs on `Task.Run` with `CancellationTokenSource` (2s timeout per agent, ~10s total worst case)
- Dialog shows `IsScanning` spinner during scan; user can cancel via "取消"
- Multiple concurrent scans not supported (button disabled while `IsScanning`)
- No static mutable state — every scan gets fresh local state

## Security Considerations

- Scanner only reads PATH and spawns `--version` (read-only, no shell expansion)
- Dialog never auto-launches agents — user must explicitly add + launch separately
- No `Process.Start(cmd, ...)` with user-controlled input (only `Process.Start(execPath, "--version")`)
- ResolvedPath displayed to user (transparency)

## Open Questions Resolved Here

- Q: Should catalog include version detection regex per agent? — A: No. Use one generic regex (`\d+\.\d+(?:\.\d+)?`) for all. Specific agents can override later if needed.
- Q: Should we dedupe against existing entries by `command` field? — A: Yes, by `ExecName` substring in `command`. Simpler than full template resolution.
- Q: First-run auto-trigger? — A: No in v2.1; ship manual trigger first. Add `settings.discoveryAutoTriggerDismissedAtUtc` field later if user feedback warrants.

## Implementation Phases

See `implement.md`.
