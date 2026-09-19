# 文档索引

> 检索顺序：当前事实 → 参考数据 → 研究 → 归档。
> Active 文档必须能回答“这份文档维护什么、什么时候改、改哪里验证”。
> 移动文件时先改本索引，再跑 `npm run verify`。

## 当前事实

| 文档 | 一句话用途 | 更新触发 |
| --- | --- | --- |
| [../README.md](../README.md) | 用户功能、配置 schema、安全口径、安装使用 | 用户可见行为或 schema 变化 |
| [../AGENTS.md](../AGENTS.md) | 全仓导航、结构基线、门禁入口、Trellis 说明 | 结构、门禁或导航变化 |
| [../DESIGN.md](../DESIGN.md) | 设计契约、视觉 Token 与组件所有权 | UI/UX 或设计 Token 变化 |
| [feature-reality-north-star-audit.md](feature-reality-north-star-audit.md) | 22 项功能现实审计与声明边界 | 功能边界或 claim 变化 |
| [business-wiring-boundaries.md](business-wiring-boundaries.md) | View → VM → Store → 外部进程接线总账 | 接线或持久化变化 |
| [wiring-map.md](wiring-map.md) | workflow 与端口唯一台账 | 新增端口或 workflow |
| [design-benchmark-digest.md](design-benchmark-digest.md) | 12 项外部设计基准翻译为本地验收规则 | 基准或验收规则变化 |
| [test-structure.md](test-structure.md) | 测试层级与文件归属规则 | 测试目录或分类变化 |
| [reference/distribution-signing-strategy.md](reference/distribution-signing-strategy.md) | 发布、签名与更新策略 | 发布或签名策略变化 |
| [使用说明.txt](使用说明.txt) | 用户快速上手 | 安装或使用流程变化 |

## 参考数据

| 路径 | 内容 |
| --- | --- |
| [reference/github-similar-projects-snapshot.json](reference/github-similar-projects-snapshot.json) | 12 项目市场对标快照，机器可读 |
| [reference/shortcut-backup-manifest.json](reference/shortcut-backup-manifest.json) | 退役快捷方式清单，机器可读 |
| [giants-shoulders/README.md](giants-shoulders/README.md) | 外部项目教训库入口与准入规则 |

## 研究

探索性、非行动方案；不进入当前事实，不参与门禁扫描。

| 文档 | 一句话用途 |
| --- | --- |
| [research/architecture-patterns-survey.md](research/architecture-patterns-survey.md) | 架构模式调研 |
| [research/wt-launcher-architecture-zero.md](research/wt-launcher-architecture-zero.md) | 退路方案，非行动方案 |
| [research/architecture-research-report.md](research/architecture-research-report.md) | 历史架构研究报告 |

## 归档

已完成或历史快照，不维护、不改写、不参与门禁扫描。

| 文档 | 一句话用途 |
| --- | --- |
| [archive/README.md](archive/README.md) | 归档清单与旧路径映射 |
| [archive/feature-first-class-upgrade-audit.md](archive/feature-first-class-upgrade-audit.md) | 历史功能升级审计 |
| [archive/trinity-endgame-architecture.md](archive/trinity-endgame-architecture.md) | 历史终局架构提案 |

其余归档文件见 `archive/README.md`，不在此重复罗列。

## 维护规则

1. Active 文档头部保留 `Status`、`Purpose`、`Update when` 三行。
2. 计数只允许出现在 `AGENTS.md` 结构基线表，其他文档不得写死数量。
3. 文档引用必须可解析；归档后只在 `archive/README.md` 登记旧路径。
4. 历史快照不改写内容，只做归档登记。
