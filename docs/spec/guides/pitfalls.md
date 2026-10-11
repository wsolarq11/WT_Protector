# 跨会话踩坑实录 — WT_Protector

> 来源：本仓库及关联项目 50+ 真实 session，经项目 session journal 取证。每条坑有 session-id 可追溯、有根因、有预防规则。禁止硬编码"N 条/N 类"；写"持续更新"。

## 维护规则

1. 踩坑 → 按章节追加 → 带 session-id/证据 → PR/commit 引用本文。
2. 禁硬数字；说"持续更新"。
3. 根因已从代码层消除 → 标 `[已修复]` 不删除，保留根因。
4. 需改编码规范 → 同步更新 `core/quality-guidelines.md` 或 `views/quality-guidelines.md`。

## 1. 工具层坑

### §1.1 Windows CRLF
- 证据：phoenix-crawler `0c49eee5-6fa`、`645a9dfe-21f`（9+ 次）
- 根因：Windows 实际 CRLF、Read 显示 LF，Edit 匹配换行不一致
- 预防：Edit `String to replace not found` → 立即整文件 Write，不修参数

### §1.2 `Read.pages: ""` 空串报错
- 证据：phoenix-crawler `45380b66-51b`、`ee372f15-393`（15+ 次）
- 根因：`pages` 仅 PDF 页码范围用，空串在 schema 层即死
- 预防：非 PDF 不传 `pages`

### §1.3 并行工具调用 >5-6 个
- 根因：单轮并行上限，超限触发限流或部分返回 null
- 预防：大扇出用 `Workflow.parallel()`，不用裸并行 Agent/Bash

### §1.4 `codebase-memory-mcp` project 名写错
- 证据：temp_codex `019eab3f-2d8`（3+ 次）
- 根因：真实索引名是 `D-_AI_-_projects-xxx` 转义格式，非短名
- 预防：先 `list_projects` 取精确名不猜；3 个 MCP 工具全 120s 超时 → 首疑 project 名

### §1.5 PowerShell 串联 `;` 非 `&&`
- 证据：poollink `019e3c29-0e0`
- 根因：PS 5.x 不支持 `&&`（Bash 语法）
- 预防：PS 用 `;`；跨平台用 `pwsh`

### §1.6 Deferred tools 报 `InputValidationError`
- 根因：schema 未加载，必须先 `ToolSearch` 再调用
- 预防：提示 deferred tools 可用 → 先 `ToolSearch(query="select:ToolName")`

### §1.7 Codex SessionStart 脚本存在 ≠ 已接线
- 证据：2026-07-13 `07-13-codex-trellis-setup`；`.codex/hooks/session-start.py` 存在但 `.codex/hooks.json` 只注册 `UserPromptSubmit`
- 根因：把生成文件存在性误当平台 settings 已注册
- 预防：验收 6 项——CLI/模板版本、项目 trusted、`features.hooks=true`、`.codex/hooks.json` 同时注册 SessionStart+UserPromptSubmit、两脚本返回匹配 `hookEventName`、交互 `/hooks` 信任；`hooks.json` 是 intentional 本地定制，更新时合并不 `--force` 覆盖

### §1.8 管道取退出码失真
- 证据：2026-09-17 门禁 session；`npm run verify 2>&1 | Select-String ...` 后 `$LASTEXITCODE` 报 0 而实际 FAIL/1
- 根因：PS 管道 `$LASTEXITCODE` 反映末端 cmdlet，非被管道命令
- 预防：`npm run verify > out.json 2>$null; $code = $LASTEXITCODE`；筛选先落文件再 `Select-String`

## 2. WT_Protector 项目坑

### §2.1 Entry/Preset ID 绕过 Store 直写
- 证据：Codex `019e79ae-773`（2026-05-30）
- 根因：VM/Workflow 散落绕过 `LauncherStore` 的写路径，不经验证/原子化/Normalize
- 预防：所有 JSON 写必须走 Store 层，禁 VM 里 `File.WriteAllText`

### §2.2 dirty JSON 规范化不一致
- 证据：同 §2.1 `019e79ae-773`
- 根因：多写点对缩进/key 排序/null vs 缺失/`""` vs `null` 处理不一致
- 预防：规范化集中在 `LauncherJson` 一处

### §2.3 KISS 合并后 catch 过宽
- 证据：commit `a8624d2`、`86d75c2`
- 根因：`TrySaveWindowBounds` 裸 catch 吞异常
- 预防：永不裸 `catch (Exception)`；窄化为 `IOException | UnauthorizedAccessException | SecurityException`

### §2.4 硬编码常量与 DesignTokens 漂移
- 证据：`guides/index.md` 已有记录
- 根因：XAML 写死 `#2563EB`，token 变更不同步
- 预防：XAML 只用 `{StaticResource PrimaryBrush}`；新增常量前 `grep -r`

### §2.5.1 `DirectoryNotFoundException` 冗余于 `IOException`
- 证据：Phase 2 实施，`guides/index.md §5`
- 根因：`DirectoryNotFoundException` 继承 `IOException`，`or` 模式中永无法匹配
- 预防：`IOException` 是最后层即可，去掉 `or DirectoryNotFoundException`

