# Research: Roslyn Analyzers Review — WTLauncher.Analyzers

- **Query**: Ultrareview F# & Analyzers — Roslyn analyzer pattern correctness, false positives/negatives, perf, wiring
- **Scope**: Internal
- **Date**: 2026-07-26

## Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Analyzers/` | Analyzer project root |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/WTLauncher.Analyzers.csproj` | Project targeting `netstandard2.0`, `IsRoslynComponent=true` |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/GlobalUsings.cs` | Shared using directives |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/LayerViolationAnalyzer.cs` | WT1001 — Core must not reference App.ViewModels/Views |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/DispatchMustBeOnlyWritePathAnalyzer.cs` | WT1003 — Save calls only from StoreGrain.Dispatch |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/CatchExceptionMustHaveWhenAnalyzer.cs` | WT1004 — catch(Exception) requires when filter |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/ResourceKeyAnalyzer.cs` | WT1005 — StaticResource keys must be defined |
| `src/avalonia-launcher/src/WTLauncher.Analyzers/JsonRegistrationAnalyzer.cs` | WT1006 — [Obsolete] placeholder |
| `src/avalonia-launcher/Directory.Build.props` | Wires analyzers to all projects via `ProjectReference OutputItemType="Analyzer"` |
| `src/avalonia-launcher/Directory.Packages.props` | Central package versions |
| `.trellis/spec/guides/pitfalls.md:1039-1047` | Documents known analyzer non-loading issue |

## Findings

---

### A. Build Wiring

#### A1. Analyzer project wired correctly via Directory.Build.props

`Directory.Build.props:16-20` applies the analyzer `ProjectReference` with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` to all projects except `WTLauncher.Analyzers` itself.

- **Condition**: `$(MSBuildProjectName) != 'WTLauncher.Analyzers'` — correctly excludes self.
- **Configuration**: Avalonia 12.1 automatically provides `.axaml` files as `AdditionalFiles`, so `ResourceKeyAnalyzer` can scan them.

#### A2. RunAnalyzersDuringBuild defaults to false locally

`Directory.Build.props:12` has:
```xml
<RunAnalyzersDuringBuild Condition="'$(CI)' == 'true' Or '$(ForceAnalysis)' == 'true'">true</RunAnalyzersDuringBuild>
```

- **Impact**: All 5 analyzers (WT1001-WT1006) are **never executed during local development builds**. They only run during CI (`$(CI)=true`) or when explicitly opting in (`-p:ForceAnalysis=true`).
- This means developers can introduce layer violations, bare catches, and direct Save calls locally without any compile-time feedback.
- Whether this is intentional or a residual issue from `pitfalls.md:10.7` (where analyzers were not loaded at all) is unclear from the code alone.

#### A3. No unit tests for any Roslyn analyzer

The test files at `src/avalonia-launcher/tests/WTLauncher.Tests/CommandUsageAnalyzerTests.cs` and `DirectoryUsageAnalyzerTests.cs` test **Core utility analyzers** (`CommandUsageAnalyzer`, `DirectoryUsageAnalyzer`), NOT the Roslyn analyzers (WT1001-WT1006). A search for `WT1001|WT1003|WT1004|WT1005|WT1006|CatchException|DispatchMust|LayerViolation|ResourceKey|JsonRegist` across all `*Test*.cs` files returned zero matches.

- **Consequence**: There is no way to verify analyzer behavior via automation. Each analyzer is compiled but never validated against test fixtures.

---

### B. WT1001 — LayerViolationAnalyzer

#### B1. Uses `.Contains()` for namespace matching (low-risk false positive)

`LayerViolationAnalyzer.cs:45,60` checks `name.Contains("WTLauncher.App.ViewModels")`. The `.Contains()` method matches substrings:
- `"WTLauncher.App.ViewModels"` — matches expected namespace
- `"WTLauncher.App.ViewModelsFoo"` — would also match (extremely unlikely namespace)
- `"SomePrefix.WTLauncher.App.ViewModels"` — would also match

This is low-risk in practice because `WTLauncher.App.ViewModels` is a unique namespace pattern unlikely to appear as a substring.

#### B2. Assembly name check may silently disable analyzer

`LayerViolationAnalyzer.cs:29`:
```csharp
if (!compilationContext.Compilation.AssemblyName?.StartsWith("WTLauncher.Core", StringComparison.Ordinal) == true)
    return;
