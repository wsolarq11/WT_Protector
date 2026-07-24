# 架构方案综合设计

> 基于 5 个维度 × 6 个 agent 的全网深度调研 + 根源追溯分析，综合出 3 套候选架构方案。

---

## 研究输入汇总

| 文件 | 维度 | 核心推荐 |
|------|------|---------|
| 00-root-cause-trace.md | 根源追溯 | R1-R8 聚合为 4 个决策失误 |
| 01-state-management.md | 状态管理 | 去 Fluxor，强化 CommunityToolkit.Mvvm + 专用状态容器 |
| 02-error-handling.md | 异常处理 | Hybrid: ErrorOr + 窄异常过滤器，删除 ExceptionBoundary |
| 03-async-concurrency.md | 异步/并发 | SemaphoreSlim → LauncherJson，消除 sync-over-async，传播 CancellationToken |
| 04-serialization-versioning.md | 序列化版本 | 读宽松 + 写严格，拆分为 LooseOptions/StrictOptions |
| 06-ci-qa-automation.md | CI/QA | AOT 验证 + gate 加固 + 自定义分析器 |

---

## 方案 A："做减法" — 安全清理

> **哲学：先清理再重构。删除所有死代码、修复明显的 bug、加固门禁。不引入新框架。**
> 影响：只处理 R2, R5, R6, R7（4/8 根因），R1/R3/R4/R8 仅部分缓解。

### 核心变更

```
┌──────────────────────────────────────────────┐
│  Fluxor NuGet  ───→  删除                     │
│  ExceptionBoundary ──→  删除 (4 方法 0 调用)   │
│  LauncherIcons ─────→  删除 (5 图标 0 引用)    │
│  StoreReducers ─────→  删除 (Fluxor 配套文件)   │
│  Fluxor.Blazor.Web  ─→  从 Directory.Packages.props 删除 │
├──────────────────────────────────────────────┤
│  LauncherJson Options ──→  拆分为 Loose/Strict │
│  LauncherStoreFile ────→  移除 Disallow 属性   │
│  LauncherStore ────────→  加下界版本检查        │
├──────────────────────────────────────────────┤
│  LauncherJson.Save ───→  加 SemaphoreSlim 锁   │
│  MemoizedSelector ────→  volatile _hasCached   │
│  AgentDiscoveryScanner →  ct 参数不丢弃         │
├──────────────────────────────────────────────┤
│  verify.ps1 ──────────→  加 catch { } 检测      │
│  CI verify.yml ───────→  加 AOT 验证 job       │
│  .editorconfig ───────→  新增                   │
└──────────────────────────────────────────────┘
```

### 消除的根因

| 根因 | 消除程度 | 说明 |
|------|---------|------|
| R1 无统一状态管理 | 缓解 | 删除了 Fluxor，只剩 AppStore，但 AppStore 本身未重构 |
| R2 废弃架构残留 | ✅ 消除 | 删除所有死代码 |
| R3 异常处理分裂 | 未处理 | ExceptionBoundary 删除了，但 IsRecoverable 重复问题仍存 |
| R4 同步-异步反模式 | 部分 | LauncherJson.Save 加锁，但启动/扫描 sync-over-async 未改 |
| R5 序列化刚性 | ✅ 消除 | Loose Read + Strict Write 解决前向兼容 |
| R6 门禁/CI 覆盖不足 | ✅ 消除 | AOT 验证 + catch {} 检测 + editorconfig |
| R7 XAML 令牌不一致 | 未处理 | 需要手动修复 XAML 中的硬编码值 |
| R8 并发安全 | 部分 | LauncherJson 加锁 + MemoizedSelector 修复，但 AppStore 不变 |

### 评估

| 维度 | 评价 |
|------|------|
| **工作量** | ~15 文件改动，2-3 天（含测试） |
| **风险** | 极低 — 主要是删除和配置变更 |
| **新依赖** | 0 |
| **行为变化** | 最小 — 序列化行为有变化（宽松读）但语义等价 |
| **验证** | 已有 410+ 测试覆盖，无需重写 |

### 推荐场景
如果团队时间有限、风险厌恶，选这个方案。可以在一个月内完成，且不会引入回归。

---

## 方案 B："统一状态管理" — 核心问题解决

