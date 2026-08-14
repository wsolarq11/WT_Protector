# Research: Config, Build System & Documentation Audit

- **Query**: Read and review configuration files, build system, project files, and documentation for accuracy and consistency
- **Scope**: Mixed (internal files across config, build system, docs, and source code)
- **Date**: 2026-07-26

## Findings

### CONFIGURATION

#### C1. launcher.json schema correctness
- **File**: `config/launcher.json`
- **Status**: Schema-valid against `LauncherStoreFile` C# record (`src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreFile.cs`)
- **Notes**: Contains `schemaVersion: 2`, `settings`, `entries` (21 entries), `presets` (2), `defaultPresetId: "core-projects"`. All required fields present.
- **Fields beyond documented schema**: `settings.backgroundMode: "solid"`, `settings.cardsGroupKey: "none"`, `window.x`, `window.y`, `window.windowState` are serialized but not documented in README.md. `entry.launchCount` present on all entries but not listed in README.md fields section.

#### C2. .gitignore
- **File**: `.gitignore` (31 lines)
- **Status**: Valid. Covers `.omx/`, `.snow/`, `.agents/`, `*.bak`, `*.log`, `*.tmp`, `artifacts/`, `bin/`, `obj/`, `TestResults/`, `.claude/`, `*.db`, `config/*.legacybak`, `config/*.legacybak2`, `*.lnk`.
- **Possible stale entries**: `config/*.legacybak` and `config/*.legacybak2` patterns -- check if backup rotation still uses `.legacybak` extension or if this is from the original three-file design.

#### C3. .gitattributes
- **File**: `.gitattributes` (23 lines)
- **Status**: LF normalization for all text, binary markers for `.db`, `.exe`, `.dll`, `.ico`, `.png`, `.jpg`, `.jpeg`, `.webp`. Trellis journal `merge=union` rule present.
- **Missing**: No `*.fs` or `*.fsproj` rules for the F# project (`src/avalonia-launcher/src/WTLauncher.Core.FSharp/`). These should also be `text=auto eol=lf` but would inherit from the `* text=auto eol=lf` catch-all rule, so not critical.

#### C4. .editorconfig
- **File**: `.editorconfig` (145 lines)
- **Status**: Comprehensive C# style rules. Root=true, UTF-8, LF. Covers file-scoped namespaces, pattern matching, null checking, naming conventions (I_PascalCase for interfaces, _camelCase for private fields), analyzer severities (CA1051, CA1062, CA1305, CA1707, CA1716, CA1848, CA2007, CA1859, IDE0011, IDE0032, IDE0062).
- **Note**: File references IDE0011, IDE0032, IDE0062 while compliance doc says they are suggestion-level. Consistent.

#### C5. global.json
- **File**: `src/avalonia-launcher/global.json`
- **Status**: SDK version `10.0.100`, `rollForward: "latestMajor"`. Consistent with `net10.0` target framework across all projects.

---

### BUILD SYSTEM

#### B1. WTLauncher.App.csproj
- **File**: `src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj`
- **Target**: `net10.0`, `WinExe`, `win-x64`, `IsAotCompatible: true`, `AvaloniaUseCompiledBindingsByDefault: true`
- **Package refs**: Avalonia 12.1.0, CommunityToolkit.Mvvm 8.4.2, Stateless 5.20.0, DynamicData 9.4.33, OneOf 3.0.271, MSBuild.CompilerCache 0.13.7
- **Project refs**: `WTLauncher.Core.csproj`
- **Warnings suppressed**: CS0618 (LaunchPlan.CommandLine [Obsolete] used for UI display)
- **Notes**: Missing reference to `WTLauncher.Core.FSharp.fsproj` -- if the F# library is intended for use, the App project does not reference it. The F# project exists in the solution but is isolated.

#### B2. WTLauncher.Core.csproj
- **File**: `src/avalonia-launcher/src/WTLauncher.Core/WTLauncher.Core.csproj`
- **Target**: `net10.0`, `IsAotCompatible: true`, `WarningsAsErrors: nullable`
- **Package refs**: ErrorOr 2.1.1, OneOf 3.0.271, Immutype 1.0.16, MSBuild.CompilerCache 0.13.7
- **Status**: Clean, minimal.

