# docs/ — 唯一文档根

> 四层路由：spec（是什么） → adr（为什么） → evidence（怎么来的） → reference（别人的）
> 依赖方向：spec ⊥ adr ⊥ evidence ⊥ reference（无反向引用）
> 真相锚点：每类事实只存一处，他处引用。

## 第一层：Spec — 现状（是什么）

代码现在是什么、应该怎么改。编码规范 + 设计契约。

| 路径 | 内容 |
| --- | --- |
| [spec/index.md](spec/index.md) | 层入口：编码规范 + 设计契约索引 |
| [spec/core/](spec/core/) | Core 层：数据类型、持久化、错误处理、质量指南 |
| [spec/views/](spec/views/) | Views 层：MVVM 契约、drag-drop、对话框、视觉质量 |
| [spec/tests/](spec/tests/) | 测试层：xUnit v3 + ArchUnitNET 覆盖 5 层 |
| [spec/guides/](spec/guides/) | 跨层思维：坑实录、代码复用、五层数据流 |
| [spec/test-structure.md](spec/test-structure.md) | 测试体系唯一设计源：漂移模型、取舍规则、北极星停止条件 |
| [spec/visual-perspective-system.md](spec/visual-perspective-system.md) | 视觉系统：令牌、控件样式、响应式规则 |

## 第二层：ADR — 决策（为什么）

每条决策可追溯：问题 → 选项 → 选择 → 后果。

| 路径 | 内容 |
| --- | --- |
| [adr/index.md](adr/index.md) | 层入口：架构决策 + 功能设计索引 |
| [adr/decision-ladder.md](adr/decision-ladder.md) | 决策阶梯 + 变化闭包（目录/代码/视觉/命令/不变量/文档单一源分配） |
| [adr/wiring-map.md](adr/wiring-map.md) | workflow 与端口唯一台账 |
| [adr/business-wiring-boundaries.md](adr/business-wiring-boundaries.md) | View → VM → StoreGrain → 外部进程接线总账 |
| [adr/feature-reality-north-star-audit.md](adr/feature-reality-north-star-audit.md) | 22 项功能映射审计与声明边界 |
| [adr/design-benchmark-digest.md](adr/design-benchmark-digest.md) | 12 项外部设计基准 → 本地验收规则 |

## 第三层：Evidence — 证据（怎么来的）

探索记录、实测结果、历史快照。不是规范，不是决策，是支撑材料。

| 路径 | 内容 |
| --- | --- |
| [evidence/index.md](evidence/index.md) | 层入口：门禁证据 + 调研 + 归档索引 |
| [evidence/branch-evidence-matrix.md](evidence/branch-evidence-matrix.md) | 门禁分支证据矩阵与判定方法示例 |
| [evidence/research/](evidence/research/) | 架构调研（2026-07 时点） |
| [evidence/archive/](evidence/archive/) | 历史快照——不维护、不改写、不参与门禁扫描 |

## 第四层：Reference — 外部归档（别人的）

市场对标、发布契约、教训库。外部事实，不参与本地决策推理。

| 路径 | 内容 |
| --- | --- |
| [reference/index.md](reference/index.md) | 层入口：发布对标 + 巨人肩膀索引 |
| [reference/distribution-signing-strategy.md](reference/distribution-signing-strategy.md) | 发布、签名与更新策略契约 |
| [reference/github-similar-projects-snapshot.json](reference/github-similar-projects-snapshot.json) | 12 项目市场对标快照 |
| [reference/shortcut-backup-manifest.json](reference/shortcut-backup-manifest.json) | 退役快捷方式清单 |
| [reference/giants-shoulders/](reference/giants-shoulders/) | 外部项目教训库（准入规则见 `giants-shoulders/README.md`） |

## SOP

操作流程——触发条件 + 步骤 + 红线。

| 路径 | 内容 |
| --- | --- |
| [sop/upstream-sync.md](sop/upstream-sync.md) | 上游跟进 SOP：触发/流程/红线 + 状态基线 |

## 导航

| 文件 | 用途 |
| --- | --- |
| [handoff.md](handoff.md) | 交接快照：「现在是什么状态」 |
| [roadmap.md](roadmap.md) | 路线图：「接下来做什么」 |

## 维护规则

1. 文档引用必须可解析——新文档列入对应层 index.md，归档后在 `evidence/archive/README.md` 登记旧路径。
2. `docs/README.md` 是本文件——唯一总索引；层 index.md 是层内清单的补充，不重复总索引已有条目。
3. 结构计数只出现在 `AGENTS.md` 结构基线派生表（`gen:structure-baseline` 生成），禁止文档硬编码文件计数。
4. Status 头按层取值：`spec/` 和 `adr/` → `Active`（可被 Superseded）；`evidence/` 非归档 → `Evidence`；`reference/` → `External`；`evidence/archive/` → `Archived`。`Purpose` 和 `Update when` 保留，`Status` 与所在目录必须一致。