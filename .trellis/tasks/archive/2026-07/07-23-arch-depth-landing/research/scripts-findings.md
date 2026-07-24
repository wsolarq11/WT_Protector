# Research: Build/Verify Scripts and CI Infrastructure Findings

- **Query**: Ultrareview -- Build/verify scripts and CI infrastructure
- **Scope**: Internal (files in repo)
- **Date**: 2026-07-23

## Findings

### Script Correctness

#### 1. Error Handling

- **tools/verify.ps1** (line 5): `$ErrorActionPreference = 'Stop'` is set, which stops on command failures. Combined with `Set-StrictMode -Version Latest` (line 3). Confidence: 100.
- **tools/build-release.ps1** (line 4-5): Same pattern. Confidence: 100.
- **src/avalonia-launcher/verify.ps1** (line 5): Same pattern. Confidence: 100.
- **tools/test-launch-args.ps1** (line 4-5): Same pattern. Confidence: 100.
- **All scripts** source `config-lib.ps1` which provides `Invoke-NativeStep` (config-lib.ps1:8-11). This function checks `$LASTEXITCODE` after executing a script block, throwing if non-zero. This handles native exit codes correctly. Confidence: 100.
- **verify.ps1** does NOT wrap its `dotnet` invocations in `Invoke-NativeStep` -- it delegates to `src/avalonia-launcher/verify.ps1` via `& $mainlineVerify` (line 505) and captures the last JSON line. There is no `Invoke-NativeStep` guard around this subprocess call; if the sub-script fails mid-way, the output parsing on line 506 would receive corrupt/missing JSON and likely throw, but the error message would be confusing (JSON parse error vs. actual cause). Confidence: 75.

#### 2. Hardcoded Paths

- **tools/build-release.ps1** (line 9): `$vsInstaller = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'` -- hardcoded path to the Visual Studio installer for locating `vswhere.exe`. This is required for Native AOT (to find the C++ compiler toolchain). It assumes the standard VS installation path; non-standard installations or VS Build Tools-only setups would miss this. Confidence: 80.
- **tools/verify.ps1** (line 534): Whitelist of active docs is hardcoded: `@('README.md', 'AGENTS.md', 'DESIGN.md', ...)`. Adding a new doc requires a code change to the gate. Confidence: 100.

#### 3. Working Directory Assumptions

- **config-lib.ps1** (`Get-RepoRoot`, line 4): Uses `Split-Path -Parent $PSScriptRoot`. This assumes `config-lib.ps1` lives in a `tools/` directory directly under the repo root. All scripts sourcing it from `tools/` are correct. Confidence: 100.
- **src/avalonia-launcher/verify.ps1** (line 7): Sources `config-lib.ps1` with a path traversal of `(Split-Path -Parent (Split-Path -Parent $PSScriptRoot))`. This assumes `verify.ps1` is at `src/avalonia-launcher/verify.ps1` (2 levels down from repo root). This is correct for the current layout but fragile -- moving the script one directory deeper breaks it silently. Confidence: 90.
- **src/avalonia-launcher/run.ps1** (lines 10-19): Does its own repo root detection by walking up the directory tree looking for `config\launcher.json` AND `README.md`. This is more robust than the config-lib approach but duplicates root-detection logic. Confidence: 95.
- **tools/verify.ps1** (line 505): Calls `& $mainlineVerify` without changing working directory. The sub-script uses `$PSScriptRoot` which is absolute, so this is fine. Confidence: 100.

#### 4. Gate Effectiveness

