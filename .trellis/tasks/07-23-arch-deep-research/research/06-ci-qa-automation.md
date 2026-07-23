# CI/QA Automation for .NET 10 Avalonia Desktop Apps

**Task:** 07-23-arch-deep-research
**Date:** 2026-07-23
**Scope:** Native AOT validation, dependency scanning, mutation testing, gate hardening, Roslyn analyzers, code style enforcement, snapshot testing

---

## Table of Contents

1. [Native AOT Validation](#1-native-aot-validation)
2. [Dependency Scanning / SCA](#2-dependency-scanning--sca)
3. [Mutation Testing](#3-mutation-testing)
4. [Gate Hardening](#4-gate-hardening)
5. [Roslyn Custom Analyzers](#5-roslyn-custom-analyzers)
6. [dotnet-format / Style Enforcement](#6-dotnet-format--style-enforcement)
7. [Snapshot / Approval Testing](#7-snapshot--approval-testing)
8. [Current Gaps Summary](#8-current-gaps-summary)
9. [Recommended CI Pipeline Additions](#9-recommended-ci-pipeline-additions)

---

## 1. Native AOT Validation

### Overview

.NET 10 fully supports Native AOT (PublishAot=true) for win-x64. Avalonia 11+ also supports AOT, with caveats around reflection-based bindings and trimming. The current CI publishes `--self-contained` but does not enable AOT.

### How to Publish AOT in CI

```yaml
- name: Publish Native AOT
  run: >
    dotnet publish src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj
    -c Release -r win-x64 --self-contained
    -p:PublishAot=true
    -p:StripSymbols=true
```

**Prerequisite:** The `windows-latest` runner includes MSVC build tools needed by the native linker. If the runner lacks them, install via:

```yaml
- name: Install MSVC build tools
  run: |
    choco install visualstudio2022-buildtools --package-parameters "--add Microsoft.VisualStudio.Component.VC.Tools.x86.x64"
```

### Verifying AOT Output

A successful AOT publish produces a native EXE with no managed DLL dependencies (except the app DLL itself for single-file opt-out). Verification strategies:

**Option A -- File existence + EXE is native (fast, CI-safe):**
```yaml
- name: Verify AOT output
  shell: pwsh
  run: |
    $exe = Get-Item "src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe"
    if (-not $exe) { throw "AOT publish did not produce an EXE" }
    if ($exe.Length -lt 5MB) { throw "AOT EXE suspiciously small (<5MB)" }
```

**Option B -- Check no JIT fallback (requires running the binary):**
- Run `dumpbin /headers` or `corflags` to confirm the PE is native (no IL-only section).
- Run the binary with `--version` or equivalent and capture exit code.
- On Windows, use `[System.Diagnostics.FileVersionInfo]::GetVersionInfo()` in PowerShell to confirm the file is a PE32+ native image.

**Option C -- Verify no managed DLLs alongside the EXE:**
```powershell
$dlls = Get-ChildItem "publish/" -Filter "*.dll" | Where-Object { $_.Name -ne "WTLauncher.App.dll" }
if ($dlls.Count -gt 0) { throw "AOT publish produced unexpected managed DLLs" }
```

### Known Avalonia AOT Issues

| Issue | Impact | Mitigation |
|-------|--------|------------|
| COM Interop unsupported in Native AOT (`dotnet/runtime#75060`) | Breaks if Avalonia internals use COM | Avalonia 11+ avoids COM on Windows; test with AOT before committing |
| Reflection-based view locator warns at publish time | Views may not load at runtime | Ensure all views have public parameterless constructors; use compiled bindings |
| IL Trimmer removes types used only via reflection | `DataContext` bindings fail | Add `[DynamicDependency]` or `<TrimmerRootAssembly>` in csproj |
| XAML resources unreachable via `avares://` | Missing styles/controls | Use `AvaloniaResource` attribute + explicit `GetResource` calls |
| `WinExe` output type breaks AOT on .NET 10 (reported) | Publish fails | Fall back to `Exe` output type if `WinExe` fails |

### Current Gap

**No AOT publish step exists in CI at all.** The existing publish step uses `--self-contained` with JIT. Adding an AOT build would catch trimming issues before merge.

### Recommendation

Add a `native-aot` job (separate from `build-and-test`, can run in parallel) that:
1. Installs MSVC build tools if needed
2. Publishes with `PublishAot=true`
3. Verifies the output is a native PE (file size + corflags check)
4. Does NOT gate the full pipeline initially -- start as informational, graduate to required once stable

---

## 2. Dependency Scanning / SCA

### Overview

.NET 10 changes the default `NuGetAuditMode` from `direct` to `all`, meaning `dotnet restore` already scans both direct and transitive dependencies for known vulnerabilities. The CI currently has a `nuget-audit` job that runs `dotnet restore` but does not explicitly check or fail on vulnerabilities.

### Built-in NuGet Audit (.NET 10)

In .NET 10 (SDK 10.0.100+), when targeting `net10.0`:
- `NuGetAuditMode` defaults to `all` (transitive dependencies included)
- `NuGetAudit` defaults to `true`
- Vulnerabilities are reported as warnings during restore

**To make vulnerabilities fail the build:**
```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditMode>all</NuGetAuditMode>
  <WarningsAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsAsErrors>
</PropertyGroup>
```

The NU190x warning codes map to severity levels:
- NU1901: Low
- NU1902: Moderate
- NU1903: High
- NU1904: Critical

### Explicit Vulnerability Check (dotnet list package)

```yaml
- name: Check for vulnerable packages
  run: |
    dotnet list package --vulnerable --include-transitive
    # Parse output for "has no vulnerable packages" vs vulnerabilities found
```

This command returns exit code 0 regardless. To fail the build, you must parse the output or use the `WarningsAsErrors` approach above.

### Dependabot

GitHub-native Dependabot scans NuGet dependencies and opens PRs for vulnerable or outdated packages. To enable:

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/src/avalonia-launcher"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
    groups:
      avalonia:
        patterns:
          - "Avalonia*"
      microsoft:
        patterns:
          - "Microsoft.*"
```

**Note:** Dependabot can time out on large NuGet projects (>55 min). Setting `open-pull-requests-limit` and using groups reduces the scan surface.

### GitHub Advisory Database + CodeQL

Enable CodeQL analysis in the repo settings or via workflow:
```yaml
- name: Initialize CodeQL
  uses: github/codeql-action/init@v3
  with:
    languages: csharp
- name: Autobuild
  uses: github/codeql-action/autobuild@v3
- name: Perform CodeQL analysis
  uses: github/codeql-action/analyze@v3
```

### Current Gap

The `nuget-audit` job runs `dotnet restore` but does not:
- Treat vulnerabilities as errors (no `WarningsAsErrors` for NU190x)
- Run `dotnet list package --vulnerable`
- Parse the restore output for vulnerability warnings
- Have Dependabot configured
- Run CodeQL analysis

### Recommendation

1. Add `WarningsAsErrors` for NU1901-NU1904 in `Directory.Build.props`
2. Replace the empty `nuget-audit` job with one that runs `dotnet list package --vulnerable --include-transitive` and checks for zero vulnerabilities
3. Create `.github/dependabot.yml` with weekly NuGet scanning
4. Add a CodeQL analysis workflow

---

## 3. Mutation Testing

### Overview

Mutation testing evaluates test quality by inserting small faults ("mutants") into source code and checking whether tests catch them. Stryker.NET is the standard tool for .NET.

### Stryker.NET

**Status:** Stryker.NET v4.16.0 is the latest. Community reports compatibility issues with .NET 9/10 -- Stryker may fail to analyze projects targeting `net10.0` (similar to the `.NET 9 -> Analyzing 0 projects` bug `stryker-net#3367`).

**Integration:**
```bash
# Install as local tool
dotnet tool install dotnet-stryker --tool-path .tools

# Run in test project directory
cd src/avalonia-launcher/tests/WTLauncher.Tests
dotnet stryker --config-file stryker-config.json
```

**Sample config (`stryker-config.json`):**
```json
{
  "stryker-config": {
    "solution": "../../WTLauncher.Avalonia.sln",
    "project": "src/WTLauncher.Core/WTLauncher.Core.csproj",
    "target-project": "tests/WTLauncher.Tests/WTLauncher.Tests.csproj",
    "mutation-level": "Complete",
    "thresholds": {
      "high": 80,
      "low": 60,
      "break": 0
    },
    "reporters": ["html", "progress", "dashboard"],
    "since": {
      "target": "main",
      "enabled": true
    },
    "excluded-mutations": ["string", "regex"]
  }
}
```

**Key considerations:**
- Use `"since"` mode to only mutate changed code on PRs (speeds up from hours to minutes)
- Exclude noise mutations (string literals, regex patterns often produce meaningless survivors)
- The `"break"` threshold causes CI failure; start at 0 (informational) and raise after baseline

**GitHub Action:**
```yaml
- name: Mutation testing
  uses: stryker-mutator/dotnet-stryker-action@v1
  with:
    configurationFile: stryker-config.json
```

### Performance with 700+ tests

Full Stryker run on a 700-test suite takes 20-60 minutes depending on project size. Mitigations:
- Use `"since": {"target": "main"}` to only test changed files in PRs
- Target only `WTLauncher.Core` (the most logic-dense layer) rather than the full solution
- Run nightly as a scheduled workflow rather than on every push

### Mutation Score Targets

| Phase | Threshold | Rationale |
|-------|-----------|-----------|
| Initial baseline | Informational only | Establish current score (~60-70% expected for a well-tested project) |
| Acceptable | >60% | Most core logic covered |
| Good | >75% | Edge cases and branching covered |
| Excellent | >85% | Rare for UI apps; likely over-investment for a launcher |

### Current Gap

**No mutation testing exists.** Code coverage is measured but mutation score is unknown. The ArchUnitNET tests test layer boundaries but not mutant survival.

### Recommendation

1. Add Stryker.NET as a local tool reference in the test project
2. Create `stryker-config.json` with `"since"` mode for PRs
3. Add a scheduled (nightly) workflow that runs full mutation testing and publishes an HTML report
4. Gate PRs on mutation score only after a stable baseline is established (start as informational)

---

## 4. Gate Hardening

### Overview

The current `verify.ps1` gate is comprehensive (25+ checks) but has coverage gaps. The PowerShell regex approach to code scanning is fragile compared to Roslyn-based analysis.

### Missing Patterns

#### 4.1 Bare `catch { }` (without even `(Exception)`)

Current regex catches `catch\s*\(\s*Exception\b` but misses bare `catch { }` blocks. A bare `catch` swallows everything including `StackOverflowException`, `AccessViolationException`, etc.

**PowerShell fix for verify.ps1:**
```powershell
# Add alongside the existing bare-catch check
$bareCatchOnlyViolations = @()
foreach ($file in $csFilesForCatchCheck) {
    $lineNum = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName -Encoding UTF8) {
        $lineNum++
        # Match 'catch' followed by whitespace and '{' but NOT '(' (typed catch)
        if ($line -match '\bcatch\s*\{') {
            $bareCatchOnlyViolations += "$($file.FullName.Substring($root.Length)):$lineNum"
        }
    }
}
```

#### 4.2 async void Methods

`async void` methods are dangerous because exceptions cannot be caught by the caller. They should only be used for event handlers.

**PowerShell check:**
```powershell
$asyncVoidViolations = @()
foreach ($file in $csFilesForCatchCheck) {
    $content = Get-Content -Raw -LiteralPath $file.FullName -Encoding UTF8
    # Match 'async void' method declarations (not event handlers)
    # This regex is approximate; Roslyn-based analysis is more reliable
    if ($content -match '(?<!event\s+)\basync\s+void\b') {
        $asyncVoidViolations += $file.Name
    }
}
```

**Note:** PowerShell regex cannot reliably distinguish event handlers from non-event-handler `async void` methods. A Roslyn analyzer (see Section 5) is the correct solution. The PowerShell gate should only catch the most obvious violations.

#### 4.3 Missing `Directory.Build.props` for Consistent Warnings

The project does not have a `Directory.Build.props` that enforces:
- `TreatWarningsAsErrors` (currently passed via CLI `-warnaserror`)
- `NuGetAudit` settings
- `EnforceCodeStyleInBuild` = true
- `AnalysisLevel` = latest

#### 4.4 Dead Code Detection

No mechanism prevents unused methods, classes, or parameters from being added. Options:
- `dotnet build` with `TreatWarningsAsErrors` catches some of these via built-in IDE warnings
- Enable `.NET Analyzers` (CAxxxx rules) in `Directory.Build.props`:
  ```xml
  <PropertyGroup>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <AnalysisMode>All</AnalysisMode>
  </PropertyGroup>
  ```
- Add `Microsoft.CodeAnalysis.NetAnalyzers` for additional dead-code rules

### Current Gaps Summary

| Pattern | Current Gate | Missing |
|---------|-------------|---------|
| `catch (Exception)` | Detected via PowerShell regex | None |
| `catch { }` (bare) | **Not detected** | Add regex or Roslyn analyzer |
| `async void` (non-event) | **Not detected** | Roslyn analyzer needed |
| `Directory.Build.props` | Does not exist | Create with enforcement settings |
| Dead code | **Not detected** | Enable built-in analyzers |

### Recommendation

1. Add bare `catch { }` detection to verify.ps1 (PowerShell regex, catches most cases)
2. Create `Directory.Build.props` at the solution level with `AnalysisLevel`, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, and `NuGetAudit` settings
3. Add a Roslyn analyzer (see Section 5) for `async void` detection
4. Enable `AnalysisMode=All` to catch unused code via built-in CA rules

---

## 5. Roslyn Custom Analyzers

### Overview

Roslyn analyzers provide compile-time detection of project-specific rule violations. They are more reliable than PowerShell regex because they operate on the syntax tree and semantic model.

### Analyzer Project Structure

```xml
<!-- WTLauncher.Analyzers.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsPackable>false</IsPackable>
    <IncludeBuildOutput>true</IncludeBuildOutput>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### Priority Analyzer Rules

#### Rule 1: No Bare catch { }

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoBareCatchAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WTL001";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, "No bare catch blocks",
        "Use a typed catch (e.g., 'catch (Exception ex)') instead of bare 'catch { }'",
        "Correctness", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeCatch, SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatch(SyntaxNodeAnalysisContext context)
    {
        var catchClause = (CatchClauseSyntax)context.Node;
        if (catchClause.Declaration == null) // bare catch { }
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule,
                catchClause.CatchKeyword.GetLocation()));
        }
    }
}
```

#### Rule 2: No async void (except event handlers)

```csharp
// WTL002 — reject async void, allow only event handler pattern
// Check MethodDeclarationSyntax with async keyword + void return type,
// then verify the declaring type is not EventArgs-derived
```

#### Rule 3: No hardcoded hex colors in App layer

```csharp
// WTL003 — reject hex color literals (#RRGGBB, #AARRGGBB) in WTLauncher.App
// This enforces the design token invariant. Check for LiteralExpressionSyntax
// matching hex color pattern.
```

#### Rule 4: No direct File.WriteAllText (must use LauncherStore)

```csharp
// WTL004 — reject calls to System.IO.File.WriteAllText/WriteAllBytes/AppendAllText
// Only LauncherStore.cs may persist data
```

### Integration

Add the analyzer project as a project reference to `WTLauncher.App`:

```xml
<ProjectReference Include="..\..\analyzers\WTLauncher.Analyzers\WTLauncher.Analyzers.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

Or package it as a NuGet and add via `Directory.Build.props`:
```xml
<ItemGroup>
  <PackageReference Include="WTLauncher.Analyzers" Version="1.0.0"
                    PrivateAssets="all" />
</ItemGroup>
```

### Testing Analyzers

Use `Microsoft.CodeAnalysis.Testing`:

```csharp
[Fact]
public async Task BareCatch_Diagnostic()
{
    var code = @"
class C {
    void M() {
        try { }
        catch { }  // should be error
    }
}";
    await new CSharpAnalyzerTest<NoBareCatchAnalyzer>
    {
        TestCode = code,
        ExpectedDiagnostics = { DiagnosticResult.CompilerError("WTL001") }
    }.RunAsync();
}
```

### Current Gap

**No Roslyn analyzer project exists.** All gate checks are PowerShell regex, which is fragile (false positives, misses patterns, no semantic understanding).

### Recommendation

1. Create a new `src/avalonia-launcher/analyzers/WTLauncher.Analyzers/` project targeting `netstandard2.0`
2. Implement WTL001 (bare catch), WTL002 (async void), WTL003 (hardcoded colors), WTL004 (direct file write)
3. Reference the analyzer from all App and Core projects via `Directory.Build.props`
4. Add analyzer tests using `Microsoft.CodeAnalysis.Testing`
5. Replace the PowerShell regex checks in verify.ps1 with build-time enforcement

---

## 6. dotnet-format / Style Enforcement

### Overview

Since .NET 6, `dotnet format` is built into the SDK. It enforces `.editorconfig` rules for whitespace, code style, and analyzer preferences.

### Current State

The project has no `.editorconfig` at the solution level. Code style is enforced manually via code review.

### Setup

Create `.editorconfig` at `src/avalonia-launcher/`:

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
insert_final_newline = true
charset = utf-8-bom

# IDE style rules
dotnet_style_qualification_for_event = false:silent
dotnet_style_qualification_for_field = false:silent
dotnet_style_qualification_for_method = false:silent
dotnet_style_qualification_for_property = false:silent
dotnet_style_object_initializer = true:warning
dotnet_style_collection_initializer = true:warning
dotnet_style_prefer_auto_properties = true:warning
dotnet_style_prefer_compound_assignment = true:warning
dotnet_style_prefer_simplified_boolean_expressions = true:warning

# C# style rules
csharp_style_var_for_built_in_types = true:warning
csharp_style_var_when_type_is_apparent = true:warning
csharp_style_var_elsewhere = false:silent
csharp_style_expression_bodied_methods = true:silent
csharp_style_expression_bodied_properties = true:silent
csharp_style_pattern_matching_over_is_with_cast_check = true:warning
csharp_style_pattern_matching_over_as_with_null_check = true:warning
csharp_style_inlined_variable_declaration = true:warning
csharp_style_prefer_null_check_over_type_check = true:warning
csharp_style_prefer_local_over_anonymous_function = true:warning
csharp_style_prefer_switch_expression = true:warning
csharp_style_prefer_tuple_swap = true:warning
csharp_style_throw_expression = true:warning

# Namespace style
csharp_style_namespace_declarations = file_scoped:warning

# Using directives
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = true

# Newline options
end_of_line = lf
```

### CI Integration

```yaml
- name: Check formatting
  run: dotnet format src/avalonia-launcher/WTLauncher.Avalonia.sln --verify-no-changes
```

The `--verify-no-changes` flag returns a non-zero exit code if formatting changes are needed, which fails the build.

**Note:** `dotnet format` requires the project to be restored first. Combine with `--no-restore` only if restore already ran in a prior step.

### Style Severity Levels

In `.editorconfig`, severity suffixes control enforcement:
- `silent` -- suggestion only (no build output)
- `suggestion` -- shown as IDE suggestion
- `warning` -- shown as build warning
- `error` -- fails the build

Start with `warning` for style rules and `error` for correctness rules. Gradually escalate as the team agrees.

### Current Gap

**No `.editorconfig` exists.** Style enforcement is done purely through code review, which is inconsistent and time-consuming.

### Recommendation

1. Create `.editorconfig` at the solution level with project-specific style rules
2. Add `dotnet format --verify-no-changes` as a step in the `build-and-test` job
3. Add `EnforceCodeStyleInBuild=true` to `Directory.Build.props` to also enforce during `dotnet build`
4. Use `warning` severity initially to avoid blocking PRs on formatting debates

---

## 7. Snapshot / Approval Testing

### Overview

Snapshot testing captures output to a `.verified.txt` file on first run, then compares against it on subsequent runs. Verification is done by diff tool or manual review. Verify (by Simon Cropp) is the dominant .NET library.

### Use Cases for WT Launcher

#### 7.1 JSON Config Output Verification

`LauncherStoreFile` records can be serialized to JSON and snapshot-tested to catch regressions:

```csharp
[Fact]
public Task StoreFile_SerializesCorrectly()
{
    var store = new LauncherStoreFile(
        SchemaVersion: 2,
        Entries: ImmutableList.Create(
            new LaunchEntry(id: "test", name: "Test", command: "cmd", directory: @"C:\tools")
        ),
        Presets: ImmutableList<LauncherPreset>.Empty,
        Settings: new LauncherSettings(...),
        DefaultPresetId: "default"
    );
    return Verify(store).UseDirectory("snapshots");
}
```

#### 7.2 LaunchArgumentBuilder Output

```csharp
[Fact]
public Task LaunchArgs_WithTemplate_GeneratesCorrectCommand()
{
    var args = LaunchArgumentBuilder.Build("$terminal", new Dictionary<string, string>
    {
        ["terminal"] = "wt.exe -d ."
    });
    return Verify(args).UseDirectory("snapshots");
}
```

#### 7.3 XAML Snapshots (Headless)

Using Avalonia's headless test platform, you can render controls and snapshot the visual tree:

```csharp
[Fact]
public async Task EntryCard_Layout_Snapshot()
{
    // Requires Avalonia.Headless and Verify.Avalonia
    var card = new EntryCardView { DataContext = testEntry };
    await Verify(card).UseDirectory("snapshots/views");
}
```

**Note:** Visual tree snapshotting requires `Verify.Avalonia` (an extension package). It serializes the control tree structure, not a pixel image.

### Setup

```bash
dotnet add package Verify.Xunit
dotnet add package Verify.Avalonia     # for Avalonia control snapshots
dotnet add package Verify.Serilog      # if using structured logging
```

### Workflow

1. First run: verify file is created as `*.received.txt`
2. Developer inspects and renames to `*.verified.txt` (or uses diff tool)
3. `*.verified.txt` is committed to source control
4. CI runs: compares output against committed `.verified.txt`
5. Test fails on mismatch -- developer decides whether to update snapshot

### Git Configuration

Add to `.gitattributes` and `.gitignore`:

```
# .gitignore
*.received.*
```

```
# .gitattributes
*.verified.txt text diff
*.received.txt text diff
```

### Integration with Existing 700-Test Suite

Add snapshot tests for:
- `LauncherStoreFile` serialization round-trip
- `LaunchArgumentBuilder` output for each template pattern
- `LauncherEntry` sorting/filtering output
- `MainWindowViewModel` state projections (IsCardsViewMode, etc.)
- Config migration paths (v1 -> v2)

Do NOT snapshot-test:
- Transient state (timestamps, GUIDs) -- use `IgnoreMembers` configuration
- Full XAML render output (too brittle for a launcher)
- Large datasets (~100+ entries) -- test the algorithm separately

### Current Gap

**No snapshot tests exist.** All assertions are hand-written, leading to:
- Brittle tests that break on unrelated changes
- High maintenance cost for complex output types
- No visual regression detection for config output

### Recommendation

1. Add `Verify.Xunit` to the test project
2. Create snapshot tests for `LauncherStoreFile` serialization and `LaunchArgumentBuilder` output
3. Configure `*.received.*` to be ignored by git
4. Add a CI step to verify snapshot tests pass (they run as normal `dotnet test`)

---

## 8. Current Gaps Summary

| Category | Current State | Gap | Priority |
|----------|--------------|-----|----------|
| **Native AOT** | `--self-contained` publish only | No AOT publish, no trimming validation | High |
| **Dependency scanning** | Empty `nuget-audit` job | No vulnerability detection, no Dependabot, no CodeQL | High |
| **Mutation testing** | None | No mutation score baseline, unknown test quality | Medium |
| **Bare catch `{ }`** | Only detects `catch (Exception)` | Misses bare `catch { }` | High (security) |
| **async void** | Not detected | Dangerous for fire-and-forget exceptions | Medium |
| **Directory.Build.props** | Does not exist | Missing centralized warning/audit config | Medium |
| **Roslyn analyzers** | None | All gate checks are fragile PowerShell regex | High |
| **EditorConfig / dotnet-format** | None | No automated style enforcement | Low |
| **Snapshot testing** | None | All assertions hand-written | Medium |
| **CodeQL** | Not configured | Missing SAST scanning | Medium |
| **CI artifact validation** | Publish step runs but not validated | No smoke test of published binary | Low |

---

## 9. Recommended CI Pipeline Additions

### 9.1 Dependabot Configuration

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/src/avalonia-launcher"
    schedule:
      interval: "weekly"
      day: "monday"
    open-pull-requests-limit: 10
    groups:
      avalonia:
        patterns: ["Avalonia*"]
      test:
        patterns: ["xunit*", "Verify*", "NSubstitute*"]
      analyzers:
        patterns: ["Microsoft.CodeAnalysis*"]
```

### 9.2 CodeQL Workflow

```yaml
# .github/workflows/codeql.yml
name: CodeQL Analysis
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 6 * * 1'  # Mondays 6am

jobs:
  analyze:
    name: CodeQL
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: github/codeql-action/init@v3
        with:
          languages: csharp
      - uses: github/codeql-action/autobuild@v3
      - uses: github/codeql-action/analyze@v3
```

### 9.3 Extended `build-and-test` Job

```yaml
jobs:
  build-and-test:
    # ... existing steps ...

    - name: Check formatting
      run: dotnet format WTLauncher.Avalonia.sln --verify-no-changes --no-restore

    - name: List vulnerable packages
      shell: pwsh
      run: |
        $output = dotnet list package --vulnerable --include-transitive 2>&1
        Write-Host $output
        if ($output -match '\d+ known vulnerable') {
          throw "Vulnerable packages found"
        }
```

### 9.4 New `native-aot` Job

```yaml
  native-aot:
    name: Native AOT Build
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore
        run: dotnet restore src/avalonia-launcher/WTLauncher.Avalonia.sln

      - name: Publish Native AOT
        run: >
          dotnet publish src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj
          -c Release -r win-x64 --self-contained
          -p:PublishAot=true
          --output artifacts/aot-publish

      - name: Verify AOT output
        shell: pwsh
        run: |
          $exe = Get-Item "artifacts/aot-publish/WTLauncher.App.exe" -ErrorAction Stop
          if ($exe.Length -lt 5MB) { throw "AOT EXE too small: $($exe.Length)" }
          $dlls = Get-ChildItem "artifacts/aot-publish" -Filter "*.dll"
          if ($dlls.Count -gt 0) {
            Write-Warning "Managed DLLs found alongside AOT EXE: $($dlls.Name -join ', ')"
          }
          Write-Host "AOT EXE size: $($exe.Length) bytes"
```

### 9.5 Mutation Testing (Nightly)

```yaml
  mutation-test:
    name: Mutation Testing
    runs-on: windows-latest
    if: github.event_name == 'schedule' && github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore
        run: dotnet restore src/avalonia-launcher/WTLauncher.Avalonia.sln

      - name: Stryker mutation test
        working-directory: src/avalonia-launcher/tests/WTLauncher.Tests
        run: |
          dotnet tool install dotnet-stryker --tool-path .tools
          .tools/dotnet-stryker --config-file stryker-config.json

      - name: Upload mutation report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: mutation-report
          path: src/avalonia-launcher/tests/WTLauncher.Tests/StrykerOutput/
```

### 9.6 `Directory.Build.props` (New File)

```xml
<!-- src/avalonia-launcher/Directory.Build.props -->
<Project>
  <PropertyGroup>
    <!-- SDK + analysis -->
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <AnalysisMode>All</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- NuGet Audit (.NET 10 defaults to 'all') -->
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <WarningsAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsAsErrors>

    <!-- Deterministic builds -->
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
    <Deterministic>true</Deterministic>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
  </PropertyGroup>

  <ItemGroup>
    <!-- Source Link -->
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.*"
                      PrivateAssets="all" />
  </ItemGroup>
</Project>
```

### 9.7 Priority Implementation Order

| Phase | What | Why |
|-------|------|-----|
| **Phase 1** (immediate) | `Directory.Build.props` with `WarningsAsErrors` for NU190x | Free security win, zero effort |
| **Phase 1** | Fix `nuget-audit` job to actually check vulnerabilities | Existing job does nothing |
| **Phase 1** | Add bare `catch { }` detection to verify.ps1 | Five-line fix, catches real bug pattern |
| **Phase 2** (this sprint) | Create `.editorconfig` + `dotnet format` CI step | Consistent style across the codebase |
| **Phase 2** | Create Roslyn analyzer project with WTL001-WTL004 | Replace fragile PowerShell regex with compile-time enforcement |
| **Phase 2** | Create `.github/dependabot.yml` | Automated dependency updates |
| **Phase 3** (next sprint) | Add `native-aot` CI job (informational) | Catch trimming issues before they reach production |
| **Phase 3** | Add CodeQL workflow | SAST scanning for security issues |
| **Phase 4** (backlog) | Snapshot tests for `LauncherStoreFile` + `LaunchArgumentBuilder` | Reduce brittle hand-written assertions |
| **Phase 4** | Stryker.NET mutation testing (nightly) | Baseline and improve test quality |
| **Phase 4** | Roslyn analyzer WTL002 (async void) | Low incidence, high value when caught |

---

## Sources

- [Native AOT deployment overview - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot)
- [Avalonia Native AOT documentation](https://docs.avaloniaui.net/docs/deployment/native-aot)
- [AvaloniaUI Native AOT Deployment on Windows (DEV Community)](https://dev.to/chuongmep/avaloniaui-native-aot-deployment-on-windows-2jg1)
- [Native AOT COM Interop unsupported - dotnet/runtime#75060](https://github.com/dotnet/runtime/issues/75060)
- [NuGet Audit in .NET 10 -- transitive by default](https://www.appsoftware.com/blog/net-10-and-nuget-audit-identifying-root-packages-for-vulnerable-transitive-dependencies)
- [Check NuGet vulnerabilities in CI/CD](https://www.mytechramblings.com/posts/check-if-your-dotnet-app-dependencies-has-a-security-vulnerability-on-you-cicd-pipelines)
- [.NET 10 supply chain changes](http://blognet.tech/2025-11-12-net10-supply-chain)
- [Dependabot options reference - GitHub Docs](https://docs.github.com/en/code-security/reference/supply-chain-security/dependabot-options-reference)
- [Stryker.NET GitHub](https://github.com/stryker-mutator/stryker-net)
- [Stryker.NET introduction](https://stryker-mutator.io/docs/stryker-net/introduction)
- [Mutation testing - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/mutation-testing)
- [Enforce .NET code style in CI with dotnet format](https://www.meziantou.net/enforce-dotnet-code-style-in-ci-with-dotnet-format.htm)
- [dotnet format command - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
- [How to enforce .NET format using EditorConfig and GitHub Actions](https://learn.microsoft.com/en-us/community/content/how-to-enforce-dotnet-format-using-editorconfig-github-actions)
- [Roslyn analyzer tutorial - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Snapshots with Verify](https://github.com/verifytests/verify)
- [Snapshot Testing in .NET with Verify (JetBrains Blog)](https://blog.jetbrains.com/dotnet/2024/07/11/snapshot-testing-in-net-with-verify)
- [Avalonia Testing documentation](https://docs.avaloniaui.net/docs/testing)
- [Enforcing async code good practices with Roslyn analyzer](https://www.meziantou.net/enforcing-asynchronous-code-good-practices-using-a-roslyn-analyzer.htm)
