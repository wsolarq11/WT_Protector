# 验证台账: docs/archive/f2-complete.md

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 文档日期 | MISSING→HISTORICAL | 内容无日期; git log --follow 首提交 ab934546 2026-07-23 | 添加 provenance 头（2026-07-23） |
| IPresetService.cs/PresetService.cs/LauncherStatusText.cs/MainWindowViewModel.Presets.cs/Grouping.cs/PresetServiceTests.cs 引用 | VERIFIED | git cat-file -e ab934546:<path> 全部 True; 现行路径存在（PresetServiceTests.cs 现位于 ViewModelBehavior/ 层） | 保留（历史点状记录） |
| FindInvalidEntryIds + EntryId.Comparer 归一化 | VERIFIED | Select-String PresetService.cs: 行130 FindInvalidEntryIds, 行133 ToHashSet(EntryId.Comparer) | 保留 |
| 677 测试通过 | HISTORICAL | 点状测试数（2026-07-23 当时值; 现基线 728 属性/811 用例） | 保留（历史, 已有日期） |
| 17/17 PresetService 测试（13原有+4新增） | HISTORICAL | 点状计数, 与 2026-07-23 提交一致 | 保留（历史） |
