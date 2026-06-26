# Core 库 Bug 修复
## Requirements
- [ ] R1: 代理对截断 off-by-one fix (LauncherEntryIds.cs:25, LauncherPresetIds.cs:44)
- [ ] R2: Entry ID 规范化统一为 FormKC (LauncherEntryIds.cs:18)
- [ ] R3: 排序预解析 DateTimeOffset (LauncherEntryVisibility.cs:124)
- [ ] R4: FilterAndSort 空 filter 跳过 ToList() (LauncherEntryVisibility.cs:42)
- [ ] R5: Ghost 计算器使用窗口限界重载
- [ ] R6: ZWJ/emoji 截断文档注释
## Acceptance
- [ ] dotnet test Core 相关 PASS，零 warning