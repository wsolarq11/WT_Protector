# ViewModel 状态修复
## Requirements
- [ ] R1: LaunchAsync 先 SaveConfig 再 RecordLaunch
- [ ] R2: 单次启动防重复点击守卫
- [ ] R3: ApplyCardSize 同步更新所有条目
- [ ] R4: SelectVisible CanExecute 守卫
- [ ] R5: IsCompactLayout 在 Micro 时为 false
- [ ] R6: FailureIndex 命名一致性
- [ ] R7: _filterText 旁路注释
- [ ] R8: 快照 Entries getter 缓存
## Acceptance
- [ ] dotnet test ViewModel 相关 PASS