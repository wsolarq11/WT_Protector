# Research: Layer Dependency Audit

- **Query**: Verify layer dependency rules: (1) Core (WTLauncher.Core) must not reference Avalonia/CommunityToolkit.Mvvm/any UI framework; (2) All layered project references are correct.
- **Scope**: internal
- **Date**: 2026-07-26

## Findings

### Core Layer (WTLauncher.Core) -- CLEAN

**Result: Zero violations.** No Avalonia, CommunityToolkit.Mvvm, or any UI framework imports found in any `.cs` file under `src/avalonia-launcher/src/WTLauncher.Core/`.

Exhaustive check performed via grep for `using Avalonia|using CommunityToolkit.Mvvm|using Avalonia.` returning zero results across all 40+ source files.

#### Core .csproj references (`WTLauncher.Core.csproj`)

Only references:
- `ErrorOr` -- error handling library
- `OneOf` -- discriminated unions
- `Immutype` (PrivateAssets=all) -- immutable codegen
- `MSBuild.CompilerCache` (PrivateAssets=all) -- build cache

No Avalonia packages, no CommunityToolkit.Mvvm, no UI framework. Compliant.

#### LauncherDesignTokens in Core -- Acceptable by design

`LauncherDesignTokens.cs` lives in Core and contains UI-related constants (color hex strings, spacing values, font sizes). This is architecturally intentional per CLAUDE.md ("Design tokens are the only color/spacing source"). The class does NOT reference any Avalonia types -- it only holds `const string` and `const double` values. This is compliant with the "no framework reference" rule.

### App Layer (WTLauncher.App) -- CLEAN

**Result: Correct.** References:
- `Avalonia` / `Avalonia.Desktop` / `Avalonia.Fonts.Inter` / `Avalonia.Themes.Fluent` -- correct for UI layer
- `CommunityToolkit.Mvvm` -- correct for MVVM ViewModel bindings
- `Stateless` -- state machine for drag reorder workflow
- `DynamicData` -- reactive collections
- `OneOf`, `MSBuild.CompilerCache`
- Project reference to `WTLauncher.Core` -- correct (lower layer)

### Test Layer (WTLauncher.Tests) -- CLEAN

**Result: Correct.** References both App and Core projects (needs access to all layers for testing). Uses `TngTech.ArchUnitNET` for architecture rule enforcement -- good practice.

### Summary

| File | Line | Status |
|---|---|---|
| `src/.../WTLauncher.Core/*.cs` | All | CLEAN -- no UI framework imports |
| `src/.../WTLauncher.Core/WTLauncher.Core.csproj` | All | CLEAN -- no UI packages |
| `src/.../WTLauncher.App/WTLauncher.App.csproj` | All | CLEAN -- has Avalonia + references Core |
| `src/.../WTLauncher.Tests/WTLauncher.Tests.csproj` | All | CLEAN -- references both projects |

## Caveats / Not Found

- No cross-references between `WTLauncher.Core` and any UI framework found.
- The `LauncherDesignTokens.cs` in Core is the only "gray area" (UI constants in a pure data layer), but it is architecturally intentional and does not break the dependency rule since it references zero UI types.
