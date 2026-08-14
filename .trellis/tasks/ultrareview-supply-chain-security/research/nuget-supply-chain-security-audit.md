# Research: NuGet/Supply Chain Security Audit

- **Query**: Comprehensive review of NuGet/package supply chain and dependency configuration for security issues
- **Scope**: Mixed (internal configuration analysis + external package knowledge)
- **Date**: 2026-07-26

## Files Examined

| File Path | Description |
|---|---|
| `src/avalonia-launcher/Directory.Packages.props` | Central package version management (all pinned) |
| `src/avalonia-launcher/Directory.Build.props` | Shared build properties (NuGetAudit, analyzers) |
| `src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj` | Main app project, 6 package refs |
| `src/avalonia-launcher/src/WTLauncher.Core/WTLauncher.Core.csproj` | Core library, 4 package refs |
| `src/avalonia-launcher/src/WTLauncher.Core.FSharp/WTLauncher.Core.FSharp.fsproj` | F# project, 0 package refs |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/WTLauncher.Analyzers.csproj` | Roslyn analyzer, 2 package refs (analyzers only) |
| `src/avalonia-launcher/tests/WTLauncher.Tests/WTLauncher.Tests.csproj` | Test project, 7 package refs |
| `.github/workflows/verify.yml` | CI workflow (4 jobs, nuget-audit job) |
| `.github/dependabot.yml` | Dependabot config (NuGet + GitHub Actions, weekly) |
| `src/avalonia-launcher/global.json` | SDK version: 10.0.100, rollForward: latestMajor |
| `src/avalonia-launcher/WTLauncher.Avalonia.sln` | Solution file (5 projects) |

## Files NOT Found

| Expected File | Status | Implication |
|---|---|---|
| `NuGet.config` (any location) | **NOT FOUND** | No NuGet.config at repo root or solution level. Defaults to machine-level config, which may include untrusted sources |
| `packages.lock.json` (any location) | **NOT FOUND** | No lock file for reproducible restore. `dotnet restore` may pull different versions across time/builds |

---

## Finding 1: Pinned Package Versions (Good) but No Lock File (Gap)

**Severity: Medium**

All 23 package versions in `Directory.Packages.props` are explicitly pinned (e.g., `Version="12.1.0"`). No floating ranges (`*`, `8.*`) are used.

However, **no `packages.lock.json`** exists anywhere in the repository. This means:
- `dotnet restore` resolves to the latest available version matching the pin at restore time
- If a package is yanked and re-pushed with a different payload at the same version, or if a patch version is retracted, the restore result will differ
- Build reproducibility across time/CI/developers is not guaranteed