```

- If the Core project is renamed, the analyzer silently stops running. No warning is produced.
- The `== true` comparison with the nullable bool from `?.` is functionally correct but slightly opaque.

#### B3. Catches both `using` directives and fully-qualified names

The analyzer registers two syntax node actions (`AnalyzeUsing` on `UsingDirective`, `AnalyzeQualifiedName` on `QualifiedName`), providing defense-in-depth against bypassing via full qualification. This is a correct pattern.

---

### C. WT1003 — DispatchMustBeOnlyWritePathAnalyzer

#### C1. Stale allowed method: `LazyInit` does not exist

`DispatchMustBeOnlyWritePathAnalyzer.cs:62` lists `LazyInit` as an allowed enclosing method of `StoreGrain`:
```csharp
enclosingMethodName is "Dispatch" or "DispatchErrorOr" or "LazyInit"
```

A search for `LazyInit` across the entire `src/avalonia-launcher/src/` directory returned matches only in the analyzer itself. The `StoreGrain` class (`StoreGrain.cs`) has `Dispatch`, `DispatchErrorOr`, and `ReloadFromDisk` — no `LazyInit` exists.

- **Impact**: Dead code in the allowlist. Does not cause false positives since the method doesn't exist, but the allowlist is misleading.

#### C2. `DispatchAsync` referenced in CLAUDE.md but absent from codebase

The CLAUDE.md states: "All callers use `Dispatch` / `DispatchAsync` / `DispatchErrorOr`." However, `DispatchAsync` does not exist anywhere in the codebase. The analyzer does NOT list `DispatchAsync` in its allowlist.

- **Impact**: If a `DispatchAsync` method is ever added to `StoreGrain`, calling `Save` inside it would trigger a WT1003 diagnostic and need a code update to the analyzer.

#### C3. Class-name-only check for `StoreGrain`

`DispatchMustBeOnlyWritePathAnalyzer.cs:61` checks `parentTypeName is "StoreGrain"` (uses only the class short name, not the full namespace `WTLauncher.App.Infrastructure.StoreGrain`). This means a different class named `StoreGrain` in a different namespace would also be allowed. In this codebase there is only one `StoreGrain` class, so this is not a real risk.

#### C4. Uses `RegisterCompilationStartAction` — correct pattern

`DispatchMustBeOnlyWritePathAnalyzer.cs:29` correctly wraps `RegisterSyntaxNodeAction` inside `RegisterCompilationStartAction`, which is the recommended pattern to avoid syntax-node-callback-per-file overhead.

#### C5. Checks only `Save` method name for `LauncherStore`/`IStorePersistence`

`DispatchMustBeOnlyWritePathAnalyzer.cs:43` checks `methodName != "Save"` (early return). This means:
- A hypothetical `LauncherStore.Write` or `LauncherStore.Persist` would silently bypass the check.
- Only `Save` is the known write method in this codebase (`IStorePersistence.cs:13`), so the check is correct for current code but fragile if new write methods are added.

---

### D. WT1004 — CatchExceptionMustHaveWhenAnalyzer

#### D1. Correctly catches both `catch { }` and `catch (Exception)`

`CatchExceptionMustHaveWhenAnalyzer.cs:46-51` explicitly handles bare `catch { }` blocks (no declaration):
```csharp
if (declaration == null)
{
    context.ReportDiagnostic(Diagnostic.Create(Rule, catchClause.GetLocation()));
    return;
}
```

This matches the spec requirement from `.trellis/spec/core/quality-guidelines.md:77` and the Phase-A implementation plan.

#### D2. Correct type comparison using SymbolEqualityComparer

`CatchExceptionMustHaveWhenAnalyzer.cs:56`:
```csharp
if (!SymbolEqualityComparer.Default.Equals(caughtType, exceptionType)) return;
```

This uses the standard Roslyn approach for type comparison. Handles type identity correctly across different compilation instances.

#### D3. Null-safe return on missing types

`CatchExceptionMustHaveWhenAnalyzer.cs:55` handles `caughtType == null || exceptionType == null` with an early return, preventing NullReferenceException when the semantic model cannot resolve the type.

#### D4. No DiagnosticDescriptor category.xml

The analyzer uses `DiagnosticSeverity.Error` but does not set the `category` parameter explicitly beyond `"Correctness"` (which is the title parameter, not the category — the parameter order is: `id, title, messageFormat, category, severity, isEnabledByDefault, description`). Looking at the constructor call:

```csharp
private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId,
    title: "Catch Exception must have a when filter",
    messageFormat: "Bare 'catch (Exception)' without 'when' filter...",
    category: "Correctness",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Bare catch (Exception) swallows fatal exceptions...");
