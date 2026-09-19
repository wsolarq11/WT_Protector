# Thinking Guides — WT_Protector

> 目的：扩展思考，捕捉本 C# Avalonia MVVM 桌面应用易漏点。

## Guides

| Guide | 用途 |
|-------|------|
| [Code Reuse](./code-reuse-thinking-guide.md) | 识别重复、减少重复 |
| [Cross-Layer](./cross-layer-thinking-guide.md) | 5 层数据流思考 |
| [Pitfalls](./pitfalls.md) | 50+ session 取证坑 + 预防规则 |

## Pre-Development Checklist

- [ ] 新 session 先速读 [Pitfalls](./pitfalls.md) 第 1、2 章。
- [ ] 特性跨 3+ 层 / JSON 字段增改 / 不确定逻辑归属 / 需跨文件原子一致 → 读 [Cross-Layer](./cross-layer-thinking-guide.md)。
- [ ] 相似校验/状态文本 3+ 处 / 新 `const` / 重复 snapshot-mutate-save → 读 [Code Reuse](./code-reuse-thinking-guide.md)。
- [ ] 改任何值前先 `grep -r "<value>" src/` 搜索（Pre-Modification Rule）。
- [ ] 诡异错误先搜 pitfalls 关键词；批量 rename 前读 §3.4/§3.5；网络问题读 §M4；三撞同 blocker 读 §3.3；drag 读 §2.5；性能读 §2.6。

## Quality Check

- [ ] 声称完成前 `npm run verify`（fresh native receipt）。
- [ ] 无裸 `catch (Exception)`（gate `no-bare-catch-exception`，自 `aae2454e` / 修复 `b3556262`）。
- [ ] 文档不硬编码"N 条/N 类"；写"持续更新"（§3.1）。

## WT_Protector 高频坑

1. 单文件原子性：一切状态在 `config/launcher.json`；`LauncherJson.Save` temp + `File.Replace` 原子提交；回滚由 `AppStore.Mutate` 内部 + VM `RestoreVmStateFromStore()`；禁 `LauncherStore` 外直写 JSON。
2. Design token 漂移：XAML 硬编码 `#2563EB` 会在 token 变更时漂移；用 `{StaticResource PrimaryBrush}`。
3. Drag 状态泄漏：拖拽中切排序/过滤必须先取消 drag；`MainWindowViewModel.Refresh()` 以 `DiscardDragReorderPreview()` 开头。
4. 单一事实源：只读投影（`IsCompactLayout`、`IsCardsViewMode` 等）由单一可写字段计算；禁存投影为第二字段。
5. 裸 catch：窄化为 `IOException | UnauthorizedAccessException | SecurityException` 或 `when (ExceptionFilters.IsRecoverable(ex))`。

## Pre-Modification Rule

改任何值前先搜：`grep -r "CardBorderRadius" src/`、`grep -r '"selected"' src/`。

## Core Principle

30 分钟思考省 3 小时调试；"直接改"永远不是第一步。
