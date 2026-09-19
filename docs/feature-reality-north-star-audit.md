Status: Active
Purpose: 22 项功能映射审计 + 声明边界账本。
Update when: 功能边界、声明或证据变化时。

# 功能现实与北极星审计

范围：`src/avalonia-launcher/` 主线、`config/launcher.json` 统一存储、`README.md`、`DESIGN.md`、
`docs/design-benchmark-digest.md`、`docs/reference/github-similar-projects-snapshot.json`、`scripts/verify.ts`。
不评估 `.snow/` 忽略状态。

窄域 north-star 判定：
1. 功能对应真实高频本地启动问题；
2. 数据结构有单一事实源；
3. 用户路径显式可见，无隐藏回退；
4. 行为有当前源码/测试/门禁证据；
5. 功能停留在 Windows Terminal 工作区启动器窄域内。

"100% 信心、0% 副作用"对启动外部进程的软件不可作为整体结论成立；正确目标是启动前本地预检、
显式副作用、失败不隐藏。

结论级别：narrow-north-star（窄域当前最优）/ strong / source-backed。
缺口类别：none-proven / future-workload / external-process-limit / visual-proof / distribution-scope / source-freshness。
证据类型：repo-battle-tested / market-informed / legacy-flow-informed / accessibility-informed / source-backed。

## 22 项功能映射

| # | 功能 | 真实问题 | 数据 owner | 结论 | 剩余缺口 |
| --- | --- | --- | --- | --- | --- |
| 1 | 启动条目对象 | 稳定名称/目录/命令替代手工终端配置 | `config/launcher.json` entries（id/name/directory/command/selected/updatedAtUtc/launchedAtUtc/launchCount） | narrow-north-star | future-workload：无外部导入导出 |
| 2 | 目录存在契约 | 失效按钮应在点击前失败 | `config/launcher.json` directory；文件系统为真值 | narrow-north-star | external-process-limit：不探测命令可执行性（命令是受信本地 shell 文本） |
| 3 | 搜索/过滤 | 条目多时按名称/路径/命令定位 | 内存 `FilterText`，由 settings 持久化 | narrow-north-star | future-workload：精确子串足够前不做模糊排序 |
| 4 | 单条启动 | 一个工作区一次显式动作 | `LaunchPlan`（选中条目 + 全局确认开关） | narrow-north-star | external-process-limit：启动请求发出后无法证明会话健康 |
| 5 | 勾选持久化与批量启动 | 反复启动同一批工作区 | `config/launcher.json` selected；命名重复意图归 presets | narrow-north-star | external-process-limit：进程启动后无法保证零副作用 |
| 6 | 命名启动组/presets | 命名、便携、可查对象替代临时勾选 | `config/launcher.json` presets 引用稳定 entry id | narrow-north-star | future-workload：无外部导入导出 |
| 7 | 预设一等操作 | 组内新建/更新/删除，避免手改 JSON | `config/launcher.json` | narrow-north-star | future-workload：无外部 JSON 并发编辑冲突 UI |
| 8 | 预设与勾选差异预览 | 盲应用组会重写勾选 | 预览派生自当前勾选与选中 preset，非第二事实源 | narrow-north-star | visual-proof：截图仅在视觉大改后 |
| 9 | 预设启动计划预览 | 启动组前需看生成的 wt.exe 命令 | `config/launcher.json` 管成员，`LaunchPlan` 管命令，`DryRunText` 为瞬时 UI | narrow-north-star | external-process-limit：真正启动后命令副作用不可控 |
| 10 | 条目增删改 | 常见改动不需手改 JSON | `config/launcher.json`，preset/settings 引用一并更新或回滚 | narrow-north-star | future-workload：无批量导入 |
| 11 | 自定义顺序与拖拽排序 | 空间记忆，字母序非最快 | settings `customEntryOrder`；拖拽会话为单一瞬时对象 | narrow-north-star | none-proven |
| 12 | 排序模式 | 不同扫描模式 | settings `sortMode`（custom/recent/updated/alphabetical/frequent） | narrow-north-star | none-proven |
| 13 | 视图模式 Cards/Details/Names | 可读动作/密集扫描/总览不能合一 | settings `entryViewMode`（cards/details/names） | narrow-north-star | visual-proof：截图仅在视觉变化后 |
| 14 | 自适应卡片密度 | 固定卡片大窗口浪费、小窗口不可用 | `LauncherEntryLayoutDensity`（可用尺寸 + 可见条目数派生） | narrow-north-star | visual-proof：截图仅在视觉大改后 |
| 15 | 全局启动确认 | 单一可见规则替代逐命令危险猜测 | settings `requireConfirmationForLaunches` | narrow-north-star | none-proven |
| 16 | 便携设置 | UI 偏好随仓库走、可审查 | `config/launcher.json` | narrow-north-star | future-workload：无跨机路径归一化（目录是本机事实） |
| 17 | JSON 持久化/备份/回滚 | 不写半截 JSON、不半应用状态 | `config/launcher.json` 经 `LauncherJson`/`FileLauncherJsonIO`（temp+`File.Replace`+`.bak`）；回滚=save-first-then-commit | narrow-north-star | future-workload：无并发外部编辑合并 |
| 18 | Core/App/Tests 分离 | 启动规则与配置可不开 GUI 测试 | Core 纯记录/规则；App VM/View；Tests 行为契约 | narrow-north-star | none-proven |
| 19 | 自包含 win-x64 发布与原生门禁 | 桌面启动器要能恢复/测试/发布/冒烟 | `scripts/verify.ts`、`scripts/verify-mainline.ts`、忽略的发布产物 | narrow-north-star | distribution-scope：无安装器/更新通道 |
| 20 | 退役旧界面 | 两条活跃 UI 线造成双真相 | Avalonia 主线；HTA/WinForms 仅归档 | narrow-north-star | none-proven |
| 21 | 设计基准与巨人肩膀库 | UI/产品决策不靠感觉或抄框架 | `DESIGN.md`、`docs/design-benchmark-digest.md`、`docs/giants-shoulders/` | narrow-north-star | source-freshness：外部来源失效需刷新 |
| 22 | AI Agent CLI 自动发现 | 手动录入多个 agent exec+目录繁琐 | `AgentCatalog.cs`、`AgentDiscoveryScanner.cs`、`DiscoverAgentsViewModel.cs`、`MainWindowViewModel.Discovery.cs` | narrow-north-star | future-workload：目录需随市场刷新，未安装 agent 静默跳过 |