> **哲学：解决 R1+R4+R8（状态管理分裂导致的并发/异步问题）以及 R3（异常处理分裂）。**
> 影响：处理 R1-R8 中 6/8 根因，R6/R7 在方案 A 中已覆盖。

### 架构总览

```
┌─────────────────────────────────────────────────────────┐
│  旧的 (删除)                   新的 (保留/引入)            │
│  ────────────                ────────────────            │
│  Fluxor (死代码)              CommunityToolkit.Mvvm       │
│  AppStore (旧)               → LauncherStore (增强)     │
│  ExceptionBoundary (死代码)   ErrorOr + 异常过滤器       │
│  LauncherIcons (死代码)       WeakReferenceMessenger     │
│  StoreReducers (死代码)                                   │
└─────────────────────────────────────────────────────────┘
```

### 详细变更

#### 1. 状态管理重构

```
当前: AppStore (SemaphoreSlim + 事件) + Fluxor (死代码)
目标: LauncherStore2 (统一的单状态容器)

LauncherStore2:
  - 持有 ALL 状态: entries, presets, settings, UI modes, drag state, search history
  - 原子 Update<T>(Func<State, T>) 模式 → 读-改-写 在一个锁内
  - INotifyPropertyChanged + INotifyCollectionChanged for direct binding
  - SemaphoreSlim(1,1) 线程安全 (已有模式)
  - CancellationToken 参数
  - 不再有 Select/SelectAsync/Dispatch/DispatchAsync 4 种方法的混乱

ViewModel:
  - 保持 ObservableObject / [ObservableProperty] / [RelayCommand]
  - 注入 LauncherStore2, 不直接操作 LauncherStoreFile
  - WeakReferenceMessenger 用于跨 VM 通知
```

#### 2. 异常处理统一

```
删除:
  - ExceptionBoundary (4 方法 0 调用)
  - LauncherErrors.IsRecoverable (重复实现)

保留并强化:
  - ExceptionFilters.IsRecoverable (唯一的 IsRecoverable, 带 DEBUG 守卫)
  - ErrorOr (已有部分使用，扩展到更多方法)

新模式:
  - 领域预期错误: ErrorOr<T> (验证、未找到、冲突)
  - 异常边界 (IO/安全/运行时): catch when (ExceptionFilters.IsRecoverable(ex))
  - ErrorOrExtensions.Try/TryAsync 作为桥接 (去掉 when 过滤器, 让调用层决定)
```

#### 3. 异步/并发修复

```
启动路径:
  - 当前: fluxorStore.InitializeAsync().GetAwaiter().GetResult()
  - 改为: Task.Run(() => init).GetAwaiter().GetResult() (逃逸 UI SynchronizationContext)
  - 长期: 重构为异步启动 (show loading → init → resolve)

扫描路径:
  - AgentDiscoveryScanner.Scan() 改为 Task<AgentDiscoveryResult> ScanAsync()
  - 将 ct 参数传递给 Parallel.ForEachAsync 的 options

锁:
  - LauncherJson.Save: 加 private static SemaphoreSlim _saveLock
  - AppStore: 保持 SemaphoreSlim, 加 CancellationToken 参数

传播:
  - 所有 Core 层 async 方法: CancellationToken ct = default 作为末位参数
  - ViewModel: 创建 CTS, Core 层只接收
```

#### 4. 序列化 (方案 A 内容)

```
- LooseOptions / StrictOptions 拆分
- 移除 Disallow 属性
- MinimumSupportedSchemaVersion 检查
- 前向兼容测试
```

### 消除的根因

| 根因 | 程度 | 说明 |
|------|------|------|
| R1 无统一状态管理 | ✅ **消除** | LauncherStore2 是唯一状态容器，Fluxor 和旧 AppStore 删除 |
| R2 废弃架构残留 | ✅ 消除 | 所有死代码删除 |
| R3 异常处理分裂 | ✅ **消除** | 单 IsRecoverable + ErrorOr 明确分工 |
| R4 同步-异步反模式 | ✅ **消除** | 启动/扫描改为 async，CancellationToken 系统化 |
| R5 序列化刚性 | ✅ 消除 | Loose/Strict 拆分 |
| R6 门禁/CI 覆盖不足 | 方案 A 覆盖 | — |
| R7 XAML 令牌不一致 | 未处理 | XAML 硬编码需要额外的手动修复 |
| R8 并发安全 | ✅ **消除** | LauncherJson 锁 + MemoizedSelector + CancellationToken 传播 |

