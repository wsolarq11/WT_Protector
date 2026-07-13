# Implementation plan

## Preparation

- [x] Link scope and evidence with `07-12-fma-p2-services` and `07-12-fma-p3-thin-vm`; update their PRDs so there is one implementation path.
- [x] Load App/ViewModel, Core boundary, DI/composition and test specs.
- [x] Use codebase-memory to trace preset, selection, layout/filter and launch hotspots before each extraction.

## Slice 1 — Explicit service injection and presets

- [x] Move concrete Entry/Preset service construction to composition root and keep tests source-compatible through explicit helpers.
- [x] Finish routing preset dialog/CRUD/eligibility/reference/preview behavior through `IPresetService`; ViewModel retains state apply, transaction and notifications only.
- [x] Add/extend PresetService contract tests.

## Slice 2 — Existing selection/layout owners

- [x] Reuse selection snapshot/preview owners; do not pass the whole ViewModel or add a forwarding service.
- [x] Keep filter/sort in `LauncherEntryQuery` and extract the missing search-history transformation to Core.
- [x] Keep observable collection diffing and notification ordering in ViewModel.
- [x] Add direct service/Core tests plus existing ViewModel regression tests.

## Slice 3 — Launch ownership audit

- [x] Confirm injected `LauncherLaunchWorkflow` remains the plan/preflight/execution owner; do not wrap it with a duplicate service.
- [x] Keep UI cancellation, status and receipt mapping in ViewModel until CQ-06 supplies a typed result boundary.
- [x] Run single/batch launch safety and save-failure tests through the complete suite.

## Ownership audit

- [x] Trace direct Core workflow/calculator calls remaining in `MainWindowViewModel.*`; document why each retained call is composition rather than business ownership.
- [x] Confirm no new partial, dependency, storage path or second mutable aggregate.
- [x] Add architecture/service contract checks preventing Views/File dependencies.

## Quality gates

- [x] Focused tests after every slice.
- [x] `trellis-check` and required spec update.
- [x] Full `dotnet test -c Release`.
- [x] `git diff --check` and `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`.
- [ ] Commit/archive CQ-01 and reconcile/archive the reused FMA Phase 2/3 tasks.
