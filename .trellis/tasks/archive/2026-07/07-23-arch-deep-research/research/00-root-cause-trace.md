# 根源追溯分析：R1-R8 架构根因追踪

> 基于 25 agent × 234 文件超全面审查（ultrareview）的结果，追溯每项根因至架构决策层面。

---

## R1. 无统一状态管理策略

### 现象
- Fluxor 已初始化但零消费者（dead code），ServiceProvider 在 `App.axaml.cs:OnFrameworkInitializationCompleted` 方法结束时被 Dispose
- AppStore 是自定义的 SemaphoreSlim 守卫 + 事件订阅，与 Fluxor 无关
- 同一项目中存在两套不兼容的状态管理机制，无优胜者
- 导致：同步-异步混用（`GetAwaiter().GetResult()`）、无 CancellationToken 传播、UI 层直接读写持久化

### 根源决策追溯

| 层次 | 决策 | 时间 | 后果 |
|------|------|------|------|
| 架构选择 | 最初选择 **AppStore**（自定义 `SemaphoreSlim` + 事件）作为状态管理 | 项目早期 | 工作正常但扩展性有限，没有中间件/Effect 概念 |
| 中期决定 | 引入 **Fluxor** 试图替代 AppStore | `b33f6a1` | 注册骨架 + 初始状态同步已完成 |
| 最终决定 | Fluxor 迁移被搁置（ponytail） | 上一步（07-23-fluxor-migration） | 注册代码保留但零 Reducer/Effect 接线 |
| 残留 | 两个框架共存，都没有完整采用 | 现在 | 维护者困惑，两者都不敢删除也不敢依赖 |

**核心问题**：项目经历了从「自制方案 → 标准方案 → 放弃」的完整 cycle，但没有回滚自制方案也没有完成迁移。决策悬在半空。

### 关联根因
- → R4（同步-异步反模式）：Fluxor 的 InitializeAsync 被 `GetAwaiter().GetResult()` 调用
- → R8（并发安全）：AppStore 手动管理 SemaphoreSlim，易错

---

## R2. 废弃架构残留物（Dead Code）

### 现象
- **ExceptionBoundary**：4 个方法定义（`Run<T>`, `RunAsync<T>`, `RunAction`, `RunActionAsync`），34 个 catch 子句，**ZERO 调用者**
- **Fluxor 骨架**：注册 + 初始同步已完成，但无任何 Reducer/Effect 接线
- **LauncherIcons**：5 个 SVG 路径定义，从不引用
- **StoreReducers.AddSearchTerm**：方法定义了但从不调用

### 根源决策追溯

| 死代码 | 引入原因 | 废弃原因 | 遗留原因 |
|--------|---------|---------|---------|
| ExceptionBoundary | 试图统一异常处理模式 | 灵活性不够（固定错误消息），调用点需要定制处理 | 没人敢删除（怕以后要用） |
| Fluxor | 试图标准化状态管理 | 迁移成本 > 预期收益 | 骨架已提交，删除感觉像回退 |
| LauncherIcons | 集中管理 SVG 图标 | XAML 直接内联 PathIcon 更简单 | 遗忘 |
| StoreReducers | Fluxor 配套 | Fluxor 未完成 | 文件整体未清理 |

**核心问题**：缺乏「删除未使用代码」的文化和工具。`verify.ps1` 未检查死代码。没有标记废弃→删除的生命周期流程。

---

## R3. 异常处理分裂

### 现象
- `LauncherErrors.IsRecoverable(ex)` 在 Release 中过于宽泛（包含几乎所有非致命异常）
- `ExceptionFilters.IsRecoverable` 有 DEBUG 守卫（DEBUG 下 crash on NRE/IOOR/ANE），而 `ExceptionBoundary` 使用 `LauncherErrors.IsRecoverable` 无守卫
- 34 处 catch 子句使用手动模式，未使用 ExceptionBoundary

### 根源决策追溯