- **verify.ps1** is comprehensive with ~25 `Add-Check` calls covering: line-ending policy, `.gitignore`/`.snow/` enforcement, retired path absence, bare `catch(Exception)` detection, raw hex color detection in App layer (lines 95-139 with self-testing fixture), mainline layout existence, README section structure, feature audit with 22/22 narrow north-star verification, claim boundary ledger validation (4 allowed + 5 forbidden claims), GitHub similar-project snapshot freshness and structural validation, launcher.json schema, preset integrity, settings validation, command template cross-validation, shortcut backup manifest, WinForms retired design, HTA retired config, Avalonia mainline pass-through, stale three-file reference detection, settings single-source invariant, DSC yaml structural markers, imperative UI construction ban in Views, and 3 security audit regression gates (F1 DirectoryPath `Path.GetFullPath`, F2 IPresetService `knownEntryIds`, F3 EntryId `LatinScript`). Confidence: 100.
- **Self-testing fixture** (lines 126-134 of verify.ps1): Tests the raw-hex regex by creating a temp file with `#123456` and verifying the check catches exactly 1 violation. Good pattern. Confidence: 100.
- **What the gate does NOT check**: It never runs `dotnet restore --locked-mode` to prevent lock file drift. It doesn't validate that `Directory.Packages.props` has no orphaned package versions (unused references). It doesn't measure binary size regressions. Confidence: 85.

#### 5. Test Result Parsing

- **src/avalonia-launcher/verify.ps1** (lines 40-45): Parses the TRX file via `[xml]$testResults = Get-Content ...`. Checks `$counters.total >= 1` and `$counters.failed == 0`. This is correct and catches zero-test runs and failures. Confidence: 100.
- **tools/verify.ps1** (lines 506-513): Parses the JSON receipt from the mainline verify sub-script. Checks `$mainline.status -ne 'PASS'`. This is a pass-through check that relies on the sub-script's own pass/fail determination. If the sub-script fails hard (throws), the parent sees a different error. Confidence: 90.

### CI Pipeline

#### 1. Cache Strategy

- **NuGet caching** in `.github/workflows/verify.yml` (lines 24-30): Uses `actions/cache@v4` with key `${{ runner.os }}-nuget-${{ hashFiles('**/Directory.Packages.props') }}` and restore key `${{ runner.os }}-nuget-`. Caches `~/.nuget/packages`. Confidence: 100.
- Cache key only hashes `Directory.Packages.props`. Changes to individual `.csproj` files that add/remove package references won't invalidate the cache. However, since centralized package management (`ManagePackageVersionsCentrally=true`) means all versions are in `Directory.Packages.props`, this is acceptable -- adding a new package reference in a `.csproj` doesn't change the version, it just adds a `PackageReference` node. The cache hit would still restore all packages, but a restore would pull any newly referenced packages. Low risk. Confidence: 80.
- **full-gate job** does NOT use NuGet caching. It re-runs restore, build, test, and publish from scratch. This means it's slower but more reliable for the comprehensive gate. Confidence: 100.

#### 2. Matrix Strategy

- Single OS: `windows-latest` only. The app targets `win-x64` only (self-contained), so multi-OS doesn't apply. Confidence: 100.
- Single configuration: Release only. Confidence: 100.

#### 3. Secret Handling

- No secrets are passed to any scripts in `.github/workflows/verify.yml`. The `gh` CLI is not needed. Confidence: 100.

#### 4. Artifact Retention

- Test results uploaded via `actions/upload-artifact@v4` (line 46) with `if: always()`. No explicit `retention-days` set -- uses GitHub default (90 days). Confidence: 100.
- Artifact path: `src/avalonia-launcher/tests/WTLauncher.Tests/TestResults/`. This is the default xUnit TRX output directory in `--no-build` mode (the test step uses `--no-build` so results go to default location, NOT the custom `--results-directory` path used in local verify.ps1). Confidence: 100.

#### 5. Trigger Configuration

- Trigger: `push` on `main`/`master` (lines 3-4) and `pull_request` on `main`/`master` (lines 5-6). Confidence: 100.
- `full-gate` job (line 70): Condition `github.event_name == 'pull_request' || github.ref == 'refs/heads/main' || github.ref == 'refs/heads/master'` means it runs on PRs targeting main/master AND on pushes to main/master. However, on pushes to main/master, the `build-and-test` job already ran. The `full-gate` re-runs everything from scratch without caching. This is intentional -- the full gate is the authoritative check. Confidence: 100.

#### 6. Missing `release.yml`

- `.github/workflows/release.yml` does NOT exist. There is no automated release pipeline. Releases are built locally via `tools/build-release.ps1`. Confidence: 100.

