# 朋友分发就绪:干净打包 + 首次运行引导 + 上手文档

## Goal

让 WT Launcher 达到"自豪地递给最好的朋友用"的分发就绪状态。朋友画像:开发者,但不一定用 AI CLI;核心价值是「添加工作区 + 一键打开」,「发现工具」是加分项而非前提。朋友拿到的应是一个解压即用、第一次打开就知道怎么上手、文档三步能跑起来的成品,而不是一个工程师自用的开发工程。

## Background(已确认事实,证据在仓库)

- 工程质量内核可靠:原子 JSON 写入 + 失败回滚(StoreGrain)、410+ 测试、31 项 verify 门禁、五层架构。文件缺失时应用优雅降级为默认空 store,不崩溃(`FileLauncherJsonIO.cs:45` Load 返回 defaultValue)。
- `tools/build-release.ps1` 已经是 Native AOT 发布:`-p:PublishAot=true`,产出单一原生 exe(注释:"PublishAot=true produces single native exe, no .dll"),并且已传 `-p:Version=$version`(git describe,版本信息已注入 exe)。**发布产物本身已有版本号**。
- `artifacts/publish/win-x64/` 当前目录内容是陈旧产物(含 6 个 .pdb、若干 dll、receipt.json),与 AOT 单文件事实不符,是历史遗留,不是当前 build-release 的输出。
- 应用**没有图标**:csproj 无 `ApplicationIcon`,`MainWindow.axaml` 无 `Icon` 属性,项目内无 .ico 文件。任务栏/标题栏是默认图标。
- 配置文件路径解析(`FileStorePersistence.cs:69-102` ResolveStorePath):显式 `--config` > 当前目录向上找 `config/launcher.json` > exe 目录向上找 > 回退 `<根>/config/launcher.json`;不存在则用空 store 启动,首次保存时才写盘。即:朋友解压 exe 到任意目录,双击即可运行(只要 CWD 或 exe 目录下有 `config/launcher.json`,或者接受空启动)。
- `config/launcher.json` 已从版本库移除并 git-ignore(commit d8b7db0,含本机绝对路径,脱敏合理)。**但 README.md 未同步**——README 第 8 行仍把 `config/launcher.json` 列为"仓库内容",文档已与仓库现状脱节(事实错误,必须修)。
- 空状态现状:`LauncherStatusText.cs:34` 在状态栏显示欢迎文案("欢迎使用 WT Launcher — 点击上方「管理 → 地址管理」添加项目目录,或使用「发现工具」自动扫描"),但中部 EntriesView 无任何可视空状态面板——朋友第一眼看到的是空白区域 + 一行小字。
- 顶部工具栏(`LauncherControlBar.axaml:94-148`)已有「新增按钮」(AddEntryCommand)、「发现」(DiscoverAgentsCommand)、「管理」(命令与模板/地址)按钮;选择与刷新、启动组维护等高级能力齐全。
- 添加/编辑对话框 `EntryDetailsWindow.axaml:22-51`:手填 ID、名称、目录、命令四个字段。ID 标签是"ID(稳定唯一,不要重复)",目录无浏览按钮(目录浏览仅存在于 `DirectoryEditDialog.axaml:31` 和 `DirectoryManagementWindow.axaml.cs:75`,即"地址管理"维护工具里,不在新增入口)。对朋友而言「新增按钮」需要手敲路径、自己编 ID、填命令,摩擦大。
- 已有 `DirectoryManagementWindow`(地址管理):列出目录引用次数、双击改地址可浏览、可合并地址。是维护工具,不是新手创建流程。
- 运行依赖:Windows Terminal + PowerShell 7,`.wt-protector.dsc.yaml` winget 配置存在(开发者式安装)。朋友机器若无 wt.exe 则无法启动工作区(这是平台前提,文档需说明)。
- `run.ps1` 面向开发(要求仓库根存在 config/launcher.json 且抛错),朋友不需要它;朋友直接用 exe。
- 未签名 exe 会触发 SmartScreen"Windows 已保护你的电脑"拦截(双击场景,可点"更多信息 → 仍要运行"绕过)。
- **发布路径当前是断的(2026-08-02 实测)**:`tools/build-release.ps1` 执行 `dotnet publish $app -p:PublishAot=true` 报 `NETSDK1207: 目标框架不支持提前编译 [WTLauncher.Analyzers.csproj]`。根因:`Directory.Build.props:17-19` 给除 Analyzers 外的所有项目注入 `ProjectReference`(OutputItemType=Analyzer),命令行 `-p:` 是全局属性,传播到 netstandard 的 Analyzers 项目 → AOT 失败。影响自 commit 4ff088a("reconnect analyzers",2026-07-26,分析器接线改动)后出现(7-24 的 6bb0d63 首接分析器时成功发布过 AOT,receipt `publishedDll: null` 为证);当前 HEAD(fd49569)实测必败。注意:仓库历史于 2026-08-02 被重写,引用提交一律以当前哈希为准。
- **CI `aot-verify` 同步失效且静默**:`verify.yml:73` 该任务 `continue-on-error: true`,且第 89 行对整个解决方案 `-p:PublishAot=true`(方案里还有 netstandard Analyzer 与 F# 项目,本就不该整体 AOT),失败不阻断合并,因此一直无人发现。
- **verify 门禁不覆盖 AOT 路径**:`src/avalonia-launcher/verify.ps1:30` 的隔离发布是 `--self-contained true` 非 AOT(其 receipt 要求 publishedDll,AOT 无 dll),所以 gate 保持绿色而发布路径已坏。
- 用户全局规范:图标库只用 lucide;不加 emoji;KISS;功能核心/命令式外壳;中文。

## Requirements

- **R0 修复发布路径(NETSDK1207,阻塞前置)**:`tools/build-release.ps1` 恢复可用——AOT 属性改为项目级作用域(`WTLauncher.App.csproj` 内 `<PublishAot Condition="'$(EnableNativeAot)' == 'true'">`),build-release 传 `-p:EnableNativeAot=true` 代替 `-p:PublishAot=true`;CI `aot-verify` 改为只 publish App 项目 + 同一 marker。Analyzer 项目不再被全局属性污染。
- **R1 应用身份**:应用有正式图标(.ico,渲染进 exe,任务栏/标题栏/资源管理器可见)与完整版本信息(产品名、版本、公司字段可选;版本号 build-release 已注入,补 ProductName 等可读字段)。
- **R2 干净分发物**:发布目录不含 .pdb 等调试符号;stale 的 `artifacts/publish/win-x64/` 旧产物被清理或规范化,目录内容与"单一 exe + 说明 + 示例配置"事实一致。
- **R3 分发打包**:一条命令产出可分发给朋友的产物(建议 zip),内含:exe、使用说明(朋友版)、示例配置(或空配置模板)。朋友解压 → 按说明三步 → 可用。
- **R4 首次运行体验**:零条目时空状态为可视面板(不是一行状态文字):欢迎文案 + 大按钮入口「添加第一个工作区」「发现已安装工具」(发现按钮对不用 AI CLI 的朋友隐藏语义或弱化,但保留)。面板指引复用现有流程,不新建独立向导。
- **R5 添加工作区体验**:从"添加第一个工作区"入口进入的对话框对新手友好:目录可浏览选择、ID 自动生成(不需要用户发明)、命令有默认值(pwsh)与模板提示。改造范围限 EntryDetailsWindow(或新增轻量入口),不重写现有逻辑。
- **R6 朋友版文档**:一份零术语的三步上手中文说明(仓库内 `docs/` 单一起源,打包时拷入 zip):是什么、三步添加第一个工作区、如何启动、SmartScreen 拦截如何绕过、前置依赖(Windows Terminal)说明。
- **R7 README 修正**:修正 README 与仓库现状的脱节(launcher.json 已 git-ignore、内容清单、示例配置指引),不展开为朋友文档。
- **R8 示例配置**:`config/launcher.example.json` 进版本库(空 entries + blank-manual 预设 + 默认 settings,无本机路径),zip 内附;朋友复制为 `config/launcher.json` 或直接用空启动。

## Acceptance Criteria

- [x] AC0: `tools/build-release.ps1` 成功完成 AOT 发布(修复 NETSDK1207),产出单一原生 exe;CI `aot-verify` 任务改为发布 App 项目后不再失败。
- [x] AC1: `tools/build-release.ps1` 输出目录无 `.pdb`、无陈旧 `config/`,含 exe + 必需原生 dll(libSkiaSharp/libHarfBuzzSharp/av_libglesv2,AOT 非单文件,实测确认)+ receipt;exe 属性页显示产品名与版本,图标为正式图标。
- [x] AC2: 一条命令(新增 `tools/package-friend.ps1` 或并入 build-release)产出 zip,内含 exe、使用说明、示例配置;zip 解压到全新目录后双击 exe 可启动。
- [x] AC3: 全新无配置环境启动 exe → 出现可视空状态面板,含「添加第一个工作区」「发现已安装工具」入口;点击任一入口走通对应流程。
- [x] AC4: 从「添加第一个工作区」添加一个目录:可通过浏览选择目录,无需手敲路径,无需自己发明 ID,默认命令可直接启动(pwsh 或等价);添加成功后卡片出现并可启动。
- [x] AC5: 朋友版文档在仓库 `docs/` 存在且被 zip 打包;文档按步骤执行一次可完成"添加并启动第一个工作区"(不依赖 AI CLI)。
- [x] AC6: README 不再声称 `config/launcher.json` 是版本库内容;README 提及示例配置与朋友文档的入口。
- [x] AC7: `config/launcher.example.json` 存在、进版本库、通过现有 config-schema 校验(gate 不红),且不含任何本机绝对路径。
- [x] AC8: 全部既有验证保持绿:`tools/verify.ps1` 31 项检查通过,410+ 测试通过;空状态/添加对话框改动不破坏 drag/selection/preset 既有行为。

## Out of Scope

- 安装器(NSIS/WiX)与开始菜单快捷方式、自动更新。
- 代码签名证书采购/CI 签名(文档引导"更多信息 → 仍要运行"替代,除非用户另行决策)。
- 多语言(仅中文)。
- AppData/注册表状态迁移(架构不变,仍单文件 JSON)。
- 重写现有添加对话框为多步向导、重做 UI 主题。
- 「发现工具」功能本身的增强(仅入口与空状态呈现)。

## Decisions(用户已确认)

- D1 分发形态:**zip 压缩包**(exe + 使用说明 + 示例配置,解压即用)。2026-08-02 确认。
- D2 首次运行引导:**最小可视空状态面板**(欢迎文案 + 大按钮入口,复用现有流程,不改写盘语义)。2026-08-02 确认。
- D3 添加对话框:**轻量改造**(目录浏览按钮 + ID 自动生成 + 命令默认 pwsh,复用已有 OpenFolderDialog 模式)。2026-08-02 确认。
- D4 SmartScreen:**文档引导绕过**(使用说明写明"更多信息 → 仍要运行")。2026-08-02 确认。

## Open Questions

- 无(全部决策已收敛)。
