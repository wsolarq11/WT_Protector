# WT Protector 全功能现实问题能力评估与一等公民改造建议

Reviewed on: 2026-06-10

Scope: HEAD `22f57a9` 的 Avalonia 主线、根配置、发布/验证脚本和项目文档。独立审计，不抄旧报告（2026-06-09）结论。

> 复核注（2026-08-15）：快照哈希 `22f57a9` 在当前 git 历史中不可解析（`git cat-file -t 22f57a9` 失败，`git log --all` 无此对象），保留为 2026-06-10 审计时的记录，无法在当前历史复核；同期提交 `61f7716`（2026-06-10）仍可解析。

Current evidence used in this pass:

- `git rev-parse --short HEAD` → `22f57a9`; worktree clean on source files.
- `config/launcher.json` → 19 entries / 19 unique ids / 19 existing directories; 2 presets（`core-projects`, `blank-manual`）; settings with `sortMode=custom`, `entryViewMode=cards`, `requireConfirmationForLaunches=false`, 3 commandTemplates, 2 searchHistory entries.
- `git diff --check` → PASS.
- `dotnet test` → 542 tests PASS.
- Full gate `npm run verify` → 16/16 root checks structural PASS (GUI smoke skipped on non-Windows; mainline verify receipt validates SHA256 of published DLL).
- GitHub similar-projects snapshot `capturedAtUtc: 2026-06-09T15:47:55Z` → age: 1 day, within 30-day window.

## 判断标准

一等公民五条铁律：

1. **解决真实、高频、当前存在的问题。** 不是"理论上未来需要"。
2. **数据有唯一权威源，不搞第二套真相。** 一个状态一个 owner。
3. **用户路径显式可见，不靠隐藏 fallback。** 用户能看见、能理解、能控制。
4. **失败在契约边界暴露，不能吞错继续跑。** 异常有回执、脏状态有回滚。
5. **验证能覆盖当前声明，不能靠旧日志和愿望。** 测试/门禁钉住行为。

五星标度：⭐占位/退化 | ⭐⭐严重缺口 | ⭐⭐⭐勉强可用 | ⭐⭐⭐⭐好有小缺口 | ⭐⭐⭐⭐⭐一等公民

## 总体结论

【核心判断】✅ 窄域一等公民，不堆积功能。

当前 23 项能力审计结果：

| 星级 | 数量 | 功能 |
|------|------|------|
| ⭐⭐⭐⭐⭐ | 17 | 1,2,4,5,6,7,8,9,10,14,16,17,18,19,20,21,23 |
| ⭐⭐⭐⭐ | 5 | 11,12,13,15,22 |
| ⭐⭐⭐ | 1 | 3 |
| ≤⭐⭐ | 0 | — |

关键变化 vs 昨天审计（全部 5 星）：

- **F3 (Search/filter) 降为 3 星**：`searchHistory` 字段被收集但从未渲染给用户——这是 JSON 文件里的死数据。无去抖、无 Escape 取消聚焦。
- **F11 (Drag reorder) 降为 4 星**：`LauncherDragReorderWorkflow` 只有构造函数测试，Preview/Commit/Cancel 仅通过集成测试覆盖。
- **F12 (Sort modes) 降为 4 星**：`ParseLaunchedAt`（宽松，返回 MinValue）和 `ParseUpdatedAt`（严格，抛异常）设计不对称；recent 排序没有 ValidateSortProjection 检查。
- **F13 (View modes) 降为 4 星**：Details 列宽硬编码、视图间滚动条容器不一致导致焦点/滚动位置丢失。
- **F15 (Global confirmation) 降为 4 星**：全局开关专项测试仅 1 个、不覆盖 preset 路径、默认回调返回 false 无法区分"未注入 UI"和"用户主动拒绝"。
- **F22 (Command templates) 降为 4 星**：`LauncherCommandTemplateResolver` 零专用单元测试——4 条代码路径仅通过 settings 加载和全栈启动流程间接覆盖。

这不是质量退步。这是更严格的审视——昨天审计把"功能有代码"等同于"功能完美"，本次审计区分了"数据模型正确"和"用户路径完整/测试隔离充分"。

## 功能逐项评估