#### 7. NuGet Audit Job

- The `nuget-audit` job (lines 51-64) only runs `dotnet restore`. It does NOT run `dotnet nuget audit` or any explicit auditing command. The actual audit is performed at build time via `NuGetAudit=true` and `NuGetAuditMode=all` in `Directory.Build.props` (lines 10-11). The `nuget-audit` CI job name is misleading -- it only restores, it doesn't audit. Confidence: 95.

### Project Configuration

#### 1. Target Framework

- All three projects target `net10.0`. Consistent across: `WTLauncher.App.csproj:4`, `WTLauncher.Core.csproj:3`, `WTLauncher.Tests.csproj:3`. Confidence: 100.
- No multi-targeting complexity. Single TFM. Confidence: 100.

#### 2. Package Versions

- All package versions are centrally managed in `Directory.Packages.props` (line 3: `ManagePackageVersionsCentrally=true`). Confidence: 100.
- **All versions are pinned** (no floating `*` or `x` ranges). Examples: `Avalonia 12.1.0`, `CommunityToolkit.Mvvm 8.4.2`, `Fluxor 6.10.0`, etc. Confidence: 100.
- **Unused package version**: `Fluxor.Blazor.Web` version `6.10.0` (Directory.Packages.props:21) is listed in the centralized versions file but is NOT referenced by any `.csproj`. This is an orphaned entry -- no project has a `PackageReference` for it. Confidence: 100.
- **Potential version inconsistency**: `Microsoft.Extensions.DependencyInjection` version `8.0.0` (Directory.Packages.props:11). This targets .NET 8 era, but the project targets `net10.0`. This may work (NuGet packages are generally backwards-compatible) but there may be a newer build available. Not necessarily a bug. Confidence: 60.
- **Inconsistency between App and Tests no-warn**: App csproj suppresses CS0618 for `LaunchPlan.CommandLine` obsolete usage (App:14). Tests csproj also suppresses CS0618 for backward-compat verification (Tests:9). This is intentional and documented in comments. Confidence: 100.

#### 3. Trimming / Publishing

- `IsAotCompatible=true` set on both App.csproj (line 11) and Core.csproj (line 6). Tests do not set this. Confidence: 100.
- `PublishAot` is NOT set in any `.csproj`. It is passed via CLI argument only in `build-release.ps1` (line 48: `-p:PublishAot=true`). The CI `build-and-test` job and `full-gate` job both publish WITHOUT `PublishAot` (they use `--self-contained true` for the smoke publish). This means CI never validates Native AOT compilation. Confidence: 100.
- `BuiltInComInteropSupport=false` on App.csproj (line 12) -- correct for AOT compatibility. Confidence: 100.
- `SelfContained` is only specified via CLI (`--self-contained`), not in the csproj. Confidence: 100.

#### 4. Warning-as-Error Configuration

- `Directory.Build.props` line 5: `TreatWarningsAsErrors=true`. This applies to all projects in the solution. Confidence: 100.
- `Directory.Build.props` line 7: `AnalysisLevel=latest-recommended` -- aggressive analyzer configuration. Confidence: 100.
- Core.csproj and Tests.csproj additionally set `WarningsAsErrors=nullable` (Core:5, Tests:6). This is redundant with `TreatWarningsAsErrors=true` in Directory.Build.props, since nullable warnings would already be errors. However, it doesn't cause issues -- it just re-emphasizes nullable warnings specifically. Could be a leftover from before Directory.Build.props was added. Confidence: 80.
- App.csproj (line 14): `NoWarn` for CS0618 is added. This is intentional and documented for `LaunchPlan.CommandLine` obsolete usage. Confidence: 100.
- Tests.csproj (line 9): Same CS0618 suppression. Intentional for backward-compat verification. Confidence: 100.

#### 5. Nullable Settings

- `Nullable=enable` set globally in Directory.Build.props (line 3). Confidence: 100.
- All three `.csproj` files also set `Nullable=enable` individually (App:6, Core:4, Tests:5). This is redundant with the Directory.Build.props setting but harmless. Confidence: 100.

