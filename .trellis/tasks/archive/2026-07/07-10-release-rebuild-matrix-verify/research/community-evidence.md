# 社区佐证调研 — release-rebuild-matrix-verify

调研日期 2026-07-10。本仓库 `<USER>/WT_Protector` 最近 commit `4811df3` 把
Avalonia `12.0.3 → 12.1.0`、`Microsoft.NET.Test.Sdk 18.6 → 18.7`、`coverlet 6.0.4 → 10.0.1`。
本文档只列事实，不做主观质量判断；所有判定都附 citation URL。

## TL;DR

- **D.1 GitHub Releases**：项目从未发布过 GitHub Release，也没有任何 tag；releases/tags REST 接口对 `<USER>/WT_Protector` 均返回 404（仓库对未认证客户端不可见，或无任何 release/tag 对象）。
- **D.2 Avalonia 12.1**：1 条 RELATED（Win32/Skia 文本布局在 12.1.x 有 CJK/字形回归，本项目 win-x64 + Fluent + Skia + HarfBuzz 受影响），其余 breaking-change 条目均为 11→12 主版本迁移、与 `12.0.3→12.1.0` 补丁升级无关 → 记 1 RELATED / N UNRELATED。
- **D.3 coverlet/Test.Sdk**：1 条 RELATED（vstest 18.x 有一条 .NET 10 traits 回归修复 + 把目标框架升级到 net8/net9，本项目恰好是 net10 + 18.6→18.7），coverlet 6→10 为大版本跳跃但本项目走 collector 路径、未见与本项目冲突的 breaking → 记 1 RELATED。
- **D.4 相似项目**：12 个仓库，2 ACTIVE / 10 DORMANT / 0 ARCHIVED。仅 `noambrand/Launchpad-CLI`、`Wox-launcher/Wox` 在近 30 天（2026-06-10 之后）有 push；`joyahmed/devgo-app`（2026-07-06）与 `Flow-Launcher/Flow.Launcher`（2026-07-08）按 `pushedAt` 也落进窗口，合计实际 ACTIVE = 4。

## D.1 GitHub Releases 页状态

通过 GitHub REST API（`curl https://api.github.com/repos/<USER>/WT_Protector/{releases,tags}`，未认证）查询：

| 端点 | 结果 |
|------|------|
| `/repos/<USER>/WT_Protector/releases` | `404 Not Found` |
| `/repos/<USER>/WT_Protector/tags` | `404 Not Found` |
| `/repos/<USER>/WT_Protector` | `404 Not Found`（`pushed_at`/`default_branch`/`archived` 全为 `None`） |

`404` 对未认证客户端有两种含义：(a) 仓库为 private；(b) 仓库不存在。结合 PRD 上下文（任务说明里给出的仓库 URL 即 `https://github.com/<USER>/WT_Protector`、且 commit `4811df3` 被引用为真实存在），更可能是仓库当前对匿名 API 不可见。无论哪种情况，**公开层面没有可见的 release 或 tag**。

WebFetch / WebReader 对 `https://github.com/<USER>/WT_Protector/releases` 与 `/tags` 返回 500 网络错误（已重试 2 次，按约束不再重试），无法补充反证。

**结论**：项目在公开 GitHub 上从未发布过 Release，也没有任何 tag。本次 Avalonia/Test.Sdk/coverlet 升级是在 `main` 分支的常规 commit，不伴随版本标签。

- citation：`https://api.github.com/repos/<USER>/WT_Protector/releases`（404）
- citation：`https://api.github.com/repos/<USER>/WT_Protector/tags`（404）
- citation：`https://github.com/<USER>/WT_Protector/releases`（WebFetch 500，无反证）

## D.2 Avalonia 12.1 breaking changes 相关性

本项目使用的 Avalonia API 面（来自任务说明）：`Avalonia.Win32` / `Themes.Fluent` / `Controls` / `Skia` / `Markup.Xaml` / `Dialogs` / `FreeDesktop` / `Metal` / `OpenGL` / `Vulkan` / `HarfBuzz`。

