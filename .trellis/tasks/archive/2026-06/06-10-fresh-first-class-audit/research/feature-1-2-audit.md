# Research: Feature 1-2 Audit (Launch Entry Object + Existing-Directory Contract)

- **Query**: Audit 2 features against 5 standards (real problem, data ownership, user path, failure contract, test coverage)
- **Scope**: internal
- **Date**: 2026-06-10

## Feature 1: Launch Entry Object

### Source Anchors

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LaunchEntry.cs` | 7-field record definition with `[JsonRequired]` on id/name/directory/command |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherConfigStore.cs:176-218` | `Normalize()` pipeline validates every field at load time |
| `src/avalonia-launcher/src/WTLauncher.Core/LaunchArgumentBuilder.cs:11-36` | `Build()` takes `LaunchEntry`, validates fields, produces `LaunchPlan` |
| `config/config.json` | 19 live entries consuming the contract |

### Data Ownership

- **Single source of truth:** `LaunchEntry` record (7 fields: id, name, directory, command, selected, updatedAtUtc, launchedAtUtc)
- **Entry points that consume it:** `LauncherConfigStore.Load()` normalizes + validates at load; `LaunchArgumentBuilder.Build()` validates at launch time
- **No second source:** `selected` is the only mutable field stored alongside the entry — prior audit flagged this as future-workload but not a current problem (19 entries, single-user)
- JSON schema enforced by `[JsonRequired]` on core fields (id/name/directory/command), `UnmappedMemberHandling.Disallow` prevents unknown keys

### Failure Contract

| Layer | What fails | How |
|---|---|---|
| JSON load (`LauncherConfigStore.Normalize`) | Null entry, duplicate id, empty name/directory/command | `InvalidDataException` with index |
| JSON load (`RequireEntrySelectionFields`) | Missing `selected` field | `InvalidDataException` with index |
| Build time (`LaunchArgumentBuilder.Build`) | Empty name/directory/command | `InvalidOperationException` with entry name |
| Build time (when `validateDirectory=true`) | Directory does not exist on disk | `InvalidOperationException` |

### Test Coverage

- `LaunchArgumentBuilderTests.cs`: 5 test methods covering Build contract, confirmation parameter, directory validation parameter, Windows command-line escaping (4 inline data cases), null rejection
- `LauncherConfigStoreTests.ConfigPersistenceTests.cs`: covers Load cycle including `RequireEntrySelectionFields`
- Tests validate the `Build` output format against literal expected argument strings

### Key Strengths

- Record type enforces immutability-by-default
- `[JsonRequired]` + `UnmappedMemberHandling.Disallow` prevents silent degradation from malformed JSON
- No hidden defaults — every field is either required or explicitly nullable
- Normalization is systematic: every field passes through `Normalize()`/`RequireEntryText()` before use

## Feature 2: Existing-Directory Contract

### Source Anchors

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LaunchArgumentBuilder.cs:25-26` | Runtime directory existence check |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherLaunchWorkflow.cs:23-24` | Production path: always calls `Build(entry, validateDirectory: true, ...)` |
| `tools/verify.ps1:299-301` | Root gate: `Test-Path -LiteralPath $item.directory -PathType Container` for every entry |
| `src/avalonia-launcher/verify.ps1` | Mainline verify: runs tests + publishes + GUI smoke |
| `src/avalonia-launcher/tests/WTLauncher.Tests/LaunchArgumentBuilderTests.cs:37-46, 64-73` | Tests for both validateDirectory modes |

### Data Ownership

- **Runtime check:** `LaunchArgumentBuilder.Build(entry, validateDirectory: true, ...)` calls `System.IO.Directory.Exists(directory)` — single gate, single call site
- **Gate check:** `tools/verify.ps1` iterates every config entry and asserts `Test-Path -LiteralPath $item.directory -PathType Container`
- No tri-state or fuzzy matching — directory exists or it does not
- `validateDirectory` is a parameter, not a global state flag — caller decides

### User Path

1. User edits `config/config.json`, sets a `directory` value
2. User runs `tools/verify.ps1` (pre-commit/pre-publish gate) → directory must exist on the machine running verify
3. At launch time, `LauncherLaunchWorkflow.BuildPlan(entry, requireConfirmation)` always calls `Build(entry, validateDirectory: true, ...)`
4. If directory is missing, `InvalidOperationException` surfaces before any process starts
5. In batch launch, the failure is caught per-entry and reported in the `LauncherBatchLaunchReport`

### Failure Contract

| Checkpoint | What happens on failure |
|---|---|
| `tools/verify.ps1` (config-schema gate) | `throw "Config item $idx directory does not exist: $($item.directory)"` — hard stop, no recovery |
| `LaunchArgumentBuilder.Build(validateDirectory: true)` | `throw new InvalidOperationException($"Launch entry '{name}' directory does not exist: {directory}")` |
| `LaunchArgumentBuilder.Build(validateDirectory: false)` | Skips check — used in test (`Build_ProducesCurrentWtArgumentContract` creates temp dir, `Build_UsesDirectoryValidationParameter` tests missing dir with validation disabled) |
| `LauncherLaunchWorkflow.PreflightBatch` | Catches `Exception` per-entry, records failure with entry name — does not abort remaining entries |

### Test Coverage

- `Build_UsesDirectoryValidationParameter`: explicitly tests that `validateDirectory: false` allows a missing directory
- `Build_RejectsMissingDirectoryWhenValidationEnabled`: creates entry with non-existent GUID directory, asserts `InvalidOperationException` is thrown when `validateDirectory: true`
- Gate-level: `tools/verify.ps1` config-schema check covers 100% of entries in the current config

### Key Strengths

- Fail-fast at the exact boundary: before process creation, not after
- Two independent checkpoints (verify.ps1 gate + runtime Build) provide defense in depth
- `validateDirectory` parameter makes the behavior testable — tests can disable validation without creating real directories
- Production path (`LauncherLaunchWorkflow`) hardcodes `validateDirectory: true` — no accidental bypass