#### 6. Assembly Versioning

- No explicit `Version`, `AssemblyVersion`, or `FileVersion` in any `.csproj`. Confidence: 100.
- `build-release.ps1` (lines 26-42) generates version string from `git describe --tags --always --dirty`. E.g., tag `v1.2.3` produces `1.2.3`, tag without version prefix produces `0.1.0-<githash>`. This version is passed via `-p:Version=` and `-p:InformationalVersion=` on line 48. Confidence: 100.
- The CI `build-and-test` job (verify.yml) runs `dotnet test --no-build` and `dotnet publish` without specifying any version. This means CI builds get the default version `1.0.0.0`. Confidence: 100.

### Program.cs Analysis

#### 1. Exception Handling at Top Level

- `AppDomain.CurrentDomain.UnhandledException` handler (lines 22-31): Writes to `Debug.WriteLine`, `Trace.WriteLine`, `Console.Error.WriteLine`, and `ErrorLog.Write`. Exits with code 2 via `Environment.Exit(2)`. Confidence: 100.
- `TaskScheduler.UnobservedTaskException` handler (lines 34-42): Flattens aggregate exception, writes to same outputs, calls `e.SetObserved()` to prevent process crash. Confidence: 100.
- Both handlers use filtered catch through the event pattern (not catch blocks), which is the correct pattern for domain/task events. Confidence: 100.

#### 2. Mutex Handling for Single Instance

- Named mutex `Local\WTLauncher_ConfigMutex` (line 72) with prefix `Local\` (per-session, not global). This means mutex isolation is per-session -- two instances in different RDP sessions could both start. Documentation comment says "per-operation locking inside LauncherJson would be finer-grained." Confidence: 100.
- `TryAcquireInstanceMutex` (lines 68-101): Uses `WaitOne(0)` for non-blocking check. Returns `false` (shows message, exits) if another instance holds it. Confidence: 100.
- Handles `AbandonedMutexException` (line 83) -- a previous instance that crashed without releasing the mutex. Claims ownership. Confidence: 100.
- Graceful failure on permissions/availability (lines 90-101): Catches `UnauthorizedAccessException` and `WaitHandleCannotBeOpenedException` with a filtered catch, logs, and proceeds without protection. This is a ponytail-design best-effort pattern. Confidence: 100.
- `ReleaseInstanceMutex` (lines 104-118): Handles `ApplicationException` and `ObjectDisposedException` via filtered catch. Confidence: 100.
- `ShowAlreadyRunningMessage` uses P/Invoke `MessageBoxW` (lines 121-131) instead of Avalonia dialog. This is intentional -- Avalonia hasn't started yet when the mutex check runs (it's before `BuildAvaloniaApp()`). Confidence: 100.

#### 3. Environment-Specific Branching

- None in `Program.cs`. No `#if DEBUG`, no `env:CI` checks. The branching happens in the verify scripts and run.ps1. Confidence: 100.
- `DeleteStaleTempFiles` (lines 136-149): Best-effort cleanup at startup. Handles `IOException`, `UnauthorizedAccessException`, `DirectoryNotFoundException` via filtered catch. Uses `LauncherStore.ResolveStorePath` and `LauncherJson.CleanupStaleTempFiles`. This is environment-agnostic. Confidence: 100.

### Notable Patterns

#### `ponytail:` Comments

Found across the codebase carrying deliberate simplification notes:
- `verify.ps1:529`: "Negative gate -- active docs must not reference the abandoned three-file architecture."
- `verify.ps1:555`: "this signature (`Entries = _list.MutableEntries.Select... Settings = `) is the unique fingerprint of LauncherStoreFile hand-assembly"
- `verify.ps1:569`: "`winget configure validate` requires DSC v3 on the host -- that's a machine-setup dependency"
- `verify.ps1:606-608`: "Scans for `Path.GetFullPath(` in DirectoryPath.cs only. If the normalization moves to a different class ... this gate misses the regression."
- `verify.ps1:623-626`: "Checks that `knownEntryIds` string appears in IPresetService.cs. A rename of the parameter or migration to a different interface ... would bypass this gate."
- `verify.ps1:640-643`: "Checks that `LatinScript` appears in EntryId.cs. If the whitelist is renamed ... this gate misses the regression."
- `Program.cs:13-16`: "global mutex held for app lifetime. Per-operation locking inside LauncherJson would be finer-grained"
- `Program.cs:92-93`: "best-effort -- permissions or namespace restrictions prevent the mutex."

