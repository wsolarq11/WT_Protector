Status: External
Purpose: 记录 CodexSwitch（Avalonia + .NET 10 + Native AOT）的核对观察与可借/不借边界。
Update when: 上游核对或本地技术栈结论变化时。

# CodexSwitch：Avalonia + .NET 10 + Native AOT

- 上游仓库：<https://github.com/AIDotNet/CodexSwitch>
- 核对 commit：`6152418fd1de7779010d4786b79060e651649109`
- UI 子模块 commit：`cbb5aaae69b498e5f6b324d8c9632a55bc2dbd29`（`CodexSwitchUI`）
- 核对方式：`git ls-remote https://github.com/AIDotNet/CodexSwitch.git HEAD refs/heads/master`

## 为什么收录

WT Launcher 退役了旧单文件 WinForms/GDI 线，把 Avalonia/.NET 线提升为 `src/avalonia-launcher/`。
本条目是该迁移的外部参照，也是防止盲抄 CodexSwitch 产品范围的护栏。

CodexSwitch 的价值：证明现代 C# 桌面无需 Electron/WebView2/Tauri：
.NET 10 + Avalonia 12 + XAML + MVVM/CommunityToolkit.Mvvm + CodexSwitchUI + Lucide.Avalonia + xUnit +
自包含 Native AOT + 多平台 CI。教训不是"抄 CodexSwitch"，而是 C# 桌面能拿到现代布局、样式、组件、
可测状态与发布纪律。

## 观察到的技术栈

（下表为上游核对 commit 的观察记录，每行链接到核对源码文件；本仓库不可本地验证。）

| 领域 | 观察 | 来源 |
| --- | --- | --- |
| 桌面 UI 框架 | `Avalonia`、`Avalonia.Desktop`、`Avalonia.Themes.Fluent`、`Avalonia.Fonts.Inter` | `CodexSwitch.csproj` |
| 运行时目标 | `TargetFramework` = `net10.0`；`OutputType` = `WinExe` | `CodexSwitch.csproj` |
| Native 发布 | `PublishAot=true`、`TrimMode=full`、CI `--self-contained true` | `CodexSwitch.csproj`、`ci.yml` |
| MVVM | `CommunityToolkit.Mvvm`；生成属性/命令 | `Directory.Packages.props`、`MainWindowViewModel.cs` |
| 图标 | `Lucide.Avalonia` | `Directory.Packages.props` |
| UI 结构 | `Views/`、`ViewModels/`、`Controls/`、`Styles/`、XAML 页/对话框/壳 | `MainWindow.axaml`、`HomePage.axaml` |
| 组件库 | `CodexSwitchUI` 子模块：控件、主题、Token、文档、测试 | `CodexSwitchUI` 子模块 |
| 测试 | xUnit 覆盖 config/protocol/usage/i18n/updates/UI 基础设施 | `CodexSwitch.Tests.csproj`、`README.zh-CN.md` |
| 发布打包 | Windows Inno Setup、Linux AppImage、macOS DMG/签名/公证 | `Package-ReleaseArtifact.ps1`、`ci.yml` |

## 值得借用的教训

1. UI 脱离手绘 WinForms：卡片、悬停、截断、统计、对话框在手工绘制下成本高；Avalonia 让卡片成为带
   布局/绑定/样式/主题资源的组件。
2. 启动器状态走 MVVM：状态建模为 VM，命令逻辑可不开 GUI 测试。本地落地为 `MainWindowViewModel`
   （`ViewModels/` 下 partial + `LayoutManager` + `EntryListViewModel`）。
3. Token/组件一等化：卡片、状态、批量栏、对话框是独立文件与绑定。本地组件：`Views/LauncherEntriesView.axaml`、
   `Views/Controls/StatusBar.axaml.cs`、`Views/ConfirmBatchLaunchWindow.axaml`、`ConfirmLaunchWindow`、
   `CommandEditDialog`、`EntryDetailsWindow`、`QuickLaunchWindow`。
4. 发布证明贴近应用：本地入口是 `scripts/build-release.ts`（Native AOT 已落地）；先用普通 win-x64 自包含发布
   证明产品，再优化发布姿态。

## 不照抄的边界

| CodexSwitch 特征 | 为什么不直接抄 |
| --- | --- |
| 本地 AI 代理、协议适配器、OAuth、用量面板 | 与本地命令启动器无关的产品范围 |
| 全平台打包矩阵 | 当前 Windows-first，只做 win-x64 |
| 一开始就 Native AOT | 本地已落地；原则是产品证明前避免无谓摩擦 |
| 大侧栏/仪表盘形态 | HTA 教训：少装饰 |
| 子模块/组件库依赖 | 从本地最小组件开始 |
| 直接复制上游源码 | 核对时上游未声明 license；用思想与公开文档，不抄源码 |

## 本地现状

已提升的主线：`src/avalonia-launcher/`（`WTLauncher.Avalonia.sln` + `src/WTLauncher.App` +
`src/WTLauncher.Core` + `tests/WTLauncher.Tests`）。退役 WinForms 线记于 `retired/design/winforms-launcher.md`；
旧根 `WT Launcher.exe` 与 `src/winforms-launcher/` 不存在，不得恢复（门禁 `retired-paths-removed`）。

## 与其他候选的结论

| 技术栈 | 结论 |
| --- | --- |
| 退役 WinForms | 只留设计教训，不恢复 |
| WebView2 | 最快接近 HTA 观感，但引入 JS 桥与 web 边界 |
| Avalonia | 当前主线：现代 C# 桌面 + 可测状态 |
| Tauri | 长期 web/Rust 路线，对本小启动器迁移偏重 |
| WPF / WinUI 3 | 优于 WinForms，但跨平台现代 .NET 与组件复用不及 Avalonia |
| Electron | UI 友好但过重 |

## 官方参考

- Avalonia overview: <https://docs.avaloniaui.net/docs/overview/what-is-avalonia>
- Avalonia getting started: <https://docs.avaloniaui.net/docs/get-started/test-drive/introduction>
- CommunityToolkit.Mvvm: <https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/>
- .NET Native AOT deployment: <https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/>
- .NET self-contained deployment: <https://learn.microsoft.com/en-us/dotnet/core/deploying/>
- xUnit: <https://xunit.net/>
