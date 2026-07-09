# Parent PRD: Schema-Driven Architecture Migration

## Source
Follow-up to the goal "深度全网deepsearch调研出新架构来剪掉一切问题的源头，而不是仅仅是修复". Research delivered 13 bug-generating sources; 11 are addressable by migrating to a 5-layer Schema-Driven Architecture. This parent task tracks the migration across 8 independently-verifiable child tasks.

## Bug sources being cut (11/13)
- **S1** Multi-layer validation drift → **Misidentified**: value-object `Parse` (EntryId/EntryName/PresetId) already implements Parse-Don't-Validate; no drift exists. **No action.** (T5)
- **S2** Schema implicit in C# types → **Misidentified**: C# records ARE the schema for app-internal persistence; Corvus.JsonSchema migration rejected (would create second source of truth). **No action.** (T7)
- **S3** SchemaVersion magic constant → **Partially valid**: dead `LauncherSettings.SchemaVersion` field removed; top-level `ExpectedStoreSchemaVersion=2` const kept (correct for offline portable app, not `$schema` URL). (T7)
- **S4** Exception-based control flow → **Misidentified**: catch sites are error boundaries (not control flow); 2025 modern .NET practice confirms exceptions are correct for UNEXPECTED technical errors in single-user desktop persistence. **No action.** (T5)
- **S6** Hand-rolled atomic I/O → Confirmed sufficient (no change, decision recorded)
- **S9** Docs/code drift → **Already cut**: T2's verify.ps1 negative gate enforces no three-file references. **No action.** (T7)
- **S10** Flat settings record → **Misidentified**: `LauncherSettings` already contains only UI state; business state lives at `LauncherStoreFile` top level. **No action.** (T4)
- **S11** Multi-process file contention → **Already cut**: `Local\WTLauncher_ConfigMutex` in `Program.cs:71-105` (Mutex + AbandonedMutexException handling + best-effort fallback). No Named Pipe IPC added: no CLI args need forwarding (ponytail: speculative abstraction). **No action.** (T6)
- **S12** Hand-rolled migration → **Misidentified**: only ONE migration exists (`MigrateFromLegacyFiles`, one-time legacy→unified, already executed on all installations). No v3 schema bump planned. Registry pattern would be "interface with one implementation" (ponytail forbidden). Deferred until v3 breaking change actually required. **No action.** (T8)
- **S13** UI/business state entanglement → **Misidentified**: UI/business state already separated at data-structure level (Settings nested, Entries/Presets/DefaultPresetId at top). Single-file atomic write beats multi-file split. **No action.** (T4)
- ShellCommand 62-line dead class + LiteDB PackageVersion orphan → T1

## Sources NOT cut (2/13, no architectural solution)
- **S5** Record copy propagation drift — C# language limitation
- **S7** Sync I/O — ROI too low for single-user desktop scale

