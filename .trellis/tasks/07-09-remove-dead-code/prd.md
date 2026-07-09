# Remove ShellCommand + LiteDB dead code

## Goal
Eliminate two pieces of dead code identified in the architecture audit:
1. `ShellCommand.cs` (62 lines, value object + JsonConverter + Resolve() — never consumed)
2. `<PackageVersion Include="LiteDB" Version="5.0.21" />` in Directory.Packages.props (orphaned after LiteDB was removed from csproj)

## Background
- `ShellCommand` was a half-finished fix for pitfalls §7.4 (hardcoded shell prefix). The class exists but `LaunchArgumentBuilder.cs:9` still uses `public const string ShellCommandPrefix = "-- pwsh -NoExit -EncodedCommand";`. grep confirms 16 references to ShellCommand are all internal to ShellCommand.cs.
- LiteDB was removed from all csproj in commit (archived task `07-09-remove-litedb-deadcode`) but the central PackageVersion declaration in Directory.Packages.props was missed.

## Requirements
- Delete `src/avalonia-launcher/src/WTLauncher.Core/ShellCommand.cs` entirely
- Remove LiteDB PackageVersion line from `Directory.Packages.props`
- No functional changes — `LaunchArgumentBuilder.ShellCommandPrefix` const remains as-is
- No other files reference these symbols (verified via grep before delete)

## Acceptance Criteria
- [ ] `git ls-files | grep -i shelcommand` returns 0 matches (after commit)
- [ ] `git ls-files | grep -i litedb` returns 0 matches in `Directory.Packages.props`
- [ ] `dotnet build -c Release` succeeds with 0 warnings, 0 errors
- [ ] `dotnet test -c Release` passes 451+ tests, 0 failures
- [ ] `dotnet publish -c Release -r win-x64 --self-contained true` succeeds (sanity check, no runtime LiteDB dependency)

## Out of scope
- Replacing hardcoded `ShellCommandPrefix` with configurable shell (deferred — current `pwsh -EncodedCommand` works for the only user)
- Adding Roslyn analyzer to prevent future orphaned PackageVersion entries
