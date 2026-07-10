# Modernization Audit Followup P1-P7

## Goal

Parent task orchestrating the 7 followup items produced by the 2026-07-10 modernization audit
(`docs/`-class review against .NET 10 / Avalonia 12 / xUnit v3 industry frontier). Owns cross-child
acceptance criteria, integration review, and the final gate / commit gate.

## Context

Audit verdict: overall ≈ 92% aligned (A grade). Architecture / toolchain / safety / test discipline
all A/A+. The single systemic discipline debt is **bare-catch relaxation** (9 sites). Smaller gaps:
no `.editorconfig`, CI audit non-blocking, one redundant `ExitCode` line, `xUnit1051` blanket-suppressed,
Native AOT not evaluated, CompiledBindings decision needs re-confirmation.

Each P1–P7 item is independently verifiable; this parent guarantees they compose cleanly.

## Child task map

| Child | Slug | Priority | Type |
|-------|------|----------|------|
| P1 | `p1-narrow-catches` | P1 | Code fix (9 sites) |
| P2 | `p2-editorconfig-analysis-level` | P2 | Config addition |
| P3 | `p3-ci-nuget-audit-blocking` | P2 | CI policy |
| P4 | `p4-program-exit-code-cleanup` | P3 | 1-line nit |
| P5 | `p5-xunit1051-investigation` | P3 | Investigation |
| P6 | `p6-native-aot-evaluation` | P3 | Research + decision |
| P7 | `p7-compiled-bindings-reassessment` | P3 | Decision re-confirmation |

## Cross-child acceptance criteria

- [ ] All 7 children either `completed` (delivered) or `archived` (deliberately deferred with documented reason).
- [ ] `tools/verify.ps1` 16-check gate stays green.
- [ ] `dotnet build -c Release` 0 warnings / 0 errors.
- [ ] `dotnet test -c Release` 453/453 (or the new equivalent count) pass.
- [ ] No regression in existing 9 test files that touch touched code paths.
- [ ] PRD/design/implement artifacts persisted under each child task directory.

## Out of scope

- Any change to `LaunchEntry` / `LauncherStoreFile` data shape (would bump `schemaVersion`).
- Visual / UX changes.
- New entry-level features.

## Notes

- This parent task is NOT the implementation target. Implementation happens inside each child.
- Each child must use `ExceptionFilters.IsRecoverable` or narrow types when fixing catch sites,
  never add new `catch (Exception)` without filter.