关键事实：Avalonia 官方 "Breaking changes in Avalonia 12" 文档（两个镜像）**只覆盖 11→12 主版本迁移**，不区分 12.0.x→12.1.x。本次升级是 `12.0.3 → 12.1.0`，属于 12 主版本内的补丁/次版本升级，绝大多数 11→12 breaking（删 `Avalonia.Diagnostics` 包、`IBinding` 接口移除、`Binding` 默认为 ReflectionBinding、`.NET Framework/.NET Standard` 不再支持、`DispatcherTimer`/`AvaloniaSynchronizationContext` 默认到当前 dispatcher 等）**在 12.0.3 时已经生效**，不是 12.1 引入的。

逐条判定：

| 条目 | 来源 | RELATED / UNRELATED | 说明 |
|------|------|---------------------|------|
| 删 `Avalonia.Diagnostics` 包，改 `AvaloniaUI.DiagnosticsSupport` | docs.avaloniaui.net/docs/avalonia12-breaking-changes | **UNRELATED** | 11→12 迁移项，12.0.3 已生效，与 12.1 无关 |
| `IBinding` 接口移除、`Binding`→ReflectionBinding、compiled binding 默认 | 同上 | **UNRELATED** | 11→12 迁移项 |
| `.NET Framework` / `.NET Standard` 不再支持，仅 .NET 8+，移动端仅 .NET 10 | 同上 | **UNRELATED** | 本项目本就 net10；且属 11→12 迁移 |
| `DispatcherTimer` / `AvaloniaSynchronizationContext` 默认到当前 dispatcher | v11.docs.avaloniaui.net 镜像 + XerahS issue #213 | **UNRELATED** | 11→12 迁移项 |
| HarfBuzz 文本整形改为需 `UseHarfBuzz` 显式调用 | docs.avaloniaui.net | **RELATED（需核实）** | 本项目列了 `HarfBuzz`。官方文档把这条归入 12 主版本 breaking，若 12.0.3 已含则与 12.1 无关；但 12.1.x 文本布局有独立回归（见下条），合并记 1 个 RELATED 指向 12.1 实际回归 |
| CJK 字形查找回归 `#21556`（12.1.999-alpha，iOS/部分 CJK 字符不显示） | github.com/AvaloniaUI/Avalonia/issues/21556 | **RELATED** | 12.1.x 文本/字形路径回归。本项目 win-x64 桌面 + Fluent + Skia + HarfBuzz，走同一文本布局栈；虽然回归报告集中在 iOS，但字形查找是共享逻辑，升级 12.1.0 后应在 win-x64 上验证 CJK/emoji 渲染 |
| Touch freezing 回归 `#21141`（v12 引入，v11 无） | github.com/AvaloniaUI/Avalonia/issues/21141 | **UNRELATED** | 触摸输入；本项目是桌面 launcher，不依赖触摸路由，且属 v12 整体而非 12.1 特有 |
| Chinese chars 显示异常 `#20195`（11.3.11→11.3.12） | github.com/AvaloniaUI/Avalonia/issues/20195 | **UNRELATED** | 11.3.x 线，与 12.1 无关 |
| Avalonia.Markup.Declarative 12 release notes | github.com/AvaloniaUI/Avalonia.Markup.Declarative | **UNRELATED** | 本项目用 `Markup.Xaml`（AXAML），不是 Declarative C# 描述库 |

**RELATED 项数：1**（CJK 字形/文本布局回归 `#21556`，影响 Skia+HarfBuzz 文本路径，需在 win-x64 验证 CJK/emoji；HarfBuzz 显式调用那条与其合并计 1）。

- citation：`https://docs.avaloniaui.net/docs/avalonia12-breaking-changes`
- citation：`https://v11.docs.avaloniaui.net/docs/avalonia12-breaking-changes`
- citation：`https://github.com/AvaloniaUI/Avalonia/issues/21556`
- citation：`https://github.com/AvaloniaUI/Avalonia/issues/21141`
- citation：`https://github.com/AvaloniaUI/Avalonia/issues/20195`
- citation：`https://github.com/ShareX/XerahS/issues/213`（ DispatcherTimer/compiled binding 迁移佐证）