| 决策 | 后果 |
|------|------|
| `ExceptionFilters.IsRecoverable` 在 App 层，有 DEBUG 守卫 | DEBUG/Release 行为不一致是故意的（开发期 crash fast，生产期尽可能恢复） |
| `LauncherErrors.IsRecoverable` 在 Core 层，无 DEBUG 守卫 | Core 不能引用 App 的 `ExceptionFilters`，被迫复制逻辑 |
| `ExceptionBoundary` 使用 `LauncherErrors.IsRecoverable` | 意味着在 DEBUG 下 ExceptionBoundary 不会 crash on NRE — 调试体验不一致 |

**核心问题**：`ExceptionFilters`（App 层）和 `LauncherErrors`（Core 层）之间的 IsRecoverable 实现分裂。Core 层的实现没有 DEBUG 守卫，但 `ExceptionBoundary`（App 层）使用 Core 层的实现而不是 App 层的 `ExceptionFilters`，导致即使 App 层也不一致。

**时间线推测**：`LauncherErrors.IsRecoverable` 在先（Core 层），`ExceptionFilters.IsRecoverable` 在后（App 层，增加了 DEBUG 守卫），`ExceptionBoundary` 最后但选择了错误的引用路径。

---

## R4. 同步-异步反模式

### 现象
- `GetAwaiter().GetResult()` 出现在扫描和启动路径中
- `Parallel.ForEachAsync` 中 CancellationToken 被丢弃为 `_`
- 无系统性异步策略

### 根源决策追溯

| 位置 | 代码 | 风险 |
|------|------|------|
| `App.axaml.cs:154` | `fluxorStore.InitializeAsync().GetAwaiter().GetResult()` | 阻塞 UI 线程 |
| `App.axaml.cs:155` | `fluxorProvider.GetRequiredService<IStore>()` 同上 | — |
| `AgentDiscoveryScanner.cs:76` | `Parallel.ForEachAsync(..., ct)` 但 ct 被 `_` 丢弃 | 无取消手段 |
| `MainWindowViewModel*` | 多个 `async void` 事件处理 | 异常无法被 catch |

**核心问题**：Avalonia 启动路径是同步的（`OnFrameworkInitializationCompleted` 是 `void` 返回），但数据加载是异步的。没有一个「异步启动」模式。团队选择了阻塞（sync-over-async）而非重构初始化流程。

没有 CancellationToken 传播的原因是状态管理中没有位置存储 CancellationToken — AppStore 不持有它，Fluxor 未接线。

---

## R5. Schema/序列化刚性

### 现象
- `UnmappedMemberHandling.Disallow` 阻止前向兼容（旧版本无法打开新版本配置 — 因为未知字段会抛异常）
- 无 schema 版本下界验证（没有检查"当前代码能处理哪些旧版本"）
- 无序列化测试覆盖版本迁移

### 根源决策追溯

| 决策 | 位置 | 后果 |
|------|------|------|
| `UnmappedMemberHandling.Disallow` | `LauncherJson.cs` 序列化配置 | 任何新增字段都会导致旧版本解析新配置时抛 `JsonException`。这阻止了非破坏性的前向升级 |
| 无 `schemaVersion` 下界 | - | 如果 schema 跳到 v3 而代码只能处理 v2，没有优雅降级路径 |
| 序列化配置与记录定义同文件 | `LauncherJson.cs` + `LauncherStoreFile.cs` | 版本迁移逻辑无处放置 |

**核心问题**：`UnmappedMemberHandling.Disallow` 是一个过于严格的设置。它的意图是「捕获拼写错误」，但代价是牺牲了前向兼容性。在多版本共存的桌面应用中（特别是用户可能不更新），前向兼容比字段验证更重要。

### 时间线
- 最初可能使用默认设置（`Allow`）— 前向兼容
- `Disallow` 是在某次重构中添加的（未找到确切 commit）
- 无对应的版本迁移测试

---

## R6. 门禁/CI 覆盖不足

### 现象
- CI 从未验证 Native AOT 编译（`PublishAot=true` 仅用于本地 `build-release.ps1`）
- `catch { }` 裸捕获模式未被 gate regex 捕获
- `nuget-audit` CI job 只跑 `dotnet restore`，不跑审计（名不副实）
- 无 SCA（依赖漏洞扫描）
- 无死代码检测

### 根源决策追溯

