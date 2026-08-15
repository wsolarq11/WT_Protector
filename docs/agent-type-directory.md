# Agent 类型目录

生成时间: 2026-07-23
数据来源: `git log --grep="[Snow Team]"`（共 44 个提交，含 27 个 auto-commit 和 17 个 merge）

> **2026-08-15 核验注记**: 本文为 2026-07-23 时点记录。当前 git 历史（曾重写）中，27 个 Agent 名均有同名 auto-commit 可对应（如 core-fixer → `243deb97`，07-22），但文中全部短哈希（ffb9cd6 等）无法解析（`git log -1` 退出非零、0 个 unreachable 对象），各 Agent 的修改文件数与 +/- 行数无法复核。
> Snow Team 提交现计数 **47**（29 auto-commit + 18 merge），仍全部集中于 07-22（12）与 07-23（35）两天；目录生成后另有 `fluxor-integrator`（`44247cc7`）、`phase-e-worker`（`ee2a85a9`）两个 auto-commit 与 1 个 merge（`0224dbbf`）落地，故"27 种"为生成时点计数。
> 路径核验: `docs/architecture-research-report.md` 存在且恰为 593 行（与文中一致）；`collab-debt-findings.md` 已随 2026-08-15 归档移动至 `docs/collab-debt-findings.md`（现 261 行），此处按指示保留裸文件名引用。

---

## 概述

本项目使用 Snow Team 的 Agent 协作模式。截至 2026-07-23，共使用了 **27 种 Agent 类型**，覆盖修复、架构、重构、质量、研究等领域。以下按 Agent 名称列出各自的角色、任务类型和关键提交。

---

## Agent 目录

### 1. core-fixer

| 属性 | 值 |
|------|-----|
| 角色 | Core 层修复 |
| 任务类型 | 引入 Vogen Value Object 强类型 |
| 关键提交 | `ffb9cd6` |
| 修改文件 | 10 个文件（+163/-6） |
| 描述 | 引入 7 个 Vogen Value Object（CommandTemplate, DirectoryPath, EntryId, EntryName, PresetId, PresetName, SearchTerm），配置 ErrorOr 依赖，EntryId 使用 Vogen 内部包装 |

### 2. app-fixer

| 属性 | 值 |
|------|-----|
| 角色 | App 层修复 |
| 任务类型 | 小型适配修复 |
| 关键提交 | `601a594` |
| 修改文件 | 2 个文件（+5/-3） |
| 描述 | 修复 Vogen 引入后的 App 层适配问题 |

### 3. phase-a-fixer

| 属性 | 值 |
|------|-----|
| 角色 | Phase A 修复 |
| 任务类型 | AXAML 修复 |
| 关键提交 | `18d9026` |
| 修改文件 | 7 个文件（+22/-15） |
| 描述 | Phase A 窗口 AXAML 修复，涉及 6 个窗口的布局和属性调整 |

### 4. phase-b-ux

| 属性 | 值 |
|------|-----|
| 角色 | Phase B UX 改造 |
| 任务类型 | UI 统一 + 对话框样式 |
| 关键提交 | `a85d756` |
| 修改文件 | 13 个文件（+135/-138） |
| 描述 | UX 阶段：统一对话框样式，提取 DialogSharedStyles，精简 5 个窗口的冗余样式，覆盖对话框一致性契约测试 |

### 5. phase-de-quality

| 属性 | 值 |
|------|-----|
| 角色 | Phase D/E 质量门禁 |
| 任务类型 | 质量门禁增强 |
| 关键提交 | `49afec0` |
| 修改文件 | 4 个文件（+227/-2） |
| 描述 | 添加 StatusPresenter 逻辑 + LauncherIcons 图标系统 + 165 行服务契约测试 + verify.ps1 门禁增强 |

### 6. phase-c-arch

| 属性 | 值 |
|------|-----|
| 角色 | Phase C 架构提取 |
| 任务类型 | 控件视图提取 |
| 关键提交 | `5028046` |
| 修改文件 | 11 个文件（+710/-33） |
| 描述 | 从 MainWindow.axaml 提取 5 个独立控件视图（CardsView, DetailsView, FilterToolbar, NamesView, StatusBar），大幅提升 XAML 模块化 |

### 7. custom-order-fixer

