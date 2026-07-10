# P7: CompiledBindings decision re-check

## Goal

Re-verify the 07-09 decision to keep `AvaloniaUseCompiledBindingsByDefault=false`.
The original comment cited "~30+ DataTemplates, 100 build errors" without
re-confirming the numbers in today's codebase, and without weighing the
cost/benefit after the 07-09 → 07-10 modernization wave.

## Decision

**Keep `false`.** The 07-09 finding is re-confirmed by clean-room measurement
on the current codebase. ROI is negative for this app.

## Re-measurement (2026-07-10)

Set `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`
in `WTLauncher.App.csproj`, cleared `obj/` + `bin/`, and rebuilt:

- **Errors**: 100 (mix of AVLN2000 "Unable to resolve property" and AVLN2100
  "Cannot parse a compiled binding without an explicit x:DataType directive").
- **Root cause**: `MainWindow.axaml` has ~30 `<DataTemplate>` blocks, none
  declare `x:DataType`. Without it, the XAML compiler falls back to
  `XamlPseudoType` and every bound member becomes unresolvable.

## Cost / benefit

| | Enable CompiledBindings | Keep reflection |
|---|---|---|
| Initial work | Add `x:DataType` to ~30 DataTemplates, fix 100 errors | 0 |
| Regression risk | GUI smoke required; 453 unit tests don't cover all binding shapes | 0 |
| Runtime perf gain | Marginal (sub-second startup, ≤200 entries) | — |
| Compile-time binding typo checks | Yes (new benefit) | No |
| Native AOT (P6) unblock | Yes | No (P6 also deferred) |

The only real benefit is compile-time binding validation — which is nice but
not free given the regression surface.

## Trigger to revisit

Re-enable CompiledBindings if any of these becomes true:

1. Entry count grows past ~10× current (~2,000+ entries) and binding
   enumeration cost shows up in a profiler.
2. A startup-time budget is introduced that CompiledBindings is the cheapest
   way to meet.
3. Native AOT becomes a real requirement (would still need this work first).

## Out of scope

- The actual 100-error churn. Revisit when one of the triggers above fires.

## Acceptance criteria

- [x] `<AvaloniaUseCompiledBindingsByDefault>false</...>` retained with an
      updated `ponytail:` comment referencing this re-check.
- [x] `dotnet build -c Release` 0/0.
- [x] `dotnet test -c Release` 453/453 pass.

## Sources

- [Compiled bindings - .NET MAUI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/compiled-bindings?view=net-maui-10.0)
- [XAML compilation | Avalonia Docs](https://docs.avaloniaui.net/docs/xaml/compilation)
- [Compiled Bindings Pitfalls · AvaloniaUI/Avalonia · Discussion #19184](https://github.com/AvaloniaUI/Avalonia/discussions/19184)
