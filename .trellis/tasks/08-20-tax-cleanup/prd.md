# 税项清理：六个维度简化

## Goal

Reduce maintenance tax across six dimensions (拓扑/契约/状态/数据/路径/验证) while preserving
all existing functionality and discipline. The project's doc+gate/src ratio is 0.53 (every 100
lines of code incur 53 lines of gate and docs). This task aims to bring it closer to 0.2 by
removing dead code, fixing wiring bugs, and collapsing abstractions that no longer carry their
weight.

## Requirements

### Must fix (behavioral bugs, no tolerance)

1. **ConfigExchangeService static facade wiring** — `ConfigExchangeService.LoadImport` calls
   `LauncherStore.Load` (uses `DefaultPersistence` null!), `SaveExport` calls `LauncherJson.Save`
   (uses `DefaultIO` null!). Both static fields are only set in `TestStartup.cs`. Production
   import/export always fails. Fix: remove the static facades, wire `ConfigExchangeService` to use
   `FileStorePersistence` / `FileLauncherJsonIO` directly.

2. **runInTray null default bug** — `LauncherSettingsDefaults.cs:84` `RunInTray = !presentKeys.Contains(RunInTrayJsonKey) || settings.RunInTray`
   returns `false` for explicit `null`, while docs say null should fall back to `true`. All other
   optional fields use `value ?? DEFAULT` pattern correctly. Fix: align with the same pattern.

### Should clean (dead code, no behavioral impact)

3. **Remove dead code** — `AppError.cs` (42 lines, 0 production references) and its `OneOf` dependency.
   The project uses `ErrorOr` everywhere; `AppError` is a leftover from a past architecture.

4. **Remove stale schema fields** — `searchHistory` (persisted but never rendered in UI, documented
   as dead data), `runHistory` (persisted, only count visible in UI, no user action path),
   `lastFilterText` (UI state leaking into durable storage). These add schema complexity,
   serialization cost, and migration burden for zero user-facing value.

### Could simplify (architecture, low risk)

5. **Collapse static facades** — `LauncherStore` and `LauncherJson` are static facades that
   redundantly wrap `IStorePersistence` and `ILauncherJsonIO`. After fixing #1, these facades
   only exist as test-time convenience. Remove them and let all callers use the injected
   interfaces directly.

6. **Collapse `Grain<T>` into `StoreGrain`** — 51-line generic base class with a single concrete
   subclass. The abstraction adds no value; `StoreGrain` can own the lock directly.

### Documentation cleanup

7. **Fix stale docs** — `LauncherSettings.cs` comment says `UnmappedMemberHandling=Disallow`
   (actually Loose Read). Archive `docs/feature-first-class-upgrade-audit.md` (unverifiable hash,
   overtaken by events). Add deprecation note to `docs/trinity-endgame-architecture.md` and
   `docs/architecture-reform-proposal.md`. Remove `checkClaimBoundaryLedger` and
   `checkFeatureObjectiveBoundary` gate checks.

## Acceptance Criteria

- [x] All existing tests pass: `dotnet test -c Release --no-restore` = 847 PASS
- [x] Full gate passes: `$env:CI='true'; npm run verify` = all checks PASS
- [x] No `dotnet build -warnaserror` warnings
- [x] `ConfigExchangeService` static facade removed: `LauncherStore.DefaultPersistence` and
      `LauncherJson.DefaultIO` no longer exist
- [x] `runInTray: null` correctly defaults to `true`
- [x] `AppError` and `OneOf` removed from source and project references
- [x] `searchHistory`, `runHistory`, `lastFilterText` removed from `LauncherSettings` and
      `LauncherStoreFile`
- [x] `Grain<T>` removed, logic inlined into `StoreGrain`
- [x] Static facades removed: `LauncherStore.cs` and `LauncherJson.cs` deleted or reduced to
      thin wrappers
- [x] Stale docs comments fixed, obsolete architecture docs archived
- [x] Gate check count reduced appropriately

## Non-goals

- Do not change the `config/launcher.json` file format version (keep schemaVersion=2)
- Do not change the store file on disk — existing configs must be readable
- Do not remove `LauncherDesignTokens` or any UI-facing contract
- Do not change the 5-layer architecture structure
- Do not add new features or refactor beyond the scope listed above