# 设计:朋友分发就绪(friend-handoff)

## 架构边界

所有改动遵守 5 层架构与既有不变量。分层映射:

| 需求 | 层 | 触碰文件 |
|------|-----|----------|
| R1 应用身份 | 构建配置 + Assets | `WTLauncher.App.csproj`、`assets/app.ico`(新)、`tools/generate-icon.ps1`(新) |
| R2 干净分发物 | 构建脚本 | `tools/build-release.ps1` |
| R3 zip 打包 | 构建脚本 | `tools/package-friend.ps1`(新) |
| R4 空状态面板 | 4-Views + 3-Orchestration | `LauncherEntriesView.axaml`、`MainWindowViewModel`(只读投影) |
| R5 添加对话框 | 4-Views(对话框 code-behind) | `EntryDetailsWindow.axaml(.cs)`、`DialogService.cs`(不动) |
| R6 朋友文档 | docs | `docs/使用说明.txt`(新,UTF-8 BOM) |
| R7 README 修正 | docs | `README.md` |
| R8 示例配置 | config + 门禁 | `config/launcher.example.json`(新)、`tools/verify.ps1`(schema 检查) |

Core 层零改动。不新增第二套添加流程(无向导)。

## R0 修复发布路径(NETSDK1207)— 全部工作的前置阻塞

**现状(2026-08-02 实测失败)**:`tools/build-release.ps1` 传 `-p:PublishAot=true`,命令行为全局属性,经 `Directory.Build.props:17-19` 注入的 Analyzer ProjectReference 传播到 netstandard2.0 的 `WTLauncher.Analyzers` 项目 → NETSDK1207。CI `aot-verify`(verify.yml:70-89)对整个解决方案传同一全局属性且 `continue-on-error: true`,失败静默。历史参照:`artifacts/publish/win-x64/receipt.json`(2026-07-24,`publishedDll: null`)证明该路径曾成功产出 AOT 单 exe;目录里现有的 `*.dll`/`*.pdb`/`config/` 是更早非 AOT 发布的残留(日期 4-7 月),R2 清理一并处理。

**修复(项目级作用域 + 无害 marker)**:

1. `WTLauncher.App.csproj`:`<PublishAot Condition="'$(EnableNativeAot)' == 'true'">true</PublishAot>` — PublishAot 只在 App 项目内求值,不传播到被引用项目。
2. `tools/build-release.ps1`:`-p:PublishAot=true` → `-p:EnableNativeAot=true`(EnableNativeAot 对 Analyzer 项目无意义,传播无害)。其余 `-r win-x64`/`-p:Version` 不变。
3. `.github/workflows/verify.yml` aot-verify:`dotnet publish` 目标从解决方案改为 `src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj`,参数 `-c Release -r win-x64 -p:EnableNativeAot=true --no-restore`。保留 `continue-on-error: true`(维持 informational 语义;是否转强制留待用户决策)。

**不动**:`src/avalonia-launcher/verify.ps1:30` 的非 AOT 隔离发布(其 receipt 依赖 publishedDll,与 AOT 输出不兼容,保持现状);Analyzer 在普通构建/测试中照常生效(仅 AOT 发布时其项目不再收到 PublishAot 全局属性)。

**验证**:本地 `tools/build-release.ps1` 成功;`git diff` 确认只改三处。

## R1 应用身份(图标 + 版本信息)

