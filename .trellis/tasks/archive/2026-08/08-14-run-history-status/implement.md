# 启动运行历史与状态回执 — Implement

## Order

1. Core: `LauncherRunHistory` 模型、Create/Append/Sanitize、`runHistory` store 字段与 JSON 注册。
2. Batch report 增加 failure entry id/name，workflow 写入。
3. LaunchOrchestrator 单条/批量/取消/失败链路写历史。
4. MainWindowViewModel 历史状态、BuildStoreFile、Restore、菜单命令。
5. RunHistoryViewModel + RunHistoryWindow + DialogService。
6. Tests：Core 裁剪/降级、VM 单条/批量/取消、报告身份、JSON roundtrip、UI 契约。
7. build + dotnet test + Avalonia verify。

## Verification Gates

- `dotnet build ... -warnaserror`
- `dotnet test ... -c Release --no-restore`
- `src/avalonia-launcher/verify.ps1`
- 手动冒烟：单条/勾选/启动组启动后历史可见，失败和跳过可区分。

## Rollback

- 新功能增量；回滚 = 删除新增 Core/VM/View 文件、菜单项和 `runHistory` 字段。
- 不改现有启动预检、确认、回执契约（除报告新增只读失败身份字段）。