#### Redundant `WarningsAsErrors=nullable`

- `WTLauncher.Core.csproj:5` and `WTLauncher.Tests.csproj:6` both set `WarningsAsErrors=nullable`. This is redundant because `Directory.Build.props:5` already sets `TreatWarningsAsErrors=true` which promotes all warnings (including nullable warnings) to errors. This redundancy is harmless but suggests either: (a) these projects existed before Directory.Build.props was introduced and the setting was never cleaned up, or (b) the maintainer wanted explicit emphasis on nullable warnings. Confidence: 80 that this is leftover from before Directory.Build.props.

#### Contract Validation in Mainline Verify

- `src/avalonia-launcher/verify.ps1` (lines 47-78) runs three contract-level validations in addition to tests: (1) check that `[JsonRequired]` fields are extractable from Core models, (2) check that no `Save*()` call discards its return value in ViewModels, (3) check that no bare integers are used in XAML TimeSpan properties. These are structural invariants not covered by unit tests. Confidence: 100.

#### Process Smoke Test

- `src/avalonia-launcher/verify.ps1` (lines 86-134): On Windows, launches the published exe with a temp config. Waits up to 4 seconds (40 iterations x 100ms) for it to either exit cleanly or show a window handle. This catches boot-time crashes (missing `[JsonRequired]` fields, XAML parse errors) that unit tests miss. On non-Windows, this is skipped. Confidence: 100.
- The `processSmoke` result has 3 possible values: `'started-exited-cleanly'`, `'booted-window-visible'`, `'booted-no-window-yet'`. On CI, GUI smoke is always `'skipped-ci-headless'`. This means CI never validates that the app boots successfully -- it only validates publish succeeds. Confidence: 100.

#### CI publish vs release publish mismatch

- CI `build-and-test` publish step (verify.yml line 42): `dotnet publish src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -r win-x64 --self-contained --no-restore`. This publishes the entire SOLUTION, not just the app project. Publishing the solution produces output for all projects that have `IsPublishable` enabled, which by default only applies to the App project. However, publishing at solution level is broader than needed. Confidence: 85 that this is slightly wasteful but functionally correct.
- This CI publish does NOT use `PublishAot=true`. Only `build-release.ps1` enables Native AOT. Confidence: 100.
- The `full-gate` job delegates to `tools/verify.ps1` which runs `src/avalonia-launcher/verify.ps1`, which publishes to `artifacts/verify-publish/win-x64/` (line 13) -- a DIFFERENT directory from the release publish path at `artifacts/publish/win-x64/`. This isolation prevents gate publish from conflicting with a running release exe. Confidence: 100.

#### `run.ps1` uses dotnet run, not published exe

- `src/avalonia-launcher/run.ps1` (line 27): Runs `dotnet run --project $project --configuration Release -- --config $store`. This runs in development mode, not from published artifacts. It skips the verification that the built output matches what the published exe would do. Confidence: 100.

#### `build-release.ps1` blocks if exe is running

- Lines 21-24: Checks via WMI `Get-CimInstance Win32_Process` if the published exe is currently running. Throws with PID info if found. This prevents the `File.Replace` atomic write from being blocked by a running/locked executable. However, the WMI query uses `$_.ExecutablePath` which requires administrative privileges in some configurations. On systems where WMI restricts access to process paths, this check would fail to detect the running exe. Confidence: 70.

#### `build-release.ps1` uses publish dir from config-lib

- Line 14: `$publishDir = Get-AvaloniaPublishDir` resolves to `src/avalonia-launcher/artifacts/publish/win-x64`. This path is hardcoded in config-lib.ps1 line 64. Confidence: 100.

### Files Found