### §2.5.2 删除 AppStore 外观方法时测试代码是最大风险
- 证据：Phase 2，`AppStore.Mutate` → `Dispatch` 迁移
- 根因：8 处测试 + 4 处生产同报错，测试常被 grep 漏掉
- 预防：删非私有方法 → `grep -rn "\.MethodName\("` 全仓库（含 tests）一起改验

### §2.5.3 CancellationToken 参数传播需全链修改
- 证据：Phase 2，`AgentDiscoveryScanner.Scan` + `CancellationToken`
- 根因：加可选参数 → xUnit1051 要求测试传 token → 委托签名/测试工厂连锁变
- 预防：加 public 可选参数三件事——查 xUnit 分析器效果、更新所有 `Func<>` 签名、测试工厂 + 全部调用点一起改

### §2.5.4 AOT 安全 JsonSerializer 重载选择
- 证据：Phase 1，IL2026/IL3050
- 根因：`Deserialize<T>(stream, options)` / `Serialize(value, typeof(T), options)` 带 `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`
- 预防：用 `JsonTypeInfo<T>` 重载（`GetTypeInfo(typeof(T))`），不传 `JsonSerializerOptions`；新类型加 `[JsonSerializable]`

### §2.5.5 StoreGrain Dispatch 后必须 SetState
- 证据：07-24-vtuner-killer-impl，AppStore→StoreGrain 迁移，35 测试失败
- 根因：`Dispatch` 落盘但未写 `Grain<T>` 私有 `_state`，`Select` 读到旧态
- 预防：`Dispatch`/`DispatchErrorOr` 末尾 `SetState(after)` 且在 `LauncherStore.Save` 之后

### §2.5.6 `async void` 事件处理必须 ExceptionFilters 保护
- 证据：07-24-vtuner-killer-impl，4 dialog 文件 11 处
- 根因：Avalonia 对话框 `async void` 签名，未处理异常崩溃进程；`Dispatcher.UnhandledException` 不可靠
- 预防：所有 `async void` 事件处理 `try-catch(ExceptionFilters.IsRecoverable)` + `ErrorLog.Write`

### §2.6 OLE DragDrop 嵌套消息循环是性能杀手
- 证据：`5d6ef926-604a`（2026-06-26），6 轮优化
- 根因：`DoDragDropAsync` 启动 Win32 OLE 嵌套消息循环，输入消息优先、渲染回调降级；隐藏 450+ 元素零改善证明瓶颈在消息循环
- 预防：应用内 reorder 永远 Pointer capture 不用 OLE（OLE 仅跨应用）；配 `ForceCleanup()` 双路径释放 + handler try-catch

### §2.6 性能排查方法论：先消除，后优化
- 证据：同上 session，4 轮无效优化后才做消除测试
- 根因：认知偏差默认瓶颈在"可见代码开销"非"不可见系统行为"
- 预防：第 1 轮做消除测试（隐藏组件测 delta）→ 定位层 → 分析该层开销 → 针对性修复

### §2.7 [已反转] LiteDbStore 非原子写入
- 状态：LiteDB 已于 2026-07-09 移除（`07-09-remove-litedb-deadcode`），保留供历史参考
- 证据：UltraReview 2026-07-05，LiteDbStore.cs:62-63
- 根因：`DeleteAll()` + `Insert()` 无事务，中间崩溃丢数据
- 预防：所有 DB/文件写必须原子（JSON 用 `File.Replace`）；绝不假设两步不中断

### §2.8 AutoCompleteBox Enter 检测 e.Source 指向内部 TextBox
- 证据：UltraReview 2026-07-05，MainWindow.axaml.cs:178
- 根因：`ReferenceEquals(e.Source, SearchTextBox)` 恒 false（真实焦点是内部 TextBox）
- 预防：复合控件（AutoCompleteBox/ComboBox）别用 `ReferenceEquals` 匹配组件

### §2.9 架构变更必须含迁移计划
- 证据：UltraReview 2026-07-05，F06 三文件→统一 store 无迁移
- 根因：无代码处理旧配置，启动即弃旧数据
- 预防：存储格式变更 PRD 必含迁移；迁移是功能一部分，`implement.md` 排第一批

### §2.10 文档同步是代码变更必要组成
- 证据：UltraReview 2026-07-05，D01-D10 十项文档脱节
- 根因：三文件→统一 store、OLE→Pointer、searchHistory 变更后文档全旧
- 预防：文档修改是 AC 一部分；每 batch 检查"是否需更新文档"；gate 检 README/spec 关键条目

### §2.11 IStore 接口实现必须验证参数
- 证据：UltraReview 2026-07-05，F01 LiteDbStore 忽略 storePath
- 根因：构造固定连接，Load/Save 丢弃参数静默读写固定库
- 预防：实现必须用/验证全部参数；留意"构造固定实现参数而接口参数被丢弃"

### §2.12 单元测试必须覆盖加载失败路径
- 证据：UltraReview 2026-07-05，F13 60+ 测试文件无加载失败测试
- 根因：套件假设 JSON 始终有效
- 预防：持久化组件测成功 + 失败（corrupt/empty/backup/locked/50MB）

