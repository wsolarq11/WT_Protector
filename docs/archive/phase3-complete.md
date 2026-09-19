Status: Archived
Purpose: 记录 Phase 3（补充低断言测试文件）的改动与验收。
Original scope: 2026-07-23 完成，三个低断言测试文件补充至 >= 5 断言。

# Phase 3 完成报告: 补充低断言测试文件

## 结果

三个低断言测试文件均补充至 >= 5 断言，总计新增 9 个测试方法、16 个断言。全部 688 个测试通过。

## 关键证据

| 测试文件 | 断言变化 | 新增覆盖 |
| --- | --- | --- |
| `MainWindowDragInsertDecisionTests.cs` | 2 → 16 | x=0/x=100 左右边界、y=0/y=40 上下边界、hysteresis 三段阈值 |
| `DiscoverAgentsViewModelTests.cs` | 3 → 12 | 正常扫描填充 Rows + 状态翻转、空结果提示 |
| `LauncherBatchLaunchPlanTextTests.cs` | 3 → 11 | 空列表、单条目、多条目换行分隔 |

## 原因

遵循测试金字塔原则，补足边界覆盖：拖拽决策边界、扫描正常/空路径、批量启动文本格式化分隔。

## 验证

```powershell
dotnet test src/avalonia-launcher/tests/WTLauncher.Tests
通过: 688, 失败: 0, 跳过: 0
```

## 风险/后续

- hysteresis 测试中 `(false, 55, true)` 因浮点精度（`100*0.55 < 55`）改用 `x=56` 作为 true 值。
- 所有新测试遵循项目既有 xUnit + Theory/Fact 风格。
