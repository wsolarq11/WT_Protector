# Trivial cleanups: LiteDB PackageVersion + CompiledBindings comment

## Goal

Land two zero-risk items from the 2026-07-09 second-pass audit:

- **R3**: `Directory.Packages.props` still declares `<PackageVersion Include="LiteDB" Version="5.0.21" />` even though no .csproj references it. Central Package Management (CPM) means unreferenced PackageVersion is a no-op, but it's noise. 1-line delete.
- **R4**: `WTLauncher.App.csproj` has `<AvaloniaUseCompiledBindingsByDefault>false</AvaloniaUseCompiledBindingsByDefault>` with no comment explaining why. Either document the reason OR enable it (per second-pass audit: enabling requires ~30 DataTemplate x:DataType edits, 100 build errors).

## Requirements

### R3 (must)
- Delete the LiteDB PackageVersion line from `Directory.Packages.props`.
- Verify no csproj references LiteDB after the change (grep).
- `tools/verify.ps1` continues to PASS (it has no LiteDB-specific check, so this is automatic).

### R4 (must)
- Investigate WHY CompiledBindings is off. Either:
  - (a) Find historical context — check git log, comments, design docs
  - (b) Try enabling + build — count errors
- Decision: enable OR add `ponytail:` comment explaining why it stays off.
- Default: add comment (audit said enabling requires ~30 DataTemplate edits + 100 errors).

## Acceptance Criteria

- [ ] LiteDB PackageVersion line removed from Directory.Packages.props
- [ ] CompiledBindings decision landed (either enabled with build passing, OR ponytail: comment added)
- [ ] `dotnet build` succeeds, 0 errors
- [ ] 453/453 tests passing
- [ ] `tools/verify.ps1` 18/18 PASS

## Out of scope

- Touching any other PackageVersion entries
- Refactoring DataTemplates (only if R4 enables CompiledBindings, which is unlikely)

## Conclusion (audit-hypothesis-verification)

**Both R3 and R4 are NO-ACTION — already completed in earlier tasks same day (2026-07-09).**

| Item | Audit claim | Actual state | Resolved by |
|------|-------------|--------------|-------------|
| R3 | LiteDB PackageVersion lingers in `Directory.Packages.props` | `src/avalonia-launcher/Directory.Packages.props` has NO LiteDB entry (lines 5-15 list Avalonia + CommunityToolkit + xUnit only) | task `07-09-remove-dead-code` (commit `6dc4b88`/`ef978b7`, same day) |
| R4 | `AvaloniaUseCompiledBindingsByDefault=false` has no comment | `WTLauncher.App.csproj:7-10` has full `ponytail:` comment explaining why off (enabling needs ~30 DataTemplate edits, 100 build errors; single-user ≤200 entries runtime reflection invisible; revisit if perf demands) | task `07-09-enable-compiled-bindings` (commit `b24c254`/`b0cca49`, same day) |

**Lesson reinforced:** the audit-driven-verify discipline (each proposal verified against actual code before action) — second-pass audit was conducted mid-day 2026-07-09 but published AFTER several of its own recommended fixes had already landed. The audit's "lingering" / "no comment" claims were already stale by publication time. Always grep the actual code before believing an audit finding — even one written the same day.

No code changes. Task archived without commit (degraded mode auto-commit may produce empty commit; if so, will skip).