#### B3. WTLauncher.Core.FSharp.fsproj
- **File**: `src/avalonia-launcher/src/WTLauncher.Core.FSharp/WTLauncher.Core.FSharp.fsproj`
- **Target**: `net10.0`, `win-x64`, `IsAotCompatible: true`, `Nullable: enable`
- **No package references** at all -- no F#-specific packages (like FSharp.Core). This may cause build failures since the project needs FSharp.Core implicitly.
- **Content**: Single `Library.fs` with data record types (LaunchEntry, LauncherSettings, LauncherPreset, LauncherStoreFile) and `LaunchArgumentBuilder.quoteArgument` function.
- **Status**: In the solution but NOT referenced by any C# project. Effectively isolated -- compiles as a standalone library but not consumed by anything.

#### B4. WTLauncher.Analyzers.csproj
- **File**: `src/avalonia-launcher/src/WTLauncher.Analyzers/WTLauncher.Analyzers.csproj`
- **Target**: `netstandard2.0`, `LangVersion: latest`, `IsRoslynComponent: true`, `EnforceExtendedAnalyzerRules: true`
- **Package refs**: Microsoft.CodeAnalysis.CSharp 4.11.0, Microsoft.CodeAnalysis.Analyzers 3.11.0
- **Status**: Correct for a Roslyn analyzer. Warnings suppressed: RS2008.

#### B5. WTLauncher.Tests.csproj
- **File**: `src/avalonia-launcher/tests/WTLauncher.Tests/WTLauncher.Tests.csproj`
- **Target**: `net10.0`, `IsPackable: false`, `WarningsAsErrors: nullable`
- **Package refs**: Microsoft.NET.Test.Sdk 18.7.0, TngTech.ArchUnitNET 0.13.3, TngTech.ArchUnitNET.xUnitV3 0.13.3, xunit.v3 3.2.2, xunit.runner.visualstudio 3.1.5, coverlet.collector 10.0.1, CsCheck 4.2.0, MSBuild.CompilerCache 0.13.7
- **Project refs**: `WTLauncher.App.csproj`, `WTLauncher.Core.csproj`
- **Warnings suppressed**: CS0618, WT1003
- **Notes**: Missing `Using Include="Xunit"` -- present on line 33. Uses xUnit v3 (not v2).

#### B6. WTLauncher.Avalonia.sln
- **File**: `src/avalonia-launcher/WTLauncher.Avalonia.sln`
- **Projects**: WTLauncher.Core, WTLauncher.App, WTLauncher.Tests, WTLauncher.Analyzers, WTLauncher.Core.FSharp
- **Solution folders**: `src` (Analyzers and Core.FSharp nested under it)
- **Notes**: Core.App and Core are NOT nested under `src` folder in the solution -- only Analyzers and Core.FSharp are.

#### B7. Directory.Build.props
- **File**: `src/avalonia-launcher/Directory.Build.props`
- **Settings**: Nullable enabled, ImplicitUsings enabled, TreatWarningsAsErrors, Deterministic, AnalysisLevel=latest-recommended, EnableNETAnalyzers, EnforceCodeStyleInBuild, NuGetAudit (mode=all)
- **Analyzer reference**: Auto-includes WTLauncher.Analyzers as an Analyzer for all projects except Analyzers itself.
- **Condition**: `RunAnalyzersDuringBuild` set for CI or ForceAnalysis.
- **Status**: Clean.

#### B8. Directory.Packages.props
- **File**: `src/avalonia-launcher/Directory.Packages.props`
- **Central package management**: Enabled.
- **Package versions**: 28 entries listed.
- **Issue**: 4 package versions are defined but never referenced by any `.csproj`:
  - `Microsoft.Extensions.DependencyInjection` 8.0.0
  - `Vogen` 8.0.7
  - `OneOf.SourceGenerator` 3.0.271
  - (Note: OneOf.SourceGenerator may be a transitive dependency of OneOf; if so, pinning the version is intentional.)
- **Version notes**: `Microsoft.NET.Test.Sdk` 18.7.0 -- verify this is correct for xUnit v3 (typically should be 17.x for .NET 10 compatibility, 18.7.0 may not exist).