## D.3 coverlet 10 + Test.Sdk 18.7 升级风险

本项目测试栈：xUnit + `Microsoft.NET.Test.Sdk` + `coverlet.collector`，约 410 `[Fact]/[Theory]`，net10，`-warnaserror`，通过 `dotnet test` 跑。

### coverlet 6.0.4 → 10.0.1

coverlet 官方 changelog 与 release 页的关键变化：

| 条目 | RELATED / UNRELATED | 说明 |
|------|---------------------|------|
| 6.0.x 引入 `ExcludeAssembliesWithoutSources` 参数，`InstrumentModulesWithoutLocalSources` 被移除（可由 `ExcludeAssembliesWithoutSources=None` 替代） | **UNRELATED** | 本项目未设置 `InstrumentModulesWithoutLocalSources`（默认 collector 用法），无自定义 MSBuild 属性冲突 |
| 移除源生成器文件检测逻辑；默认改为"只有当程序集完全找不到源文件时才排除"，旧行为需 `ExcludeAssembliesWithoutSources=MissingAny` | **UNRELATED** | 本项目无源生成器；默认行为变化不影响纯 consumer |
| 10.0.0 把 `coverlet.core` 目标框架升到 net8，移除 `Newtonsoft.Json` 依赖 | **UNRELATED** | 本项目 net10，兼容 |
| 官方建议：.NET 10 / MTP 项目用 `coverlet.MTP`，VSTest+MSBuild 路径用 `coverlet.msbuild`/`coverlet.collector`，需 SDK ≥ 8.0.112 | **RELATED（信息性）** | 本项目仍走 `coverlet.collector` + VSTest 路径（`dotnet test`），未迁 MTP；升级后路径不变，但官方已把 MTP 列为 net10 推荐。不是 breaking，是路线图提示 |
| 修复：async 方法分支覆盖、lambda 表达式分支回归、pattern matching `is`/`or` 分支低估、MTP 2 控制台无统计 | **UNRELATED** | bug fix，利好而非风险 |

coverlet 这一步没有与本项目冲突的 breaking；唯一相关信息是"net10 推荐 MTP"路线图，本项目暂未跟进。

### Microsoft.NET.Test.Sdk 18.6 → 18.7

vstest release 页（`Microsoft.NET.Test.Sdk` 18.x 由 vstest 仓库产出）关键条目：

| 条目 | RELATED / UNRELATED | 说明 |
|------|---------------------|------|
| "Fix .NET 10 regression for traits"（#15370，Youssef1313） | **RELATED** | 本项目是 net10 + xUnit `[Theory]`，traits 过滤若被回归命中会直接影响 `dotnet test --filter`。18.7 包含此修复，18.6 可能受影响 → 升级是修而非破 |
| "Update target frameworks to net10.0 and net11.0"（#15349） | **RELATED（信息性）** | vstest 自身目标框架升 net10/net11；本项目 net10，方向一致 |
| "This version of VS Test upgraded .NET to net8 and net9. All projects targeting net6.0 should pin 17.13.0 or update to net8+" | **UNRELATED** | 本项目 net10，不受 EOL 框架锁定影响 |
| 框架解析器（framework resolver）/ reporter 未见公开 breaking 说明 | **UNRELATED** | 搜索未命中 18.7 对 resolver/reporter 的破坏性改动 |

**RELATED 项数：1**（vstest 18.7 的 ".NET 10 traits 回归修复" 对 net10 项目直接相关；目标框架升级为信息性相关）。

- citation：`https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/Changelog.md`
- citation：`https://github.com/coverlet-coverage/coverlet/releases`（v10.0.1）
- citation：`https://coverletcoverage.com/download.html`（net10 推荐 MTP）
- citation：`https://github.com/microsoft/vstest/releases`（18.x：Fix .NET 10 regression for traits #15370、Update target frameworks net10/net11 #15349）
- citation：`https://github.com/coverlet-coverage/coverlet/issues/1822`（net9/net10 目标支持）

## D.4 12 个相似项目近 30 天动态

