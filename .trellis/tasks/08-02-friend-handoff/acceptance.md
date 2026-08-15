# friend-handoff 验收记录 (2026-08-15)

- AC0: PASS — `tools/build-release.ps1` 成功 AOT 发布；`receipt.json` 的 `publishedDll` 为 null；CI 仅 publish App 项目并传 `-p:EnableNativeAot=true`。
- AC1: PASS — 发布目录无 `.pdb`；exe 的 `VersionInfo` 为 Product=WT Launcher、FileVersion=0.1.0.0；图标提取成功（32x32）。
- AC2: PASS — `tools/package-friend.ps1` 产出 `artifacts/friend/WTLauncher-0.1.0.0.zip`（18 MB）；zip 含 `WTLauncher.App.exe`、使用说明、`config/launcher.example.json`；全新目录解压后启动进程存活。
- AC3: PASS（代码级）— `LauncherEntriesView.axaml` 有 `IsEmptyEntries` 空状态面板和「添加第一个工作区」「发现已安装工具」入口；`StatusPresenter.IsEmptyEntries` 为只读投影。
- AC4: PASS（代码级）— `EntryDetailsWindow` 有目录「浏览...」按钮；新增模式 ID 留空自动派生、命令默认 `pwsh`；编辑模式预填不变。
- AC5: PASS — `docs/使用说明.txt` 存在，包含三步上手、SmartScreen 绕过、前置依赖与 FAQ；被 `package-friend.ps1` 打入 zip。
- AC6: PASS — README 将 `config/launcher.json` 标为 git-ignored 运行时文件，并引用 `config/launcher.example.json` 与朋友版文档。
- AC7: PASS — `config/launcher.example.json` 可解析、schemaVersion=2、entries 为空、`blank-manual` 锁定预设、无绝对路径；`config-example-schema` 门禁检查存在。
- AC8: PASS — 全量 `tools/verify.ps1` 40 项检查全绿，测试 820/820 通过；`git diff --check` 通过。