| File Path | Description |
|---|---|
| `tools/verify.ps1` | Native verification gate (comprehensive checks) |
| `tools/build-release.ps1` | Stable release publish with Native AOT |
| `tools/config-lib.ps1` | Shared helper library for all tools scripts |
| `tools/test-launch-args.ps1` | LaunchArgumentBuilder-focused test runner |
| `src/avalonia-launcher/verify.ps1` | Avalonia mainline verify script (used by root gate) |
| `src/avalonia-launcher/run.ps1` | Development-mode run script |
| `.github/workflows/verify.yml` | CI workflow (build, test, NuGet audit, native gate) |
| `.github/workflows/release.yml` | Does NOT exist (no automated release pipeline) |
| `src/avalonia-launcher/WTLauncher.Avalonia.sln` | Solution file (3 projects: Core, App, Tests) |
| `src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj` | App project (WinExe, net10.0, AOT-compatible) |
| `src/avalonia-launcher/src/WTLauncher.Core/WTLauncher.Core.csproj` | Core library (net10.0, nullable, AOT-compatible) |
| `src/avalonia-launcher/tests/WTLauncher.Tests/WTLauncher.Tests.csproj` | Tests project (xUnit v3, ArchUnitNET) |
| `src/avalonia-launcher/Directory.Build.props` | Shared MSBuild properties |
| `src/avalonia-launcher/Directory.Packages.props` | Centralized package versions |
| `src/avalonia-launcher/src/WTLauncher.App/Program.cs` | App entry point |

### Caveats / Not Found

1. **No `release.yml`**: There is no automated release pipeline. Releases are built manually via `tools/build-release.ps1`. This is documented but is a notable absence for a project with CI infrastructure.

2. **`Fluxor.Blazor.Web` is orphaned**: Version `6.10.0` specified in `Directory.Packages.props:21` but no project references it. This could be removed or was added in anticipation of future use.

3. **CI never validates Native AOT**: The CI `build-and-test` and `full-gate` jobs both publish without `PublishAot=true`. Only `build-release.ps1` enables Native AOT. A regression in AOT compatibility would not be caught until a release build is performed locally.

4. **CI does not validate boot**: The process smoke test in `src/avalonia-launcher/verify.ps1` is skipped in CI (`skipped-ci-headless`). CI validates that the app compiles and publishes, but does NOT validate that it starts without crashing.

5. **`nuget-audit` job name is misleading**: The CI job named "NuGet Security Audit" only runs `dotnet restore`, not an actual audit. The actual audit happens at build time via `NuGetAudit=true` in `Directory.Build.props`.

6. **Subprocess error handling gap in verify.ps1**: The root gate calls `& $mainlineVerify` (line 505) without wrapping in `Invoke-NativeStep`. If the sub-script throws, the parent receives the error, but if the sub-script produces malformed output without throwing, the parent's JSON parsing on line 506 would fail with a confusing error.

7. **Redundant `WarningsAsErrors=nullable`**: Both `Core.csproj:5` and `Tests.csproj:6` set this, but `Directory.Build.props:5` already enables `TreatWarningsAsErrors=true`, making the project-level settings redundant.

8. **No `dotnet restore --locked-mode` check**: The gate restores packages but doesn't use `--locked-mode` to detect lock file drift or unintended package version changes.

9. **No binary size regression tracking**: The gate records SHA256 and byte sizes but has no history/comparison to alert on unexpected size changes.

10. **WMI process detection may fail without admin**: `build-release.ps1:21` uses `Get-CimInstance Win32_Process` and accesses `.ExecutablePath`, which can return null without sufficient privileges, potentially failing to detect a running exe.

11. **Assembly version in CI builds**: CI builds get default `1.0.0.0` version since no version is passed via `-p:Version=` to the publish/test steps. Only `build-release.ps1` generates a version from git tags.

12. **`run.ps1` root detection diverges from config-lib**: `run.ps1` has its own repo root detection logic (walking up looking for `config\launcher.json` + `README.md`), while all other scripts use `config-lib.ps1`'s `Get-RepoRoot`. If the repo root detection needs to change, both paths must be updated.