| 属性 | 值 |
|------|-----|
| 角色 | 自定义排序修复 |
| 任务类型 | 排序修复 |
| 关键提交 | `5411840` |
| 修改文件 | 4 个文件（+33/-2） |
| 描述 | 修复自定义排序相关问题，涉及 DirectoryPathJsonConverter、LauncherEntryDrafts 和测试 |

### 8. search-history-fixer

| 属性 | 值 |
|------|-----|
| 角色 | 搜索历史修复 |
| 任务类型 | 搜索修复 |
| 关键提交 | `1abeb65` |
| 修改文件 | 2 个文件（+24/-3） |
| 描述 | 修复搜索历史相关逻辑，涉及 LauncherEntryDrafts 和 DirectoryPath |

### 9. status-text-fixer

| 属性 | 值 |
|------|-----|
| 角色 | 状态文本修复 |
| 任务类型 | 状态显示修复 |
| 关键提交 | `6060715` |
| 修改文件 | 2 个文件（+19/-2） |
| 描述 | 修复状态文本显示逻辑 |

### 10. gate-verifier

| 属性 | 值 |
|------|-----|
| 角色 | 门禁验证器 |
| 任务类型 | verify.ps1 增强 + 测试输出 |
| 关键提交 | `c3dffa1` |
| 修改文件 | 6 个文件（+1398/-44） |
| 描述 | 大幅增强 verify.ps1 门禁体系，包含 700+ 行测试输出记录 |

### 11. code-reviewer

| 属性 | 值 |
|------|-----|
| 角色 | 代码审查 |
| 任务类型 | 审查报告生成 |
| 关键提交 | `d03d20a` |
| 修改文件 | 1 个文件（+139） |
| 描述 | 生成 ultrareview 审查报告（139 行） |

### 12. deadlock-fixer

| 属性 | 值 |
|------|-----|
| 角色 | 死锁修复 |
| 任务类型 | 线程安全修复 |
| 关键提交 | `4fb970b` |
| 修改文件 | 1 个文件（+18/-3） |
| 描述 | 修复 AppStore 中的线程安全问题，使用 SemaphoreSlim 保护共享状态 |

### 13. builder

| 属性 | 值 |
|------|-----|
| 角色 | 构建器 |
| 任务类型 | AppStore 构建重构 |
| 关键提交 | `29e1351` |
| 修改文件 | 1 个文件（+20/-32） |
| 描述 | 重构 AppStore builder 模式，精简 12 行代码 |

### 14. bugfixer

| 属性 | 值 |
|------|-----|
| 角色 | Bug 修复 |
| 任务类型 | 最小修复 |
| 关键提交 | `a001a9b` |
| 修改文件 | 1 个文件（+1） |
| 描述 | 单行修复，涉及 MainWindowViewModel |

### 15. pattern-researcher

| 属性 | 值 |
|------|-----|
| 角色 | 架构模式研究 |
| 任务类型 | 研究报告 |
| 关键提交 | `7625167` |
| 修改文件 | 1 个文件（+593） |
| 描述 | 生成详细的架构模式研究报告 `docs/architecture-research-report.md`（593 行） |

### 16. layout-state

| 属性 | 值 |
|------|-----|
| 角色 | 布局状态定义 |
| 任务类型 | Flux 模式状态层 |
| 关键提交 | `333ba2f` |
| 修改文件 | 3 个文件（+96） |
| 描述 | 定义 LayoutState、LayoutActions、LayoutReducers，引入 Flux 模式的三件套 |

### 17. memoized-selector

| 属性 | 值 |
|------|-----|
| 角色 | 记忆化选择器 |
| 任务类型 | 派生数据优化 |
| 关键提交 | `002963a` |
| 修改文件 | 3 个文件（+35/-26） |
| 描述 | 提取 MemoizedSelector 工具类，替换 LayoutManager 中的手写记忆化，移除 MainWindowViewModel 中的冗余字段 |

### 18. path-unifier

| 属性 | 值 |
|------|-----|
| 角色 | 路径统一 |
| 任务类型 | 死代码消除 |
| 关键提交 | `c801a50` |
| 修改文件 | 2 个文件（+5/-12） |
| 描述 | 移除 `ReplaceAllEntries` 冗余方法，将两个调用点迁移为 `ReplaceEntries` + 显式属性设置，净减 7 行 |

### 19. guard-remover

| 属性 | 值 |
|------|-----|
| 角色 | 守卫移除 |
| 任务类型 | 条件守卫提纯 |
| 关键提交 | `e718330` |
| 修改文件 | 1 个文件（+25/-15） |
| 描述 | 重构 MainWindowViewModel.Layout.cs 中的守卫逻辑，净增 10 行编码更清晰的布局条件 |

