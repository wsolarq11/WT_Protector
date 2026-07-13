# Design

## Composite exemplar

- 代码质量：本仓 5 层架构、FMA 子任务目标、Core 零 Avalonia 依赖、集中服务与薄 ViewModel。
- 视觉：CodexSwitchUI 的组件/主题/token 分离原则，加 Windows/Fluent、WCAG 和仓库 `DESIGN.md`。
- 实操：Raycast、Alfred、VS Code Workspaces、Windows Terminal Profiles、JetBrains Run Configurations 等现有研究中的 first-class object 与反馈模式。

## Audit matrix

每项使用统一字段：`ID / 维度 / 当前证据 / 典范证据 / 用户影响 / 根因 / 最小改造 / 现有任务 / 可行性 / 风险 / 验证`。

## Ranking

- P0：可靠性、数据损失、核心任务阻断、明显无障碍阻断。
- P1：高频流程摩擦、架构热点、明显视觉噪音或反馈不足。
- P2：收益明确但依赖前序或频率较低。
- Reject/Defer：YAGNI、产品范围不匹配或成本高于窄领域收益。

## Evidence boundaries

源码结构和调用关系优先使用 codebase-memory；字符串、XAML token 和非代码文档可使用文件搜索。视觉结论必须落到 XAML/样式或可运行截图，不仅凭文字描述。
