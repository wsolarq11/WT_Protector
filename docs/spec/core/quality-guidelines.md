# Quality Guidelines — Core Layer

> 代码标准、测试要求、禁止模式；不变量由 `tests/WTLauncher.Tests/ArchitectureRules.cs`（R1-R10）+ Roslyn 分析器（WT1001-WT1006）强制。

## Invariants（永不可破）

1. Core 禁引用 Avalonia（R1）。
2. Core 禁引用 CommunityToolkit.Mvvm（R2）。
3. Core 禁依赖 `WTLauncher.App`（R3、R6 兜底禁 App.ViewModels）。
4. 持久化 JSON 一律 LF。
5. `launcher.json` 每条 entry 的 `id` / `name` / `directory` / `command` 非空（`LauncherStore` 强制）。
6. 每状态单一写所有者（view / sort / drag / card density）。

## 单一事实源

| 状态 | 唯一写所有者 | 只读投影 |
|------|-------------|---------|
| View mode | `_selectedDisplayMode` | `IsCardsViewMode` / `IsDetailedListMode` / `IsNamesViewMode` |
| Sort mode | `_selectedSortMode` | `IsCustomSortMode` |
| Card 尺寸 | `LayoutManager.CardLayout`（CardLayoutProvider） | `LauncherEntryViewModel.CardWidth` / `.CardHeight` / `.NameTileWidth` / `.NameTileHeight` |
| Card density | `LauncherEntryLayoutDensity` | `IsCompactLayout` / `IsMicroLayout` |
| Drag cues | `DragCueState` | `IsDragPreviewTarget` / `.Source` / `.InsertBefore` / `.InsertAfter` |
| Drag session | `DragReorderSession?` | 禁散字段 |
| Selection | `IsSelected`（用户）/ `SetSelectionFromOwner`（批量） | 禁全局批量开关 |

规则：只读投影必须是计算属性，不存第二份状态。

## 测试

xUnit v3 + ArchUnitNET；计数随代码演进，跑 `ls tests/WTLauncher.Tests/*.cs | wc -l` + `dotnet test -c Release` 取现数。类别：Core 单测 / ViewModel / Drag-reorder / View 逻辑 / 契约集成 / Persistence / Architecture。范式：一生产类一测试文件；共享 fixture 在 `LauncherPersistenceTestFixtures.cs` / `TestRepositoryPaths.cs` / `TempDirectory.cs`；工厂方法造数据；成功 + 失败路径都测（每个持久化测试含回滚场景）。

## 编译期强制（2026-07-24 / 26）

| 规则 | ID | 范围 | 状态 |
|------|----|------|------|
| Core 禁引用 App.ViewModels/Views（含 using 与全限定名） | WT1001 | 仅 Core | Active |
| `IStorePersistence.Save` / `LauncherStore.Save` 仅 `StoreGrain.Dispatch` | WT1003 | 生产代码（测试豁免） | Active |
| `catch (Exception)` 必须带 `when` | WT1004 | 全源码（分析器项目除外） | Active |
| `{StaticResource Key}` 必须已定义 | WT1005 | 全 `.axaml`（AdditionalFiles） | Active |
| `LauncherJson.Save/Load<T>` 类型必须 `[JsonSerializable]` | WT1006 | App + Core | [Obsolete] 交叉引用 |

接线关键不变量：新分析器项目进 .sln 不够，消费 `.csproj` 必须 `ProjectReference ... OutputItemType="Analyzer" ReferenceOutputAssembly="false"`，否则 DLL 编译但从不加载。放 `Directory.Build.props` 统一覆盖；自引用守卫 `$(MSBuildProjectName) != 'WTLauncher.Analyzers'`。分析器仅 CI（`$(CI)=='true'`）或 `-p:ForceAnalysis=true` 运行。

## Forbidden Patterns