- 图标来源:lucide `terminal` 图标(用户全局规范"图标库只用 lucide")。lucide terminal path:`M4 17l6-6-6-6M12 19h8`,stroke 型。
- 生成:`tools/generate-icon.ps1` 用 System.Drawing 渲染——品牌蓝圆角方块(#2563EB 或 LauncherDesignTokens 主色值,以 tokens 为准)上画白色粗描边 terminal 字形,导出多尺寸 .ico(16/24/32/48/64/128/256)。脚本与产物 `assets/app.ico` 一起提交,保证可复现。
- csproj:`<ApplicationIcon>..\..\..\assets\app.ico</ApplicationIcon>`(相对路径按项目位置定),加 `<Product>WT Launcher</Product>`、`<AssemblyTitle>WT Launcher</AssemblyTitle>`、`<Company>` 留空。版本号 build-release 已注入(`-p:Version`),不动。
- 校验:`(Get-Item WTLauncher.App.exe).VersionInfo` 显示 ProductName/FileVersion;图标在资源管理器可见。

## R2 干净分发物

- `tools/build-release.ps1` publish 后新增清理步骤:删除 `$publishDir` 下的 `*.pdb`、`*.dll`、历史遗留 `config/` 子目录(陈旧产物一次清掉;AOT 单文件本来无 dll,删除幂等安全)。`receipt.json` 保留。
- 不动 `verify.ps1` 的隔离 verify-publish 目录(它的 pdb 用途独立)。
- 注:R0 修复后 `build-release.ps1` 成功产出 AOT 单 exe,届时实测确认是否带 .pdb(AOT 默认带调试符号,清理逻辑按实测结果写)。

## R3 zip 打包(新脚本 `tools/package-friend.ps1`)

- 输入:AOT exe(`artifacts/publish/win-x64/WTLauncher.App.exe`)+ `docs/使用说明.txt` + `config/launcher.example.json`。
- 输出:`artifacts/friend/WTLauncher-<version>.zip`,版本取自 exe 的 `VersionInfo.FileVersion`(或 receipt.json)。
- zip 内结构(顶层单文件夹,朋友解压后是干净一包):

```
WTLauncher/
  WTLauncher.App.exe
  使用说明.txt
  config/launcher.example.json
```

- 用内置 `Compress-Archive`,无新依赖。脚本前置检查:exe 存在、说明存在、示例配置存在;否则报错停(不产出残次包)。
- 说明:朋友解压后双击 exe 即启动(ResolveStorePath 支持:exe 目录找不到 config 时向上/回退空启动,`FileStorePersistence.cs:69-102`);复制 example 为 `config/launcher.json` 是可选步骤。
- run.ps1 是开发路径,朋友不用,zip 不含。

## R4 空状态面板

- 挂载点:`LauncherEntriesView.axaml` 根 Grid(第 9 行起)内,ScrollViewer 之后加一个覆盖面板:`Border` + 居中 `StackPanel`:标题「开始使用」、副文案、两个大按钮「添加第一个工作区」(`AddEntryCommand`)、「发现已安装工具」(`DiscoverAgentsCommand`)。全部用现有 `{StaticResource}` token,不硬编码颜色。
- 可见性:绑定 VM 只读投影 `IsEmptyEntries`。按"单源投影"不变量:该投影派生自条目集合计数(单一可写源是 store 的 entries),不是第二个可写字段。入口列表已有 `NotifyCountChanged`/`CountChanged` 事件与 TotalCount badge 机制,投影复用同一数据源刷新。
- 投影实现(遵循既有模式,已核实):`LayoutManager.cs:121-147` 的范式是"get-only 派生属性 + 在源变化处 `OnPropertyChanged(nameof(...))`"。对应实现:`public bool IsEmptyEntries => TotalCount == 0;` 并在 `MainWindowViewModel.OnEntryCountChanged()`(`MainWindowViewModel.cs:217-219`,即 TotalCount 被写入处)追加 `OnPropertyChanged(nameof(IsEmptyEntries))`。纯派生零漂移,不新增可写字段。
- 状态栏欢迎文案(`LauncherStatusText.cs:34`)保留不动——面板出现时二者不冲突,删除反而减少回归面。
- 三个视图模式(cards/details/names)在空状态时都显示同一个面板(面板置于三者之上,IsVisible 由 IsEmptyEntries 控制)。

## R5 添加对话框轻量改造(`EntryDetailsWindow`)

- 目录浏览:目录字段行加「浏览...」按钮,code-behind 复用 `DirectoryEditDialog.axaml.cs:36-55` 的 `OpenFolderDialog` 模式(`Title = "选择目录"`,失败走 `ErrorLog.Write`)。编辑/新增模式都可用。
- ID 自动生成(仅新增模式,prefill == null):`Submit()` 时若 IdBox 空白 → 由 NameBox 派生 `EntryId.Normalize(name)`;派生结果不可用则 `entry-<yyyyMMddHHmmss>`。唯一性由现有服务层校验兜底(`EntryService.AddEntryAsync` 已有重复校验路径),不新造校验。
- 命令默认值(仅新增模式):`CommandBox.Text = "pwsh"`。模板提示机制(`SetupTemplatePreview`)不动,用户改填 `$key` 时预览照常工作。
- 编辑模式(prefill != null)行为零变化。
- 不动 `DialogService.RequestEntryDraftAsync` 签名(新增模式 prefill 仍传 null,默认值在对话框内注入,Keep views 层自足)。

## R6 朋友文档(`docs/使用说明.txt`)

- 单一起源,UTF-8 with BOM(记事本中文不乱码),纯文本零术语。
- 内容:一句话是什么;前置依赖(Windows Terminal,给 winget 一键命令);三步上手(解压→「添加第一个工作区」浏览目录→点卡片启动);SmartScreen 绕过步骤(更多信息→仍要运行);「发现已安装工具」一句带过;常见问题(找不到 wt.exe、目录失效报错、配置文件在哪/怎么备份)。
- zip 打包直接拷入;README 引用。

## R7 README 修正

- 内容清单:把 `config/launcher.json` 条目改为"本地运行时文件(已 git-ignore;仓库内提供 `config/launcher.example.json` 模板)"。
- 新增「首次使用 / 朋友版」小节,链接 `docs/使用说明.txt`。
- 全文扫一遍其余 launcher.json 引用,只修事实脱节,不重写。

## R8 示例配置(`config/launcher.example.json`)

- 内容:schemaVersion 2、`entries: []`、`blank-manual` 锁定预设、默认 settings(与本地运行文件一致的结构,cardsGroupKey/backgroundMode 等字段照现有格式)、`defaultPresetId: "blank-manual"`、window 宽高。无任何本机绝对路径。
- 门禁:`tools/verify.ps1` 增加 Add-Check:example 可解析、顶层结构符合 LauncherStoreFile、entries 为空、不含 `D:\`/`C:\Users\` 等绝对路径痕迹(正则)。

## 兼容性与回滚

- 不变量保护:无第二可写状态源(IsEmptyEntries 是投影);不写 JSON 于 LauncherStore 之外;不引入裸 catch;颜色全部走 token。
- 回滚点:`build-release.ps1` 清理步骤若误伤,`git revert` 即恢复;新脚本独立无依赖;对话框改动只影响 EntryDetailsWindow 自身,回归靠既有 410 测试 + 手动编辑模式验证。
- AOT 发布产物当前是否带 .pdb 需在实现第一步用一次真实 `build-release.ps1` 验证,清理逻辑按实测结果写。

## 验证命令

- `tools/verify.ps1`(全绿,31 项 + 新增 example 检查)
- `tools/build-release.ps1`(产出无 pdb/dll,exe 属性含版本,图标可见)
- `tools/package-friend.ps1`(zip 结构 = exe + 说明 + 示例)
- 手动冒烟:全新空目录解压 zip → 双击 exe → 空状态面板出现 → 添加工作区(浏览目录)→ 卡片出现并可启动
