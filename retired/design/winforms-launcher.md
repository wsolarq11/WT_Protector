# Retired WinForms launcher design

Status: retired on 2026-05-17.

This document absorbs the useful design lessons from the former root `WT Launcher.exe` and `src/winforms-launcher/Program.cs` before those implementation artifacts were deleted. The active code line is promoted from the Avalonia launcher into `src/avalonia-launcher/`.

## Source receipt

| Artifact | Former path | SHA-256 | Bytes |
| --- | --- | --- | ---: |
| Built WinForms executable | `WT Launcher.exe` | `A3A575648D93679C0106A6D654BA91BE4A39CE4AFAF0464C87414C7BA1426C11` | 43008 |
| WinForms source | `src/winforms-launcher/Program.cs` | `40A2609F8D49F5A18811B50857AE2EB08EF7910DCD825214CE00DFC145145F1F` | 75324 |

The executable was a compiled snapshot, not an editable source of truth. The source was a single large `.NET Framework` WinForms file that had become harder to evolve than the Avalonia line.

## What was worth keeping

1. **Portable config discovery**
   - Prefer repository-root `config/config.json` beside `README.md`.
   - Fall back to executable-local `config/config.json` for standalone copies.
   - Keep UI preferences in sibling `config/settings.json` instead of AppData or registry.

2. **Human-readable launch contract**
   - Launch items use stable `id`, display `name`, `directory`, `command`, `confirm`, and persistent `selected`.
   - `selected` is real state, not throwaway UI state; one-click launch should respect it across restarts.

3. **Launch safety boundary**
   - Validate that `directory` exists before launch.
   - Reject empty commands.
   - Quote `wt.exe -d ... pwsh -NoExit -Command ...` arguments centrally.
   - Keep `dangerously*` commands behind confirmation.

4. **HTA-derived low-friction UX lessons**
   - Reduce chrome.
   - Keep cards readable.
   - Truncate long paths/commands with ellipsis instead of layout shifting.
   - Do not move cards on hover.
   - Keep batch selection visible and persistent.

5. **Verification habits**
   - A root `tools/verify.ps1` should be the native no-interaction gate.
   - The gate should prove config parsing, dangerous-command hardening, launch argument rules, and publish/build viability.

## Why the implementation retired

- One 75 KB `Program.cs` mixed config IO, launch quoting, rendering, layout, theme, edit dialogs, batch state, and persistence.
- The UI was hand-painted with WinForms/GDI, so small comfort improvements required imperative redraw/layout work.
- Reflection-based smoke tests around the compiled exe were useful but indirect.
- The root executable was a generated artifact; keeping it as a primary path made source ownership less clear.

## Successor directive

The Avalonia line should keep the contracts above, but implement them as separated Core/App/Tests surfaces:

- config and launch rules in Core;
- UI state and commands in view models;
- dialogs/views in App;
- tests around Core contracts and repository config behavior;
- publish artifacts under ignored `artifacts/`, not as tracked root binaries.

Do not restore the WinForms source or root `WT Launcher.exe` unless the Avalonia line fails the same native gate and an explicit rollback decision is made.
