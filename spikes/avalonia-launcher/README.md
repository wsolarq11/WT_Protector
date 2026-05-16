# Avalonia Launcher Spike

Purpose: prove whether a modern Avalonia/.NET launcher can beat the current WinForms/GDI executable on comfort, clarity, and maintainability without replacing the active path.

This spike is **not** the active launcher. The active executable remains:

```text
WT Launcher\WT Launcher.exe
```

## Run for experience

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\spikes\avalonia-launcher\run.ps1
```

The app reads the repository-root `config.json` and shows launch entries as large searchable cards.

## Verify

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\spikes\avalonia-launcher\verify.ps1
```

The local spike gate runs:

1. `dotnet restore`
2. `dotnet test -c Release`
3. `dotnet publish -c Release -r win-x64 --self-contained true`

It does not replace the active WinForms executable.

## Acceptance question

Keep this spike only if it clearly feels better than WinForms while keeping the same or stronger launch/config safety. If it does not win, archive it.

