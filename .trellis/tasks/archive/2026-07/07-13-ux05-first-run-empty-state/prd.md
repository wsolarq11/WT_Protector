# UX05 首次使用空状态

## Goal

为首次运行（零 entry 配置）提供有帮助的引导提示，替代当前通用的"没有匹配的启动项"文本。

## Confirmed facts

- ViewModel 已有 `TotalCount`（总 entry 数）和 `VisibleCount`（过滤后可见数），`IsVisibleEmpty` 基于 `VisibleCount == 0`
- 当前 `EmptyStateText` 是静态字符串 `"没有匹配的启动项"`，无法区分"首次运行"和"过滤无结果"
- 三个视图模式（Cards/Details/Names）各有一个独立的空状态 TextBlock

## Requirements

| # | Area | What | How |
|---|------|------|-----|
| R1 | First-run detection | 当 TotalCount == 0 时识别为首次运行 | `IsFirstRunEmpty` 推算属性 |
| R2 | Differentiated text | 首次运行显示引导信息；过滤无结果显示原有文本 | `EmptyStateText` 按 TotalCount 返回不同字符串 |
| R3 | Quick-action hints | 首次运行时提示用户添加/发现/配置的入口命令 | 状态栏自动显示快速操作提示 |
| R4 | AutomationProperties | 空状态 TextBlock 保持可访问名 | 已有 AutomationProperties.Name，更新文本即可 |

## Acceptance Criteria

- [ ] AC1: 零 entry 时显示首次运行引导提示（非"没有匹配的启动项"）
- [ ] AC2: 有 entry 但过滤无匹配时仍显示"没有匹配的启动项"
- [ ] AC3: 状态栏在首次运行时自动设置 helpful 提示
- [ ] AC4: `dotnet test -c Release` 全部通过
