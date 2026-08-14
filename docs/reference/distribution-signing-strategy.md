# Distribution, Signing, and Update Strategy

Scope: win-x64 portable builds. Installer/update logic must never create a second config authority; `config/launcher.json` remains the only source of truth.

## Current State

- `tools/build-release.ps1` publishes a win-x64 AOT exe and now writes `update-manifest.json` containing version, file name, SHA-256, byte size, publish time, and notes.
- `tools/install-portable.ps1` installs a publish folder to `%LOCALAPPDATA%\Programs\WTLauncher`, creates Start Menu (and optional Desktop) shortcuts, writes `uninstall.ps1`, and preserves any user `config` folder during uninstall.
- The app exposes `管理 -> 检查更新`; it parses `update-manifest.json` through `LauncherUpdateManifestParser` and reports newer/up-to-date/error without downloading or overwriting config.

## Signing Policy

- Without a code-signing certificate, release builds remain unsigned and print the existing SmartScreen guidance. They must still build successfully.
- With a certificate, CI should sign `WTLauncher.App.exe` with `signtool sign` after publish, then write the signed hash into `update-manifest.json`.
- Recommended trusted-signing path: Azure Trusted Signing (OID-based, no private-key custody) or an EV code-signing cert (Sectigo, SSL.com, GlobalSign). Costs and CI rotation details are purchasing/environment decisions, not code assumptions.

## Update Channel Contract

- `update-manifest.json` schema: `{ schemaVersion: 1, version, fileName, sha256, bytes, publishedAtUtc, notes }`.
- `settings.updateManifestUrl` may point to an HTTP(S) URL or local file path. Empty means the check dialog reports the missing configuration instead of guessing a remote source.
- The update action remains explicit. The app detects and displays a newer version; it does not auto-download, replace files, or migrate `config/launcher.json`.

## Out of Scope

- Multi-platform installers.
- Cloud update backend, telemetry, forced updates.
- Registry/AppData configuration storage.
