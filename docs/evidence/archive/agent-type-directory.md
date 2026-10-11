Status: Archived
Purpose: 记录 Snow Team Agent 类型历史目录、分类统计与协作模式。
Original scope: 2026-07-23 由 `git log --grep="[Snow Team]"` 生成，覆盖 27 种 Agent。

# Agent 类型目录

## 结论

- 截至 2026-07-23 共 27 种 Agent，覆盖修复（8）、架构（7）、质量（4）、安全（2）、文档（4）、DevOps（2）；全部按顺序执行，无并行。
- 协作集中在 2026-07-22（12）与 2026-07-23（35）两天，属脉冲式批量使用。
- 目录生成后另有 `fluxor-integrator`（`44247cc7`）、`phase-e-worker`（`ee2a85a9`）与 1 个 merge（`0224dbbf`）落地，故“27 种”为生成时点计数。

## 关键证据

| Agent | 提交 | 职责 |
| --- | --- | --- |
| core-fixer | `ffb9cd6` | 引入 7 个 Vogen Value Object |
| app-fixer | `601a594` | Vogen 引入后的 App 层适配 |
| phase-a-fixer | `18d9026` | 6 个窗口 AXAML 修复 |
| phase-b-ux | `a85d756` | 统一对话框样式，提取 DialogSharedStyles |
| phase-de-quality | `49afec0` | StatusPresenter + LauncherIcons + verify.ps1 门禁增强 |
| phase-c-arch | `5028046` | 从 MainWindow.axaml 提取 5 个控件视图 |
| custom-order-fixer | `5411840` | 自定义排序修复 |
| search-history-fixer | `1abeb65` | 搜索历史逻辑修复 |
| status-text-fixer | `6060715` | 状态文本显示修复 |
| gate-verifier | `c3dffa1` | verify.ps1 大幅增强 |
| code-reviewer | `d03d20a` | 生成 ultrareview 审查报告 |
| deadlock-fixer | `4fb970b` | AppStore 线程安全修复（SemaphoreSlim） |
| builder | `29e1351` | AppStore builder 重构 |
| bugfixer | `a001a9b` | 单行最小修复 |
| pattern-researcher | `7625167` | 生成 `docs/architecture-research-report.md`（593 行） |
| layout-state | `333ba2f` | 定义 LayoutState/Actions/Reducers（Flux 三件套） |
| memoized-selector | `002963a` | 提取 MemoizedSelector 工具类 |
| path-unifier | `c801a50` | 移除 ReplaceAllEntries 冗余方法 |
| guard-remover | `e718330` | MainWindowViewModel.Layout.cs 守卫提纯 |
| declarative-props | `8c17f8f` | LauncherEntryViewModel 布局属性声明式化 |
| collab-debt-reviewer | `e71ff5b` | 生成 collab-debt-findings.md |
| homoglyph-researcher | `b6064a9` | 实现 F3 同形字保护（Latin 脚本白名单） |
| preset-validator | `1f4f15a` | 实现 F2 preset entryId 运行时交叉验证 |
| doc-fixer | `99e3cb5` | 文档修复 + emoji 清理 |
| test-enhancer | `459fb6f` | 测试断言补充 |
| ponytail-adder | `b5bfbee` | 为 F1/F2/F3 门禁添加 ponytail 注释 |
| ci-enhancer | `af77a95` | GitHub Actions 工作流优化 |

## 使用模式

1. 顺序执行：一个 auto-commit 完成后才开始下一个，无并行。
2. Merge 策略：每个 Agent auto-commit 后经 2-parent merge 合入主线。
3. 粒度跨度：从单行修复（bugfixer）到 700+ 行重构（phase-c-arch）。

## 核验注记（2026-08-15）

- 文中全部短哈希在当前重写后的 git 历史中无法解析，各 Agent 修改文件数与 +/- 行数无法复核。
- Snow Team 现计数 47（29 auto-commit + 18 merge）。
- `docs/architecture-research-report.md` 存在且恰为 593 行；`collab-debt-findings.md` 已归档至 `docs/archive/`。
