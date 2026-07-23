# Phase 3 完成报告: 补充低断言测试文件

## 结果

三个低断言测试文件的断言数量均已补充至 >= 5，总计新增 9 个测试方法，16 个断言。全部 688 个测试通过。

## 改动详情

### 1. MainWindowDragInsertDecisionTests.cs (原 2 断言 -> 现 16 断言)

| 测试方法 | 断言数 | 说明 |
|---------|--------|------|
| ShouldInsertAfterUsesHorizontalMidpointForCardAndNameModes | 5 | 新增 x=0(左边缘)、x=100(右边缘) 边界值 |
| ShouldInsertAfterUsesVerticalMidpointForDetailedListMode | 5 | 新增 y=0(上边缘)、y=40(下边缘) 边界值 |
| ShouldInsertAfterRespectsHysteresisThreshold | 6 | 新增: 测试 currentInsertAfter=true/false/null 三段阈值的 hysteresis 行为 |

### 2. DiscoverAgentsViewModelTests.cs (原 3 断言 -> 现 12 断言)

| 测试方法 | 断言数 | 说明 |
|---------|--------|------|
| ScanAsync_WhenScanFuncThrowsRecoverable_SurfacesErrorAndClearsScanning | 3 | 已有(未改动) |
| ScanAsync_WhenScanSucceeds_PopulatesRowsAndUpdatesStatus | 6 | 新增: 正常扫描场景，验证 Rows 填充、StatusText 状态、IsScanning 状态翻转 |
| ScanAsync_WhenScanReturnsEmpty_ShowsNoAgentMessage | 3 | 新增: 空结果场景，验证特殊提示语 |

### 3. LauncherBatchLaunchPlanTextTests.cs (原 3 断言 -> 现 11 断言)

| 测试方法 | 断言数 | 说明 |
|---------|--------|------|
| FormatUsesPreflightNameSnapshotWhenEntryNameMutates | 3 | 已有(未改动) |
| Format_EmptyList_ReturnsEmptyString | 1 | 新增: 空列表返回空字符串 |
| Format_SingleItem_ContainsEntryNameAndCommandLine | 3 | 新增: 单条目包含条目名和命令行 |
| Format_MultipleItems_SeparatesEachWithNewline | 4 | 新增: 多条目标记每项 (实际:) 出现次数验证分隔正确性 |

## 原因

- 遵循测试金字塔原则，确保每个测试文件有足够的边界覆盖
- `MainWindowDragInsertDecisionTests`: 添加 x=0/x=100 左右边界和 y=0/y=40 上下边界，以及 hysteresis 阈值三段测试
- `DiscoverAgentsViewModelTests`: 添加正常扫描路径(填充 Rows、状态翻转)和空结果路径
- `LauncherBatchLaunchPlanTextTests`: 添加 Format 边界场景(空列表、单条目、多条目的格式化分隔)

## 验证

```
dotnet test src/avalonia-launcher/tests/WTLauncher.Tests
通过: 688, 失败: 0, 跳过: 0
```

## 风险/后续

- MainWindowDragInsertDecision 的 hysteresis 测试中，`(false, 55, true)` 因浮点数精度问题(100*0.55 < 55)使用 x=56 作为 true 值
- 所有新测试遵循项目已有的 xUnit + Theory/Fact 风格