#### B9. GitHub Actions CI
- **File**: `.github/workflows/verify.yml`
- **Jobs**: Build & Test, NuGet Security Audit, Native AOT Compile (informational, continue-on-error), Full Native Gate (verify.ps1)
- **Cache**: NuGet packages via `actions/cache@v4`
- **Build**: `dotnet build --no-restore -warnaserror`
- **Test**: `dotnet test --no-build` with TRX logger
- **AOT**: `continue-on-error: true` for AOT publish
- **Publish (full-gate)**: Runs `.\tools\verify.ps1` via PowerShell. **Note**: The verify.ps1 is executed from the PowerShell shell, but the working directory context may not be the repo root. The script uses `$PSScriptRoot` to resolve paths, so this should work.

#### B10. build-release.ps1
- **File**: `tools/build-release.ps1`
- **Flow**: Restore solution win-x64, publish app with PublishAot=true, generate receipt (SHA256).
- **Running-check**: Validates the published exe is not currently running via WMI query.
- **Version**: Extracted from `git describe --tags`.
- **Note**: Line 10-11 adds VS Installer to PATH for vswhere (needed by Native AOT C++ compiler). This assumes VS Build Tools are installed with C++ workload.

### INTERNALS VISIBLE TO

- `WTLauncher.Core/LauncherJson.cs:5`: `[assembly: InternalsVisibleTo("WTLauncher.App")]`
- `WTLauncher.Core/LauncherJson.cs:6`: `[assembly: InternalsVisibleTo("WTLauncher.Tests")]`
- `WTLauncher.App/Infrastructure/ErrorLog.cs:4`: `[assembly: InternalsVisibleTo("WTLauncher.Tests")]`

**Correct**: Tests can see internals of both Core and App. App can see Core internals.

---

### DOCUMENTATION

#### D1. README.md
- **File**: `README.md` (294 lines)
- **General**: Comprehensive, well-maintained. Architecture section reflects current state.
- **Schema section (lines 72-136)**: Documents core fields, settings, presets. Good.
- **Undocumented fields in launcher.json**: The following serialized fields present in `LauncherSettings`/`LauncherWindowSettings` C# records are NOT documented in README.md:
  - `backgroundMode` -- present in `LauncherSettings.cs:46`
  - `cardsGroupKey` -- present in `LauncherSettings.cs:28`
  - `window.x`, `window.y`, `window.windowState` -- present in `LauncherWindowSettings.cs:69-81`
  - `launchCount` on entries -- present in `LaunchEntry.cs:50-51`
- **Stale preset example**: Line 95 shows `"entryIds": ["WT_Protector"]` but all actual presets in launcher.json use `"desktop-scripts"`, `"opencode_WT_Protector"` etc. The example is simplified, not necessarily wrong.
- **CLI reference example**: Line 118 shows `"codex": "codex --enable goals --dangerously-bypass-approvals-and-sandbox"` which matches the current template. Correct.

#### D2. DESIGN.md
- **File**: `DESIGN.md` (184 lines)
- **Last refreshed**: 2026-07-23
- **Status**: Well-maintained. Design principles, visual language, component ownership, accessibility, responsive behavior all documented.
- **Discrepancy**: Line 8 mentions `docs/design-benchmark-digest.md` as evidence reviewed, but this file was not checked in the recent commits (it exists but is not in the recent git log). The file still exists on disk but wasn't verified in this audit.

#### D3. AGENTS.md
- **File**: `AGENTS.md` (90 lines)
- **Repo tree (lines 7-36)**: Generally accurate. Maps all major directories.
- **File count claims**: 
  - Line 20: `WTLauncher.Core/` = "24 files" -- NOT VERIFIED
  - Line 21: `WTLauncher.App/ViewModels/` = "28 files" -- NOT VERIFIED
  - Line 22: `WTLauncher.App/Views/` = "13 files" -- NOT VERIFIED
  - Line 23: Tests = "61 test files" -- NOT VERIFIED
- **Tools table (lines 40-47)**: `verify.ps1` check count says "31 items" -- the actual verify.ps1 check count may differ. NOT VERIFIED in this pass.
- **TRELLIS block (lines 69-89)**: Standard Trellis instructions. Correct.

