# Distribution, Signing, and Update Strategy

> Verified 2026-08-15 — 依据 .trellis/tasks/08-15-docs/verification-protocol.md 逐项查证后重写；以下脚本/代码/配置声明均已对照工作树核实。

Scope: win-x64 portable builds. Installer/update logic must never create a second config authority; `config/launcher.json` remains the only source of truth.

## Current State

- `scripts/build-release.ts` publishes a win-x64 Native AOT exe (`dotnet publish … -r win-x64 -p:EnableNativeAot=true`) to `src/avalonia-launcher/artifacts/publish/win-x64/` (per `scripts/config-lib.ts` `publishDir`) and writes `update-manifest.json` containing `schemaVersion`, `version`, `fileName`, `sha256`, `bytes`, `publishedAtUtc`, and `notes`. A `receipt.json` is written alongside.
- `npm run install:portable` installs the publish folder to `%LOCALAPPDATA%\Programs\WTLauncher`, creates a Start Menu shortcut (plus a Desktop shortcut with `--desktop`), deploys a .NET self-contained `uninstaller.exe`, and on uninstall backs up any user `config` folder to `%USERPROFILE%\Documents\WTLauncher-config-backup\config-<timestamp>` before removing the install directory.
- The app exposes `管理 -> 检查更新` (`Views/LauncherControlBar.axaml`); it parses `update-manifest.json` through `LauncherUpdateManifestParser` (`src/WTLauncher.Core/LauncherUpdateManifest.cs`) and reports 发现新版本 / 当前已是最新版本 / 检查失败 without downloading or overwriting config.

## Signing Policy

- Without a code-signing certificate, release builds remain unsigned. The SmartScreen bypass guidance (更多信息 → 仍要运行) is documented in `README.md` and the release `使用说明.txt`; the build must still succeed (the gate runs the publish).
- With a certificate, the intended future step is to sign `WTLauncher.App.exe` after publish (e.g. `signtool sign`) and write the signed hash into `update-manifest.json`. **Status: proposal only — verified 2026-08-15, no signing/signtool code exists in `scripts/` or `.github/workflows/`**, and `scripts/build-release.ts` currently records the SHA-256 of the unsigned build artifact.
- Recommended trusted-signing path: Azure Trusted Signing (OID-based, no private-key custody) or an EV code-signing cert (Sectigo, SSL.com, GlobalSign). External reference — these are purchasing/environment decisions with no local code evidence; costs and CI rotation details are not code assumptions.

## Update Channel Contract

- `update-manifest.json` schema: `{ schemaVersion: 1, version, fileName, sha256, bytes, publishedAtUtc, notes }` — matches the `LauncherUpdateManifest` record (`SchemaVersion == 1` enforced by `LauncherUpdateManifestParser`, plus required-field validation) and the writer in `scripts/build-release.ts`.
- `settings.updateManifestUrl` may point to an HTTP(S) URL or a local file path (`UpdateCheckService`: http/https via `HttpClient`, otherwise `File.ReadAllTextAsync`). Empty means the check dialog reports the missing configuration ("未配置更新清单地址；可在 launcher.json 的 settings.updateManifestUrl 设置。") instead of guessing a remote source — currently `config/launcher.json` has `updateManifestUrl: ""`.
- The update action remains explicit. The app detects and displays a newer version; it does not auto-download, replace files, or migrate `config/launcher.json`. The gate protects the contract: `update-manifest-generation` and `portable-installer-contract` checks in `scripts/verify.ts`.

## Out of Scope

- Multi-platform installers.
- Cloud update backend, telemetry, forced updates.
- Registry/AppData configuration storage.
