# 分发与更新：安装器 / 签名 / 更新通道 — Implement

## Order

1. Core: `LauncherUpdateManifest` + parser + JSON 注册 + settings 字段。
2. `UpdateCheckService` + `UpdateCheckViewModel` + `UpdateCheckWindow` + 管理菜单。
3. `tools/build-release.ps1` 生成 `update-manifest.json`。
4. `tools/install-portable.ps1` 安装/卸载/保留 config。
5. `tools/verify.ps1` 增加安装器与 manifest 契约检查。
6. 文档与签名策略。
7. build + dotnet test + Avalonia verify。

## Verification Gates

- `dotnet build ... -warnaserror`
- `dotnet test ... -c Release --no-restore`
- `src/avalonia-launcher/verify.ps1`
- `tools/verify.ps1`（当前本地红项仅来自已有陈旧 config 目录）

## Rollback

- 新功能增量；回滚 = 删除新增 Core/App/Tooling 文件、菜单项和 `updateManifestUrl`。
- 不改变便携配置发现、启动链路或现有发布结构。
