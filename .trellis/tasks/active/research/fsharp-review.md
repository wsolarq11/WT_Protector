# Research: F# Code Review — WTLauncher.Core.FSharp

- **Query**: Ultrareview F# & Analyzers — F# idiomatic code patterns, C# interop, edge cases
- **Scope**: Internal
- **Date**: 2026-07-26

## Files Found

| File Path | Description |
|---|---|
| `src/avalonia-launcher/src/WTLauncher.Core.FSharp/Library.fs` | Single F# source file — 5 record types + `LaunchArgumentBuilder.quoteArgument` module |
| `src/avalonia-launcher/src/WTLauncher.Core.FSharp/WTLauncher.Core.FSharp.fsproj` | F# project file, targets `net10.0`, `IsAotCompatible=true` |
| `src/avalonia-launcher/WTLauncher.Avalonia.sln` | Solution includes F# project at line 16 |

## Findings

### 1. Orphaned project — no consumers

The F# project `WTLauncher.Core.FSharp` is included in the `.sln` (line 16) but **no C# project references it**. A grep for `Core.FSharp` in all `.csproj` and `.cs` files returns zero matches. The entire codebase is unreachable.

- **Evidence**: Grep `WTLauncher.Core.FSharp` across all `.csproj` files returned no matches; grep across all `.cs` files returned no matches.
- **Impact**: The `quoteArgument` function, all 5 record types, and the `LaunchArgumentBuilder` module are compiled but never loaded or executed by any consumer. Zero test coverage.

### 2. Null handling gap at F#/C# boundary

- `Library.fs:56-75` — `let quoteArgument (value: string)` has no null guard.
- F# strings are non-nullable by default, but at the C# interop boundary a C# caller could pass `null`. The `for ch in value` loop (line 60) would throw a `NullReferenceException` with no context.
- C# version at `LaunchArgumentBuilder.cs:124` has `ArgumentNullException.ThrowIfNull(value)` as the first line.
- **Fix needed**: Add a null guard if this code will ever be consumed from C#.

### 3. No consumers across the entire codebase

- Grep for `FSharp` across all `.cs` files in `src/avalonia-launcher` returned zero matches.
- The C# `WTLauncher.Core.LaunchArgumentBuilder.QuoteArgument` is the canonical implementation, used by production code at `LaunchArgumentBuilder.cs:72` and tested at `LaunchArgumentBuilderTests.cs:57,63`.
- The F# version is a second, unused copy with no tests.

### 4. AOT compatibility concern

- `WTLauncher.Core.FSharp.fsproj:6` sets `<IsAotCompatible>true</IsAotCompatible>`.
- Record types use F# `string list` (e.g., `CustomEntryOrder: string list`, `SearchHistory: string list`, `EntryIds: string list` at `Library.fs:32,35,43`). F# lists (`Microsoft.FSharp.Collections.FSharpList<T>`) require the FSharp.Core runtime and are linked-list allocated per element.
- For Native AOT, the FSharp.Core dependency adds measurable binary size. However, since this project has no consumers, this is currently a non-issue.

### 5. Code duplication with C# types

- The 5 F# record types (`LaunchEntry`, `LauncherWindowSettings`, `LauncherSettings`, `LauncherPreset`, `LauncherStoreFile`) at `Library.fs:9-52` duplicate the same types already in `WTLauncher.Core` (C#). They are structurally identical.
- If the F# module were ever consumed, there would be two parallel type definitions for the same data — a maintenance burden.

### 6. quoteArgument logic parity

- The F# `quoteArgument` function (`Library.fs:55-75`) is a structurally faithful port of the C# `QuoteArgument` method (`LaunchArgumentBuilder.cs:122-158`).
- Backslash counting, quote escaping, and trailing backslash doubling are identical in both implementations.
- The only functional difference is the absence of the null guard (see finding 2).

### 7. `fsproj` does not define `<FSharpPreferNetFrameworkReferences>`

- The `.fsproj` (`WTLauncher.Core.FSharp.fsproj`) is minimal — only `TargetFramework`, `RuntimeIdentifiers`, `IsAotCompatible`, and `Nullable`.
- No `<FSharpPreferNetFrameworkReferences>false</FSharpPreferNetFrameworkReferences>` or `<MicrosoftFSharpPackageVersion>` pinning.
- This is fine for default behavior but means the build depends on whatever FSharp.SDK version the host dotnet SDK selects.

## Caveats / Not Found

- No architectural document describes the intended purpose of the F# project. It appears to be a proof-of-concept or experimental module.
- No tests exist for the F# code.
- No F# specific `.editorconfig` or coding guidelines in the repo reference F# conventions or code style.
