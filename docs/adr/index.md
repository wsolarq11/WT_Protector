# 决策 — ADR

> 本层回答：为什么这么设计。每条决策可追溯：问题 → 选项 → 选择 → 后果。

## 架构与分源

| 路径 | 内容 |
| --- | --- |
| [decision-ladder.md](decision-ladder.md) | 决策阶梯 + 变化闭包（目录/代码/视觉/命令/不变量/文档单一源分配） |
| [wiring-map.md](wiring-map.md) | workflow 与端口唯一台账 |
| [business-wiring-boundaries.md](business-wiring-boundaries.md) | View → VM → StoreGrain → 外部进程接线总账 |

## 功能与设计

| 路径 | 内容 |
| --- | --- |
| [feature-reality-north-star-audit.md](feature-reality-north-star-audit.md) | 22 项功能映射审计与声明边界 |
| [design-benchmark-digest.md](design-benchmark-digest.md) | 12 项外部设计基准 → 本地验收规则 |