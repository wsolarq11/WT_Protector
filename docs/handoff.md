# 交接快照

> 最后一棒状态：「现在是什么状态」。

## 当前状态（2026-10）

**WT_Protector** — 处于收敛维护期。Core 53 + App 121 + Analyzers 7 文件，740 测试全绿。MVVM 架构收敛，无过度抽象待砍。门禁 42 步验证通过。

## 最近一次大变更

- `docs/` 重构为认识论四层：spec / adr / evidence / reference
- 旧 Active/Reference/Research/Archive 生命周期分层已退役

## 关键锚点

| 事实 | 唯一源 |
| --- | --- |
| 架构分层 | `src/avalonia-launcher/README.md` 五层 |
| 命令 | `package.json`（机器源）+ `AGENTS.md` 脚本工具箱 |
| 配置 schema | Core record 定义 + `LauncherStoreValidator` |
| 视觉 | `docs/spec/visual-perspective-system.md` + `LauncherDesignTokens.cs` |
| 文档 index | `docs/README.md` |

## 待办

见 [roadmap.md](roadmap.md)。