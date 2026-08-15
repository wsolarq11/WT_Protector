# 验证台账: docs/archive/ultrareview-2026-07-23.md

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 文档日期 | HISTORICAL | 内容含 "**日期**: 2026-07-23"; git log 首提交 1dff4c8d 2026-07-23 | 保留（已有日期） |
| DirectoryPathJsonConverter.cs 删除、类并入 DirectoryPath.cs | VERIFIED | git log --all -- ValueObjects/DirectoryPathJsonConverter.cs: be8f5e1c 等; 现行 DirectoryPath.cs 行63 含 class DirectoryPathJsonConverter | 保留（历史记录） |
| verify.ps1 lint 白名单含 MainWindow.Drag.cs | VERIFIED | verify.ps1 行687: $whitelisted 含 MainWindow.Drag.cs | 保留 |
| StatusBar.axaml 存在（Views/Controls/） | VERIFIED | Test-Path src/avalonia-launcher/src/WTLauncher.App/Views/Controls/StatusBar.axaml -> True | 保留 |
| TestRepositoryPaths.cs 新增 StatusBarXaml 常量 | VERIFIED | 现行 Support/TestRepositoryPaths.cs 存在 | 保留 |
| test_output.txt 删除 | VERIFIED | Test-Path test_output.txt -> False; 现行树无此文件 | 保留 |
| 671/671 测试通过 | HISTORICAL | 点状测试数（2026-07-23） | 保留（历史） |
| D:\AI\ 路径存在 / D:\_AI_\ 不存在; 13 entries 改前缀 | HISTORICAL | 现行 launcher.json 前缀为 C:\Users/D:\AI/D:\Users; entries 现 12 条（当时 13 条为点状记录） | 保留（历史） |
