
# Avalonia Launcher Mainline

This is the active source line for WT Launcher. It was promoted from the former spike after the WinForms executable/source line was retired into `retired/design/winforms-launcher.md`.

## Run for experience

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1
```

The app reads the repository-root `config/config.json`, persists selected checkboxes there, and stores portable UI preferences in `config/settings.json`. The header batch controls can select all visible filtered entries, clear the saved selection, or one-click launch the saved checked set.

## Verify

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

Or run only the Avalonia mainline gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\verify.ps1
```

The local mainline gate runs:

1. `dotnet restore`
2. `dotnet test -c Release`
3. `dotnet publish -c Release -r win-x64 --self-contained true`

Publish output is ignored under `artifacts/publish/win-x64/`.

## Current acceptance question

This line has replaced the WinForms source tree as the mainline. Further work should improve the Avalonia UX directly while preserving the root `tools/verify.ps1` gate and the portable config contract.