### §2.13 跳过 ApplyFilter 必须验证所有派生投影重建
- 证据：FMA 2026-07-12，`MainWindowViewModel.Drag.cs`
- 根因：`CommitDragReorder` 返回 `RefreshEntries=false` 跳过 `ApplyFilter`，卡片视图 `VisibleGroups` 未重建
- 预防：`CommitDragReorder` 无条件 `ApplyFilter()`；任何条件跳过须验证 `VisibleGroups`/`VisibleCount`/`SelectVisibleCommand`/`IsVisibleEmpty`

### §2.14 `[ObservableProperty]` 不能有后备字段访问的转发方法
- 证据：2026-07-21 MWVM Stage 5，`MVVMTK0034`
- 根因：分析器禁类内直接访问 `_field` 后备字段；绕过生成属性的方法触发错误
- 预防：构造顺序 (1) new 协作类 → (2) 设初始属性（`OnXxxChanged` null 守卫返回）→ (3) 赋值桥接委托 → (4) 订阅事件；禁 `SetXxxDirect(bool)`

### §2.15 DynamicData ObserveOn 在测试中 SynchronizationContext==null 崩溃
- 证据：Trinity Phase 2，EntryProjection 测试崩溃
- 根因：`.ObserveOn(SynchronizationContext.Current)` 在 xUnit 收 null
- 预防：`syncCtx is not null ? pipeline.ObserveOn(syncCtx) : pipeline`