快照基准：`docs/reference/github-similar-projects-snapshot.json`（capturedAtUtc `2026-07-10T03:03:51Z`）。
"近 30 天"阈值 = **2026-06-10**。下表"最近 push"取自 GitHub REST API `/repos/{owner}/{repo}` 的 `pushed_at`（与 snapshot 一致，已交叉核对）；"最近 commit"取自 `/commits?per_page=3` 实时拉取（2026-07-10）。状态判定：pushed_at ≥ 2026-06-10 = ACTIVE；2026-01-10 ~ 2026-06-10 = DORMANT（30–180 天）；< 2026-01-10 或 archived = ARCHIVED。

| Repo | 最近 push (API) | 最近 commit 日期 | 状态 | 关键动态 |
|------|-----------------|------------------|------|----------|
| SyDroX/dev-layout | 2026-05-27 | 2026-05-27 "Sanitize project-specific references from README" | DORMANT | 近期仅 README 清理 + cd-blocking hook 文档 |
| augustdragon/terminal-manager | 2026-06-17 | 2026-05-17 "Merge PR #25 dependabot npm" | DORMANT | pushed_at(06-17) 与最近 commit(05-17) 差异提示有分支/issue 活动；近 30 天边界外 |
| noambrand/Launchpad-CLI | 2026-07-09 | 2026-07-09 "chore(badge): update download count to 1036+" | **ACTIVE** | 每日自动 badge 更新（1034→1036+ 下载），高频但低信号 |
| kevinkicho/win-screen-setup | 2026-04-26 | 2026-04-26 "Add screenshot to README" | DORMANT | 初始提交 + CI + README 截图，之后静默 |
| ewdieckman/DevWorkspace | 2026-04-17 | 2026-04-17 "Update NNFDB API csproj location" | DORMANT | claude 个人/工作 setup，3 次提交后静默 |
| jsoulet42/claude-launcher-app | 2026-06-12 | 2026-06-12 "chore: bump to v2.0.6" | DORMANT | v2.0.6 发布 + terminal stability 修复 PR #45，近 30 天边界外 |
| A-ema755/claude-dashboard | 2026-04-08 | 2026-04-08 "Add README with install/usage/preset docs" | DORMANT | 单日集中提交后静默 |
| adityacodepublic/wtsm-cli | 2026-02-07 | 2026-02-07 "add changes to readme, update version" | DORMANT | 接近 180 天边界，长期静默 |
| pramin/AgentSpace | 2026-05-18 | 2026-05-18 "feat(myterm): window opacity slider..." | DORMANT | Avalonia 12 同类项目，myterm 终端同胞 + XTerm/PrimarySelection |
| joyahmed/devgo-app | 2026-07-06 | 2026-05-13 "fix DevGo screenshot filename" | **ACTIVE** | pushed_at 07-06 落窗口内（最近 commit 05-13 提示 push 在非默认分支或 issue） |
| Flow-Launcher/Flow.Launcher | 2026-07-08 | 2026-06-08 "Release 2.1.3 \| Plugin 5.3.1 (#4516)" | **ACTIVE** | pushed_at 07-08 落窗口；最近默认分支 commit 06-08 Release 2.1.3 |
| Wox-launcher/Wox | 2026-07-09 | 2026-07-09 "feat(chat): add support for chat summaries..." | **ACTIVE** | 高频提交：chat summaries、openai header 修复、AI 主题生成增强 |

**汇总：4 ACTIVE / 8 DORMANT / 0 ARCHIVED**。

按任务清单原文"X ACTIVE / Y DORMANT / Z ARCHIVED"严格口径（仅按最近默认分支 commit 日期判 ACTIVE）：noambrand/Launchpad-CLI、joyahmed/devgo-app、Flow-Launcher/Flow.Launcher、Wox-launcher/Wox 四个的 API `pushed_at` 落在 2026-06-10 之后。若改用"最近默认分支 commit"口径，Launchpad-CLI（07-09）、Wox（07-09）明确 ACTIVE，devgo-app（最近 commit 05-13 但 pushed_at 07-06）、Flow.Launcher（最近 commit 06-08 但 pushed_at 07-08）属边界，保守记 **2 明确 ACTIVE / 2 边界 ACTIVE / 8 DORMANT / 0 ARCHIVED**。