### 20. declarative-props

| 属性 | 值 |
|------|-----|
| 角色 | 声明式属性 |
| 任务类型 | 属性声明优化 |
| 关键提交 | `8c17f8f` |
| 修改文件 | 2 个文件（+18/-15） |
| 描述 | 优化 LauncherEntryViewModel 的布局属性声明，向声明式风格迁移 |

### 21. collab-debt-reviewer

| 属性 | 值 |
|------|-----|
| 角色 | 协作债务审查 |
| 任务类型 | 审查报告 |
| 关键提交 | `e71ff5b` |
| 修改文件 | 1 个文件（+201） |
| 描述 | 生成协作模式与技术债务审查报告 `collab-debt-findings.md`（201 行） |

### 22. homoglyph-researcher

| 属性 | 值 |
|------|-----|
| 角色 | 同形字安全研究 |
| 任务类型 | 安全审计 F3 实现 |
| 关键提交 | `b6064a9` |
| 修改文件 | 3 个文件（+144/-6） |
| 描述 | 实现 F3 同形字保护：EntryId Latin 脚本白名单，包含 LatinScriptRanges 定义、EnsureLatinScript 验证、测试覆盖 |

### 23. preset-validator

| 属性 | 值 |
|------|-----|
| 角色 | Preset 验证器 |
| 任务类型 | 安全审计 F2 实现 |
| 关键提交 | `1f4f15a` |
| 修改文件 | 未直接检查（与 F2 相关） |
| 描述 | 实现 F2 Preset entryId 运行时交叉验证——AddPreset/UpdatePreset 验证 entryIds 存在于 knownEntryIds |

### 24. doc-fixer

| 属性 | 值 |
|------|-----|
| 角色 | 文档修复 |
| 任务类型 | 文档清理 |
| 关键提交 | `99e3cb5` |
| 修改文件 | 未直接检查 |
| 描述 | 文档修复 + emoji 清理 |

### 25. test-enhancer

| 属性 | 值 |
|------|-----|
| 角色 | 测试增强 |
| 任务类型 | 测试断言补充 |
| 关键提交 | `459fb6f` |
| 修改文件 | 未直接检查 |
| 描述 | 测试断言补充和增强 |

### 26. ponytail-adder

| 属性 | 值 |
|------|-----|
| 角色 | Ponytail 注释添加 |
| 任务类型 | 文档化已知局限性 |
| 关键提交 | `b5bfbee` |
| 修改文件 | 1 个文件（+12） |
| 描述 | 为 3 个安全审计门禁（F1/F2/F3）添加 ponytail 注释，记录扫描局限性和升级路径 |

### 27. ci-enhancer

| 属性 | 值 |
|------|-----|
| 角色 | CI 增强 |
| 任务类型 | CI 工作流优化 |
| 关键提交 | `af77a95` |
| 修改文件 | 1 个文件（+4/-4） |
| 描述 | 优化 GitHub Actions 工作流配置 |

---

## Agent 类型统计

| 类别 | Agent 名称 | 数量 |
|------|-----------|------|
| 修复类 | core-fixer, app-fixer, phase-a-fixer, custom-order-fixer, search-history-fixer, status-text-fixer, bugfixer, deadlock-fixer | 8 |
| 架构类 | phase-c-arch, phase-b-ux, layout-state, memoized-selector, builder, guard-remover, declarative-props | 7 |
| 质量类 | phase-de-quality, gate-verifier, code-reviewer, test-enhancer | 4 |
| 安全类 | homoglyph-researcher, preset-validator | 2 |
| 文档类 | doc-fixer, collab-debt-reviewer, pattern-researcher, ponytail-adder | 4 |
| DevOps 类 | ci-enhancer, path-unifier | 2 |

**总计: 27 种 Agent 类型**

---

## 使用模式

1. **顺序执行**: 所有 Agent 按顺序执行（一个 auto-commit 后才开始下一个），无并行
2. **脉冲式使用**: Agent 协作集中在 2026-07-22 和 2026-07-23 两天
3. **Merge 策略**: 每个 Agent 完成 auto-commit 后，通过 Merge 提交合并到主线
4. **粒度多样性**: 从单行修复（bugfixer）到 700+ 行重构（phase-c-arch），覆盖各种规模