### §2.16 CsCheck 随机字符串生成器产生路径分隔符
- 证据：Trinity Phase 4，`SyncWithEntries` PBT 报 `ID contains path separator`
- 根因：`Gen.String` 可生成 `/`、`\`、`..` 被 `EntryId` 拒绝
- 预防：用业务特定生成器（`Gen.Int[0,N].Select(i => $"id{i}")`）；`Gen.String` 不满足业务约束

### §2.17 MemoizedSelector 缓存键不完备
- 证据：2026-07-25 卡片布局错乱，`LayoutManager.TryRefreshCardSize`
- 根因：缓存键 `(availableSize, visibleCount)` 不含对象身份，`ReplaceEntries` 新 VM 命中旧缓存
- 预防：窗口几何/条目数派生的尺寸禁存 `[ObservableProperty]`；用共享 `CardLayoutProvider` 只读委托；memoizedSelector 已删（计算廉价）

### §2.18 防漂移检查自身输入漂移 → 静默假绿
- 证据：2026-09-17，`checkNoStaleThreeFileRefs`
- 根因：硬编码文档路径列表 + 目标缺失静默 `continue`，检查缩小覆盖面照报 PASS
- 预防：扫描类检查目标缺失必须抛错；只允许内容过滤跳过与"跳过但写进 detail"两类

### §2.19 `dotnet test`/`build` 隐式还原改写 `packages.lock.json`
- 证据：2026-09-17，删 EntryProjection + DynamicData 后
- 根因：隐式还原用默认参数（无 RID/AOT），lock 内容随之变
- 预防：C# 构建/测试一律 `--no-restore`，先显式完整参数还原；唯一权威判据 `git diff --numstat -- '*.lock.json'`；`--force-evaluate` 写 CRLF 不影响 git 判定

### §2.20 门禁 stdout 被 native 子进程污染
- 证据：2026-09-17，装 .NET SDK 后首次完整 native 层
- 根因：`stdio: "inherit"` 让 dotnet 日志直通 stdout，混在 JSON receipt 前
- 预防：机器可读数据 + 会写 stdout 的子进程不能假设 stdout 纯净；加定界标记或子进程日志导 stderr

### §2.21 `global.json` 放子目录等于没钉固
- 证据：2026-09-17，SDK 钉固长期未生效
- 根因：`global.json` 向下生效、向上查找；dotnet 从仓库根 cwd 走不到子目录那份
- 预防：在**实际执行命令的 cwd** 跑 `dotnet --version` 验证；同陷阱适用 `.editorconfig`/`Directory.Build.props`/`.npmrc`

### §2.22 整文件回滚连带丢弃同文件需要的改动
- 证据：2026-09-17，删 DynamicData 之后
- 根因：同文件两类改动（要丢的隐式还原污染 + 要留的依赖变更），`git restore` 全抹
- 预防：回滚前问"全部改动都想要丢吗"；混在则 `git restore -p` 或先固化要留部分；生成物尤其危险

### §2.23 断言"违规不存在"的检查必须自证能命中
- 证据：2026-09-17，扫 `scripts/checks/static.ts` 8 条正则检查活性
- 根因：PASS 双义（确实无违规 vs 匹配器/扫描目标失效）；只断言结果为空未断言证据链有效
- 预防：三件套——正向 fixture（命中）、反向 fixture（不误伤）、`scannedFiles > 0`；自检用非全局正则副本（`test()` 推进 `lastIndex`）

### §2.24 测了却不留痕等于没测
- 证据：2026-09-17，`verify-mainline.ts` 的 perf 被 run.ts 抄录丢弃
- 根因：跨结构抄录无机制保证字段覆盖，新增字段默认不被抄
- 预防：抄录优先整体展开或逐字段对账；关键测量有一处持久留痕（receipt/日志）

### §2.25 文档引用可移动路径，归档一次全断
- 证据：2026-09-17，8 处任务台账 `.md` 引用失效
- 根因：引用的"可解析性"从未被断言
- 预防：文档引用可移动路径前先确认目标长期存在；归档动作发生当次同步引用路径

### §2.26 删检查 vs 删产品代码——引用密度 + 分析器硬编码是判据
- 证据：2026-09-17；StoreGrain（29 引用 + 分析器硬编码 → 删检查）；AppStateMachine（0 引用 → 删产品）；trinity 其余 4 组件（2-12 引用无硬编码 → 保留检查）
- 判据：广泛引用 + 分析器硬编码 → 删检查；定义文件外 0 引用 → 删产品死代码；少量引用无硬编码 → 保留检查防内联/合并
- 预防：删前跑外部引用计数 + 查分析器硬编码；`addCheck` name 统一对齐 step id

### §2.27 私有仓库触发 CI 必须先转公开
- 证据：2026-09-30 拖拽整卡化收口，run `36724477560` 私有 push 触发 CI failure（`runner_id=0` / `steps=0`，runner 从未分配），转公开 rerun 后 success
- 根因：私有仓库 Actions 账单拦截（`recent account payments have failed`）导致 runner 不分配，失败与代码无关；先私有跑再转公开，白白多一次失败 run + 重跑等待
- 预防：凡将触发 CI 的 push/rerun，前置动作一律先转公开（`gh repo edit wsolarq11/WT_Protector --visibility public --accept-visibility-change-consequences`），CI 绿后再转回私有；识别账单拦截 = job 的 `runner_id=0` 且 `steps` 为空

## 3. 关联项目可迁移教训

### §3.1 不要在文档里硬编码"N 条/N 类"
- 证据：poolswitch `019e7521-316`（"13 条"/"55 类"）
- 预防：写"持续更新"；`docs/spec/` 与 `AGENTS.md` 禁硬数字

### §3.2 认证错误不是网络参数错误
- 证据：clashverge `019eaba8-f28`
- 根因：两系统 `OPENAI_API_KEY` 不一致，调 retry/cooling 无效
- 预防：先验证认证层（直打 API 测 401/200），再调网络参数

### §3.3 同一个坑三次 = 流程/目标缺失
- 证据：poollink `019e7c6c-c69`（三次无 remote 仍 push）
- 预防：该标 blocked 就标 blocked；外部依赖/授权/远端反复阻挡时不绕过、不编造

### §3.4 大规模机械 edit 引入连锁编译错误
- 证据：phoenix-crawler `2ec9bb38-226`
- 预防：机械替换后立即 `cargo check`/`dotnet build`，不攒多步再查

### §3.5 编辑联动失误——删除函数体+引用分两步
- 证据：phoenix-crawler `7b98e04d-a30`
- 预防：函数定义 + 引用点联动删除一次 Edit 完成；删 C# 类/方法 + 更新 using/引用一枪打

### §3.6 Feature-gate stub 签名不匹配
- 证据：phoenix-crawler `30f0127e-83b`
- 预防：stub 每个 public 方法签名与原模块一致；条件编译/DI 切换后立即 compile check

### §3.7 `scripts/`/`tools/` 被第三方依赖污染
- 证据：zero_to_first_revenue `019e5e0b-c89`
- 预防：目录迁移前 `--exclude` node_modules/.git/__pycache__；`tools/`、`scripts/` 只放自写工具

### §3.8 UTF-8 BOM 被意外去掉
- 证据：poollink `019e6ad8-80e`
- 预防：PowerShell 脚本需 UTF-8 BOM；`git diff` 首行字符级变化时警惕；`verify.ps1` 提交前查 BOM

### §3.9 `grep -c "Add-Check"` 把函数定义也算进去
- 证据：wt-protector `c38ce17d-7e8`（2026-07-09）
- 预防：计数用锚定正则排除定义（`grep -cE "^\s*Add-Check\s+['\"]"`）或直接解析 receipt 的 checks 数组长度

### §3.10 PRD 引用 spec 前必须验证存在性（phantom reference）
- 证据：wt-protector `c38ce17d-7e8`（8 处引用从未存在的文件）
- 预防：引用前 Glob/`ls` 验证；更稳的是准则内容直接内联；删文件时 grep 反向引用

### §3.11 .NET 10 NuGet audit 经 MSBuild 属性触发
- 证据：wt-protector `52e06929-0c1e`（2026-07-10）；`dotnet restore --audit` 是 unknown option
- 预防：audit 由 `Directory.Build.props` `<NuGetAudit>true</NuGetAudit>` + `<NuGetAuditMode>all</NuGetAuditMode>` 触发；禁 `--audit` CLI 标志

### §3.12 xunit.v3 xUnit1051 = CancellationToken 传播
- 证据：wt-protector `52e06929-0c1e`（2026-07-10）
- 预防：异步测试方法加 `CancellationToken` 传 `TestContext.Current.CancellationToken`；v3 规则与 v2 不完全兼容

### §3.13 快照恢复空桩导致回滚测试假绿
- 证据：wt-protector `289f3860`（2026-07-19 P5）
- 预防：`Restore*`/`RestoreSnapshot` 必须有回归测试证明真实回滚；空桩 = 未完成

### §3.14 PBT 假属性测试——生成器值被丢弃
- 证据：wt-protector 2026-07-26；`SerializeDeserializeRoundtrip` 丢弃随机计数
- 预防：`.Sample(assert: ...)` 的生成值必须实际用于构造；审查断言是否有来自输入的变量引用

## 4. 元教训

### M1 基于模式的阻断会被绕过；基于能力的授权不会
- 来源：Figma Plugin Security（phoenix-crawler `b006771a-85c`）
- 适用：验证/门禁查"能力有没有"，不查"用法像不像坏"

### M2 不要削弱 gate 换速度
- 来源：Poollink
- 适用：fresh native receipt 才能声明当前 PASS，不用旧日志冒充

### M3 持久化 > 记忆
- 来源：项目核心原则
- 适用：Conversations 会 compact，文件不会；决策/教训/约定必须落盘

### M4 验证诊断顺序：认证 → 连通 → 参数 → 性能
- 来源：clashverge CPA 认证坑
- 适用：先 curl API 验 200 vs 401，再调性能参数

### M5 未读 = 先读
- 来源：CLAUDE.md
- 适用：Edit 前未在当前 session Read 过文件 → 先 Read，无例外

### M6 不拿旧 PASS 冒充当前验证
- 来源：Poollink 交接 session
- 适用：汇报"当前 PASS"必须证据是刚跑的命令输出

### M7 简单 > 聪明
- 来源：Linus Torvalds
- 适用：if-else 够用不引设计模式

### M8 快照必须在 await 之后
- 来源：2026-06-23 ultrareview B6/B10/B11
- 修复：快照移到 `await` 对话框确认后；先 await 确认再 capture 快照

### M9 布尔标志组合爆炸 → enum 消除
- 来源：2026-06-23 ultrareview Q2
- 修复：`CompareEntries` 4 bool → `LauncherSortMode { Recent, Updated, Alphabetical, Custom }`；≥2 互斥 bool 用 enum

### M10 收集数据但不消费 = 纯浪费
- 来源：2026-06-23 ultrareview E1/Q6（searchHistory）
- 修复：收集时机改为仅 Enter 写，补 AutoCompleteBox 消费端

### M11 UX 缺陷和正确性缺陷同等严重
- 来源：2026-06-23 polaris F13b/E8
- 修复：F13b 每视图 `_scrollOffsets`；E8 移除 `FailedCount>0` 跳过强制确认；"用户能忍"不是拒修理由

### M12 Spec 不变量必须有 gate 强制层
- 来源：2026-07-10 `07-10-discovery-scan-narrow-catch`（`266aa6f`）
- 修复：裸 catch 不变量 + `no-bare-catch-exception` gate；每条不变量问"gate 在哪强制"

## 5. UltraReview 全量修复（2026-06-24）

### §5.1 可变 API → 不可变 API 的变更传播
- 证据：UltraReview H2，ImmutableList 迁移
- 预防：`void` → `T` 时每个调用方确认返回值被消费（C# 不警告丢弃返回值）；可用 `[MustUseReturnValue]`

### §5.2 Frozen snapshot 破坏 `with { }` 引用一致性
- 证据：UltraReview，`DiscardDragReorderPreview` bug
- 根因：`with { }` 建新实例，workflow 持有旧引用被覆盖
- 预防：持有别组件 record 引用时每次变更后同步；首选不持有引用，getter 访问

### §5.3 `?` 可空标记的级联效应
- 证据：UltraReview M3，28 测试失败
- 预防：消除 `null!` 的正确方向是消除 null 源（保证 Normalize 后永非 null），不是给类型加 `?`

### §5.4 `.bak` 测试契约与实现语义耦合
- 证据：UltraReview C6
- 预防：模拟 I/O 失败阻塞核心路径（`File.Delete` + `Directory.CreateDirectory(mainPath)`），不依赖非关键路径副作用

### §5.5 修改序列化 record 字段 → 合约测试必挂
- 证据：2026-06-24 L33/M11（93 与 57 测试失败）
- 预防：新字段默认 `[JsonIgnore]`，显式 opt-in 序列化；运行时状态字段一律 `[JsonIgnore]`

### §5.6 异常类型是公共 API
- 证据：2026-06-24 M24，153 测试失败
- 预防：统一异常类型前审计所有 `Assert.Throws<T>`；价值小于成本则不做

### §5.7 `CanXxx` 和 `DoXxx` 守卫逻辑必须一致
- 证据：2026-06-24 C2
- 预防：`CanXxx` 检查操作执行所需全部前置条件；`DoXxx` 有 `FirstOrDefault ?? throw` 则 `CanXxx` 验证非 null

### §5.8 `File.Replace` 要求目标已存在
- 证据：2026-06-24 L1，163 测试失败
- 预防：`File.Replace` 仅目标存在时；首次创建用 `File.Move`

### §5.9 校验延迟到持久化层 = 崩溃风险
- 证据：2026-06-24 H5/M8
- 预防：校验在突变点执行，持久化层校验是最后兜底（防手改 JSON 绕过 UI）

## 6. UltraReview 二次清零（2026-06-25）

### §6.1 `[JsonIgnore]` + SchemaVersion = 死代码版本检查
- 证据：2026-06-25 R1
- 预防：SchemaVersion 绝不 `[JsonIgnore]`；向后兼容不加 `[JsonRequired]` 靠默认值

### §6.2 Store.Load 损坏文件不触发备份恢复
- 证据：2026-06-25 R3
- 预防：Load 异常处理覆盖"文件存在但反序列化失败"；`.bak` 就是为此存在

### §6.3 跨文件 ID 规范化契约必须一致
- 证据：2026-06-25 R10
- 预防：同一概念规范化函数行为一致；截断/trim/消控字符不抛异常

### §6.4 `SaveConfig()` 返回值必须检查
- 证据：2026-06-25 R6
- 预防：任何 `Save*()` 返回值检查；失败不得覆盖已有错误状态文本

### §6.5 静态可变 `List<T>` 线程不安全
- 证据：2026-06-25 R14
- 预防：永不静态可变集合；缓存用 `ImmutableList`/`FrozenSet`，返回值用元组/record

### §6.6 批处理失败策略必须一致（break vs continue）
- 证据：2026-06-25 R9
- 预防：同 workflow 多阶段同失败策略，不同需注释说明；统一为 continue

### §6.7 内部标记注释（`// H4`）对维护者无意义
- 证据：2026-06-25 R30
- 预防：注释描述做什么/为什么，不引用外部 issue ID；引用写完整 URL

