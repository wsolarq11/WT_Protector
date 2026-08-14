# 分发与更新：安装器 / 签名 / 更新通道 — Design

## Change Boundary

- 当前发布是 zip + 未签名 exe + SmartScreen 文档引导；没有可解析版本清单或应用内更新检查。
- 目标：增加便携安装器、更新清单生成与解析、应用内显式检查更新，并记录签名策略；不改变便携配置语义。

## Data Flow

```text
tools/build-release.ps1
  -> artifacts/publish/win-x64/WTLauncher.App.exe + update-manifest.json (version/sha256/bytes)
  -> tools/install-portable.ps1 (shortcuts + uninstall.ps1 + config-preserve)
  -> 管理 -> 检查更新
  -> UpdateCheckService -> LauncherUpdateManifestParser
  -> UpdateCheckWindow (newer / up-to-date / error)
```

## Key Decisions

- 安装器采用 PowerShell 便携脚本，不引入 Inno/NSIS 构建依赖；创建开始菜单/桌面入口并生成卸载脚本。
- 更新清单是元数据：应用只检测和展示，不自动下载/覆盖文件；`config/launcher.json` 永不被更新器改写。
- 签名策略写入 `docs/reference/distribution-signing-strategy.md`；无证书时构建仍成功，仅输出未签名说明。
- `settings.updateManifestUrl` 为可选字段；空值时检查窗口明确提示未配置。

## Files Expected to Change

- Core: `LauncherUpdateManifest.cs`、`LauncherJsonContext.cs`、`LauncherSettings.cs`
- App: `UpdateCheckService.cs`、`UpdateCheckViewModel.cs`、`UpdateCheckWindow.*`、`DialogService.cs`、`MainWindowViewModel.Update.cs`、`LauncherControlBar.axaml`
- Tooling: `tools/build-release.ps1`、`tools/install-portable.ps1`、`tools/verify.ps1`
- Docs: `README.md`、`docs/reference/distribution-signing-strategy.md`
- Tests: manifest parser, update service, VM dialog, settings/JSON contract, UI menu contract

## Explicitly Not Doing

- 不做多平台安装器。
- 不做云更新后端、遥测、强制更新。
- 不把配置迁移到安装器管理的注册表/AppData。