| # | 能力 | 星级 | 真实问题 | 关键证据 | 改造建议 |
|---|------|------|----------|----------|----------|
| 1 | Launch entry object | ⭐⭐⭐⭐⭐ | 19 entries 证明真实使用；消灭重复 cd/敲命令 | `LaunchEntry.cs` 8 字段（id/name/directory/command/selected/updatedAtUtc/launchedAtUtc/launchCount）、`[JsonRequired]`；Loose Read + Strict Write（未知键跳过；2026-07-23 起不再使用 `UnmappedMemberHandling.Disallow`） | 无——数据结构最小、硬、正确 |
| 2 | Existing-directory contract | ⭐⭐⭐⭐⭐ | 陈旧目录瞬间破坏用户信任 | `LaunchArgumentBuilder.cs` wt.exe 路径探测（LOCALAPPDATA→ProgramFiles→FileNotFoundException）runtime check；`npm run verify` gate 双重防御 | 无——fail-fast 边界位置正确 |
| 3 | Search/filter | ⭐⭐⭐ | 19 entries 需要快速定位 | `LauncherEntryQuery.cs` MatchesFilter 多词匹配（名称/目录/命令）正确；`searchHistory` 当时仅被收集未渲染（死数据）；无去抖、无 Escape 取消聚焦 | 加 150ms 去抖（当前无去抖）；或保持当前实现——完整搜索 history 已经可用 |
| 4 | Direct single launch | ⭐⭐⭐⭐⭐ | 核心动作——找到并启动 | `LaunchArgumentBuilder.cs` 构造 `wt.exe` plan；snapshot + confirmation + receipt 三层契约 | 无——不用加"危险命令分类剧场" |
| 5 | Checkbox selection & batch launch | ⭐⭐⭐⭐⭐ | 每天一键启动一组工作区 | preflight/confirmation/receipt 分离；snapshot 防 mutation 漂移；`LauncherBatchLaunchReport` 不可变 | 可选：`continueOnError` 参数支持"全启"模式 |
| 6 | Named launch groups / presets | ⭐⭐⭐⭐⭐ | 子集需要命名、审查、可移植 | `launcher.json` presets 引用 stable entry ids；`LauncherPresetWorkflow.cs`（Core）校验；Normalize id 消除大小写特殊情况 | 无——这是项目最好的数据结构 |
| 7 | First-class preset actions | ⭐⭐⭐⭐⭐ | 不能要求用户手改 JSON | `MainWindowViewModel.Presets*.cs` CRUD + locked preset + rollback；快照-写入-回滚 3 操作一致 | 无 |
| 8 | Preset-vs-selection preview | ⭐⭐⭐⭐⭐ | 应用前必须预知变化 | `LauncherPresetSelectionPreview` 纯函数；Matched/Missing/Extra 三元组；已匹配时禁用 no-op | 可选：展示差异条目名称（数据结构已备好） |
| 9 | Preset launch plan preview | ⭐⭐⭐⭐⭐ | 批量启动前看到实际命令 | 复用 `PreflightBatch`——预演和实际启动同一路径；预演不启动进程快照验证 | 无 |
| 10 | Add/edit/delete launcher buttons | ⭐⭐⭐⭐⭐ | 配置维护不能只靠 JSON | 7 步统一模式：DiscardDrag→Dialog→Validate→Snapshot→Mutate→SaveChain(rollback)→StatusText | 补充 Clone 专用测试（当前 0 条） |
| 11 | Custom order & drag/non-drag reorder | ⭐⭐⭐⭐ | 空间记忆 > 字母序 | `customEntryOrder` 单一 owner；lazy preview 防无差别重排；context-menu reorder 共用模型 | 补充 `LauncherDragReorderWorkflow.Preview/Commit/Cancel` 隔离单元测试 |
| 12 | Sort modes | ⭐⭐⭐⭐ | 自定义/最近/更新时间/字母序/使用频率五种真实扫描 | 5 mode 契约（custom/recent/updated/alphabetical/frequent）；NormalizeSortModeId 白名单；但 ParseLaunchedAt 宽松隐晦排序 | 统一 `ParseLaunchedAt` 和 `ParseUpdatedAt` 的严格度 |
| 13 | View modes: Cards/Details/Names | ⭐⭐⭐⭐ | 可读操作/紧凑扫描/一眼总览三种任务 | 单一 `CurrentEntryViewMode` 驱动 3 个只读投影；save-rollback 完整 | 统一三种视图的 ScrollViewer；Details 列宽从 DesignTokens 引用 |
| 14 | Adaptive card sizing & density | ⭐⭐⭐⭐⭐ | 窗口变化不能挤烂卡片 | `LauncherEntryLayoutCalculator` 纯函数；单一 `_layoutDensity` enum 消 bool 组合爆炸；`FastWrapPanel` 消除 O(n²) | 给 LayoutCalculator 加纯函数级单元测试 |
| 15 | Global launch confirmation | ⭐⭐⭐⭐ | 启动安全规则必须唯一、可见 | 全局开关 + snapshot/rollback；per-entry `confirm` 已拒绝；但 1 个专项测试不覆盖 preset 路径 | 补充 preset 路径测试；区分"回调未注入"和"用户拒绝" |
| 16 | Portable settings | ⭐⭐⭐⭐⭐ | 偏好跟仓库走，不藏 AppData | `LauncherSettings` POCO 单源；31 个设置测试覆盖所有 null/空白/大小写/未知字段拒绝 | 无 |
| 17 | JSON persistence, backup & rollback | ⭐⭐⭐⭐⭐ | 配置半写比功能缺失更糟 | temp-file→`.bak` backup rotation→`File.Replace` 原子写入；3 代备份（`FileLauncherJsonIO.DefaultMaxBackups=3`）；save-first-then-commit 回滚（`StoreGrain.Dispatch`，快照类 2026-07-21 移除） | 无——单进程本地工具足够 |
| 18 | Core/App/Tests separation | ⭐⭐⭐⭐⭐ | 启动参数/排序/配置规则必须可测试 | Core 零 Avalonia 依赖；542 tests；Test 结构镜像生产架构 | 无 |
| 19 | Self-contained win-x64 publish & native gate | ⭐⭐⭐⭐⭐ | 桌面工具必须产出可运行 exe | `npm run release:build` SHA256 receipt；`npm run verify` checks + GUI smoke；双 receipt 路径 | 无 |
| 20 | Retired legacy surfaces | ⭐⭐⭐⭐⭐ | HTA/WinForms 留作活路径 = 第二套真相 | `retired/` 归档；root gate 阻止 4 个旧路径复活；退役文档完整性验证 | 无 |
| 21 | Design benchmark & giants-shoulders | ⭐⭐⭐⭐⭐ | 设计判断不能靠感觉 | 12 基准→10 本地验收规则（`design-benchmark-digest.md`）；12 工具对标→本地翻译（`launcher-first-class-patterns.md` 12 借用模式表）；xUnit 预设套件锁定 presets 结构 + 门禁 `presets-schema`/`security-audit-f2` | 考虑 CI 中自动检查快照新鲜度 |
| 22 | Command templates | ⭐⭐⭐⭐ | 长命令重复制造错误 | `LauncherCommandTemplateResolver` 纯函数；unknown key fail-fast；`$key` 预览 | 补 20 行 xUnit 合约测试 |
| 23 | Design tokens | ⭐⭐⭐⭐⭐ | 颜色/间距分散→视觉漂移 | `LauncherDesignTokens.cs` 93 行单一源；`App.axaml.cs` 注入→XAML `{StaticResource}`；合约测试禁止硬编码（`VisualTokenContractTests`） | 无 |