### §6.8 Unicode 正规化缺位 → 搜索排序不一致
- 证据：2026-06-25 R26
- 预防：Normalize/Sanitize 必须 `Normalize(NormalizationForm.FormKC)`

### §6.9 命令行长度无上限 → CreateProcess 静默失败
- 证据：2026-06-25 R27
- 预防：`arguments.Length > 32767` 抛错；构造系统调用参数前查目标 API 限制

### §6.10 XAML TimeSpan 解析隐式假设 → 发布 exe 崩溃
- 证据：2026-06-26 smoke
- 预防：XAML TimeSpan 用 `h:m:s.ms` 显式格式，禁裸整数（`"150"`=150 天）

### §6.11 `SaveConfig()` 返回值被丢弃
- 证据：2026-06-26 H4，`Selection.cs:142`
- 预防：`if (startedCount > 0 && !SaveConfig()) return;`；所有 `Save*`/`Try*` 返回值检查

### §6.12 `LoadWithRecovery` catch filter 遗漏异常
- 证据：2026-06-26 H1，`LauncherJson.cs:63`
- 预防：catch filter 枚举覆盖被调 API 全部异常含继承链（`DirectoryNotFoundException` 继承 IOException）

### §6.13 默认实例绕过验证管道
- 证据：2026-06-26 H3
- 预防：fallback/default 值经过与正常路径相同 Validate/Normalize

