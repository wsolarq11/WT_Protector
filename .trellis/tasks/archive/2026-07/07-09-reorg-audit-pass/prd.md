# 全仓新一轮审计 (reorg audit pass)

> 父任务：`07-09-project-wide-reorg`

## Goal

在 2026-07-09 第二轮架构审计 + R5 dual-truth 修复刚落地、R1/R2/R6/R7 显式 defer 之后，对全仓做一次**只读**的增量审计，产出分诊清单 `audit-findings.md`，作为后续三个子任务（`reorg-docs` / `reorg-code` / `reorg-deferred-decisions`）的真实输入。

**只读**：本子任务不修改 `src/`、`docs/`、`tools/`、`config/`、`.trellis/spec/`、`README.md`、`AGENTS.md`、`DESIGN.md`、`CLAUDE.md` 任何源文件。所有产出物只写入 `.trellis/tasks/07-09-reorg-audit-pass/`。

## Audit dimensions (强制覆盖 5 维)

| 维度 | 检查内容 | 期望输出 |
|------|---------|---------|
| D1 文档漂移 | README/AGENTS/DESIGN/CLAUDE/docs 中引用的文件计数（"26 Core 文件"/"34 ViewModels"/"77 测试"等）、命令示例、schema 字段名、行号、章节链接是否还准确；`.trellis/spec/*` 是否反映 schema-driven-arch-migration 之后的真实状态 | 漂移条目表，每条 `file:line` + 当前实际值 + 文档原值 |
| D2 代码死代码 | `src/avalonia-launcher/src/**` 中未用 `using`、未用 private/internal 成员、孤儿类型、不可达分支、被注释掉的死代码、`ponytail:` 残留是否还合理 | 死代码清单，每条 `file:line` + 类型 + 建议处置 |
| D3 spec 与代码同步 | `.trellis/spec/{core,views,tests,guides}` 引用的类名/方法名/文件路径/常量名在当前代码中是否还存在（schema-driven-arch-migration 之后可能漂移）；`pitfalls.md` 引用的修复点是否还成立 | spec 漂移清单，每条 `spec:line` + `code:line` + 漂移类型 |
| D4 deferred 当前证据 | R1（MainWindowViewModel 组合重构）/ R2（LauncherEntryVisibility rename）/ R6（DI）/ R7（async I/O）四个显式 defer 项，**当前**代码状态下是否还维持 defer 合理？revisit trigger 是否已触发？ | 每个 R 一段结论：仍 defer / 触发执行 / 正式否决，附当前代码证据 |
| D5 风格一致性 | `ponytail:` 注释是否都已带 ceiling 标注；命名是否一致（如 `Launcher*` vs `WTLauncher*` 前缀混用）；中英文混用是否在 user-facing 文案与代码标识符之间分裂 | 风格条目表，每条 `file:line` + 类型 + 建议 |

## Requirements

- 每个发现必须可验证：`file:line` 引用 + 当前实际值（不能用模糊描述如"很多"）
- 每个发现必须给出三选一建议：`exec-now`（建议立即处置）/ `defer-with-trigger`（什么信号触发再议）/ `no-action`（不需要动）
- `no-action` 建议必须给出 2025–2026 现代实践引用或当前代码证据
- D4 维度对每个 R 项的结论必须显式引用其原始 defer 文档（`.trellis/tasks/archive/2026-07/07-09-composition-di-async-evaluation/prd.md` 等）中记录的 revisit trigger，并判断该 trigger 当前是否触发
- 严守审计准则：每条都是假设，必须用当前代码/文档验证

## Out of scope

- 任何源代码 / 文档 / spec 的修改（即使发现明显错误也只记录到 audit-findings.md，由对应子任务处置）
- 重新审议 07-09 已 closed 的 S1/S4/S10/S11/S12/S13/T4/T5/T6/T8 等（这些已带 citation close）
- `retired/` 目录（父任务 out-of-scope）
- 性能 / 安全 / 可访问性专项审计（本轮不做）
- 用户使用层面的 UX 体验审计（属于 DESIGN.md 范畴，不在本轮）

## Acceptance criteria

- [ ] `.trellis/tasks/07-09-reorg-audit-pass/audit-findings.md` 存在并覆盖全部 5 个维度
- [ ] 每个发现条目有 `file:line` + 当前实际值 + 三选一建议
- [ ] D4 维度对 R1/R2/R6/R7 各自给出当前证据下的明确结论
- [ ] `git diff` 显示本子任务未修改任何 `.trellis/tasks/07-09-reorg-audit-pass/` 之外的文件（仅 `.trellis/tasks/07-09-reorg-audit-pass/audit-findings.md` 是新增）
- [ ] 父任务 PRD 的 child task map 中三轴子任务的创建条件被本审计产出明确触发或显式跳过（即：要么产出足以建 docs/code/deferred 三子，要么显式说明某轴本轮无可执行项）
- [ ] 审计产出包含一份"是否需要下一轮 audit"的判断（默认 NO）

## Deliverable shape

`audit-findings.md` 推荐结构：

```
# Audit findings — 2026-07-09 reorg pass

## Summary
- 总条目数 / exec-now 数 / defer 数 / no-action 数
- 是否触发 docs/code/deferred 三子任务

## D1 文档漂移
| file:line | 文档原值 | 当前实际值 | 建议 |

## D2 代码死代码
| file:line | 类型 | 建议 |

## D3 spec 与代码同步
| spec:line | code:line | 漂移类型 | 建议 |

## D4 deferred 当前证据
### R1 ... ### R2 ... ### R6 ... ### R7 ...
每个 R：原始 trigger / 当前证据 / 结论

## D5 风格一致性
| file:line | 类型 | 建议 |

## Next-round audit 判断
```

## Notes

- 审计本身不改代码，因此 `tools/verify.ps1` 不强制在本子任务跑；但若审计发现某项 verify 检查与实际状态不符，需在 D1 中标记（属文档漂移）
- 审计完成、产出 `audit-findings.md` 后，按其结论触发父任务下一批子任务的创建