## Child task execution order (smallest/safest first)
1. **T1 07-09-remove-dead-code** — ShellCommand + LiteDB PackageVersion
2. **T2 07-09-stale-strings-remediation** — Doc cleanup + verify.ps1 negative gate
3. **T3 07-09-enable-compiled-bindings** — Verify XAML then enable or document why-off
4. **T4 07-09-split-settings** — ✅ Closed: S10/S13 misidentified, no action needed (investigation recorded)
5. **T5 07-09-result-types-migration** — ✅ Closed: S1/S4 misidentified, current exception flow is 2025 modern practice (investigation recorded)
6. **T6 07-09-single-instance-lock** — ✅ Closed: mutex already cuts S11; no CLI args need IPC forwarding (investigation recorded)
7. **T7 07-09-schema-first-persistence** — ✅ Closed: Schema-First migration rejected (C# records ARE the schema); real dual-SchemaVersion bug fixed (dead field removed, 4 mutation sites cleaned, 2 new pin tests)
8. **T8 07-09-schema-migration-registry** — ✅ Closed: deferred until v3 breaking change (S12 misidentified; only one-shot migration exists)

## Cross-child acceptance criteria
- After EACH child: `dotnet build -c Release` 0 warnings 0 errors + `dotnet test` 451+ pass
- After EACH child: commit on main (no long-lived branches)
- Parent complete when all 8 children archived AND final integration review confirms:
  - launcher.json schema-driven (single source of truth)
  - ExceptionFilters.IsRecoverable removed from production paths
  - SchemaVersion 3-way inconsistency resolved
  - verify.ps1 enforces no three-file references in docs
  - Single-instance behavior verified
  - No regressions in 451-test suite

## Final integration review (2026-07-09, all 8 children archived)

### What was actually delivered

| Task | Verdict | Code change |
|------|---------|-------------|
| T1 remove-dead-code | Real fix | ShellCommand class deleted, LiteDB PackageVersion orphan removed |
| T2 stale-strings-remediation | Real fix | Doc cleanup (15+ refs) + verify.ps1 negative gate |
| T3 enable-compiled-bindings | Investigation | CompiledBindings stays off (documented why) |
| T4 split-settings | Closed misidentified | No code change (Settings already correct shape) |
| T5 result-types-migration | Closed misidentified | No code change (exceptions are 2025 modern practice) |
| T6 single-instance-lock | Closed already-cut | No code change (Mutex already exists, no IPC needed) |
| T7 schema-first-persistence | Mixed | Schema-First rejected; dead `LauncherSettings.SchemaVersion` removed, 4 mutation sites cleaned, 2 pin tests added |
| T8 schema-migration-registry | Closed misidentified | No code change (deferred until v3 breaking change) |

### Scorecard vs original audit claim (11 sources)

- **2 real fixes:** S3 (dual-version drift), ShellCommand dead code + LiteDB orphan
- **2 already-cut:** S9 (T2 gate), S11 (existing Mutex)
- **7 misidentified:** S1, S2, S4, S6, S10, S12, S13

### Cross-child acceptance criteria status

- [x] All 8 children archived on main
- [x] `dotnet build -c Release` 0 warnings 0 errors (verified after each task)
- [x] `dotnet test` 453 tests passing (451 baseline + 2 new T7 pin tests)
- [x] `tools/verify.ps1` 16/16 checks PASS, including new `no-stale-three-file-refs` gate
- [x] SchemaVersion 3-way inconsistency resolved (T7: dead `LauncherSettings.SchemaVersion` removed, single source at top level)
- [x] verify.ps1 enforces no three-file references in docs (T2 gate)
- [x] Single-instance behavior verified (T6: existing Mutex re-confirmed)
- [x] No regressions in test suite (453/453 pass)
- [~] **`launcher.json` schema-driven** — adjusted: C# records ARE the schema (single source), Corvus.JsonSchema migration rejected
- [~] **`ExceptionFilters.IsRecoverable` removed from production paths** — adjusted: kept; 2025 modern practice confirms exceptions are correct for UNEXPECTED technical errors

### Ponytail outcome

The original premise — "5-layer Schema-Driven Architecture migration cuts 11 bug sources" — was largely incorrect. 9 of 11 sources were misidentified or already-cut. The 2 real fixes were small (dead code + dual-version field). The audit-driven "cut every source" approach would have introduced MORE complexity (Corvus.JsonSchema dependency, broken `record with { }` patterns, registry abstractions with one implementation, named-pipe IPC without args to forward) without cutting bugs.

The lazy senior developer move was: investigate each source, reject the speculative ones with evidence, fix the real ones at root cause, document the reasoning in spec to prevent future re-litigation.

## Out-of-scope (explicitly deferred)
- MainWindowViewModel further splitting (decision: don't touch, 8 partials sufficient)
- Microsoft.Extensions.DependencyInjection (decision: keep manual wiring, ROI low)
- async I/O migration (decision: keep sync, ROI low)
- ReactiveUI migration (decision: CommunityToolkit.Mvvm sufficient)
- Corvus.JsonSchema migration (decision: C# records ARE the schema)
- SchemaMigrationRegistry (decision: deferred until v3 breaking change)
- Result<T, StoreError> (decision: exceptions are 2025 modern practice for unexpected errors)
- Named Pipe IPC for single-instance (decision: no CLI args need forwarding)

## Autonomy contract
Per goal directive "创建后无需确认" — each child proceeds without inter-child confirmation. Decision points resolved via web research, defaulting to most-modern-practice solution. Exceptions logged in child prd.md.