```

Actually, this is correct — the 4th positional parameter IS `category`. So the category is `"Correctness"`. No issue here.

---

### E. WT1005 — ResourceKeyAnalyzer

#### E1. `CollectDefinedKeys` only matches bare `Resources["Key"]`

`ResourceKeyAnalyzer.cs:76`:
```csharp
&& elem.Expression.ToString() is "Resources"
```

This check matches:
- `Resources["Key"] = value` (bare identifier) -- **matches**
- `this.Resources["Key"] = value` -- does **not** match (`"this.Resources"` != `"Resources"`)
- `App.Current.Resources["Key"]` -- does **not** match

The `InjectDesignTokens()` method in `App.axaml.cs` uses bare `Resources["Key"]` (without `this.`), so all current resource definitions **are** matched. However, if any C# code uses a qualified `Resources` access, those key definitions will be missed.

#### E2. StaticResource regex does not match keys with dots or hyphens

`ResourceKeyAnalyzer.cs:28`:
```csharp
private static readonly Regex StaticResourceRegex = new(
    @"\{StaticResource\s+(\w+)\}",
    RegexOptions.Compiled);
```

`\w+` matches `[a-zA-Z0-9_]`. Keys containing dots (e.g., `{StaticResource SystemAccentColor}`) or hyphens (e.g., `{StaticResource Some-Key}`) would **not** be matched as references and would therefore never be flagged as undefined. However, the project's design tokens use `PascalCase` keys (e.g., `PrimaryBrush`, `ErrorBrush`), so this is currently a non-issue.

#### E3. XAML comments trigger false positives

References inside XAML comments, like:
```xml
<!-- {StaticResource OldKey} -->
```
would be matched by the regex and flagged as an undefined resource key. The analyzer has no mechanism to skip XAML comment nodes. The analyzer scans the raw text of `.axaml` files as strings (`sourceText.ToString()`), not as parsed XML/AXML nodes.

#### E4. `CollectXamlDefinedKeys` may double-count keys from `CollectDefinedKeys`

`ResourceKeyAnalyzer.cs:47-49`:
```csharp
foreach (var key in CollectXamlDefinedKeys(compilationContext))
    definedKeys.Add(key);
```

If a resource key is both defined in C# (`Resources["Foo"] = ...`) AND in XAML (`x:Key="Foo"`), it is correctly still just one entry in the `HashSet`. No double-counting issue.

#### E5. `GetText(default)` passes default CancellationToken

`ResourceKeyAnalyzer.cs:101,127`:
```csharp
var sourceText = additionalText.GetText(default);
```

This passes `default(CancellationToken)` which is `CancellationToken.None`. This is acceptable but should ideally receive the compilation context's cancellation token for responsive cancellation. In a `RegisterCompilationAction` callback, there is no direct cancellation token parameter on the `CompilationAnalysisContext`, so this is pragmatic.

#### E6. No `CompilationEnd` custom tags convention

`ResourceKeyAnalyzer.cs:24` adds `customTags: ["CompilationEnd"]`. These tags are informational only and have no effect on Roslyn analyzer behavior. Not a bug, but the tag serves no purpose visible in the codebase.

#### E7. XAML `.axaml` files ARE available as AdditionalFiles

Confirmed from `WTLauncher.App\obj\Release\net10.0\WTLauncher.App.GeneratedMSBuildEditorConfig.editorconfig` lines showing `build_metadata.AdditionalFiles.SourceItemGroup = AvaloniaXaml`. The Avalonia build system provides `.axaml` files correctly.

---

### F. WT1006 — JsonRegistrationAnalyzer

#### F1. Properly marked `[Obsolete]` with clear rationale

`JsonRegistrationAnalyzer.cs:21-22`:
```csharp
[Obsolete("WT1006 is a scaffolding analyzer. " +
    "Use runtime null-safety checks and AOT compilation validation instead.")]
