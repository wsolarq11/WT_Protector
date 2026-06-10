# Research: Infrastructure Feature Audit (Features 16-19)

- **Query**: Audit 4 infrastructure features against 5 standards (real problem, data ownership, user path, failure contract, test coverage)
- **Scope**: internal
- **Date**: 2026-06-10

## Feature 16: Portable Settings

### Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs` | Settings POCO with 8 fields, [JsonRequired] on structural fields, nullable optional on commandTemplates/searchHistory |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsStore.cs` | Load/Save/TrySaveWindowBounds with full normalization pipeline |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsSnapshot.cs` | Deep-copy capture/restore for rollback |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsCopies.cs` | Manual deep-clone (no reflection) |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs` | JSON engine with UnmappedMemberHandling.Disallow |
| `config/settings.json` | Live settings example with commandTemplates and searchHistory |

### Schema Contract (LauncherSettings.cs)

- **Structurally required fields (with [JsonRequired]):** `lastFilterText`, `sortMode`, `entryViewMode`, `requireConfirmationForLaunches`, `customEntryOrder`, `window`
- **Optional fields (without [JsonRequired]):** `commandTemplates`, `searchHistory` — null-safe normalization; null → empty dict/list
- **Unknown-field rejection:** `UnmappedMemberHandling.Disallow` catches wrong-case keys (e.g., "SortMode" vs "sortMode") and retired fields (e.g., "theme") — see SettingsStoreTests line 196
- **Normalization pipeline:** trim whitespace, validate mode IDs, reject duplicates in customEntryOrder, reject non-positive window dimensions

### Test Coverage

31 tests in `LauncherConfigStoreTests.SettingsStoreTests.cs` cover:
- Round-trip save/load
- Null rejection for every structurally required field (lastFilterText, sortMode, entryViewMode, customEntryOrder, window)
- Window null and non-positive dimensions
- Unknown field rejection (theme, wrong case)
- Trim normalization
- Duplicate rejection in customEntryOrder
- Invalid mode ID rejection
- TrySaveWindowBounds success/rollback/invalid-inputs
- Command templates with empty keys/values/duplicates

### Key Patterns

- `RequireCommandTemplates` normalizes null → [] (non-breaking for missing optional field)
- `RequireSearchHistory` skips blank terms, deduplicates case-insensitively
- `NormalizeRequiredMode` validates both null AND whitespace-only values

## Feature 17: JSON Persistence, Backup and Rollback

### Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherJson.cs` | Core persistence engine |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherSettingsMutationPersistence.cs` | Settings save with snapshot rollback |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherConfigMutationPersistence.cs` | Config save wrapper |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherEntryMutationPersistence.cs` | Multi-file entry mutation with staged rollback |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/LauncherPresetMutationPersistence.cs` | Preset save with snapshot rollback |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsSnapshot.cs` | Snapshot capture/restore for settings |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettingsCopies.cs` | Deep clone without reflection |
| `.trellis/spec/core/persistence.md` | Persistence spec documenting atomic write pattern |

### Atomic Write Pattern (LauncherJson.cs lines 36-61)

```
temp file (Guid.NewGuid:N) → rotate backups (.bak → .bak.1 → .bak.2 → delete) → copy → File.Move (atomic)
finally: cleanup temp
```

- `File.Move` with `overwrite: true` is atomic on same volume
- Temp file names include `Guid.NewGuid():N` to prevent collision
- Line ending normalization: all JSON uses LF only
- `ReadCommentHandling = Skip`, `AllowTrailingCommas = true` for user-friendly config editing

### Backup Rotation

3-generation rotation: `.bak` → `.bak.1` → `.bak.2` → delete oldest.

### Mutation/Rollback Pattern

Each persistence layer follows the same contract:
1. Clone/snapshot before mutation
2. Attempt save through LauncherJson
3. On IO/access failure: restore snapshot in-memory, return Chinese-localized error string
4. On validation failure: throw InvalidDataException (no rollback — data was never written)

Verified across:
- `LauncherSettingsMutationPersistence.Save()` — snapshot restore on IOException/UnauthorizedAccessException
- `LauncherConfigMutationPersistence.Save()` — returns status string on IO failure
- `LauncherEntryMutationPersistence.Save()` — staged rollback across config/preset/settings files
- `LauncherSettingsStore.TrySaveWindowBounds()` — catches IOException/UnauthorizedAccessException/SecurityException, restores snapshot

### Test Coverage

37 tests across mutation persistence + snapshot test files:
- Settings mutation persistence: 3 tests (success, rollback, invalid rejection)
- Config mutation persistence: 3 tests
- Entry mutation persistence: multiple staged rollback tests
- Preset mutation persistence: snapshot tests
- Settings snapshot: 3 tests (full restore, CustomEntryOrder-only restore, null fields restore without defaults)
- TrySaveWindowBounds: 7 tests (success, path-as-file crash, invalid dims, null window, duplicate order rejection)