### §6.14 `ForUser()` fallback 泄漏敏感信息
- 证据：2026-06-26 H8
- 预防：异常→用户消息白名单（已知安全透传，其他通用消息），禁黑名单

### §6.15 Smoke 配置缺必需字段 → 门禁假绿
- 证据：2026-06-26 smoke settings.json
- 预防：JSON schema 变更后搜并更新所有生成该 JSON 的 fixture/脚本/README

### §6.16 诊断洋葱——第一个 Bug 掩盖第二个
- 证据：2026-06-26 smoke 双重修复
- 预防：修 gate-blocking bug 后完整重跑 gate，不假设一次绿；每次 failure 是解锁下一隐藏 bug 的钥匙

### §6.17 不对称操作路径——成对代码未同步
- 证据：2026-06-26 H4（single vs batch `SaveConfig`）
- 预防：加"X 的批量版"逐行对比原版错误处理/返回值检查；成对路径 review 时 diff

### §6.18 跨层验证不一致
- 证据：2026-06-28 H6/C14（保留名 case、`>1000` vs `>=1000`）
- 预防：跨层验证单一权威源（共享常量/方法）；加跨层一致性测试；`>N` vs `>=N` 视为 bug

### §6.19 应用层事务协议 vs 文件系统原子性
- 证据：2026-06-28（CommitJournal 165 行被 File.Replace 取代）
- 预防：发现实现"跨文件事务"先问能否合并单文件；单文件 + 原子 rename 优于多文件 + 应用层事务

### §6.20 不可变记录拷贝必须传播所有字段
- 证据：2026-06-28 C1（`CreateEntry` 漏 `LaunchedAtUtc`）
- 预防：新增字段后 grep `= new RecordType`/`= new()` 确认传播；工厂接受 previousEntry 并注明传播行为

### §6.21 Schema 版本迁移：不接受旧版本 = 测试崩溃
- 证据：2026-06-28 Ponytail（`Validate` 写 v2，`Load` 只读 v1，89 测试失败）
- 预防：Schema 版本单一常量；升级接受旧版本（≥1 主版本回退）或显式迁移；`schemaVersion < expected` 触发迁移非拒绝

