# Discovery scan narrow catch invariant restore

## Goal

Restore the project-wide "no bare `catch (Exception)`" invariant on
`DiscoverAgentsViewModel.ScanAsync` (introduced by `4c67183 feat(discovery)`
before the invariant was enforced project-wide). The current bare catch
swallows unrecoverable exceptions (OOM, SEH, AccessViolation) the same way
it swallows recoverable scanner orchestration failures — which hides real
bugs behind a friendly status string. Adopt the existing project pattern
(`ExceptionFilters.IsRecoverable`) used by `MainWindowViewModel.Discovery`
for the sibling dialog-open catch.

## Background

- Invariant source: `.trellis/spec/guides/index.md` §5 "Never `catch (Exception)` — Narrow It".
- Project pattern: `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` + `ErrorLog.Write(ex, scope)` for diagnostics.
- Pre-existing usage: `MainWindowViewModel.Discovery.cs:39-44` is the canonical reference.
- This is the **only** bare `catch (Exception)` left in `src/` (grep-confirmed across `WTLauncher.Core` + `WTLauncher.App`).
- Per-agent scanner failures are already isolated inside `AgentDiscoveryScanner.Scan` (`IOException | UnauthorizedAccessException | Win32Exception | InvalidOperationException` → `AgentDiscoveryResult.Failures`); the outer catch only sees orchestration-level escapes.

## Requirements

- R1 — Replace bare `catch (Exception)` at `DiscoverAgentsViewModel.cs:70` with the project's recoverable-filter pattern: `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))`.
- R2 — Log the escaped exception via `ErrorLog.Write(ex, "DiscoverAgents.Scan")` so post-mortem diagnostics exist (matches `MainWindowViewModel.Discovery` scope naming).
- R3 — Preserve the existing user-visible contract: failed scan sets `StatusText = "扫描过程中发生错误"` and `IsScanning = false` in the `finally`. No new user-facing strings.
- R4 — Add a focused unit test proving the narrowed catch still produces the contract behavior when the injected scan Func throws a recoverable exception (e.g. `InvalidOperationException`): status text set, `IsScanning` cleared, no rows populated.
- R5 — No separate unrecoverable-exception test. The "unrecoverable re-throws" path is covered by C# language semantics of the `when` filter (`false` → catch skipped) plus the project-wide `MainWindowViewModel.Discovery.cs:39` precedent using the same `ExceptionFilters.IsRecoverable` filter. Adding a test that throws a reserved runtime exception (OOM/SEH/AccessViolation) would force a `CA2201` suppression, which is analyzer theater for a behavior the language already guarantees.

## Acceptance Criteria

- [ ] `DiscoverAgentsViewModel.ScanAsync` no longer contains a bare `catch (Exception)`; the catch uses `ExceptionFilters.IsRecoverable(ex)` filter.
- [ ] `ErrorLog.Write(ex, "DiscoverAgents.Scan")` is called inside the narrowed catch.
- [ ] One new test in `WTLauncher.Tests` (`DiscoverAgentsViewModelTests`) exercises: recoverable exception → status text + `IsScanning=false` + empty rows.
- [ ] `tools/verify.ps1` PASS with the same 19-check shape; test count rises from 504 → 505.
- [ ] Grep `catch (Exception)` (no `when`/`ex`) across `src/avalonia-launcher/src` returns zero hits in non-`obj` files.
- [ ] No new NuGet; no `design.md` / `implement.md` needed (PRD-only lightweight task).

## Out of scope

- Any change to `AgentDiscoveryScanner` — its per-agent catches are already narrow and correct.
- Any UX change to the discovery dialog.
- Refactoring other filtered `catch (Exception ex) when (...)` sites — those are already compliant.
