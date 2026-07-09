# 整编规整全项目 (project-wide reorg)

## Goal

对仓库做一次有边界的全轴整编：在 2026-07-09 第二轮架构审计与 R5 dual-truth 修复刚落地之后，针对**文档、代码、规范、deferred 决议**四个轴各做一次收敛，目标是把残留漂移、死代码、规范失同步、未落地决议一次性收口，并把"不动作"的结论以可复查的形式固化进 `.trellis/spec/`，防止未来重复审议。

**不是从零重写**。07-09 一轮已对 R1–R7 backlog 给出 action-or-no-action 结论（R3/R4/R5 落地，R1/R2/R6/R7 defer-with-trigger）。本轮的增量必须是审计能证实存在、且未被前一轮覆盖的真实漂移或残留。

## Scope (四轴，用户确认"全做")

| 轴 | 范围 | 不在范围 |
|----|------|----------|
| Audit | 全仓新一轮 audit（doc drift / code dead-code / spec sync / deferred re-assess / 风格 / ponytail 残留） | 重新审议 07-09 已 close 的 S1/S4/S10/S11/S12/S13/T4/T5/T6/T8 等 |
| Docs | README/AGENTS/DESIGN/CLAUDE/docs/.trellis-spec 之间的冗余、漂移、计数失真、章节重叠 | 引入新的设计系统文档；改写 brand/principle 内容 |
| Code | `src/` 内死代码 / 未用 `using` / 未用 private 成员 / `ponytail:` 残留 / 风格不一致 | 跨层重构；新加抽象；改 Layer 0 契约（`LaunchEntry`/`LauncherJson`/`LaunchArgumentBuilder`/ExceptionFilters 等已 best-in-class，不动） |
| Deferred | 对 R1（MainWindowViewModel 组合重构）/ R2（LauncherEntryVisibility → LauncherEntryQuery 改名）/ R6（DI 容器）/ R7（UI 线程同步 I/O async 化）给出**当前证据下**的执行或正式否决 | 引入新依赖；为投机需求加抽象 |

## Out of scope (显式)

- `retired/` 目录的删除、瘦身或内容修改（用户决议：保留不变；`tools/verify.ps1` 中相关检查保持原样）
- 新架构提案（2026-07-09 deepsearch 已验证 0 候选通过）
- 新增第三方依赖
- 重新审议 07-09 已 closed 的决议项（每个 close 都带 citation 或代码证据）
- 改动 Layer 0 的 `LaunchEntry` 数据形状、`LauncherJson` 持久化契约、`LaunchArgumentBuilder` 的注入安全 quoting、`ExceptionFilters` —— 这些在前一轮被认定为 best-in-class，refactor 风险高于收益
- 任何会让 `tools/verify.ps1` 退化为非自动化的改动

## Cross-child acceptance criteria

- [ ] 四个轴每个都有可复查的结论文档（action / no-action-with-reasoning / defer-with-trigger 三选一，每条都要可验证）
- [ ] 每个执行项落地后 `tools/verify.ps1` 全绿、`dotnet test -c Release` 全过
- [ ] 每个 defer 项在 `.trellis/spec/` 或对应子任务 PRD 中记录 revisit trigger（什么信号出现时重新评估）
- [ ] 每个 no-action 项至少一条 2025–2026 的现代实践引用或当前代码证据，防止未来重复审议
- [ ] 未引入投机抽象（interface-with-one-impl、factory-for-one-product、未用 config flag 等）
- [ ] `retired/` 路径在整轮结束时与开始时字节级一致（git diff 必须为空）
- [ ] 父任务最终集成评审记录净 delta（文件数 / 行数 / 提交数），并给出"是否需要再做下一轮 audit"的明确判断

## Child task map

| Child | 轴 | 何时创建 | 何时规划 |
|-------|----|---------|---------|
| `reorg-audit-pass` | Audit | 父任务激活后立即 | 父任务 PRD 评审通过即写该子的 prd/design/implement |
| `reorg-docs` | Docs | 审计子任务产出 doc-drift 清单后 | 基于真实漂移项写 design |
| `reorg-code` | Code | 审计子任务产出 code-cleanup 清单后 | 基于真实死代码项写 design |
| `reorg-deferred-decisions` | Deferred | 审计子任务对 R1/R2/R6/R7 给出当前证据后 | 仅当至少一项确认要执行时才写 design；全部维持 defer/no-action 则只产 conclusion 文档不写 design |

> Ponytail 原则：docs/code/deferred 三子**不提前写 design.md**。审计未产出真实 backlog 之前，提前规划就是投机。

## Final integration review (父任务 own)

子任务全部归档后，父任务 owner 必须：

1. 跑 `tools/verify.ps1` 与 `dotnet test -c Release`，记录 PASS
2. `git diff <父任务创建 commit>..HEAD --stat`，记录净 delta
3. 对照 cross-child acceptance criteria 逐项核对
4. 在父任务 `conclusions.md` 中记录：本轮净改了什么、defer 了什么、是否需要再做下一轮 audit（默认 NO，除非有具体证据）

## Notes

- 全程严守审计准则：每条提案都是假设，必须用当前代码/文档验证后才决定动作。
- 全程严守 `.trellis/spec/guides/pitfalls.md`：每开始一个子任务先 skim §1（工具层坑），遇到诡异失败先在 pitfalls.md 搜关键词，新坑按 session-id 追加。
- 父任务正常情况不应直接承担实现工作；只在最终集成评审时由 owner 操作。
