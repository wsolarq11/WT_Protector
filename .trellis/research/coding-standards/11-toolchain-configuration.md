# Research: Toolchain Configuration

- **Query**: Review .editorconfig, .gitignore, .gitattributes, Directory.Build.props, verify.yml
- **Scope**: Internal
- **Date**: 2026-07-26

## Findings

### .editorconfig

**File:** `.editorconfig`

Key settings:
- `root = true` — correct, prevents cascading
- UTF-8, LF line endings, space indentation — correctly set
- `.cs` rules: file-scoped namespaces (warning), braces preferred (warning), var-for-built-in-types (suggestion), pattern matching (warning), null checking (warning)
- Modifier order: `public, private, protected, internal, file, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, required, volatile, async` (warning)
- Braces: Allman-style (opening brace on new line)
- Naming rules: Interface `I` prefix (warning), private `_camelCase` (warning), const PascalCase (warning), public PascalCase (warning), parameter camelCase (suggestion)
- Analyzer severities: CA1051 (warning), CA1062 (none — manual), CA1305 (none), CA1707 (none — test naming), CA1716 (none — false positives), CA1848 (none), CA2007 (none — Avalonia)
- IDE0011 (braces): suggestion — historical mixed style
- IDE0032 (auto-property): suggestion — intentional for LaunchPlan
- IDE0062 (static local): suggestion
- CA1859 (prefer concrete types): suggestion

**Assessment:** The .editorconfig is comprehensive and well-documented. All rule overrides have inline comments explaining the rationale.

### .gitignore

**File:** `.gitignore`

Items covered:
- `.omx/`, `.snow/`, `.agents/` — local orchestration
- `*.bak`, `*.bak.*`, `*.bak-*` — local backups
- `*.log`, `*.tmp` — logs and scratch artifacts
- `artifacts/`, `bin/`, `obj/`, `TestResults/` — build outputs
- `omx_wiki/session-log-*.md`, `*.db` — session logs
- `.claude/` — Claude local config
- `config/*.legacybak`, `config/*.legacybak2` — legacy backups
- `*.lnk` — Windows shortcuts

**Assessment:** Complete. Ensures local/runtime state is not committed.

### .gitattributes

**File:** `.gitattributes`

- `* text=auto eol=lf` — normalizes all text to LF. Correct.
- Binary files (`.db`, `.exe`, `.dll`, `.ico`, `.png`, `.jpg`, `.jpeg`, `.webp`) marked as `binary` — correct.
- Trellis journals use `merge=union` for clean parallel-session merging — documented.

**Assessment:** Correct and complete.

### Directory.Build.props

**File:** `src/avalonia-launcher/Directory.Build.props`

Settings:
- `<Nullable>enable</Nullable>` — Correct
- `<ImplicitUsings>enable</ImplicitUsings>` — Correct
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — Strict enforcement
- `<Deterministic>true</Deterministic>` — Reproducible builds
- `<AnalysisLevel>latest-recommended</AnalysisLevel>` — Latest analyzers
- `<EnableNETAnalyzers>true</EnableNETAnalyzers>` — Enables built-in analyzers
- `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` — Enforces .editorconfig rules at build time
- `<NuGetAudit>true</NuGetAudit>`, `<NuGetAuditMode>all</NuGetAuditMode>` — NuGet security audit
- `<RunAnalyzersDuringBuild>` — Conditional (CI or explicit ForceAnalysis)

Custom analyzers: References `WTLauncher.Analyzers` (4 custom Roslyn analyzers: WT1001-WT1006 for architecture enforcement).

**Assessment:** Correct and comprehensive. Enforces all code style rules at build time.

### verify.yml (CI)

**File:** `.github/workflows/verify.yml`

Jobs:
1. **build-and-test** (windows-latest, .NET 10.0.x):
   - Checkout, Setup .NET, Cache NuGet
   - `dotnet restore`, `dotnet build --no-restore -warnaserror`, `dotnet test --no-build`
   - `dotnet publish` (smoke check)
   - Upload test results

2. **nuget-audit** (separate job):
   - `dotnet nuget audit`

3. **aot-verify** (continue-on-error):
   - `dotnet publish -p:PublishAot=true`

4. **full-gate** (depends on build-and-test, runs on PR/main):
   - `.\tools\verify.ps1`

**Assessment:** Complete CI pipeline. Note: `full-gate` has `if` condition that runs on `push` events for main/master and on `pull_request` events. For push events on other branches, `build-and-test` runs but `full-gate` does not (because the `needs` condition requires it, and `build-and-test` completes for push events).

## Summary

All toolchain configuration files are correct and consistent. No violations found.
