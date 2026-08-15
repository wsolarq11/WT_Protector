# 台账：docs/giants-shoulders/codexswitch-avalonia-native-aot.md（验证日期 2026-08-15）

> 协议：.trellis/tasks/08-15-docs/verification-protocol.md。判定词：VERIFIED / STALE / UNVERIFIABLE / HISTORICAL / EXTERNAL。

| 声明 | 判定 | 证据（命令 + 结果） | 处理 |
| --- | --- | --- | --- |
| Reviewed on: 2026-05-17 | HISTORICAL | `git log --oneline --follow -- docs/giants-shoulders/codexswitch-avalonia-native-aot.md` → aba9bcbf 2026-05-17（Promote Avalonia launcher as the source mainline） | 保留带日期 |
| 上游仓库 URL + 评审提交哈希 6152418f… / 子模块 cbb5aaae… | EXTERNAL（外部研究记录） | `git ls-remote https://github.com/AIDotNet/CodexSwitch.git HEAD refs/heads/master` → OK，master=ab93a928…（≠ 评审提交，正常）；GitHub API 校验被限流（403），提交存在性未独立复核 | 保留为带日期的评审记录；注明外部不可本地验证 |
| "WT Launcher retired its former single-file WinForms/GDI line and promoted the Avalonia/.NET line into src/avalonia-launcher/" | VERIFIED | `Test-Path src/avalonia-launcher` → True；`Test-Path retired/design/winforms-launcher.md` → True；`Test-Path src/winforms-launcher` → False；门禁 retired-paths-removed（verify.ps1 行65）+ winforms-retired-design（行518） | 保留 |
| 观察栈表（Avalonia/net10.0/PublishAot/Lucide.Avalonia/xUnit/CI 打包等） | EXTERNAL | 表格每行带上游源码 URL（github.com/AIDotNet/CodexSwitch/blob/6152418f/…），URL 齐备 | 保留为外部研究 |
| 本地结构："src/avalonia-launcher/ src/WTLauncher.App/ src/WTLauncher.Core/ tests/WTLauncher.Tests/" | VERIFIED | 三个目录 Test-Path → True；WTLauncher.Avalonia.sln → True | 保留 |
| Core 职责描述（config parsing、launch argument builder、safety policy） | VERIFIED | Core 53 文件含 LauncherJson.cs/LauncherStore.cs/LauncherStoreFile.cs/LaunchArgumentBuilder.cs/LauncherStoreValidator.cs（目录清单与 AGENTS.md 基线 53 一致） | 保留 |
| Lesson 2 MVVM 草图（LauncherViewModel: Entries/FilterText/…） | STALE（命名） | 实际实现为 MainWindowViewModel + 15 partial（AGENTS.md 基线），无 "LauncherViewModel" 类型（`grep 'class LauncherViewModel'` → 无命中） | 改写为实际类型名，保留教训 |
| Lesson 3 组件名（LauncherCard/CommandPreview/EditEntryDialog/BatchBar/StatusBadge） | STALE（命名） | `grep 'class LauncherCard|CommandPreview|EditEntryDialog|BatchBar|StatusBadge'` → 无命中；实际组件：Views/LauncherEntriesView.axaml（卡片）、Controls/StatusBar.axaml.cs、ConfirmBatchLaunchWindow.axaml、各对话框（ConfirmLaunchWindow/CommandEditDialog/EntryDetailsWindow） | 改写为实际组件，保留"组件一等公民"原则 |
| "Native AOT should remain a later option… current mainline first proves normal self-contained win-x64 publish" | STALE | tools/build-release.ps1 现用 `-p:EnableNativeAot=true` 发布 Native AOT；git log build-release.ps1：23213609 2026-07-12（Native AOT 现代化）、ab5b9e05 2026-07-21（AOT publish 修复）、01596287 2026-08-02；AGENTS.md/README 亦言 self-contained（含 AOT） | 改写为已验证现实（AOT 已落地，注明评审时曾推迟的日期事实） |
| verify.ps1 / build-release.ps1 本地前端 | VERIFIED | 两文件 Test-Path → True | 保留 |
| 退役 WinForms 记录于 retired/design/winforms-launcher.md；旧根 WT Launcher.exe 与 src/winforms-launcher/ 不得回归 | VERIFIED | 上述 Test-Path 证据 + verify.ps1 行65 retired-paths-removed（paths 含 'src\\winforms-launcher'、'WT Launcher.exe'） | 保留 |
| "upstream README states no license file was declared at review time" | EXTERNAL/HISTORICAL | 带 "at review time"（2026-05-17）限定；外部记录，本地无证据 | 保留带日期限定 |
| 决策透镜表（WinForms/WebView2/Avalonia/Tauri/WPF/WinUI/Electron） | EXTERNAL | 外部评估性内容 | 保留 |
| 官方参考链接（Avalonia/CommunityToolkit/Native AOT/xUnit） | EXTERNAL | URL 齐备 | 保留 |
| "The HTA lesson says WT Launcher wants less chrome" | HISTORICAL（引用退役教训） | retired/ 仅存 config.json + design md（协议已知事实）；HTA 于 825ecb5b 2026-07-06 移除 | 保留为历史教训引用 |

## 处理总结
- VERIFIED：8 项；HISTORICAL：2 项；EXTERNAL：5 项；STALE：3 项（AOT 时机、MVVM 类名、组件名）；UNVERIFIABLE：0
- 重写点：AOT 段落、Lesson 2/3 命名锚定、头部加验证与外部性说明；其余保留。

## 重写
文档已写回 docs/giants-shoulders/codexswitch-avalonia-native-aot.md。