## 7. 类型驱动架构重构教训（2026-06-29/30）

### §7.1 多层验证不一致 → 保存-崩溃循环
- 证据：`LauncherPresetWorkflow` 不查 256 字符，`LauncherPresetFiles.Validate` 查 → 保存成功重启崩溃
- 预防：Parse-Don't-Validate（类型构造即验证）；验证只在单一入口，下游信任类型

### §7.2 规范化在错误的地方做 → 注入检测失效
- 证据：`Build()` 在 FormKC 后检测 `%`，但 draft 已提前 FormKC
- 预防：注入检测在原始输入上执行（类型构造时比较 raw vs normalized）；类型内部为"已验证"态

### §7.3 [已反转] 手写崩溃恢复不可靠
- 证据：`CommitJournal.Replay` 5 个 CRITICAL 缺陷
- 现状：2026-07-09 回退 LiteDB；`LauncherJson.Save` temp + `File.Replace` + `.bak`；Load 损坏回退 `.bak`；零应用层恢复代码
- 预防：优先单文件 + 文件系统原子性

### §7.4 硬编码环境假设 → 静默故障
- 证据：`ShellCommandPrefix` 硬编码 `pwsh`，无 PowerShell Core 静默失败
- 预防：`ShellCommand.Resolve(shellName)` 配置驱动，默认 `pwsh` 可配，解析验证存在

### §7.5 异常用于控制流 → 编译器无法保证完整性
- 证据：`IsRecoverable` 黑名单过滤，裸 catch 捕获致命异常继续运行
- 预防（历史提案）：`Result<T,E>` 消除异常控制流（注：2026-07-09 复核决定保留异常，见 error-handling.md）

### §7.6 [Obsolete] + 委托模式 = 渐进迁移安全网
- 证据：Phase 1 引入 6 value object，142 调用方
- 预防：旧方法标 `[Obsolete]` + 委托新类型；抑制 CS0618 直到迁移完成；Phase 3 删旧方法

## 8. 架构演进教训（2026-07）

### §8.1 Partial 类拆分是机械操作
- 证据：2026-07-09 Stage 2，6 partial 提取 451/451 测试通过
- 预防：推迟清理用具体阻塞点论证，不用架构审美论证；partial 编译期合并为单类型，共享状态是语言特性非风险

### §8.2 FindUpward 幽灵配置文件陷阱
- 证据：10+ 次（2026-07）
- 根因：子目录 `README.md` + 幽灵 `config/launcher.json` 被误判仓库根
- 预防：不加 `--config` 依赖 auto-discovery 向上搜索；视图为空先查 FindUpward 路径

### §8.3 [已反转] 旧 LiteDB 进程文件锁死循环
- 证据：8+ 次（LiteDB 时代）；2026-07-09 移除
- 预防：嵌入式 DB 进程级文件锁对桌面单用户是净负担；单文件 JSON 无锁

### §8.4 Schema 合并遗留死字段 → 双源漂移
- 证据：2026-07-09 `07-09-schema-first-persistence`（T7）；`settings.SchemaVersion` 死字段
- 预防：schema 合并/迁移完成时 grep `schemaVersion` 类字段标 dead 清理；每个 schema-bearing record 有 pin 测试；二次发现——`tools/*.ps1` 门禁脚本也是 schema 消费者，移除字段 grep 三处（src/tests/tools.ps1）

## 9. 无障碍生产审计教训（2026-07-20）

### §9.1 Avalonia 12.1.0 无 ReducedMotion API
- 证据：2026-07-20 V-04；`strings` 确认无 `ReducedMotion` 字符串
- 现状：XAML `.reduced-motion Border.card` stub 就位；`ApplyReducedMotion()` 空方法体；动态检测 deferred 到 Avalonia 升级；PRD AC2 标 `[~]`
- 预防：升级时 grep `PlatformSettings` 确认暴露；先从 XAML 侧准备样式

## 10. Unified Store 架构迁移教训（2026-07-21）

### §10.1 RestoreVmStateFromStore 事件链断裂
- 证据：2026-07-21 Phase 3，7 个 save-failure 测试失败
- 根因：`ReplaceAllEntries` 触发 `EntriesChanged` → `ApplyFilter` → `ValidateCustomOrder` 在 `_settings` 同步前
- 预防：`ReplaceEntries`（不触发事件）+ 显式 `EnsureCustomOrderContainsEntries()` 再 `RefreshAllProjections()`；restore 前追踪状态依赖 DAG

### §10.2 Inline 重构丢失副作用（DiscardDragReorderPreview）
- 证据：2026-07-21 Phase 3
- 预防：方法 inline 时审计所有副作用；删方法前列出其调用的外部方法；留意双入口不一致

### §10.3 Avalonia TransformParser 不接收 translateY 无单位非零值
- 证据：2026-07-24 Mode 2 调试
- 预防：非零值必须带 px（`translateY(-2px)`）；编译不捕获，运行时验证

### §10.4 ExperimentalAcrylicMaterial 颜色不匹配
- 证据：2026-07-24 Mode 2
- 预防：Win10 亚克力优先 `SetWindowCompositionAttribute`，`ExperimentalAcrylicBorder` 仅兜底