### 评估

| 维度 | 评价 |
|------|------|
| **工作量** | ~30-40 文件改动，1-2 周 |
| **风险** | 中 — 状态容器重构影响所有 ViewModel，需仔细测试 |
| **新依赖** | 0 — 完全基于现有依赖 (CT.Mvvm + ErrorOr) |
| **行为变化** | 中等 — 状态管理内部变化，但 App 行为保持一致 |
| **验证** | 需要更新 ~50-80 个测试（模拟层变化），增加 ~20 个新测试 |

### 推荐场景
**推荐作为核心方案**。它解决了产生最多 bug 的根因 (R1+R3+R4+R8)，且不引入新框架依赖，风险可控。

---

## 方案 C："现代化重构" — 全面革新

> **哲学："剪掉一切问题的源头"。不仅解决现有问题，还要防止未来问题。**
> 影响：理论上消除 R1-R8 全部根因。

### 方案 B + 额外变更

#### 额外: UI 架构现代化

```
方案 B 中所有内容, 加上以下可选增强:

1. 拆分 MainWindowViewModel (目前 ~2000 行) 为多个专注 VM:
   - EntryListViewModel (卡片/详情/名称切换)
   - PresetViewModel
   - LayoutViewModel
   - SearchViewModel
   - LauncherEntryViewModel (已存在, 强化)
   保留 MainWindowViewModel 作为协调者 (Facade 模式)

2. 添加 (或选择不添加) MediatR:
   - 观点: 当前 5 层架构已提供分离, MediatR 增加间接性
   - 选择: 方案 C 不强制 MediatR, 保持 RelayCommand 模式

3. Roslyn 自定义分析器:
   - WTL001: 裸 catch 检测
   - WTL002: async void 检测  
   - WTL003: 硬编码颜色/字号检测
   - WTL004: 直接 File I/O 检测 (绕过 LauncherStore)
```

#### 额外: CI/QA 自动化增强

```
方案 B 中所有 CI 变更, 加上:

1. Stryker 突变测试 (since mode, informational first)
2. Verify 快照测试 (LauncherStoreFile 序列化输出)
3. Dependabot + CodeQL
4. 修复 nuget-audit job (实际运行审计)
5. 自定义 Roslyn 分析器 (WTL001-WTL004)
```

#### 额外: XAML 设计令牌修复

```
- LauncherDesignTokens: 添加缺少的 FontSize 变体 (11, 13, 18, 28)
- XAML 全局搜索硬编码字号 → 替换为 {StaticResource}
- 注册 BorderBrush 到 InjectDesignTokens
- 添加 XAML 资源键编译期验证测试
```

### 消除的根因

| 根因 | 程度 | 说明 |
|------|------|------|
| R1-R6 | ✅ 消除 | 同方案 B |
| R7 XAML 令牌不一致 | ✅ **消除** | 添加缺失令牌 + 全局替换 + 自动化测试 |
| R8 并发安全 | ✅ **消除** | 同方案 B |

### 评估

| 维度 | 评价 |
|------|------|
| **工作量** | ~60-80 文件改动，3-5 周 |
| **风险** | 中高 — VM 拆分影响所有 View/XAML 绑定 |
| **新依赖** | 方案 B 无新依赖 + Stryker (dev-only) + Verify (test-only) |
| **行为变化** | 中到大 — 大规模重构可能引入回归 |
| **验证** | 需要重写 ~100-150 个测试，新增 ~50 个测试 |

### 推荐场景
仅在团队有充足时间和测试覆盖率保障时选择此方案。适合作为愿景目标（north star），分阶段实施。

---

## 方案对照

| 维度 | A: 安全清理 | B: 状态统一 | C: 全面革新 |
|------|------------|------------|------------|
| **根因消除** | 4/8 (仅部分缓解) | 6/8 (+ R1/R3/R4/R8) | 8/8 |
| **工作量** | 2-3 天 | 1-2 周 | 3-5 周 |
| **风险** | 极低 | 中 | 中高 |
| **新依赖** | 0 | 0 | 少量 (dev-only) |
| **死代码清理** | ✅ | ✅ | ✅ |
| **状态统一** | ❌ | ✅ | ✅ |
| **异步修复** | 部分 | ✅ | ✅ |
| **异常统一** | ❌ | ✅ | ✅ |
| **序列化修复** | ✅ | ✅ | ✅ |
| **门禁加固** | ✅ | ✅ | ✅ |
| **VM 拆分** | ❌ | ❌ | ✅ |
| **XAML 修复** | ❌ | ❌ | ✅ |
| **突变测试** | ❌ | ❌ | ✅ |
| **自定义分析器** | ❌ | ❌ | ✅ |
| **未来可维护性** | 低 (症状修复) | 中高 (根因解决) | 高 (预防性) |