#### D4. retired/README.md (STALE)
- **File**: `retired/README.md`
- **Line 10**: "The active config remains `config/config.json` at the repository root."
- **BUG**: The actual active config is `config/launcher.json`. `config/config.json` does NOT exist. This is a stale documentation error.

#### D5. retired/design/winforms-launcher.md (MINOR STALE)
- **File**: `retired/design/winforms-launcher.md`
- **Lines 19-20**: Mentions `config/config.json` as the preferred config path. This was true during the WinForms era. Since the document is archived (retired), this is understandable but may confuse readers.
- **Line 24**: Documents `confirm` field in the launch contract. This field was removed from the unified store by the "No per-entry confirm" invariant.

#### D6. docs/giants-shoulders/launcher-first-class-patterns.md
- **File**: `docs/giants-shoulders/launcher-first-class-patterns.md`
- **Status**: Historical reference. 12-tool survey. Still accurate as a reference document.
- **Line 72**: Documents the rollback point as dropping `presets` and `defaultPresetId` from the config. This is now the production architecture, so the rollback point paragraph is stale.

#### D7. docs/giants-shoulders/codexswitch-avalonia-native-aot.md
- **File**: `docs/giants-shoulders/codexswitch-avalonia-native-aot.md`
- **Status**: Reference document. Reviewed at commit `6152418fd1de7779010d4786b79060e651649109`. Still valid as reference material.
- **Line 98**: Says "Native AOT should remain a later option, not a day-one requirement. The current mainline first proves normal self-contained win-x64 publish." -- but `tools/build-release.ps1` now passes `-p:PublishAot=true`, and CI has an AOT job (informational). The document's advice is now partially overtaken by events.

#### D8. docs/feature-first-class-upgrade-audit.md
- **File**: `docs/feature-first-class-upgrade-audit.md`
- **Date**: 2026-06-10, reviewed at HEAD `22f57a9`
- **Status**: Detailed audit of 23 features. References outdated git HEAD (`22f57a9`), current HEAD is `4fce44f`. May not reflect recent changes.
- **Line 10**: `config/launcher.json` with 19 entries -- current launcher.json has 21 entries. This is expected drift in an audit document.

#### D9. docs/exemplar-gap-refactor-audit.md
- **File**: `docs/exemplar-gap-refactor-audit.md`
- **Date**: 2026-07-13
- **Status**: Refactor assessment. Concentration hotspots analysis at lines 29-37. CQ-03 mentions `CommandManagementWindow.cs` is 432 lines and constructor spans 32-192. This file has likely been refactored since the audit.

#### D10. src/avalonia-launcher/README.md
- **File**: `src/avalonia-launcher/README.md` (91 lines)
- **Status**: Good. Describes 5-layer architecture correctly. Run/verify instructions accurate.
- **Discrepancy**: Line 66 references `AppStore` as the persistence + rollback layer ("`AppStore.Mutate` handles automatic rollback..."). The actual implementation uses `StoreGrain`, not `AppStore`. The README references `AppStore` which was renamed to `StoreGrain` (or vice versa) -- needs alignment check.
- **Line 68**: References `LauncherStore.Save` which the CLAUDE.md says should not be called directly. Let me verify.

---

### STALE / UNUSED CODE

#### S1. LauncherPresetValidator (DISCONNECTED)
- **File**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherPresetValidator.cs`
- **Line 1**: `// TODO: Wire this into PresetManager.Load after Phase A is complete`
- **Status**: Contains full validation logic for `LauncherPresetFile` but is NOT called from any production code in `WTLauncher.App`. Only referenced in tests (`LauncherPresetWorkflowTests.cs`). The TODO has not been resolved. The `PresetManager.Load` method does NOT call `LauncherPresetValidator.Validate` -- it assigns the preset file directly.

#### S2. dead package versions in Directory.Packages.props
- **File**: `src/avalonia-launcher/Directory.Packages.props`
- **Unreferenced packages**: `Microsoft.Extensions.DependencyInjection` (8.0.0), `Vogen` (8.0.7), `OneOf.SourceGenerator` (3.0.271) are defined as package versions but no `.csproj` references them. If these are transitive dependencies, they should be removed from explicit listing. (OneOf.SourceGenerator may be a transitive dependency of OneOf.)

