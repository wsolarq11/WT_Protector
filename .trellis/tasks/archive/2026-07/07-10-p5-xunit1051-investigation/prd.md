# P5: xUnit1051 / xUnit1031 root cause investigation

## Goal

Audit the test project's `<NoWarn>xUnit1031;xUnit1051;CS0618</NoWarn>` block in
`WTLauncher.Tests.csproj`. The two xUnit suppressions are suspected dead code
inherited from an earlier test layout — verify each is still needed, remove
those that aren't, and document why the remaining ones stay.

## Investigation findings

- **xUnit1031** ("Do not disable parallelization for collection"):
  No `[Collection(...)]` declarations exist anywhere in the test tree.
  → Suppression was dead code, removed.
- **xUnit1051** (xunit.v3 redefined this rule — "Calls to methods which
  accept CancellationToken should use TestContext.Current.CancellationToken"):
  After removing the suppression, 5 sites fired:
  - `LauncherBatchLaunchReceiptTextTests.cs:27, 61, 78`
  - `LauncherLaunchWorkflowTests.cs:98, 208`

  All 5 are `workflow.StartBatch(preflight)` calls. The `StartBatch` overload
  at `LauncherLaunchWorkflow.cs:73` accepts a `CancellationToken` (defaulted
  to `default`). xunit.v3 best practice: pass `TestContext.Current.CancellationToken`
  so the test runner can cancel a hung batch-launch path promptly.
  → Fixed all 5 sites to pass `TestContext.Current.CancellationToken`.
- **CS0618** (Obsolete):
  11 test call-sites reference `LaunchPlan.CommandLine` (marked `[Obsolete]`
  in `LaunchPlan.cs:29`). These tests deliberately exercise the deprecated
  plaintext API for backward-compat verification.
  → Suppression stays, with an inline comment naming the reason.

## Requirements

- Remove `xUnit1031` from `<NoWarn>` (dead code).
- Remove `xUnit1051` from `<NoWarn>` and fix the 5 violating call-sites by
  passing `TestContext.Current.CancellationToken` to `StartBatch`.
- Keep `CS0618`, add an inline XML comment naming the deprecated API.

## Out of scope

- Migrating tests off `LaunchPlan.CommandLine` (separate backward-compat task).

## Acceptance criteria

- [ ] `<NoWarn>` only contains `CS0618`, with a comment.
- [ ] All `StartBatch(preflight)` test call-sites pass `TestContext.Current.CancellationToken`.
- [ ] `dotnet build -c Release` 0/0.
- [ ] `dotnet test -c Release` 453/453 pass.