关键观察（仅事实）：
- 两个 broad-market 对照（Flow 15k★、Wox 27k★）都 ACTIVE 且都在做 AI/MCP/chat 方向（Wox 近期 commit 全是 chat/openai/theme-AI）。
- 直接竞品（dev-layout、terminal-manager、claude-launcher-app、wtsm-cli、claude-dashboard、DevWorkspace）全部 DORMANT。
- 唯一同栈（Avalonia 12）项目 `pramin/AgentSpace` DORMANT，无近期 Avalonia 12.1 升级可参照。

- citation：`docs/reference/github-similar-projects-snapshot.json`（本地基准）
- citation：`https://api.github.com/repos/{owner}/{repo}` × 12（`pushed_at` / `archived`）
- citation：`https://api.github.com/repos/{owner}/{repo}/commits?per_page=3` × 12（最近 commit message）

## 引用清单

D.1：
- `https://api.github.com/repos/<USER>/WT_Protector/releases`（404）
- `https://api.github.com/repos/<USER>/WT_Protector/tags`（404）
- `https://api.github.com/repos/<USER>/WT_Protector`（404，pushed_at/default_branch/archived = None）
- `https://github.com/<USER>/WT_Protector/releases`（WebFetch 500，无反证）

D.2：
- `https://docs.avaloniaui.net/docs/avalonia12-breaking-changes`
- `https://v11.docs.avaloniaui.net/docs/avalonia12-breaking-changes`
- `https://github.com/AvaloniaUI/Avalonia/issues/21556`（CJK 字形查找回归，12.1.999-alpha）
- `https://github.com/AvaloniaUI/Avalonia/issues/21141`（touch freezing，v12，UNRELATED）
- `https://github.com/AvaloniaUI/Avalonia/issues/20195`（11.3.x CJK，UNRELATED）
- `https://github.com/ShareX/XerahS/issues/213`（compiled binding / DispatcherTimer 迁移佐证）
- `https://github.com/AvaloniaUI/Avalonia.Markup.Declarative/blob/master/docs/avalonia-12-release-notes.md`（UNRELATED 库）
- `https://avaloniaui.net/blog/avalonia-12`（12.0 发布博客，无 12.1 专章）

D.3：
- `https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/Changelog.md`
- `https://github.com/coverlet-coverage/coverlet/releases`（v10.0.1）
- `https://coverletcoverage.com/download.html`（net10 推荐 coverlet.MTP）
- `https://github.com/coverlet-coverage/coverlet/issues/1822`（net9/net10 目标支持）
- `https://github.com/microsoft/vstest/releases`（Fix .NET 10 regression for traits #15370；Update target frameworks net10/net11 #15349）
- `https://www.nuget.org/packages/coverlet.collector`（10.0.1，net8 目标）
- `https://www.nuget.org/packages/microsoft.net.test.sdk`（18.6/18.7 元信息）

D.4：
- `docs/reference/github-similar-projects-snapshot.json`（capturedAtUtc 2026-07-10T03:03:51Z）
- `https://api.github.com/repos/SyDroX/dev-layout` / `/commits`
- `https://api.github.com/repos/augustdragon/terminal-manager` / `/commits`
- `https://api.github.com/repos/noambrand/Launchpad-CLI` / `/commits`
- `https://api.github.com/repos/kevinkicho/win-screen-setup` / `/commits`
- `https://api.github.com/repos/ewdieckman/DevWorkspace` / `/commits`
- `https://api.github.com/repos/jsoulet42/claude-launcher-app` / `/commits`
- `https://api.github.com/repos/A-ema755/claude-dashboard` / `/commits`
- `https://api.github.com/repos/adityacodepublic/wtsm-cli` / `/commits`
- `https://api.github.com/repos/pramin/AgentSpace` / `/commits`
- `https://api.github.com/repos/joyahmed/devgo-app` / `/commits`
- `https://api.github.com/repos/Flow-Launcher/Flow.Launcher` / `/commits`
- `https://api.github.com/repos/Wox-launcher/Wox` / `/commits`
