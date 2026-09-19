Status: Active
Purpose: docs 目录唯一索引，按 Active / Reference / Research / Archive 分层。
Update when: 文档新增、移动、归档或用途变化时。

# 文档索引

检索顺序：Active → Reference → Research → Archive。

## Active

| 文档 | 用途 | 更新触发 |
| --- | --- | --- |
| [../README.md](../README.md) | 用户功能、配置 schema、安全口径、安装使用 | 用户可见行为或 schema 变化 |
| [../AGENTS.md](../AGENTS.md) | 全仓导航、结构基线、门禁入口 | 结构、门禁或导航变化 |
| [../DESIGN.md](../DESIGN.md) | 设计契约、视觉 Token 与组件所有权 | UI/UX 或 Token 变化 |
| [feature-reality-north-star-audit.md](feature-reality-north-star-audit.md) | 22 项功能映射审计与声明边界 | 功能边界或 claim 变化 |
| [business-wiring-boundaries.md](business-wiring-boundaries.md) | View → VM → StoreGrain → 外部进程接线总账 | 接线或持久化变化 |
| [wiring-map.md](wiring-map.md) | workflow 与端口唯一台账 | 新增端口或 workflow |
| [design-benchmark-digest.md](design-benchmark-digest.md) | 12 项外部设计基准 → 本地验收规则 | 基准或验收规则变化 |
| [test-structure.md](test-structure.md) | 测试层级与文件归属规则 | 测试目录或分类变化 |
| [reference/distribution-signing-strategy.md](reference/distribution-signing-strategy.md) | 发布、签名与更新策略 | 发布或签名策略变化 |
| [使用说明.txt](使用说明.txt) | 用户快速上手 | 安装或使用流程变化 |

## Reference

| 路径 | 内容 |
| --- | --- |
| [reference/github-similar-projects-snapshot.json](reference/github-similar-projects-snapshot.json) | 12 项目市场对标快照，机器可读 |
| [reference/shortcut-backup-manifest.json](reference/shortcut-backup-manifest.json) | 退役快捷方式清单，机器可读 |
| [giants-shoulders/README.md](giants-shoulders/README.md) | 外部项目教训库入口与准入规则 |

## Research

探索性记录，非行动方案。

| 文档 | 用途 |
| --- | --- |
| [research/architecture-patterns-survey.md](research/architecture-patterns-survey.md) | 架构模式调研（2026-07-21 时点） |
| [research/wt-launcher-architecture-zero.md](research/wt-launcher-architecture-zero.md) | 放弃 GUI 的退路方案，非行动方案 |
| [research/architecture-research-report.md](research/architecture-research-report.md) | 行业架构调研（2026-07-23 时点） |

## Archive

历史快照，不维护、不改写、不参与门禁扫描。清单见 [archive/README.md](archive/README.md)。

## 维护规则

1. Active 文档头部保留 `Status`、`Purpose`、`Update when` 三行。
2. 结构与门禁计数只允许出现在 `AGENTS.md` 结构基线表。
3. 文档引用必须可解析；归档后只在 `archive/README.md` 登记旧路径。
