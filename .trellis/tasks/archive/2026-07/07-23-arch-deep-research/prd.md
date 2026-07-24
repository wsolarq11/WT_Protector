# 深源架构调研：剪掉一切问题的源头

## Goal

对 WT Launcher 现有架构进行全面的根源分析，通过深度全网技术调研（Web Search + 前沿架构文献），提出一套能够**从源头消除**全部已发现问题的全新架构方案，而非逐个修复。产出须包含具体的技术选型、架构图、迁移路径和风险分析。

## Background — 超全面审查发现的 8 大架构根源问题

基于 25 个子 agent、142 个源文件 + 92 个测试文件的超全面审查（ultrareview），以下问题是架构性的、无法通过局部修复解决的根源问题：

### R1. 无统一状态管理策略
- Fluxor 已初始化但零消费者（dead code），ServiceProvider 在方法结束时被 Dispose
- AppStore 是自定义的 SemaphoreSlim 守卫 + 事件订阅，与 Fluxor 无关
- 同一项目中存在两套不兼容的状态管理机制，无优胜者
- 导致：同步-异步混用（`GetAwaiter().GetResult()`）、无 CancellationToken 传播、UI 层直接读写持久化

### R2. 废弃架构残留物（dead code）
- ExceptionBoundary：4 个方法定义，34 个 catch 子句，ZERO 调用者
- Fluxor 骨架：注册 + 初始同步已完成，但无任何 Reducer/Effect 接线
- 代码库中存在「建了但没用」的架构框架，混淆维护者

### R3. 异常处理分裂
- `IsRecoverable(ex)` 在 Release 构建中过于宽泛，会吞掉不该吞的异常
- `ExceptionFilters.IsRecoverable` 有 DEBUG 守卫，而 `ExceptionBoundary` 使用 `LauncherErrors.IsRecoverable` 无守卫 — 调试行为不一致
- 34 处 `catch` 仍手动编写模式，未使用 `ExceptionBoundary`

### R4. 同步-异步反模式
- `GetAwaiter().GetResult()` 出现在扫描和启动路径中 — 死锁风险
- `Parallel.ForEachAsync` 中 CancellationToken 被丢弃为 `_`
- 无系统性异步策略

### R5. Schema/序列化刚性
- `UnmappedMemberHandling.Disallow` 阻止前向兼容（旧版本无法打开新版本配置）
- 无 schema 版本下界验证
- 无序列化测试覆盖版本迁移

### R6. 门禁/CI 覆盖不足
- 无 Native AOT 验证（尽管 .NET 10 支持）
- `catch { }` 裸捕获模式未被 gate regex 捕获
- 无 SCA（软件组合分析）/ 依赖漏洞扫描

### R7. XAML 与设计令牌不一致
- 8 处硬编码 FontSize 未使用 LauncherDesignTokens
- `{StaticResource BorderBrush}` 引用未注册的资源（运行时 KeyNotFoundException）
- 代码生成器中设计令牌的单一来源原则被违反

### R8. 并发安全漏洞
- LauncherJson 无锁（无 `lock`/`SemaphoreSlim`），多线程写入竞争
- `MemoizedSelector` 值元组结构撕裂风险
- `ConcurrentBag` 丢失 Agent 发现顺序

## Requirements

### RQ1. 架构根源分析（输出）
- [ ] 对 R1-R8 每个根因进行根本原因追溯（不仅描述现象，要定位到架构决策层面）
- [ ] 识别出哪些根因是同一决策失误的多种表现（例如「状态管理策略缺失」同时导致 R1/R4/R8）
- [ ] 标注每个根因的引入时间线（如果可追溯 git 历史）

### RQ2. 全网深度技术调研
- [ ] 对以下每个架构维度进行 Web Search + 前沿技术调研（至少 3-5 个候选方案/模式）：
  - **状态管理**：Fluxor vs ReactiveUI/ReactiveObject vs CommunityToolkit.Mvvm 消息 vs SignalR vs 纯 Immutable 架构
  - **异常/错误处理策略**：ErrorOr + 异常过滤器 vs 铁路编程 vs OneOf vs 纯 Result 模式 — 适合 Avalonia/.NET 10 的方案
  - **异步/并发模式**：Channel vs Dataflow (TPL) vs AsyncLock vs Nito.AsyncEx — 与 SemaphoreSlim 对比
  - **序列化/版本策略**：STJ 多态 + 版本转换器 vs JSON schema 验证 vs Protobuf — 前向兼容方案
  - **UI 架构**：Fluxor + Redux vs ReactiveUI (ReactiveObject) vs MVC/MVVM-Evolved vs 声明式 (Compose-like for Avalonia)
  - **门禁/QA 自动化**：Native AOT 验证 + 依赖扫描 + 架构测试 (ArchUnitNET) + 突变测试
- [ ] 每个候选方案需要：优点、缺点、与 WT Launcher 的适配性评估、生态系统成熟度、社区活跃度

### RQ3. 新架构方案综合设计
- [ ] 基于调研结果提出 2-3 套完整架构方案（含依赖图、数据流、层间契约）
- [ ] 每套方案须明确说明如何消除 R1-R8 中的哪些根因
- [ ] 每套方案须包括迁移路径（从现状到目标的增量步骤）
- [ ] 每套方案须标注风险等级和回滚策略

### RQ4. 推荐方案及实施路线图
- [ ] 明确的推荐方案（含理由：为什么优于其他方案）
- [ ] 分阶段实施路线图（Phase 1-3，每阶段可独立验证）
- [ ] 每个阶段的工作量估计（文件数、新增/删除行数估算）
- [ ] 不推荐的方案及原因

## Acceptance Criteria

- [ ] **R1-R8 根源追溯完成**：每项根因追溯至架构决策层面，附时间线（可追溯 git 历史）
- [ ] **技术调研覆盖≥6 个维度**：每个维度至少 3 个候选方案，含适配性评估
- [ ] **架构方案≥2 套**：每套方案明确标注消除的根因列表、数据流图描述、层间契约
- [ ] **推荐方案含阶段路线图**：Phase 1-3，每阶段可独立验证，含风险和工作量估算
- [ ] **产出物结构完整**：研究结果输出为 `.trellis/tasks/07-23-arch-deep-research/research/` 下的结构化报告，而非松散笔记

## Out of Scope

- 本任务不进行任何代码修改
- 本任务不实现任何方案 — 仅调研和推荐
- 不评价 Avalonia 框架本身替换（WT Launcher 保持 Avalonia）

## Notes

- 本任务是纯研究任务（无代码修改），PRD-only 适用于研究目标
- 产出物是 `.trellis/tasks/07-23-arch-deep-research/research/` 下的结构化研究报告
- 允许使用 `trellis-research` sub-agent 进行并行调研
- 调研完成后进入 Phase 3（finish）进行提交