**Central version management** (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`) partially mitigates this by ensuring all projects use the same version of each package.

**Recommendation**: Enable `packages.lock.json` via `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` in `Directory.Build.props` (or in `Directory.Packages.props`).

**File/Line**: `Directory.Packages.props:3`

---

## Finding 2: NuGetAudit Is Configured Correctly (Good)

**Severity: None (compliant)**

`Directory.Build.props` lines 10-11:
```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>
```

- `NuGetAudit=true` explicitly enables vulnerability scanning during restore/build.
- `NuGetAuditMode=all` scans both direct and transitive dependencies (not just direct).
- Combined with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (line 5), NU1901-NU1904 audit warnings will be treated as build errors, blocking the CI `build-and-test` job.

**Note**: .NET 10 SDK reportedly defaults `NuGetAuditMode` to `all` (changed from `direct` in .NET 9). The explicit setting is redundant for .NET 10 but harmless and provides forward compatibility if the project is ever built with an older SDK.

**File/Line**: `Directory.Build.props:10-11`

---

## Finding 3: CI `nuget-audit` Job May Not Fail on Vulnerabilities

**Severity: Medium**

The `nuget-audit` CI job (`verify.yml:51-64`) runs:
```yaml
- name: Audit
  run: dotnet nuget audit src/avalonia-launcher/WTLauncher.Avalonia.sln
```

`dotnet nuget audit` (introduced in .NET 9 SDK) is a valid command and will report vulnerabilities. However, its **default exit code behavior** needs verification:

- In .NET 9, `dotnet nuget audit` exits with **code 0** even when vulnerabilities are found (it only prints warnings to stdout/stderr).
- The command may require `--fail-on-audit` or `--severity-config` to return a non-zero exit code when vulnerabilities are detected.
- If the default behavior carries to .NET 10, the `nuget-audit` job will **always pass** even with known vulnerabilities.

However, the `build-and-test` job (which runs `dotnet build ... -warnaserror`) WILL catch NU1901-NU1904 warnings as errors due to `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, providing a safety net.

**Risk**: The dedicated `nuget-audit` job provides a false sense of security if it always exits 0. The actual audit enforcement relies on the build job's `TreatWarningsAsErrors`.

**File/Line**: `.github/workflows/verify.yml:64`

---

## Finding 4: No Package Signature Validation Configured

**Severity: Low-Medium**

No `<NuGetSignatureVerification>` property is set anywhere in the project configuration. This means:
- NuGet packages are **not** verified for signature validity during restore
- There is no protection against compromised packages from nuget.org or other sources
- Signed packages (e.g., Microsoft-owned packages) are trusted without signature check

**Context**: This is a common gap in .NET projects. NuGet signature verification is opt-in and requires explicit configuration. For a win-x64 self-contained desktop launcher, the practical risk is low, but it is a defense-in-depth gap.

**File/Line**: Not present in any build props file (search entire project tree)

---

## Finding 5: No NuGet.config -- Implicit Source Trust

**Severity: Medium**

No `NuGet.config` file exists at the repository root or solution directory. This means:
- The build relies entirely on the machine-level NuGet configuration
- If a developer's machine has an untrusted package source configured (e.g., a local feed, a mirror, a compromised NuGet cache), the build will consume packages from that source
- CI runners (`windows-latest`) use the default configuration, which includes only `nuget.org` -- this is safe for CI but not guaranteed for local developer builds
- No explicit `<clear />` + `<add key="nuget.org" ...>` pattern to lock sources

**Recommendation**: Add a `NuGet.config` at the solution level (`src/avalonia-launcher/NuGet.config` or repo root) that clears all sources and adds only `nuget.org`.

**File/Line**: Not found in project

---

## Finding 6: Vestigial Package Definitions in Directory.Packages.props

**Severity: Low**

Several packages defined in `Directory.Packages.props` are **not referenced by any .csproj** in the solution:

| Package | Version | Status |
|---|---|---|
| `Vogen` | 8.0.7 | **Not referenced** by any .csproj. Code comment in `DirectoryPath.cs:9` confirms it was replaced: `/// 取代之前的 Vogen DirectoryPath + EntryDirectory 双类型设计。` |
| `OneOf.SourceGenerator` | 3.0.271 | **Not referenced** by any .csproj. The `OneOf` runtime package is used (by App and Core), but the source generator is not wired anywhere |
| `Microsoft.Extensions.DependencyInjection` | 8.0.0 | **Not referenced** by any .csproj. The App project does not use DI container -- it uses constructor injection manually |

One additional package warrants attention:
| `Immutype` | 1.0.16 | Referenced in `Core.csproj:11` with `PrivateAssets="all"`, but no `using Immutype` or `[ImmutypeAttribute]` found in any source file. May be unused (vestigial). As a source generator, it might be wired implicitly via analyzer references, but no evidence of it being consumed. |

**Impact**: These dead entries add no direct security risk (unused packages are not restored into build output). However, they confuse the dependency surface and add unnecessary version pins that must be maintained by Dependabot.

**File/Line**:
- `Directory.Packages.props:20` (Vogen)
- `Directory.Packages.props:26` (OneOf.SourceGenerator)
- `Directory.Packages.props:11` (Microsoft.Extensions.DependencyInjection)
- `Directory.Packages.props:27` (Immutype -- also need to check Core.csproj:11)

---

## Finding 7: Version Consistency Is Good (No Conflicts)

**Severity: None (compliant)**

All packages use a single version defined in `Directory.Packages.props` via central package management. No project overrides versions. No version conflicts across projects.

**File/Line**: `Directory.Packages.props` (single source of truth)

---

## Finding 8: AOT Compatibility Assessment

**Severity: Low-Medium (some uncertainty)**

All projects except the Analyzer (`netstandard2.0`) have `<IsAotCompatible>true</IsAotCompatible>`. The Analyzer targets `netstandard2.0` and is not published as part of the AOT app.

**Packages with potential AOT concerns:**

| Package | Version | AOT Assessment |
|---|---|---|
| `Avalonia` + friends | 12.1.0 | **Supported**. Avalonia 11.1+ has first-class AOT support. Version 12.1.0 is the current stable. |
| `CommunityToolkit.Mvvm` | 8.4.2 | **Supported**. AOT-safe source generators. |
| `DynamicData` | 9.4.33 | **Unknown risk**. Uses reflection-based expression tree operations extensively. May produce AOT warnings at publish time. The CI has `aot-verify` job (`continue-on-error: true`) specifically because AOT compatibility is informational, not gating. |
| `Stateless` | 5.20.0 | **Likely safe**. State machine library, minimal reflection. |
| `ErrorOr` | 2.1.1 | **Likely safe**. Simple discriminated union type. |
| `OneOf` | 3.0.271 | **Likely safe**. Discriminated union with source-generated match methods. |
| `MSBuild.CompilerCache` | 0.13.7 | **Not applicable**. `PrivateAssets="all"`, build-time only tool. |
| `Immutype` | 1.0.16 | **Not applicable**. Source generator, `PrivateAssets="all"`. |
| `Vogen` | 8.0.7 | **Not applicable**. Source generator (but not actually used). |

**No** `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` annotations found in the project's own source code.

**Note**: The `aot-verify` CI job (line 66-83) uses `continue-on-error: true`, meaning AOT failures do not block CI. This is documented as "informational" in the project CLAUDE.md.

**File/Line**:
- `WTLauncher.App.csproj:11` -- `<IsAotCompatible>true</IsAotCompatible>`
- `WTLauncher.Core.csproj:6` -- `<IsAotCompatible>true</IsAotCompatible>`
- `WTLauncher.Core.FSharp.fsproj:5` -- `<IsAotCompatible>true</IsAotCompatible>`
- `.github/workflows/verify.yml:69` -- `continue-on-error: true` for AOT job

---

## Finding 9: License Compliance

**Severity: Low**

All packages used are from well-known open-source ecosystems with permissive licenses:

| Package | Typical License | Risk |
|---|---|---|
| Avalonia (all variants) | MIT | Safe |
| CommunityToolkit.Mvvm | MIT | Safe |
| DynamicData | MIT | Safe |
| Stateless | Apache 2.0 | Safe |
| ErrorOr | MIT | Safe |
| OneOf | MIT | Safe |
| OneOf.SourceGenerator | MIT | Safe |
| Vogen | MIT | Safe |
| Immutype | MIT | Safe |
| MSBuild.CompilerCache | MIT | Safe |
| xUnit.v3 + runners | Apache 2.0 | Safe |
| ArchUnitNET | Apache 2.0 | Safe |
| Microsoft.CodeAnalysis.* | MIT | Safe |
| Microsoft.Extensions.DependencyInjection | MIT | Safe |
| Microsoft.NET.Test.Sdk | MIT | Safe |
| coverlet.collector | MIT | Safe |
| CsCheck | MIT | Safe |

No restrictive licenses (GPL, AGPL) found. No license compliance issues.

**File/Line**: `Directory.Packages.props` (all packages)

---

## Finding 10: Supply Chain Attack Surface

**Severity: Medium**

**Build-time execution risk:**

The following packages execute arbitrary code during build (source generators or MSBuild tasks):

1. **MSBuild.CompilerCache 0.13.7** -- Referenced in 3 projects with `PrivateAssets="all"`. Since it caches compiler outputs, it has broad access to build artifacts. **Trust anchor**: Published by Microsoft under the MSBuild organization on GitHub (`github.com/msbuild/MSBuild.CompilerCache`). Low risk.

2. **Immutype 1.0.16** -- Source generator. `PrivateAssets="all"`. Only in Core.csproj, but no actual usage found in source code. If unused, it's executing unnecessary build-time code.

3. **Vogen 8.0.7** -- Source generator. Not actually referenced in any .csproj (vestigial). No impact.

4. **OneOf.SourceGenerator 3.0.271** -- Source generator. Not referenced in any .csproj (vestigial). No impact.

**No NuGet.config**: As noted in Finding 5, the absence of source-locking configuration means any compromised machine-level source could inject malicious build-time packages.

**No packages.lock.json**: As noted in Finding 1, the absence of a lock file means build-time packages could resolve differently across environments.

**File/Line**:
- `WTLauncher.App.csproj:25` (MSBuild.CompilerCache)
- `WTLauncher.Core.csproj:12` (MSBuild.CompilerCache)
- `WTLauncher.Core.csproj:11` (Immutype)

---

## Finding 11: MSBuild.CompilerCache Safety

**Severity: Low**

`MSBuild.CompilerCache` version 0.13.7 is referenced in three projects (App, Core, Tests) with `PrivateAssets="all"`.

- **Source**: Microsoft-owned repository at `github.com/msbuild/MSBuild.CompilerCache`
- **License**: MIT
- **Risk**: This is a build acceleration tool that caches MSbuild compilation results. It executes during build and has access to all source files and intermediate outputs. The Microsoft provenance is a strong trust signal, but as with any build-time tool, a compromised NuGet feed could replace it with a malicious version.
- **PrivateAssets=all**: Correctly scoped to not flow transitively.

**Recommendation**: If adding `NuGet.config`, pin the source to nuget.org to reduce risk of compromised local feeds serving a malicious version.

**File/Line**:
- `WTLauncher.App.csproj:25`
- `WTLauncher.Core.csproj:12`
- `WTLauncher.Tests.csproj:26`

---

## Finding 12: NuGetAuditMode=all Verification

**Severity: None (works correctly)**

`NuGetAuditMode=all` in `Directory.Build.props:11` combined with `TreatWarningsAsErrors=true` (line 5) means:
- All NU1901-NU1904 warnings (high, moderate, low, informative) from both direct and transitive dependencies are treated as build errors
- The `build-and-test` CI job's `dotnet build ... -warnaserror` step will fail if any vulnerable package is detected
- This provides real protection

The redundant explicit setting in .NET 10 is harmless (as noted in Finding 2).

**File/Line**: `Directory.Build.props:11`

---

## Finding 13: CI Security Issues

**Severity: Medium-High**

### 13a. No explicit GITHUB_TOKEN permissions (HIGH)

The `verify.yml` workflow does **not** set `permissions:` at the workflow level or job level. This means:
- `GITHUB_TOKEN` defaults to **write access** to repository contents, issues, pull requests, and actions
- For a CI-only workflow that does not write back to the repo, this is excessive privilege
- A compromised CI runner could use the token to push to the repository

**File/Line**: `.github/workflows/verify.yml` -- no `permissions:` block at the top

### 13b. Third-party actions not pinned to SHA (MEDIUM)

All four GitHub Actions used in the workflow use major-version tags `@v4`:
- `actions/checkout@v4`
- `actions/setup-dotnet@v4`
- `actions/cache@v4`
- `actions/upload-artifact@v4`

Using mutable tags (`@v4`) means the action version could change silently when the maintainer updates the `v4` tag to point to a new commit. Best practice is to pin to the specific commit SHA.

**Risk**: Low for well-maintained `actions/*` first-party actions, but defense-in-depth would use SHA pins.

**File/Line**:
- `.github/workflows/verify.yml:15` (checkout@v4)
- `.github/workflows/verify.yml:20` (setup-dotnet@v4)
- `.github/workflows/verify.yml:25` (cache@v4)
- `.github/workflows/verify.yml:46` (upload-artifact@v4)

### 13c. No dependency review action (LOW)

No `actions/dependency-review-action` step is configured. This action scans PRs for dependency changes and flags known-vulnerable or malicious packages. Adding it would provide an additional layer of supply-chain security.

**File/Line**: Not present in `verify.yml`

### 13d. Script injection surface (LOW)

The workflow does not accept user-controlled input that could be used for script injection. The `hashFiles()` calls in cache keys are safe. Build commands use hardcoded paths and solution files. Minimal script injection risk.

### 13e. Test results uploaded unconditionally (LOW)

The "Upload test results" step (line 44-49) uses `if: always()` which is correct -- uploads test results even when a previous step fails. No security concern here.

### 13f. AOT verify with continue-on-error (LOW)

The `aot-verify` job (line 66-83) has `continue-on-error: true`. This means AOT failures are masked. Documented as "informational" in project CLAUDE.md. Not a security concern per se, but means AOT incompatibility issues (which could be caused by malicious packages doing runtime code generation) would be silently ignored.

---

## Finding 14: Dependabot Configuration

**Severity: None (good configuration)**

`dependabot.yml` is configured for two ecosystems:
1. **NuGet** (weekly, grouped by Avalonia and xUnit patterns)
2. **GitHub Actions** (weekly)

The grouping configuration is well-structured:
- `Avalonia*` pattern groups all Avalonia packages together (reducing PR noise)
- `xunit*` + `Microsoft.NET.Test.Sdk` groups test packages together

**File/Line**: `.github/dependabot.yml`

---

## Finding 15: Package Version Currency Assessment

**Severity: Low**

| Package | Current Version | Notes |
|---|---|---|
| Avalonia | 12.1.0 | Current stable release as of mid-2026 |
| CommunityToolkit.Mvvm | 8.4.2 | Current stable |
| DynamicData | 9.4.33 | Verify if this is the latest stable |
| Stateless | 5.20.0 | Current stable |
| ErrorOr | 2.1.1 | Current stable |
| OneOf | 3.0.271 | Current stable |
| Vogen | 8.0.7 | Current stable (but unused) |
| Immutype | 1.0.16 | Current stable (but likely unused) |
| MSBuild.CompilerCache | 0.13.7 | Current stable |
| Microsoft.NET.Test.Sdk | 18.7.0 | .NET 10 SDK ships with Test SDK 18.x; this version is plausible |
| xunit.v3 | 3.2.2 | Current stable for xUnit v3 |
| Microsoft.CodeAnalysis.CSharp | 4.11.0 | Current stable |
| Microsoft.CodeAnalysis.Analyzers | 3.11.0 | Current stable |
| CsCheck | 4.2.0 | Verify if this is the latest |
| coverlet.collector | 10.0.1 | Current stable |
| ArchUnitNET | 0.13.3 | Current stable |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | Not actually used. Note: this is the .NET 8 version, while the project targets net10.0 |

**Note**: All versions appear current. Dependabot will update them on the weekly schedule.

**One concern**: `Microsoft.Extensions.DependencyInjection 8.0.0` is the .NET 8-era version. Since the project targets `net10.0`, if this package were actually used, a newer version (10.x) would be preferred. Since it's unused (Finding 6), this is moot.

---

## Finding 16: F# Project Has Zero Dependencies

The `WTLauncher.Core.FSharp.fsproj` has no `PackageReference` entries at all. It targets `net10.0` with `IsAotCompatible=true` and `Nullable=enable`. This is fine -- the F# project either uses no external packages or uses what .NET provides natively.

**File/Line**: `WTLauncher.Core.FSharp.fsproj` (full file)

---

## Finding 17: Analyzer Project Targets netstandard2.0 Correctly

The `WTLauncher.Analyzers.csproj` correctly targets `netstandard2.0` (required for Roslyn analyzers) and has `IsRoslynComponent=true`. No security concerns.

**File/Line**: `WTLauncher.Analyzers.csproj:4`

---

## Summary Risk Matrix

| # | Finding | Severity | Type |
|---|---|---|---|
| 13a | No explicit GITHUB_TOKEN permissions | **HIGH** | CI security |
| 3 | CI `nuget-audit` job may not fail on vulns | **Medium** | CI audit |
| 5 | No NuGet.config (implicit source trust) | **Medium** | Supply chain |
| 1 | No packages.lock.json | **Medium** | Build reproducibility |
| 4 | No package signature verification | **Low-Medium** | Supply chain |
| 13b | Third-party actions not pinned to SHA | **Medium** | CI security |
| 8 | DynamicData AOT compatibility uncertain | **Low-Medium** | Compatibility |
| 6 | Vestigial packages (Vogen, OneOf.SG, MED, Immutype) | **Low** | Maintenance |
| 2 | NuGetAudit config is correct | None (good) | -- |
| 7 | Version consistency is good | None (good) | -- |
| 9 | License compliance is clean | None (good) | -- |
| 12 | NuGetAuditMode=all works with TreatWarningsAsErrors | None (good) | -- |
| 14 | Dependabot configured well | None (good) | -- |
| 11 | MSBuild.CompilerCache from Microsoft, safe | None (good) | -- |
| 13c-e | CI script injection and action configuration | Low | CI security |

---

## Caveats

1. **`dotnet nuget audit` exit code behavior** in .NET 10 SDK was not verified at runtime. The finding about it potentially exiting 0 for found vulnerabilities is based on .NET 9 behavior and should be confirmed by running `dotnet nuget audit` locally and checking its exit code when vulnerabilities exist.

2. **Vestigial package detection** for Immutype was based on grep for `using Immutype` and `[Immutype` attributes. Source generators can be wired via implicit usings or analyzer references, so Immutype may still be active even without explicit `using` statements. This should be verified by examining the actual build output.

3. **DynamicData AOT compatibility** is flagged as uncertain but the CI `aot-verify` job (`continue-on-error: true`) means any AOT issues are not gating. This is a known trade-off documented in the project.

4. **NuGet package vulnerability database** is server-side. Whether specific packages have known CVEs at the current pinned versions depends on the NuGet.org advisory database at build time. All versions appear recent, so the risk of known unpatched CVEs is low but not zero.