### §10.5 Ponytail 决策必须正式归档
- 证据：2026-07-24 arch-depth-landing（fluxor-migration 停 planning）
- 预防：ponytail 是"已评估决定不做"非 planning；写 result.md 同步归档子任务，不留 planning 死任务

### §10.6 ViewModel 事件订阅永不取消导致内存泄漏
- 证据：2026-07-25 C3/H5
- 预防：VM 继承 `ViewModelBase`；订阅经 `AddDisposable`；Replace/Remove 前 Dispose 旧 VM

### §10.7 Roslyn 分析器加入 Solution 但未接入消费项目
- 证据：2026-07-26 CR#1/#9，5 分析器编译但从未加载
- 预防：`Directory.Build.props` 配置 `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`；verify.ps1 加接线门禁；分析器仅 CI 全量运行

### §10.8 `when` 子句中 `throw` 被运行时吞掉
- 证据：2026-07-26 CR-1
- 根因：`catch when (filter())` 内 throw 被视为 filter 返回 false，异常不传播
- 预防：DEBUG 保护用 `Environment.FailFast()` 替代 `throw`

### §10.9 `--config <path>` 静默忽略
- 证据：2026-07-26 CR-2
- 预防：用户指定路径存在即信任，不存在直接 `FileNotFoundException`，绝不 fallthrough

### §10.10 `try` 块外取消导致批量启动回执丢失
- 证据：2026-07-26 BS-1
- 预防：`try-finally` 保证回执始终生成（含取消/异常路径）

### §10.11 MSBuild.CompilerCache 缓存键不含 `@(Analyzer)`
- 证据：2026-08-02；11 处裸 catch 违规一直未发现
- 预防：`<ForceDisableCompilationCache>true</ForceDisableCompilationCache>`；分析器是架构门禁必须每次真跑

### §10.12 AOT 发布默认不是单文件
- 证据：2026-08-02；`libSkiaSharp.dll` 等原生 DLL 必需
- 预防：发布清理只删 `*.pdb` 与陈旧 `config/`，保留一切 `*.dll`；验证以全新目录解压 + 实际启动为准

### §10.13 Roslyn `x is T` 裸类型名解析为 ConstantPatternSyntax
- 证据：2026-08-02；`when (ex is IOException or ...)` 被误报 WT1004
- 预防：`ConstantPatternSyntax` 用 `SemanticModel.GetSymbolInfo(...).Symbol is INamedTypeSymbol` 判断类型窄化

### §10.14 同一项目双重 ProjectReference（分析器 + 普通）→ 输出拷贝不可靠
- 证据：2026-08-02；20 分析器测试 `FileNotFoundException`
- 预防：测试项目加显式 `AfterTargets="Build"` 拷贝目标；不依赖 MSBuild 双引用隐式拷贝

### §10.15 PowerShell 5.1 把无 BOM UTF-8 脚本按 ANSI(GBK) 读
- 证据：2026-08-02；`使用说明.txt` 乱码
- 预防：脚本内不写非 ASCII 字面量（文件名用 `Get-ChildItem -Filter` 解析或全英文；中文用 Unicode 转义）

### §10.16 随机属性测试生成器前置条件必须镜像生产规范化
- 证据：2026-08-02；种子 `2FJUQdA7oYHh` 复现 `Command = '\u0091'`
- 预防：生成器前置条件用与生产相同规范化（`is not (Control or Format)`）；失败信息带种子与输入

### §10.17 `packages.lock.json` 的 AOT ILCompiler 条目被非 AOT restore 擦掉
- 证据：2026-08-16；`Microsoft.DotNet.ILCompiler` 被移除
- 预防：所有写 app lockfile 的 restore 统一 `-p:EnableNativeAot=true`；publish 固定 `--no-restore`；锁一致性靠"同一 restore 图"

### §10.18 fill-grid 落地问题族
- 证据：2026-08-16；`LauncherEntriesView.axaml` + `Panels.cs` + VM
- 问题族：删 `FastWrapPanel` 漏 names wall、`ItemsPanelTemplate` 编译绑定 `AVLN2000`、固定列 solver 小组列宽漂移、组面板撑满、末行拉伸失效、主文件超 1200 行
- 预防：删共享 View 前全仓引用扫描；布局投影下沉 `LauncherCardGroupViewModel`；fixedColumns 只按可用宽度 clamp；末行拉伸按整行宽度分配；删 VM 转发属性改 `RefreshCardGroupProjections()`

### §10.19 CI 远端门禁问题族
- 证据：2026-08-16 fill-grid CI
- 问题族：未跟踪 `config/launcher.json`、`dotnet nuget audit` 非受支持子命令、SDK 内部包 locked-mode 漂移、`wmic` 已移除、artifact 配额
- 预防：CI 只依赖已跟踪文件；audit 用 `dotnet list package --vulnerable --include-transitive --no-restore`；`Directory.Packages.props` 显式钉 `Microsoft.NET.ILLink.Tasks` 与 `Microsoft.DotNet.ILCompiler`；`wmic` 改 `pwsh Get-Process`；artifact 上传 `continue-on-error`