```

The `Initialize` method (line 40) registers no syntax or compilation actions. The analyzer exists as a type but never produces diagnostics. The file header (lines 1-15) documents why the planned approach was abandoned and what the alternatives are.

#### F2. DiagnosticDescriptor is never used but still defined

The `Rule` field (line 28-35) and `SupportedDiagnostics` property (line 38) are kept. This is harmless since `Initialize` registers no callbacks. No performance impact.

---

### G. General Issues

#### G1. Cross-cutting: Stale descriptions across docs

- `.trellis/spec/core/quality-guidelines.md:78` documents WT1005 as: `"`{StaticResource Key}` must be defined in `InjectDesignTokens` or XAML `x:Key`"`. The analyzer header at `ResourceKeyAnalyzer.cs:1-3` says: "defined in InjectDesignTokens or inline XAML". These are consistent.
- `.trellis/spec/core/quality-guidelines.md:79` documents WT1006 as: "`LauncherJson.Save/Load<T>` type must be `[JsonSerializable]` registered" with status "⚠️ [Obsolete] cross-reference". Consistent with the `[Obsolete]` marker.
- `docs/completion-overview.md:53-57` lists WT1001-WT1006 but WT1002 is listed with description "StateMutationInQuery" — but WT1002 analyzer does not exist. The active codebase has only WT1001, WT1003, WT1004, WT1005, WT1006.

#### G2. No WT1002 analyzer exists

The spec (`docs/completion-overview.md:53`) references WT1002 ("StateMutationInQuery / State mutation in query"), `.trellis/spec/core/quality-guidelines.md` (line 76 skips from WT1001 to WT1003), and earlier design documents (`07-24-p2-roslyn-analyzers/design.md`) list 6 planned analyzers. WT1002 was never implemented.

#### G3. `RunAnalyzersDuringBuild` condition may prevent feedback loop

The requirement from `pitfalls.md:1046` says: "(3) 分析器仅 CI 全量运行（`$(CI)=='true'`），本地 Debug 零开销". The current `Directory.Build.props:12` matches this intent by only running analyzers during CI or with explicit opt-in. This is by design, not a bug, but it means developers must remember to opt in when debugging analyzer issues.

## Consistency with CLAUDE.md Invariants

| CLAUDE.md Invariant | Relevant Analyzer | Status |
|---|---|---|
| `catch (Exception)` is forbidden — narrow to known exception types | WT1004 — flags bare catch(Exception) | Consistent |
| Selection has two write paths, both visible (checkbox vs batch) | Not analyzed by any analyzer | Not covered |
| No obsolete per-entry `confirm` key | Not analyzed by any analyzer | Not covered |
| Fluxor is gone; StoreGrain.Dispatch is the write path | WT1003 — enforces Dispatch-only writes | Consistent |
| Design tokens are the only color/spacing source | WT1005 — enforces StaticResource key registration | Consistent |

## Caveats / Not Found

- **No analyzer unit tests exist** — zero code coverage for WT1001-WT1006.
- **Local builds skip analyzers** — `RunAnalyzersDuringBuild` defaults to false (<condition> requires $(CI)==true or $(ForceAnalysis)==true). Developers writing violating code locally get no feedback.
- **WT1002 is referenced in documentation but does not exist** — no implementation or placeholder.
- **WT1003 allowlist includes `LazyInit` (nonexistent method)** — dead code in the allowlist.
