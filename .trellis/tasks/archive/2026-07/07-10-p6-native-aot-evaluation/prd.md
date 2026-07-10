# P6: Native AOT feasibility evaluation

## Goal

Decide whether to enable .NET 10 Native AOT publish for the launcher. The
answer must be grounded in this project's actual perf budget, dependency
shape, and the cost of the prerequisite work — not generic "AOT is modern"
framing.

## Decision

**Defer.** Native AOT is technically supported by the stack but blocked by
the same CompiledBindings gate that P7 reassesses. Even if P7 enables
CompiledBindings, AOT's payoff for a single-user desktop launcher is marginal
versus the migration cost. Revisit only if a measured startup-time or
cold-launch budget demands it.

## Evaluation

### Supporting factors
- Target is already `net10.0` + `win-x64` self-contained (AOT-friendly baseline).
- Avalonia 11+ officially supports Native AOT for desktop publish.
- All views have public constructors (verified: `MainWindow.axaml.cs:54`).
- No dynamic XAML loading at runtime (all views compiled at build).
- CommunityToolkit.Mvvm 8.4 source generators are AOT-friendly.

### Blocking factors
- **CompiledBindings=false** (`WTLauncher.App.csproj:11`): AOT requires
  CompiledBindings to avoid reflection-based binding — the runtime loader
  path is otherwise AOT-incompatible. The csproj comment records that turning
  it on produces ~100 build errors across ~30 DataTemplates in
  `MainWindow.axaml` (see task 07-09-enable-compiled-bindings). P6 therefore
  cannot ship without P7 first landing.
- Cold-launch perf budget is already sub-second for ≤200 entries; the
  benefit/risk ratio is upside-down for a single-user launcher.
- Native AOT publish adds ~15-30s to CI wall time and demands the Visual
  Studio C++ desktop workload on the build agent (already present on
  `windows-latest`, but worth noting).

### Expected benefit if pursued (not a commitment)
- ~30-50% startup time reduction (sub-second → faster sub-second).
- ~30% smaller publish artifact (less disk; but launcher already small).
- No JIT at runtime → marginally lower RSS after first paint.

### Cost
- Block on P7: add `x:DataType` to ~30 DataTemplates, fix ~100 build errors.
- Add `<PublishAot>true</PublishAot>` to App.csproj.
- Resolve any reflection warnings surfaced by the AOT analyzer
  (`IlcDiagnosticIdentifiers`), likely in JSON serialization (System.Text.Json
  source generators) and in `LauncherStore` schema validation.
- Add AOT smoke test to `verify.ps1`.

## Out of scope

- Implementing AOT now. This task is evaluation-only.
- Source-generating JSON serializers (`JsonSerializerContext`) — required only
  if AOT is pursued.

## Acceptance criteria

- [x] Evaluation recorded in this `prd.md`.
- [x] Decision documented ("Defer, blocked on P7").
- [x] No code changes (verify gate still 0/0, 453/453).

## Sources

- [Native AOT Deployment | Avalonia Docs](https://v11.docs.avaloniaui.net/docs/deployment/native-aot)
- [Native AOT deployment overview - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [AvaloniaUI Native AOT Deployment on Windows - DEV Community](https://dev.to/chuongmep/avaloniaui-native-aot-deployment-on-windows-2jg1)
