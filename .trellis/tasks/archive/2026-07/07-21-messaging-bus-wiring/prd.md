# P3-消息总线接线完成 — EntryUpdated 订阅 + SelectionChanged 发送 + 清理模式

## Goal

完成 P3-2..4 阶段遗留的消息总线接线工作。P0-P5 已提取 EntryListViewModel、定义 6 条 FeatureMessages、订阅 EntryAdded/EntryDeleted。本任务完成剩余消息的发送和订阅，确保跨功能通信路径完整。

## Requirements

1. **EntryUpdatedMessage 订阅** — EntryListViewModel 已发送但 MainWindowViewModel 未订阅。订阅并调用 RefreshAllProjections()。
2. **SelectionChangedMessage 发送** — 在选择变更路径中发送已定义的 SelectionChangedMessage。
3. **完整消息清理** — UnsubscribeFromMessages 覆盖所有已注册消息。
4. **保持 670 测试全绿**。

## Acceptance Criteria

- [x] EntryUpdatedMessage 订阅触发 RefreshAllProjections
- [x] SelectionChangedMessage 在选择变更时发送
- [x] UnsubscribeFromMessages 注销全部已注册消息
- [x] dotnet test -c Release 全部通过 (670/670)
