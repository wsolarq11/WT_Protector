# 验证台账 — src/avalonia-launcher/README.md

> 依据 .trellis/tasks/08-15-docs/verification-protocol.md（2026-08-15）逐项查证。
> 判定词：VERIFIED / STALE / UNVERIFIABLE / HISTORICAL。仅只读 pwsh 证据，未运行构建/门禁。

| 声明（原文摘录） | 判定 | 证据（命令 + 结果，截断） | 处理 |
|---|---|---|---|
| 活动主线；由原 spike 晋升（WinForms 可执行/源码线退役到 retired/design/winforms-launcher.md） | VERIFIED | Test-Path src/avalonia-launcher → True；retired/design/winforms-launcher.md True；WT Launcher.exe / src/winforms-launcher / spikes Test-Path False；verify.ps1 retired-paths-removed | kept |
| 运行：根目录 powershell … .\src\avalonia-launcher\run.ps1 | VERIFIED | src/avalonia-launcher/run.ps1 Test-Path True | kept |
| 应用读取根 config/launcher.json（entries+presets+settings+defaultPresetId 原子单元）并在其中持久化 | VERIFIED | FileStorePersistence.ResolveStorePath：显式/向上搜索/cwd config\launcher.json；LauncherStoreFile 四部分（另有 runHistory 可选字段，见 AGENTS.md 更新） | kept + 补 runHistory |
| 头部批量控件：全选可见/清空选择/一键启动已勾选 | VERIFIED | LauncherControlBar.axaml L29-34 全选可见/清空选择；L42 一键启动已勾选 | kept |
| 验证：根门禁 .\tools\verify.ps1 或主线门禁 .\src\avalonia-launcher\verify.ps1 | VERIFIED | 两脚本 Test-Path True | kept |
| 主线门禁步骤：1) dotnet restore -r win-x64 2) dotnet test -c Release 3) dotnet publish -c Release -r win-x64 --self-contained true 到隔离验证输出 4) Windows 隐藏临时配置启动冒烟，非 Windows 跳过 GUI | VERIFIED | src/avalonia-launcher/verify.ps1 L18 restore -r win-x64；L25 test -c Release；L30 publish -c Release -r win-x64 --self-contained true -o verify-publish；L79-118 Windows 进程冒烟（smoke-critical-journey launch+title）+ 非 CI GUI 标题检查，非 Windows 标记 skipped-non-windows | kept |
| 验证发布输出忽略于 artifacts/verify-publish/win-x64/；稳定发布输出 artifacts/publish/win-x64/ | VERIFIED | .gitignore 含 artifacts/；verify.ps1 L13 artifacts\verify-publish\win-x64；build-release.ps1 L14 Get-AvaloniaPublishDir | kept |
| 当前验收问题：已取代 WinForms 源码树为主线，继续改进 Avalonia UX 并保持根门禁与便携配置契约 | VERIFIED（政策陈述） | 与 AGENTS.md/CLAUDE.md 一致；无相反证据 | kept |
| Layer 0 — src/WTLauncher.Core/：LaunchEntry、LaunchPlan、LauncherPreset 不可变记录 | VERIFIED | record LaunchEntry（LaunchEntry.cs:8）；record LaunchPlan（LaunchPlan.cs:3）；record LauncherPreset（LauncherPreset.cs:23） | kept |
| Layer 0 — LaunchArgumentBuilder：wt.exe 参数 + Windows 风格引号 | VERIFIED | class LaunchArgumentBuilder（Core:6）；QuoteArgument / -d / --title / -- pwsh -NoExit -EncodedCommand | kept |
| Layer 0 — LauncherJson：原子 temp-file + .bak JSON 持久化 | VERIFIED | class LauncherJson（LauncherJson.cs:18）静态 facade 委托 ILauncherJsonIO；FileLauncherJsonIO.Save temp + File.Replace + .bak 旋转 | kept |
| Layer 0 — LauncherEntryOrder、LauncherEntryLayoutCalculator 纯排序/布局规则 | VERIFIED | 两类型均在 Core（LauncherEntryOrder.cs:5、LauncherEntryLayoutCalculator.cs:3） | kept |
| Layer 1 — src/WTLauncher.App/ViewModels/MainWindowViewModel.cs 持有增删改命令 | VERIFIED | class MainWindowViewModel 存在；partials（CommandManagement/DirectoryManagement/Entries/Presets/Batch/Drag/Layout/Migration/Status/Update 等） | kept |
| Layer 1 — 共享规则收窄助手如 LauncherEntryDrafts | VERIFIED | class LauncherEntryDrafts（ViewModels\LauncherEntryDrafts.cs:10） | kept |
| Layer 2 — src/WTLauncher.App/Infrastructure/AppStore.cs + MainWindowViewModel.RestoreVmStateFromStore()；AppStore.Mutate 自动回滚（捕获 before → 应用变换 → LauncherStore.Save → 失败回滚） | STALE | Test-Path AppStore.cs → False；git log -- 该路径末条 e11183cc 2026-07-24 feat: implement VTuber-Killer architecture（git show --stat：AppStore.cs 删除 165 行，StoreGrain.cs 新增）；RestoreVmStateFromStore 仍在（MainWindowViewModel.cs:419） | rewritten：改 StoreGrain.Dispatch（save-first-then-commit）+ FileStorePersistence；注明 AppStore 已由 VTuber-Killer 迁移删除 |
| Layer 3 — 单一可写 UI 状态所有者；委托 Core 与持久化边界 | VERIFIED | MainWindowViewModel 持有 layout/presets/orchestrator，写入经 StoreGrain.Dispatch | kept |
| Layer 3 — 经 MainWindow 构造函数注入对话框确认 Func<> 委托 | STALE | MainWindow ctor 只接收 MainWindowViewModel（MainWindow.axaml.cs:56）；对话框经 IDialogService/IEntryService/IPresetService 注入 MainWindowViewModel ctor（MainWindowViewModel.cs:94-102） | rewritten：改述服务经 VM 构造函数注入 |
| Layer 4 — src/WTLauncher.App/Views/；三视图模式 Cards/Details/Names | VERIFIED | LauncherEntryViewMode.cs；LayoutManager L33-38；MainWindow.axaml 宿主三模式、视图体在 LauncherEntriesView.axaml（与 AGENTS.md 现表述一致） | kept |
| Layer 4 — 拖拽重排：MainWindowDragGhostFactory（ghost 覆盖层 + 插入预览） | STALE | Select-String MainWindowDragGhostFactory src → 0 命中；实际：MainWindow.Drag.cs（ShowDragGhost/MoveDragGhost/HideDragGhost + OverlayLayer）、Core LauncherDragGhostLayoutCalculator、VM LauncherDragReorderWorkflow | rewritten：列出实际类型 |
| Layer 4 — 聚焦视图助手保留共享拖拽/命中测试；一次性菜单守卫留在 MainWindow | VERIFIED | MainWindowEntryCardResolver.cs、FastWrapPanel.cs 存在；菜单守卫在 MainWindow.axaml.cs/LauncherEntriesView.axaml | kept（措辞微调） |
| 关键不变量：每种状态单一来源；Core 不引用 Avalonia/CommunityToolkit.Mvvm；工作流返回显式结果、UI 回调停留在窗口/对话框接缝 | VERIFIED | ArchitectureRules.cs R2 强制 Core 免 UI 引用；LauncherLaunchWorkflow/LauncherDragReorderWorkflow 返回结果对象；对话框经 DialogService | kept |
