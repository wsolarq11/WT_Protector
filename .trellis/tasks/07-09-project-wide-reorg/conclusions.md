# Conclusions — project-wide reorg (07-09)

> 父任务：`07-09-project-wide-reorg`
> 集成评审日期：2026-07-09
> 评审基线：commit `68df503`（session 起点）→ HEAD

## Net delta (session 起点 → HEAD)

- **提交数**：3
  - `dd4ff1b` chore(init): add CLAUDE.md per /init
  - `171967b` docs(reorg): land audit D1 drift fixes + correct verify count methodology
  - `70ef4b5` chore(task): archive 07-09-reorg-docs
- **文件数**：25 changed, +1021 / -25
- **源代码 churn**：`src/` 0 文件、`retired/` 0 文件、`config/` 0 文件、`tools/` 0 文件（全程零行为变更）
- **文档改动**：7 文件（AGENTS.md / README.md / .trellis/spec/tests/index.md / 4 份归档 PRD）
- **新增任务跟踪**：3 task tree（父 + reorg-audit-pass 归档 + reorg-docs 归档），共 17 个 prd/design/implement/task.json/jsonl 文件
- **新文件**：1（CLAUDE.md，/init 产出）

## 子任务归档状态

| 子任务 | 状态 | 产出 |
|--------|------|------|
| `07-09-reorg-audit-pass` | 已归档 | `audit-findings.md`（5 维 13 条目：8 exec-now / 4 defer / 3 no-action）|
| `07-09-reorg-docs` | 已归档 | 8 项 D1 文档漂移全部落地；编译警告零；phantom 引用清零 |
| `reorg-code`（潜在） | **未建** | D2 静态扫描零 exec-now；编译警告零；建子任务就是空跑 |
| `reorg-deferred-decisions`（潜在） | **未建** | D4 R1/R2/R6/R7 全部维持 defer，trigger 未触发；建子任务就是空跑 |

原计划"四轴全做"实际收敛为"一轴一刀清"：审计证实 code 轴与 deferred 轴均无需动手，真实工作量只有 D1 的 8 项文档机械修改 + 1 项审计方法学订正。

## Cross-child acceptance criteria 核对

| 验收项 | 结果 | 证据 |
|--------|------|------|
| 四轴每个都有可复查结论文档 | ✅ | audit-findings.md D1–D5 全覆盖；docs 轴由 reorg-docs 实现；code/deferred 轴在 audit-findings.md D2/D4 给出三选一结论 |
| 每个执行项 verify.ps1 + dotnet test 全绿 | ✅ | verify.ps1 18/18 PASS；dotnet test 453/453 PASS；dotnet build 0 警告 |
| 每个 defer 项记录 revisit trigger | ✅ | R1：partial >1000 行或测试 setup 过贵（composition-di-async-evaluation/prd.md:58）；R2：FilterAndSort 自然拆分时（rename-evaluation/prd.md:54）；R6：composition root >5 services 或 plugin 系统（composition-di-async-evaluation/prd.md:87）；R7：file >5MB 或感知延迟或 profiler >50ms I/O（composition-di-async-evaluation/prd.md:112）|
| 每个 no-action 项附现代实践引用或代码证据 | ✅ | D5 三项 no-action 均带当前代码证据；D4 四项 defer 均带当前证据（如 R7 实测 config/launcher.json = 4989 bytes，距 5MB trigger ~1000× 裕度）|
| 未引入投机抽象 | ✅ | 零 src/ 改动；零新依赖；零新接口/工厂/config flag |
| `retired/` 字节级一致 | ✅ | `git diff 68df503..HEAD --stat retired/` 输出为空 |
| 父任务最终集成评审记录净 delta + 下轮 audit 判断 | ✅ | 本文件即是 |

## 关键发现：审计方法学 bug 已订正

本轮最重要的产出不是 8 项文档机械修复，而是发现并固化了一条**审计方法学教训**：

- 原审计在 D1 第 4 项用 `grep -c "Add-Check" tools/verify.ps1` 得到 19，但实际是 18 —— grep 把第 12 行的 `function Add-Check` 定义也算进去了
- 这个 off-by-one 在第一轮验证就被独立对抗性验证抓出（验证智能体独立跑了 `grep -cE "^\s*Add-Check\s+['\"]"` 与 verify.ps1 receipt，确认实际是 18）
- 订正后 audit-findings.md:32 已写入正确方法学：用 regex `^\s*Add-Check\s+['\"]` 排除 function 定义，或直接解析 receipt 的 `checks` 数组

这正是 audit-driven-verify 准则的价值：每条提案都用当前代码/文档验证后才决定动作。审计本身的方法学也接受同样的验证。

## 下一轮 audit 判断

**NO**——本轮 5 维全覆盖，无盲区。下一次 audit 仅在以下信号出现时启动：

1. **D4 任一 R 项 trigger 触发**：
   - R1：MainWindowViewModel 任一 partial >1000 行（当前最大 Presets 537，裕度 463 行）
   - R2：`FilterAndSort<TEntry>` 被自然拆分为独立类型
   - R6：composition root >5 services，或引入 plugin/extension system（当前 2 services）
   - R7：config/launcher.json >5MB，或用户感知保存延迟，或 profiler 显示 >50ms I/O（当前 ~5KB）
2. **引入新依赖 / 新 Layer / 跨层重构**
3. **`tools/verify.ps1` 检查数量增减**（本轮收口为 18；下次增减时同步触发 D1 文档核对）
4. **schemaVersion 升级**（v3 bump）

否则下一轮"整编规整"就是投机。

## 复盘：四轴全做收敛为一轴一刀清的合理性

用户最初选择"四轴全做"是基于对仓库状态的不确定。审计证实：

- **Docs 轴**：8 项真实漂移（落地）
- **Code 轴**：静态扫描零死代码、零未用成员、零孤儿类型；`dotnet build -warnaserror` 零警告。**无需动手**。
- **Deferred 轴**：4 项 R 决议全部维持 defer，trigger 均远未触发。**无需动手**。
- **Spec 轴**：除 tests/index.md 一项计数漂移外（已并入 D1），其余 spec 与代码完全同步。**无需动手**。

这是 ponytail ultra 的正确产出：审计证实大部分轴无需动手，真实工作量只有 D1 的 8 项机械修改 + 1 项方法学订正。提前建 code/deferred 子任务就是空跑——正是 ponytail 原则所反对的"为投机需求建抽象"。

## Notes

- 本轮零源代码行为变更，零新依赖，零新抽象
- 所有归档决议继续有效（R1/R2/R6/R7 defer；R3/R4/R5 已在前序任务落地）
- 审计准则"每条提案都是假设，必须用当前代码/文档验证后才决定动作"已通过本轮方法学订正进一步强化
- 父任务可以归档