#### S3. WTLauncher.Core.FSharp (ISOLATED)
- **Project**: `src/avalonia-launcher/src/WTLauncher.Core.FSharp/WTLauncher.Core.FSharp.fsproj`
- **Status**: In the solution, compiles as a standalone library, but no C# project references it. Contains duplicate type definitions mirroring C# records and a `quoteArgument` function ported from C#. No project reference from App or Core.
- **Missing dependency**: No `FSharp.Core` package reference in the fsproj. The project depends on the SDK to provide FSharp.Core implicitly, which works with `Microsoft.NET.Sdk`.

#### S4. LegacyPresetDocument type (STILL REFERENCED)
- **File**: `src/avalonia-launcher/src/WTLauncher.Core/LegacyPresetDocument.cs`
- **Status**: Used by `LegacyMigration.cs` for one-time migration from the three-file config. Registered in `LauncherJsonContext.cs:20` for source-gen. Still active code, not dead -- LegacyMigration is still in the codebase.

---

### DOC COMMENT BUGS

#### X1. LauncherSettings.cs doc comment wrong UnmappedMemberHandling
- **File**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs`, line 11
- **Text**: `/// (UnmappedMemberHandling=Disallow), so reintroducing settings.schemaVersion fails loudly rather than silently drifting.`
- **Reality**: The actual JSON source generator in `LauncherJsonContext.cs` uses `[JsonSourceGenerationOptions]` WITHOUT setting `UnmappedMemberHandling`. The .NET default for source-gen is `JsonUnmappedMemberHandling.Skip` (Loose Read). The comment claims `Disallow` (Strict Read), which contradicts the documented "Loose Read + Strict Write" strategy. Either the comment is wrong, or the source gen options are wrong. The actual behavior (skip unknown keys) matches the Loose Read policy, so the COMMENT is wrong.

#### X2. LauncherSettings.cs doc comment schema reference
- **File**: `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs`, lines 10-12
- **Text**: `launcher.json is validated against the C# record schema at load time (UnmappedMemberHandling=Disallow)`
- **Reality**: At load time, `LauncherStoreFile` is deserialized via source-gen `LauncherJsonContext`, which uses default `JsonUnmappedMemberHandling.Skip`. Unknown keys ARE silently skipped (by design). The comment is contradictory to the documented strategy.

---

### CODE QUALITY NOTES

#### Q1. Infinite loop in ExceptionFilters.IsRecoverable
- **File**: `src/avalonia-launcher/src/WTLauncher.App/ViewModels/ExceptionFilters.cs`, lines 19-20
- **Code**: `var inner = ex; while (inner is TypeInitializationException or AggregateException) inner = inner.InnerException ?? inner;`
- **Bug**: When `InnerException` is null, `?? inner` returns `inner` (itself), so the while-condition remains true forever. This is an infinite loop on any TypeInitializationException or AggregateException with null InnerException.
- **Spec violation**: The quality guideline says "Must have max-iteration guard (for i<16)" but this loop lacks the guard.
- Previously documented in spec-compliance-violations.md as C1.

---

### SUMMARY OF NOTABLE FINDINGS

| ID | Severity | File:Line | Description |
|---|---|---|---|
| C1 | Low | README.md | `backgroundMode`, `cardsGroupKey`, `window.x/y/windowState`, `launchCount` undocumented |
| D4 | **Medium** | **retired/README.md:10** | **Claims active config is `config/config.json` but actual file is `config/launcher.json`** |
| S1 | Low | LauncherPresetValidator.cs:1 | Stale TODO, code not wired into production |
| S2 | Low | Directory.Packages.props | 3 package versions defined but unreferenced |
| S3 | Low | WTLauncher.Core.FSharp.fsproj | F# project in solution but not consumed by any project |
| X1 | Low | LauncherSettings.cs:11 | Doc comment says UnmappedMemberHandling=Disallow but behavior is Skip |
| D10 | Low | src/avalonia-launcher/README.md:66 | References `AppStore` but actual code uses `StoreGrain` |
| B8 | Low | Directory.Packages.props | Microsoft.NET.Test.Sdk 18.7.0 -- verify this version exists |
| Q1 | **High** | **ExceptionFilters.cs:19-20** | **Infinite loop in TypeInitializationException/AggregateException unwrapping** (previously documented) |

