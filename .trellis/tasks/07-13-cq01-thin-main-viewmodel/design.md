# Design

## Target ownership

```text
Composition root
  └─ constructs feature services
       └─ MainWindowViewModel
            ├─ owns bindable state / collections / commands
            ├─ maps service outputs into observable state
            └─ invokes CQ-02 persistence transaction

Feature services and existing owners
  ├─ Entries / Presets: dialog + validation + Core workflow
  ├─ Preset service: eligibility, reference and selection-preview rules
  ├─ Existing Core owners: filter/sort and search-history transformations
  └─ Existing workflows: launch and drag state machines

Core
  └─ data records, normalization, calculators, workflows, persistence gateway
```

## Extraction rule

A method moves only when its inputs and outputs can be expressed without reading private ViewModel fields or raising property notifications. If extraction would require passing the whole ViewModel or a bag of callbacks, the seam is not ready and the method remains an orchestrator.

Services are stateless and receive snapshots/collections as method parameters. They return updated immutable Core values or small App-layer operation records. ViewModel applies results, refreshes dependent observable projections, and owns persistence/rollback.

## DI compatibility

Extend the existing constructor/service-registration path rather than add a container abstraction. Keep one convenience constructor only if tests or application startup require it; production construction must pass interfaces explicitly. Defaults must be created in the composition root, not hidden inside the ViewModel.

## Migration slices

1. Finish PresetService adoption and remove duplicated workflow/catch logic from Presets partial.
2. Keep selection mutation beside bindable entry flags while reusing the existing snapshot and preview owners; do not add a callback-heavy service.
3. Reuse `LauncherEntryQuery` for filter/sort and extract the missing search-history transformation to Core; keep collection diff application and property notifications in ViewModel.
4. Retain injected `LauncherLaunchWorkflow` as the launch owner; keep cancellation, status and receipt projection in ViewModel until CQ-06 defines typed results.
5. Audit all retained calls and record why each is composition rather than business ownership.

## Error and persistence flow

Services return existing status-compatible outcomes for this task. ViewModel performs:

```text
capture → call service/apply result → LauncherMutationTransaction.Commit
        → on failure restore full family snapshot → publish original save error
```

No service calls `LauncherStore` or `System.IO.File`.

## Trade-offs

- No arbitrary line-count target: short files can still own too much, and generated bindings make some orchestration necessarily verbose.
- No mediator/event bus: direct constructor-injected services are simpler and fully testable for a single-window desktop application.
- No service-owned mutable aggregate: that would create a second source of truth beside ViewModel binding state and `launcher.json`.

## Rollback

Each migration slice preserves the existing command surface and has focused tests. A failing slice can revert to its previous ViewModel orchestration without changing schema or other service contracts.
