# Enable Avalonia CompiledBindings

## Goal
Either enable Avalonia CompiledBindings (default `true`) or document why this project keeps it `false`. CompiledBindings give compile-time checking of XAML bindings and faster runtime performance via source-generated accessors instead of reflection.

## Background
- `WTLauncher.App.csproj:7` writes `<AvaloniaUseCompiledBindingsByDefault>false</AvaloniaUseCompiledBindingsByDefault>` with no explanation.
- CompiledBindings requires `x:DataType` on data-bound roots or it falls back to reflection at runtime (with a warning).
- 2026 best practice: enable by default, mark `x:CompileBindings="true"` and `x:DataType` on each View root.

## Decision tree
1. Remove the `<AvaloniaUseCompiledBindingsByDefault>false</...>` line.
2. `dotnet build -c Release`:
   - If 0 errors → ship it.
   - If errors are fixable binding paths → add `x:DataType` to View roots, fix paths, ship.
   - If errors indicate design-level mismatch (e.g., dynamic types) → restore `false` and add explanatory comment citing the specific reason.

## Acceptance Criteria
- [ ] Either CompiledBindings is on (line removed) with all XAML passing build, OR a `<!-- ponytail: ... -->` comment explains why `false` is required
- [ ] `dotnet build -c Release` 0 warnings 0 errors
- [ ] `dotnet test -c Release` 451+ pass
- [ ] GUI smoke (verify.ps1) still passes

## Out of scope
- Migrating all XAML to fully typed bindings if CompiledBindings can't be enabled cleanly — falls back to documented `false`