### Files Found

| File Path | Description |
|---|---|
| `config/launcher.json` | Active unified store, schema v2, 21 entries |
| `.gitignore` | Covering build output, backup, temp, and local state |
| `.gitattributes` | LF normalization, binary types, merge=union for journals |
| `.editorconfig` | Comprehensive C# style rules |
| `src/avalonia-launcher/global.json` | SDK 10.0.100, rollForward latestMajor |
| `src/avalonia-launcher/WTLauncher.Avalonia.sln` | 5 projects: Core, App, Tests, Analyzers, Core.FSharp |
| `src/avalonia-launcher/Directory.Build.props` | Shared build properties, analyzer injection |
| `src/avalonia-launcher/Directory.Packages.props` | Central package management, 28 versions |
| `src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj` | net10.0, WinExe, AOT-compatible |
| `src/avalonia-launcher/src/WTLauncher.Core/WTLauncher.Core.csproj` | net10.0, AOT-compatible |
| `src/avalonia-launcher/src/WTLauncher.Core.FSharp/WTLauncher.Core.FSharp.fsproj` | net10.0, isolated F# library |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/WTLauncher.Analyzers.csproj` | netstandard2.0, Roslyn analyzer |
| `src/avalonia-launcher/tests/WTLauncher.Tests/WTLauncher.Tests.csproj` | net10.0, xUnit v3 |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreFile.cs` | Schema record (5 fields) |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherSettings.cs` | Settings record (9 fields + window) |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherJsonContext.cs` | Source-gen context with 10 registrations |
| `src/avalonia-launcher/src/WTLauncher.Core/LauncherPresetValidator.cs` | Disconnected validator (TODO stale) |
| `src/avalonia-launcher/src/WTLauncher.Core/LegacyPresetDocument.cs` | Legacy migration type |
| `src/avalonia-launcher/src/WTLauncher.App/ViewModels/ExceptionFilters.cs` | Infinite loop bug in IsRecoverable |
| `README.md` | Main project documentation |
| `DESIGN.md` | Design principles and component ownership |
| `AGENTS.md` | Repo map and navigation |
| `retired/README.md` | Stale reference to config/config.json |
| `retired/design/winforms-launcher.md` | Archived WinForms design lessons |
| `docs/giants-shoulders/launcher-first-class-patterns.md` | 12-tool survey reference |
| `docs/giants-shoulders/codexswitch-avalonia-native-aot.md` | CodexSwitch stack reference |
| `docs/feature-first-class-upgrade-audit.md` | 23-feature audit (dated 2026-06-10) |
| `docs/exemplar-gap-refactor-audit.md` | Refactor assessment (dated 2026-07-13) |
| `src/avalonia-launcher/README.md` | Sub-project README references old AppStore name |
| `.github/workflows/verify.yml` | CI pipeline: build, test, AOT (info), full gate |
| `tools/build-release.ps1` | Release builder with PublishAot=true |

### Related Specs

- `.trellis/spec/guides/pitfalls.md` -- known gotchas
- `.trellis/spec/core/quality-guidelines.md` -- infinite loop guard requirement
- `.trellis/tasks/active/research/spec-compliance-violations.md` -- existing spec violations document (covers C1, M1-M7, L1-L6)

### Caveats / Not Found

- The `Microsoft.NET.Test.Sdk` version `18.7.0` in `Directory.Packages.props` was not verified against NuGet.org availability. It may be a tentative version that doesn't exist yet.
- The `verify.ps1` actual check count (claimed 31 in AGENTS.md) was not verified line-by-line in this pass.
- The file count claims in AGENTS.md (Core: 24 files, ViewModels: 28 files, Views: 13 files, Tests: 61 files) were not independently verified.
- The `docs/design-benchmark-digest.md` file referenced in DESIGN.md was not audited.