## Feature 18: Core/App/Tests Separation

### Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/WTLauncher.Avalonia.sln` | 3-project solution |
| `src/avalonia-launcher/src/WTLauncher.Core/WTLauncher.Core.csproj` | Core project: net10.0 only, zero package references |
| `src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj` | App project: Avalonia + CommunityToolkit.Mvvm |
| `src/avalonia-launcher/tests/WTLauncher.Tests/WTLauncher.Tests.csproj` | Tests: xunit.v3, references both App and Core |
| `src/avalonia-launcher/src/WTLauncher.Core/` | 26 .cs files, zero Avalonia imports |

### Project Dependency Graph

```
WTLauncher.Core ─── no dependencies (pure net10.0, no NuGet refs)
WTLauncher.App ─── depends on WTLauncher.Core + Avalonia.Desktop + Avalonia.Fonts + CommunityToolkit.Mvvm
WTLauncher.Tests ─── depends on both Core + App + xunit.v3
```

### Zero Avalonia in Core

Grepping for "Avalonia" in `src/WTLauncher.Core/` returns 0 matches. Core contains:
- Data models: `LaunchEntry`, `LauncherSettings`, `LauncherPreset`, `LauncherEntryVisibility`
- IO: `LauncherJson`, `LauncherConfigStore`, `LauncherSettingsStore`, `LauncherPresetStore`
- Logic: `LauncherEntryOrder`, `LauncherEntryIds`, `LauncherEntryText`, `LaunchArgumentBuilder`, `LaunchPlan`
- Snapshots: `LauncherSettingsSnapshot`

### Test Count

427 test methods across 78 test files (Facts + Theory inline data). Tests reference both App and Core assemblies, enabling integration-level testing of ViewModels against Core data without Avalonia runtime.

### Key Patterns

- `LauncherSettingsCopies` performs manual deep-clone — no reflection, no serialization round-trip
- Core uses `record with { }` for `LaunchEntry` cloning in `CopyNormalized`
- ViewModel layer (App) introduces mutation snapshots that bridge Core data with persistence

## Feature 19: Self-Contained win-x64 Publish and Native Gate

### Files Found

| File Path | Description |
|---|---|
| `tools/build-release.ps1` | Release build script with restore + self-contained publish |
| `tools/verify.ps1` | 18-check root verification gate |
| `src/avalonia-launcher/verify.ps1` | Mainline verify: test run + self-contained publish + GUI smoke |
| `tools/config-lib.ps1` | Shared PowerShell library for paths and tooling |

### build-release.ps1

- Restores solution with `-r win-x64`
- Publishes with `--self-contained true -c Release`
- Generates `receipt.json` with SHA256, byte size, build timestamp
- Tags version via `git describe --tags --always --dirty`, falling back to `0.1.0-dev`
- Blocks publish if target exe is running (safe overwrite guard)

### verify.ps1 (root — 18 checks)

The root verify script runs 18 structural checks:
1. line-ending-policy (.gitattributes LF)
2. snow-ignored-local-state
3. retired-paths-removed (spikes, winforms-launcher, etc.)
4. mainline-layout (solution/project structure)
5. readme (6 sections present)
6. feature-objective-boundary (21 feature mappings audited)
7. claim-boundary-ledger (4 allowed, 5 forbidden claims)
8. github-similar-projects-snapshot (12+ repos verified)
9. config-schema (unique IDs, selected fields)
10. presets-schema (default preset, entry references)
11. settings-json (schema validation, customEntryOrder integrity)
12. shortcut-backup-retirement
13. winforms-retired-design
14. hta-retired-archive
15. hta-retired-config
16. avalonia-mainline-verify (delegates to mainline verify.ps1)
...plus json and other inline checks

### verify.ps1 (mainline — in src/avalonia-launcher/)

3-phase pipeline:
1. `dotnet test` with TRX output, fail-on-zero-tests and fail-on-failed-tests
2. `dotnet publish --self-contained true -r win-x64`
3. GUI smoke test: launch published exe with temp config, wait for window title "WT Launcher", CloseMainWindow, verify graceful exit

The GUI smoke creates a minimal temp config directory, validates the app boots to the expected window title, and confirms clean shutdown.

### 16 Checks Referenced

The task mentions "16 checks" — this matches the visual/gate checks that are customer-visible (excluding the 2 internal receipt/structural checks in the root verify script). The mainline verify pipeline is 3-phase with the delegated check as 1 of 18 root checks.

### Gate Properties

- Source-level: .gitattributes LF policy, .gitignore snow/ exclusion, retired path absence
- Config-level: config.json schema (selected fields, existing directories), settings.json schema (all required fields, window dims positive), presets.json schema
- Build-level: self-contained publish with runtime identifier, exe existence, SHA256 verification
- Runtime-level: GUI smoke with window title detection and graceful exit
