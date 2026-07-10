# 改造5：UserControl 拆分 + CompiledBindings 重评

## Goal

评估 MainWindow.axaml (894 行) 是否应该按视图模式拆分为 3 个 UserControl，
以及 CompiledBindings 是否应该启用。

## Decision

**两个子项均保持现状**。详细原因如下。

### CompiledBindings: 保持 `false`

P7 任务（2026-07-10，archive/2026-07/07-10-p7-compiled-bindings-reassessment）
已完整重评：

- 实测开启 `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`
  产生 100 个 AVLN2000/AVLN2100 错误，来自 ~30 个缺 `x:DataType` 的 DataTemplates。
- 修复成本：1–2 小时机修 + GUI smoke 回归风险（459 单元测试不覆盖绑定形状）。
- 性能收益：不可见（单用户启动器、亚秒级启动、≤200 entries）。
- 唯一真实收益是编译期绑定拼写检查，但回归面太大。

触发重新评估的条件（来自 P7）：entry 数量增长 10x、引入启动时间预算、
或 Native AOT 成为真实需求。

### UserControl 完整拆分: 不拆

 ponytail ladder 第 1 步评估："这个改造需要做吗？"

- 现状：MainWindow.axaml 894 行，正常工作，459 单元测试 + GUI smoke 覆盖。
- CLAUDE.md 明确指出 "single MainWindow.axaml with 3 view modes" 是当前架构，
  不是问题源头。
- 拆分成本：6 个新文件（3 axaml + 3 axaml.cs）+ ~500 行 XAML 移动
  + MainWindow.axaml.cs 7 处 x:Name 引用改造（CardsScrollViewer、
  NamesScrollViewer、DetailsEntriesItemsControl、EmptyStateCardsText 等）。
- 风险：DataContext 流转、ContextMenu 路由、ScrollViewer 引用、
  EmptyState 引用、GUI smoke 行为变化。
- 收益：MainWindow.axaml 从 894 行 → ~450 行；视图独立可编辑。
  这是预防性收益，不是 bug 修复。

MainWindow 单文件的优点：3 个视图模式可并排对比修改、共享样式不需要跨文件、
DataContext 流转清晰。这些是有意设计的，不是缺陷。

## Acceptance Criteria

- [x] CompiledBindings 决策已记录（继承 P7 评估结果）。
- [x] UserControl 拆分已评估并记录决策。
- [x] `dotnet test` 459/459 通过。
- [x] 完整 18 项门禁 PASS。

## Notes

- 这与 P6 (Native AOT)、P7 (CompiledBindings) 的处理方式一致：评估后跳过，
  保留详细原因供未来重新评估。
- 改造 1-4 都已应用实质性变更，改造 5 评估后决定不动是合理的 ponytail 决策。
