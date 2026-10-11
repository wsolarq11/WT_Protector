Status: External
Purpose: win-x64 便携发布、签名与更新策略契约。
Update when: 发布路径、manifest schema、签名或更新策略变化时。

# 发布、签名与更新策略

范围：win-x64 便携构建。安装器/更新逻辑不得创建第二配置权威，`config/launcher.json` 是唯一事实源。

## 当前状态

- `scripts/build-release.ts` 发布 win-x64 Native AOT exe（`dotnet publish … -r win-x64 -p:EnableNativeAot=true`）到
  `src/avalonia-launcher/artifacts/publish/win-x64/`（`publishDir` 定义于 `scripts/config-lib.ts`），并写
  `update-manifest.json`（schemaVersion、version、fileName、sha256、bytes、publishedAtUtc、notes）与 `receipt.json`。
- `npm run install:portable` 安装到 `%LOCALAPPDATA%\Programs\WTLauncher`，建开始菜单快捷方式（`--desktop` 加桌面快捷方式），
  部署自包含 `uninstaller.exe`；卸载前备份 config 到 `%USERPROFILE%\Documents\WTLauncher-config-backup\config-<timestamp>`。
- 应用内 `管理 -> 检查更新`（`Views/LauncherControlBar.axaml`）经 `LauncherUpdateManifestParser` 解析
  `update-manifest.json`，报告 发现新版本 / 当前已是最新版本 / 检查失败，不下载、不覆盖 config。

## 签名策略

- 无代码签名证书时发布产物保持未签名；SmartScreen 绕过（更多信息 → 仍要运行）记于 `README.md`；
  门禁仍要求 publish 成功。
- 有证书时计划在 publish 后对 `WTLauncher.App.exe` 用 `signtool sign` 签名并把签名哈希写入 `update-manifest.json`。
  此步为提案：`scripts/` 与 `.github/workflows/` 无 signtool 代码；`build-release.ts` 记录的是未签名产物 SHA-256。
- 推荐签名路径：Azure Trusted Signing（OID，不持有私钥）或 EV 代码签名证书（Sectigo、SSL.com、GlobalSign）。

## 更新通道契约

- `update-manifest.json` schema：`{ schemaVersion: 1, version, fileName, sha256, bytes, publishedAtUtc, notes }`，
  对应 `LauncherUpdateManifest`（`SchemaVersion == 1` 由 `LauncherUpdateManifestParser` 强制）。
- `settings.updateManifestUrl` 可为 HTTP(S) URL 或本地文件路径（`UpdateCheckService`：http/https 走 `HttpClient`，
  否则 `File.ReadAllTextAsync`）。空值时对话框提示未配置，不猜测远端。
- 更新动作保持显式：只检测并显示新版本，不自动下载/替换/迁移 `config/launcher.json`。门禁
  `update-manifest-generation` 与 `portable-installer-contract` 保护该契约。

## 不在范围

- 多平台安装器。
- 云端更新后端、遥测、强制更新。
- Registry/AppData 配置存储。
