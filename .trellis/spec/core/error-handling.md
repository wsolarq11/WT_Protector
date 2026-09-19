# Error Handling — Core Layer

> 失败 = 异常；用户可见 = 结构化简体中文状态文本。无自定义 `Result<T,E>`：预期错误（校验/未找到）与意外错误（I/O/契约/反序列化）都以异常表达，单用户启动器持久化失败是意外错误。

决策锚点（2026-07-09 复核，任务 `07-09-result-types-migration`）：保留异常。价值对象 `Parse`、`ExceptionFilters.IsRecoverable`、`ExceptionMessageFormatter.ForUser`、`LauncherPersistenceStatus.*` 已构成层次化纪律。仅当出现高频预期错误路径（批量校验 API、插件动态加载）才重审 Result。

## Status Text

`MainWindowViewModel.cs` 内静态文本类：`LauncherMainWindowText`（就绪/加载/占位）、`LauncherPersistenceStatus`（保存/回滚失败）、`LauncherEntryStatus`（增删改）、`LauncherPresetStatus`、`LauncherSelectionStatus`、`LauncherSettingsStatus`、`LauncherSingleLaunchStatus`、`LauncherSingleLaunchReceiptText`、`LauncherBatchLaunchPlanText`、`LauncherBatchLaunchReceiptText`。

规则：
1. 全部简体中文。
2. 静态方法 + 类型化参数；调用点禁字符串插值。
3. 方法参数 `ArgumentNullException.ThrowIfNull()`。
4. 不含 PII/密钥；配置文件路径可显示。

## Validation

规则：
1. 边界校验：JSON 反序列化管结构，值对象 `Parse` 管语义；Store 层不重复校验（Normalize 管线已于 2026-07-06 移除）。
2. 配置/用户输入畸形 → `InvalidDataException`。
3. 编程错误 → `ArgumentException` / `ArgumentNullException`。
4. 错误信息含 index + 字段名。

### Parse / Normalize（取代 Result<T,E>，2026-07-06）

- `Parse` 非法输入抛 `FormatException`；绝不返回 null/Result。
- `Normalize` catch `FormatException` 返回 `string.Empty`。
- JSON converter 直接 `Parse`（异常以 `InvalidDataException` 传播）。
- 需"非法即抛"用 `Parse`；需"非法给默认"用 `Normalize` 或 try/catch。

## 异常类型表

| 异常 | 场景 |
|------|------|
| `ArgumentException` | 编程错误 |
| `InvalidDataException` | 文件/输入损坏 |
| `InvalidOperationException` | 对象状态错误 |
| `IOException` | 文件 I/O 失败 |

异常类型是公共 API；变更前审计所有 `Assert.Throws<T>`。

## Catch 纪律（gate 强制，commit `266aa6f`）

`tools/verify.ps1` `no-bare-catch-exception` 拒绝 `src/avalonia-launcher/src/**/*.cs` 中任何无 `when` 过滤的 `catch (Exception)`。

- 可枚举逃逸 → 类型过滤：`catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)`。
- 不可枚举 → 分类器：`catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` + `ErrorLog.Write(ex, "<Scope>.<Action>")` + 状态文本。
- UI 委托边界（对话框确认）的裸 `catch (Exception)` 可接受（边界未知）。见 [pitfalls §2.3](../guides/pitfalls.md#23-kiss-合并后-catch-过宽)。

契约：
- `ExceptionFilters.IsRecoverable(ex)`：对 OOM/StackOverflow/AccessViolation/SEH 返回 false（解包 Aggregate/TypeInitialization 最多 16 层）；DEBUG 下 NRE/IndexOutOfRange/ArgNull 重抛为 `InvalidOperationException` 强制崩溃。
- `ErrorLog.Write(ex, context)`：写 `%LOCALAPPDATA%/WTLauncher/errors.log`，永不抛；并发写经 `Interlocked.Exchange` 丢弃不排队。
- Scope：`"<Component>.<Action>"`。

矩阵：可恢复 → catch 生效；OOM/SEH/AV → 传播崩溃（故意）；DEBUG 逻辑 bug → 强制崩溃；RELEASE 逻辑 bug → 记日志保持运行。

## AggregateException 空 InnerException 坑

`new AggregateException()` 可零内异常 → `InnerException` 为 null。解包循环必须带界 `for i<16` + null 时 `break`。`ExceptionFilters.IsRecoverable`（App）不重复解包，委托 `LauncherErrors.IsRecoverable`（Core，带界）。参考 bug：`ExceptionFilters.cs:19-20`（2026-07-26 修复，无界 while 会永久挂线程）。

## 消息脱敏（2026-06-24）

`ExceptionMessageFormatter.ForUser()`：白名单透传（已知安全类型），其余给通用消息。禁裸 `exception.Message` 进 UI；`FileNotFoundException.Message` 含完整路径必脱敏。参考实现 `LauncherPersistenceStatus.Failed()`。

## Forbidden Patterns

- ❌ 裸 `catch (Exception)`（gate 强制）。
- ❌ 吞异常；必记日志/显示。
- ❌ 无文档的 `null` 表示"未找到"；意外缺失用异常。
- ❌ 英文状态文本。
- ❌ 状态文本内联拼接；用静态工厂。
- ❌ UI 裸 `exception.Message`；用 `ForUser()`。
- ❌ UI 回调 `catch (Exception)` 无 `IsRecoverable`；OOM/StackOverflow 必须传播。
- ❌ `finally` 里 `File.Delete` 无自身 try/catch；清理失败会替换原异常。
- ❌ Core 层 `Debug.WriteLine` 无 `Trace.WriteLine` 配对；Release 会剥离 Debug。

## finally 安全（2026-06-25）

finally 内清理必须包自身 try/catch，否则 finally 异常替换 try 原始异常。Core 诊断用 `Debug.WriteLine` + `Trace.WriteLine` 配对；App 用 `ErrorLog.Write`。