| 缺口 | 原因 | 影响 |
|------|------|------|
| 无 Native AOT 验证 | CI `publish` 命令未传递 `-p:PublishAot=true` | AOT 编译在 PR 阶段不会失败，只在本地发布时暴露 |
| `catch { }` 裸捕获未检测 | regex 只匹配 `catch\s*\(\s*Exception\b`，不匹配 `catch { }` | `PresetManager.cs:43` 和 `Batch.cs:111,133` 的裸捕获逃逸门禁 |
| `nuget-audit` 名不副实 | CI 工作流名称错误，实际只 restore | 维护者误以为有审计 |
| 无死代码检测 | 无相关 tooling | R2 的 4 处死代码持续存在 |

**核心问题**：gate 的加固是增量式的（每次发现新问题就加一个 check），但缺少系统性的「门禁覆盖地图」来识别盲区。

---

## R7. XAML 与设计令牌不一致

### 现象
- 8 处硬编码 FontSize（11, 13, 18, 28）未使用 LauncherDesignTokens
- `{StaticResource BorderBrush}` 引用未注册的资源（运行时 KeyNotFoundException）
- 代码生成器中设计令牌的单一来源原则被违反

### 根源决策追溯

| 问题 | 原因 |
|------|------|
| 硬编码 FontSize | LauncherDesignTokens.FontSizeSmall/Body/Emphasis 定义不全，缺少 11/13/18/28 变体 |
| `BorderBrush` 未注册 | 资源注册列表（`InjectDesignTokens`）与 XAML 引用列表不同步 — 没有编译期检查 |
| XAML 资源引用是字符串 | Avalonia 的 `{StaticResource}` 是运行时解析的字符串，没有编译期验证 |

**核心问题**：Avalonia 的 StaticResource 是运行时解析的。当设计令牌在 `App.axaml.cs` 中注册时，没有机制验证所有 XAML 引用都有对应的注册项。这是一个框架层面的限制，需要通过测试来弥补。

`VisualTokenContractTests.cs` 存在但只检查了部分资源键 — `BorderBrush` 漏网。

---

## R8. 并发安全漏洞

### 现象
- LauncherJson 无锁（无 `lock`/`SemaphoreSlim`），多线程写入竞争
- `MemoizedSelector` 值元组结构撕裂风险
- `ConcurrentBag` 丢失 Agent 发现顺序

### 根源决策追溯

| 漏洞 | 根因 |
|------|------|
| LauncherJson 无锁 | LauncherStore（SemaphoreSlim）保证调用者级别的序列化，但 LauncherJson 自身没保护。如果未来有并行代码路径绕过 LauncherStore 直接调用 LauncherJson，就会出现竞争 |
| MemoizedSelector 未同步 | 只从 UI 线程调用，添加同步的感知收益为 0。但如果将来从 BG 线程调用，就会出问题 |
| ConcurrentBag 无序 | `Parallel.ForEachAsync` + `ConcurrentBag` 自然无序。Agent 发现顺序变化影响用户感知 |

**核心问题**：当前状态下这些都不是活跃 bug（UI-only 调用路径保证了安全性），但它们违反了"正确性优先"原则。未来重构中如果调用路径变化，这些隐藏假设会变成活跃 bug。

---

## 根因聚合

8 个根因可以聚合为 **4 个架构决策失误**：

```
决策失误 A：状态管理策略选择不定（Fluxor vs AppStore）
  → R1（无统一状态管理）
  → R4（同步-异步反模式 — Fluxor 初始化阻塞 UI 线程）
  → R8（并发安全 — 无统一锁策略）

决策失误 B：异常处理两层架构但未统一入口
  → R2（ExceptionBoundary 废弃架构残留）
  → R3（异常处理分裂 — Core/App 层两套 IsRecoverable）

决策失误 C：序列化策略选择了安全性而非兼容性
  → R5（UnmappedMemberHandling.Disallow 阻止前向兼容）

决策失误 D：门禁/治理是反应式的而非设计性的
  → R6（CI 覆盖盲区）
  → R7（设计令牌无编译期验证 — 依赖运行时测试）

建议：新架构应优先解决决策失误 A 和 B，这两者造成的问题最多（R1+R4+R8 和 R2+R3），然后解决 D（R6+R7），最后解决 C（R5）。
```
