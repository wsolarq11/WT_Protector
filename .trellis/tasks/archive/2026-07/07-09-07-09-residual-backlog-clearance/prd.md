# Residual backlog clearance (post R5)

## Goal

Investigate and resolve the 6 remaining items from the 2026-07-09 second-pass architecture audit + R5 (dual truth fix) just landed. Apply the audit-driven-verify discipline strictly: each item is a hypothesis that must be verified against current code. **Action OR NO-ACTION with reasoning are both valid deliverables.**

## Backlog

| ID | Item | Source | Hypothesized ROI |
|----|------|--------|------------------|
| R1 | MainWindowViewModel composition refactor (collaborator boundary + constructor injection) | arch-review Outstanding + second-pass audit #5 | Large effort; now easier post-Stage 2 (8 partials in place) |
| R2 | `LauncherEntryVisibility → LauncherEntryQuery` rename | misc-cleanup deferred (84 refs verified 2026-07-09; 115 refs in another count) | Naming clarity vs churn |
| R3 | `Directory.Packages.props` LiteDB PackageVersion delete (1-line) | second-pass audit #1 | Noise removal |
| R4 | `AvaloniaUseCompiledBindingsByDefault=false` — add comment OR enable | second-pass audit #3 | Documentation / perf |
| ~~R5~~ | ~~`_settings` dual truth + SaveStoreFile duplication~~ | second-pass audit #4 | **COMPLETED 2026-07-09** (`07-09-settings-single-source`) |
| R6 | DI container introduction (`Microsoft.Extensions.DependencyInjection`) | second-pass audit #5 | Testability; deferred per audit |
| R7 | Sync File I/O async-ification on UI thread | second-pass audit #6 | Latency; no current issue at <50MB / <100 entries |

## Requirements

### Per-item investigation protocol (mandatory)

For each item R1/R2/R3/R4/R6/R7:

1. **Investigate** against actual code (grep + read)
2. **Cite modern practice** if rejecting (2025-2026 sources, one citation per rejection is enough)
3. **Decide**: action OR no-action (both are valid)
4. **Document conclusion** in the child task PRD + spec docs if non-trivial
5. **Commit + archive** with the conclusion as deliverable

### Grouping (3 children)

| Child | Items | Rationale |
|-------|-------|-----------|
| `trivial-cleanups` | R3 + R4 | <5 lines each, zero risk, immediate ROI |
| `rename-evaluation` | R2 | Mechanical rename; needs scope verification (84 vs 115 refs) |
| `composition-di-async-evaluation` | R1 + R6 + R7 | All large-scope items; expected NO-ACTION per audit but must verify |

## Acceptance Criteria

- [ ] All 6 backlog items have a documented conclusion (action OR no-action with reasoning)
- [ ] Each action item landed with tests passing + verify.ps1 green
- [ ] Each no-action item documented in spec docs (`.trellis/spec/`) to prevent re-litigation
- [ ] No speculative abstractions introduced
- [ ] All children archived under this parent

## Out of scope

- New architecture proposals (covered by 2026-07-09 deepsearch — 0 candidates passed verification)
- Adding any new dependencies
- Touching Layer 0 / LauncherJson persistence / LaunchArgumentBuilder security / ExceptionFilters (best-in-class, do not refactor)
