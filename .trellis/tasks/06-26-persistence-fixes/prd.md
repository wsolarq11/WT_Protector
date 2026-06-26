# 持久化层修复
## Requirements
- [ ] R1: CleanupStaleTempFiles 扩展 .stage-*/.bak-tmp-*/.recovery-*
- [ ] R2: Load 恢复路径加 ErrorLog.Write
- [ ] R3: catch 加 PathTooLongException
- [ ] R4: 移除冗余 Flush
- [ ] R5: AddPreset 加数量上限检查
- [ ] R6: presets/settings 加文件大小门禁
## Acceptance
- [ ] dotnet test Persistence 相关 PASS