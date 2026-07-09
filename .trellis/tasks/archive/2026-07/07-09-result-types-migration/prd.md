# Migrate exception flow to Result<T, StoreError>

## Goal

调查并定案：是否需要将 LauncherStore 异常控制流迁移到 `Result<T, StoreError>`。

## 调查结论：无需迁移（S1/S4 被误识别）

审查当前异常处理代码 + 2025 现代 .NET 实践后确认：当前异常设计**就是现代最佳实践**，全量迁移到 `Result<T, StoreError>` 是投机性重构（ponytail ultra 禁止）。

### 当前架构事实

| 边界 | 机制 | 评价 |
|------|------|------|
| Domain 值对象（EntryId/EntryName/PresetId） | `Parse` 方法返回值对象，非法输入抛 FormatException | ✅ Parse-Don't-Validate 已实现 |
| Trust 边界（文件 I/O、JSON 反序列化、Schema 校验） | 抛 IOException/JsonException/InvalidDataException | ✅ 现代 .NET 规范 |
| App/ViewModel 边界（20+ 处） | `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` | ✅ 单一分类器，非控制流 |
| 用户消息渲染 | `LauncherPersistenceStatus.StoreSaveFailed(ex)` + `ExceptionMessageFormatter.ForUser(ex)` | ✅ 已是 Result 风格的 UI 转换 |
| Process 顶层（Program.cs） | `AppDomain.UnhandledException` + `TaskScheduler.UnobservedTaskException` | ✅ 崩溃日志兜底 |

### 现代 .NET 实践判定（2025）

| 来源 | 共识 |
|------|------|
| [Microsoft Learn — Best practices for exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions) | "Use try/catch/finally to recover from errors"; exceptions 仍是处理 unexpected 错误的标准机制 |
| [Reddit r/dotnet 2025 共识](https://www.reddit.com/r/dotnet/comments/1krsajb/is_it_really_worth_using_the_result_pattern) | "Result pattern for business logic errors; Exception for technical errors" — 单用户启动器的 I/O 错误是 technical，不是 business |
| [Redgate Simple Talk — Result Pattern in ASP.NET Core](https://www.red-gate.com/simple-talk/development/dotnet-development/the-result-pattern-in-asp-net-core-minimal-apis) | "Result pattern 用于 EXPECTED failure cases (validation, not found, permission)" — 启动器的持久化失败是 UNEXPECTED，例外仍正确 |
| [Stack Exchange — Result object vs throwing exceptions](https://softwareengineering.stackexchange.com/questions/405038/result-object-vs-throwing-exceptions) | "Throw exception when error is unexpected" |

**关键判断**：单用户桌面启动器的 launcher.json 加载/保存失败属于 UNEXPECTED 错误（文件被外部破坏、磁盘满、权限丢失），不是 EXPECTED 错误。Result 模式适用于高频的业务路径（HTTP 4xx 解析、批量校验），不适用于一次性配置加载。

### 全量迁移的成本/收益分析

**成本（全量 Result<T, StoreError> 迁移）:**
- ~50 个 throw 站点改造（LauncherStore/Validator/Visibility 等）
- ~20 个 catch 站点改为 pattern-match
- ~50 个测试改写
- 引入新类型 `StoreError`（单实现，违反 ponytail "no abstraction with one implementation"）
- `ExceptionFilters.IsRecoverable` 部分失效但保留（fatal 分类仍需要）
- 跨层 API 破坏性变更

**收益（实际切断的 bug 源）:**
- ❌ 没有。当前 451 测试通过，audit 未指出任何由异常控制流导致的具体 bug。
- ❌ "Multi-layer validation drift" (S1) 在数据层面已由值对象切断
- ❌ "Exception-based control flow" (S4) 是误读 — catch 站点是错误边界，不是控制流分支

**ROI: 负。** Ponytail ultra: 删除优于添加，boring 优于 clever。

## Requirements

- **无代码变更**。本任务为"调查并拒绝"型。
- 更新 spec 文档：明确记录当前异常边界设计是有意选择，引用 2025 现代 .NET 共识。
- 更新父任务 PRD：将 S1/S4 标记为"误识别，无需行动"。

## Acceptance Criteria

- [x] 当前异常处理站点审查完成（20+ catch, 50+ throw, 1 个分类器）
- [x] 2025 现代 .NET 实践调研完成（Microsoft/Reddit/Redgate/StackExchange 一致）
- [x] spec 文档更新：明确当前设计为现代最佳实践
- [x] 父任务 `prd.md` 更新：S1/S4 标记为误识别
- [x] `dotnet build` 0 警告 0 错误
- [x] `dotnet test` 451 测试通过
- [x] `tools/verify.ps1` 16 项检查通过

## Notes

- 这是"调查并拒绝"型任务：拒绝投机性重构是有意决定。
- 真正需要 Result 模式的场景：未来若加入"批量校验 API"或"插件动态加载"（高频 EXPECTED 错误），届时再引入 Result<T, E>。当前不引入。
- `ExceptionFilters.IsRecoverable` 保留 — 它在 fatal/recoverable 分类上仍有不可替代的价值（OOM/StackOverflow 必须区分于 InvalidDataException）。