- ❌ `[ObservableProperty]` 存 ephemeral View 派生尺寸（CardWidth/CardHeight）；用共享 `CardLayoutProvider` 单一源 + VM 只读委托（2026-07-25，删 `LauncherEntryLayoutPresentation.cs`）。
- ❌ `dynamic` / `Dictionary<string, object>`。
- ❌ 无解释注释的 `#pragma warning disable`。
- ❌ 公共可变字段；用属性。
- ❌ 静态可变状态；静态类均为无状态 helper。
- ❌ `LauncherJson` 外直接 `File.WriteAllText`。
- ❌ Core 加 NuGet 引用；仅 BCL。
- ❌ 无校验静默 null→empty；坏数据应抛。
- ❌ 无窄化的 `catch (Exception)`；VM 命令用 `when (ExceptionFilters.IsRecoverable(ex))`；UI 委托边界裸 catch 可接受（pitfalls §2.3）。
- ❌ mutation 中 await 前捕获快照；快照必须在对话框确认后捕获（2026-06-23 B6）。
- ❌ 文档硬编码数量（"13 条"/"55 类"）；写"持续更新"（pitfalls §3.1）。
- ❌ positional record 冗余后备字段；用 init 默认 + auto-property。
- ❌ `LauncherDesignTokens` 未消费的 `const`；每个 token 须有 `src/` 消费者，`grep -r <TokenName> src/ --include='*.cs' --include='*.axaml'` 验证。
- ❌ 零订阅事件处理（无 `+=` 且无 XAML 接线的 `private void On*`）。
- ❌ 共享数据载体缺编译期 mutation 守卫；用 `{ get; init; }` + `sealed record`（2026-06-23 immutability hardening）。
- ❌ `SchemaVersion` 加 `[JsonIgnore]`；用 `[JsonPropertyName]` 不加 `[JsonRequired]` 向后兼容（2026-06-25 R1）。
- ❌ Store.Load 仅缺文件时才恢复 `.bak`；文件存在但损坏（Json/InvalidData）也必须恢复（2026-06-25 R3）。
- ❌ 接口实现忽略参数；未用参数注释 `// ponytail: unused parameter — [reason]`（2026-07-06）。
- ❌ 忽略 `SaveConfig()` / `Save*()` 返回值（静默数据丢失）（2026-06-25 R6）。
- ❌ 静态可变集合（`static List/Dictionary` + Clear/Add）；返回元组或不可变集合（2026-06-25 R14）。
- ❌ 批处理阶段不对称失败策略（break vs continue）；同工作流同策略，不同需显式注明（2026-06-25 R9）。
- ❌ 不透明内部标记注释（`// H4` / `// L32` / `// M11`）；写描述性注释（2026-06-25 R30）。
- ❌ 文本处理缺 Unicode 规范化；`Normalize` / `Sanitize` 必须 `Normalize(NormalizationForm.FormKC)`（2026-06-25 R26）。
- ❌ `Process.Start` 前缺命令行长度检查；`CreateProcessW` 上限 32767，builder 必须先校验并抛清晰错误（2026-06-25 R27）。
- ❌ 异常→用户消息用黑名单；`ForUser()` 必须白名单（2026-06-26 H8）。
- ❌ `[JsonRequired]` 新增后不同步全部 fixture（test factory、`verify.ps1` smoke、README 示例）。

已删除禁止项（历史，防重引入）：跨 ID 类型 Normalize 不一致（2026-07-06 删除，值对象用 `Parse(throw)` / `Normalize(catch)`）；`defaultValue` 绕过校验（2026-07-06 删除，`LoadWithRecovery` 接受 raw default）；`IStore` 绕过（2026-07-06 删除，直接用 `LauncherStore.Load/Save`）。

## 架构不变量

- Null 安全：`<Nullable>enable</Nullable>` + `<WarningsAsErrors>nullable</WarningsAsErrors>`；NullReferenceException 在数据层物理不可能。
- 不可变数据流：`sealed record` + init-only；`entry.Selected = true` 编译期拒绝，mutation 走 `with { }`。
- 不可变 workflow mutation：静态方法改 record 标量用 `ref` 参数（List 内容可变 + 标量 `with { }`），`ref` 使替换在调用点显式。
- CI 无头检测：`$env:CI -eq 'true'` 跳过 GUI smoke / 用户路径检查，receipt 记 `skipped-ci-headless`。

## Code Review Checklist

- [ ] 新 Core 代码引用 Avalonia / CommunityToolkit？（是则拒）
- [ ] 新 JSON 字段加 `[JsonPropertyName]` + `[JsonRequired]`？字符串默认 `""`？
- [ ] enum 有配对 helper `NormalizeId()` / `ToId()`？
- [ ] 新 fixture 入共享文件？新持久化路径用原子 temp 写？
- [ ] Unicode：截断查代理对 `char.IsHighSurrogate`；剥字符剥 Control + Format；ID `Normalize(FormC)`。
- [ ] % 注入：`ExpandEnvironmentVariables` 是否在 FormKC 后？（U+FF05→U+0025）
- [ ] UNC：用户目录路径拒绝 `\\` 前缀？PATH hijack：可执行文件解析不回退裸名走 `PATH`？
- [ ] 测试断言无 `Assert.Equal(X, X)` 自比较？JSON 属性检查同时验证 missing + extra（双向集合比较）？
- [ ] 原子保存走 `LauncherJson.Save`（temp→`File.Replace`→备份轮转），禁 `File.WriteAllText`。
- [ ] 备份恢复重试在原 try/catch 外？（内部 = 备份也损坏时死循环）
- [ ] schema version 用方向性 `<` / `>` 检查？旧拒绝 + 新升级提示。
- [ ] `[JsonRequired]` 传播：同步 `verify.ps1` smoke、test factory、README 示例、`config-lib.ps1` 默认（2026-06-26）。
- [ ] 堆叠 bug：修一个 gate-blocking bug 后重跑完整 gate，不假设一次过（2026-06-26）。
- [ ] `File.Replace`（`MoveFileEx MOVEFILE_REPLACE_EXISTING`）目标存在；`File.Move` 仅首次保存；`File.Move(overwrite:true)` 非原子（2026-06-28）。
- [ ] 备份轮转 copy-then-rotate（2026-06-28）。
- [ ] 跨层校验同语义（大小写、`>N` vs `>=N`）；共享常量/方法（2026-06-28）。
- [ ] 异常解包循环带 `for i<16` 上限（null InnerException 死循环）（2026-06-28）。
- [ ] NaN/Infinity：布局算术 `double.IsFinite` / `IsNaN` 守卫（2026-06-28）。
- [ ] 单文件优于多文件：跨段一致性合并单文件 + `File.Replace`；禁 journal/预提交/多文件提交协议除非真无法单文件（2026-06-28）。