22 项结论均为 narrow-domain north-star；证据以 repo-battle-tested 为主，其余补充 market-informed /
legacy-flow-informed / accessibility-informed / source-backed。

## 阻断广泛域北极星的缺口

非当前窄域产品缺陷，是不得声明"全域最优/不可改进/零副作用"的原因：

| 缺口 | 类别 | 阻断的声明 |
| --- | --- | --- |
| 无导入导出生态 | future-workload | 不可改进、优于所有相似项目 |
| 无运行历史/会话健康对象 | future-workload | 不可改进、100% 信心与零副作用 |
| 无安装器/更新/签名通道 | distribution-scope | 不可改进、优于所有相似项目 |
| 无视觉舒适截图回执 | visual-proof | 不可改进 |
| 真实进程启动副作用 | external-process-limit | 优于所有相似项目、100% 信心与零副作用 |

这些能力不预先建设，出现真实需求后再立项。

## 声明边界账本

| Claim id | 状态 | 范围 |
| --- | --- | --- |
| narrow-domain-north-star | allowed | 便携 Windows Terminal 工作区启动器 |
| narrow-slice-stronger | allowed | 便携配置/命名组/无副作用预览/回滚持久化/原生门禁切片 |
| small-beautiful-preserved | allowed | 当前小本地启动器契约 |
| pre-handoff-zero-side-effect-boundary | allowed | 交接外部进程前的仓库自查 |
| broad-launcher-platform-north-star | forbidden | 泛启动器/自动化平台 |
| unimprovable-product | forbidden | 所有可能工作流 |
| stronger-than-every-github-similar | forbidden | 每个相似 GitHub 项目 |
| absolute-confidence-zero-side-effects | forbidden | 真实 wt.exe 启动行为 |
| post-wt-terminal-health | forbidden | Windows Terminal 接受请求后的会话健康 |

## 结论

完整目标仍为 not-complete-as-written：22 项功能映射均为窄域 north-star，但仅限便携 Windows Terminal
工作区启动器；泛启动器、不可改进、优于所有相似项目、零副作用与交接后会话健康均不可声明。