---

## 推荐方案: B + 分阶段实施

### 推荐理由

1. **收益/成本比最高**: 方案 B 消除 6/8 根因，工作量为 1-2 周，不引入新依赖
2. **现有资产最大化**: 保持 CommunityToolkit.Mvvm + ErrorOr (已有投资)，不引入新框架
3. **可演进**: 方案 B 完成后可自然演进到方案 C（如果需要 VM 拆分/XAML 修复）
4. **可验证**: 每个阶段有明确的 PASS/FAIL 标准

### 分阶段路线图

#### Phase 1: 清理 + 序列化 (3-4 天)

```
Day 1-2: 死代码清理
  - 删除 Fluxor NuGet 包、注册代码
  - 删除 ExceptionBoundary 类
  - 删除 LauncherIcons, StoreReducers
  - 删除 Fluxor.Blazor.Web 包版本
  - 验证: gate 通过, 410+ 测试通过

Day 3-4: 序列化修复
  - 拆分 LooseOptions / StrictOptions
  - 移除 [JsonUnmappedMemberHandling(Disallow)] 属性
  - 添加 MinimumSupportedSchemaVersion 检查
  - 添加前向兼容测试
  - 验证: gate 通过, 新测试覆盖
```

#### Phase 2: 状态 + 异常 + 并发 (5-7 天)

```
Day 5-6: 状态管理重构
  - 设计新的 LauncherStore2 (或重构 AppStore)
  - 单一 Update<T> 方法替代 Select/Dispatch 混乱
  - 注入 WeakReferenceMessenger 替代事件
  - 验证: ViewModel 绑定正常, 410+ 测试通过

Day 7-8: 异常处理统一
  - 删除 LauncherErrors.IsRecoverable
  - 巩固 ExceptionFilters.IsRecoverable 为唯一源
  - 扩展 ErrorOr 使用范围 (关键路径)
  - 验证: 所有 catch 使用统一 IsRecoverable

Day 9-11: 异步/并发修复
  - LauncherJson.Save 加 SemaphoreSlim
  - 启动路径 sync-over-async 修复
  - 扫描路径改为 async + CancellationToken
  - MemoizedSelector volatile 修复
  - AgentDiscoveryScanner ct 参数传递
  - 验证: gate 通过, 异步测试覆盖
```

#### Phase 3: 门禁 + 自动化 (2-3 天)

```
Day 12-13: CI / QA
  - verify.ps1 加 bare catch { } 检测
  - CI 加 AOT 验证 job (informational)
  - 添加 .editorconfig + dotnet format
  - Dependabot + CodeQL
  - 修复 nuget-audit job

Day 14: 清理 + 文档
  - 更新 spec (核心指南, 架构图)
  - 更新 README 架构描述
  - 最终 gate 验证
```

### 总工作量: 10-14 天

---

## 不推荐的方案及原因

| 方案 | 原因 |
|------|------|
| **MediatR** | 5 层架构已提供分离，MediatR 增加间接性但不解决状态分裂问题。文件膨胀 (~60 新文件) |
| **ReactiveUI** | 与已有的 CommunityToolkit.Mvvm 冲突 (双框架)。订阅泄漏风险。 |
| **Pure Functional / Immutable (全栈)** | 与 Avalonia 的 INotifyPropertyChanged 绑定模型不兼容。集合更新繁琐。 |
| **Protobuf / FlatBuffers** | 破坏 JSON 可读性。不可手写编辑。构建步骤增加 CI 负担。 |
| **OneOf / LanguageExt** | ErrorOr 已存在，增加新重复库无收益。 |
| **Nito.AsyncEx** | SemaphoreSlim 已内建，不需要外部依赖。AOT 兼容性不明确。 |
| **全量重写** | 不切实际。项目 300+ 文件, 410+ 测试, 重写风险无限大。 |