## 剩余缺口账本

| 缺口 | 类型 | 是否现在做 | 原因 |
|------|------|------------|------|
| `searchHistory` 无 UI 表面 | 实现缺口 | 建议 | 数据已被收集但不对用户产生价值——这是浪费 |
| `LauncherDragReorderWorkflow` 无隔离测试 | 测试缺口 | 建议 | 当前仅集成测试覆盖，回归检测能力弱 |
| ParseLaunchedAt/ParseUpdatedAt 不对称 | 设计缺口 | 建议 | 非对称严格度制造隐晦行为差异 |
| Details 列宽硬编码 + 滚动条不一致 | 实现缺口 | 低优 | 不影响功能正确性 |
| Clone entry 零测试 | 测试缺口 | 建议 | Clone 流程无隔离验证 |
| Command template resolver 零测试 | 测试缺口 | 建议 | 4 条路径仅间接覆盖 |
| `selected` 与 entry spec 同文件 | future-workload | 否 | 19 entries 无真实 diff 痛感 |
| 动态命令模板 `{query}` | future-workload | 否 | 无真实启动时输入参数工作负载 |
| import/export 生态 | broad-scope | 否 | 推平台化 |
| 持久 run-history / terminal health | external-process-limit | 否 | `wt.exe` handoff 后不可证明 |
| installer/update/signing | distribution-scope | 否 | 分发能力，非启动器正确性 |

## Linus 式方案

1. **先固定数据结构**：`launcher.json` 单一统一源（entries + presets + settings + defaultPresetId）。不新增第二个状态文件。
2. **消除特殊情况**：保留全局 confirmation，不恢复 per-entry confirm；command template unknown key 直接 fail-fast；id 统一 normalize 消除大小写分支。
3. **用最笨但清楚的方式实现**：Core 纯规则→ViewModel mutation+rollback→View 交互边界。PreflightBatch 一函数用于预演和实际启动——消灭"预演路径≠实际路径"这个 bug 类别。
4. **确保零破坏性**：任何改造先证明不破坏 `config/launcher.json` schema、preset 引用、custom order、launch preflight、root native gate。

## 不要做的蠢事

- 不做插件系统。
- 不做 broad launcher 平台。
- 不做隐藏 fallback、默认模板、自动修配置。
- 不把 `selected` 现在就拆出去——只因为理论更漂亮。
- 不拿"已请求启动"冒充"终端会话健康完成"。
- 不给 searchHistory 加复杂搜索功能——简单的 autocomplete dropdown 足够。

## 当前结论

WT Protector 对"可移植 Windows Terminal 工作空间启动器"窄域，23 项功能全部 3 星以上、17 项 5 星、0 项存在严重缺口。真实代码缺口不是新功能，而是：

1. **6 项 3-4 星功能有小缺口**——都是测试不足或用户路径不完整，不是数据结构错误；
2. **保持 root gate 新鲜**——提交/发布前跑 `git diff --check` + `npm run verify`；
3. **做最小的改进**——给 searchHistory 加 autocomplete、补 3 个隔离单元测试、统一 sort 严格度；
4. **继续拒绝** broad launcher / 插件平台 / 静默 fallback / 预铺 speculative 功能。

