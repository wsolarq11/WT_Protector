# Phase B: Bug 歼灭

## 根因

UltraReview 发现 8 个 High+ 级别的代码 Bug。本阶段修复除分析器接线外的所有硬 Bug。

## Bug 列表

### B1: ExceptionFilters.IsRecoverable DEBUG throw 无效（CR-1）
- 文件：`ExceptionFilters.cs:21-22`
- 修复：`throw` → `Environment.FailFast`
- 验证：DEBUG 下 NullReferenceException 进入 filter → 进程立即终止

### B2: --config <path> 静默忽略（CR-2）
- 文件：`FileStorePersistence.cs:72-89`
- 修复：移除窄门检查（文件名必须 `launcher.json` + 目录 `config`），文件存在即返回
- 验证：`--config C:\custom\my-config.json` 被正确加载

### B3: 32767 限制不含 exe 路径（CR-5）
- 文件：`LaunchArgumentBuilder.cs:74`
- 修复：校验加入 `ResolveTerminalExecutable().Length + 1 + arguments.Length`
- 验证：总命令行长度 >32767 时抛出清晰异常

### B4: 批量取消回执丢失（BS-1, High）
- 文件：`LauncherLaunchWorkflow.cs:82`
- 修复：`ThrowIfCancellationRequested()` 移入 try 块内 + finally 保证回执生成
- 验证：批量启动中途取消后回执显示真实已请求数而非"0 项"

### B5: StatusFor 预检失败计数误标（BS-2, Medium）
- 文件：`LauncherLaunchReceiptText.cs:288-298`
- 修复：用 `FailureIndices.Count` 替代 `FailedCount`
- 验证：预检失败 3 + 运行时失败 2 = 摘要"5 fail"明细正确

### B6: 批量启动后 SaveConfig 失败时不设回执（BS-3, Medium）
- 文件：`LaunchOrchestrator.cs:226-229`
- 修复：统一单启动和批量启动的行为——先设回执再 Save
- 验证：SaveConfig 失败时回执仍显示

### B7: StoreGrain.Dispose 不释放 IStorePersistence（CR-3）
- 文件：`StoreGrain.cs:88`
- 修复：`Dispose` 中检查 `_persistence is IDisposable` 并 Dispose

### B8: Subscribe 返回值丢弃（CR-4）
- 文件：`MainWindowViewModel.cs:194`
- 修复：存储 `IDisposable` 到字段，在适当的时候 Dispose

## 验收标准
- [ ] 全部 8 个 Bug 修复
- [ ] `dotnet build -warnaserror` 通过
- [ ] `dotnet test` 全部通过
- [ ] `powershell -File tools\verify.ps1` 通过
