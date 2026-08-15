# 架构模式全网调研报告

> 2026-07-21
> 目标: 深度全网 deepsearch，找到能从根上剪掉问题源头而非打补丁的架构方案
>
> **验证记录 (2026-08-15)** — `.trellis/tasks/08-15-docs` 协议逐项核实：
> - ✅ 调研日期与 git 一致：`bfc7ed90 2026-07-21 refactor: Unified Store Architecture — AppStore + EntryManager 消除 + 消息简化`；本报告的推荐方案（Unified Store / AppStore）在**同一提交已实现**（AppStore.cs +102、EntryManager.cs -70、MainWindowViewModel.Messaging.cs -46、快照类删除）。
> - ⚠️ 后续演进：AppStore 于 2026-07-24 VTK（`e11183cc`）迁移中删除（门禁 `vtk-no-appstore-residue`，tools/verify.ps1:849 强制零残留），最终由 Trinity（2026-07-25，`beb47560`）取代；当前无 AppStore。
> - ⚠️ R1/R2 描述的共享集合、EntryManager（10+ Func<> 委托）为 2026-07-21 时点诊断：EntryManager 已于当次提交删除（当前仅 IEntryCardHost.cs:14 注释提及）；R3 的 WeakReferenceMessenger 至今仍在使用（ViewModels/Messages.cs）。
> - ⚠️ 快照类（LauncherEntryMutationSnapshot / LauncherSelectionMutationSnapshot）已于 2026-07-21 移除；当前回滚 = `StoreGrain.Dispatch` save-first-then-commit。
> - ⚠️ “保留不变：Core 层 0 改动、Views 层 0 改动”仅指 bfc7ed90 当次提交范围；此后 Core/Views 均有演进。
> - ⚠️ “670 测试继续通过”为当时本地运行口径（git 时点测量 bfc7ed90：[Fact]/[Theory] 属性 607、测试文件 94）。
> - “删除文件清单”中的 LauncherDragReorderWorkflow.cs 实际未删除（至今存在，生产使用 Stateless）。
> - 其余外部来源（bool.dev / Redux / Akka / Signals 等）为带日期调研记录，仓库无法以代码证据验证，保留原文。


## 当前架构根因诊断 (R1-R3)

从代码追踪（AGENTS.md、DESIGN.md、README.md、核心 source 文件）得出的 3 个根因:

| # | 根因 | 表现 | 影响范围 |
|---|------|------|----------|
| R1 | **共享可变 ObservableCollection** — `EntryListViewModel._entries` 和 `MainWindowViewModel` 引用同一集合 | 任何一方的修改都对另一方"可见但不可控" | 数据竞态的根本源头，当前未触发 bug 全靠代码纪律 |
| R2 | **EntryManager 委托聚合模式** — 10+ 个 `Func<>` 注入（`SettingsGetter`, `StatusApplier`, `SnapshotCapturer`, `ChangesApplier` 等） | 编译期无法追踪谁依赖什么；Feature VM 需要把所有 Func 传一遍 | 隐式耦合，不安全重构，扩展成本线性增长 |
| R3 | **WeakReferenceMessenger** — 全局消息总线，无编译期订阅校验 | `Send(EntryUpdatedMessage)` 时不知道谁在收 | 跨层契约靠 runtime 测试覆盖 |

## 全网搜索覆盖

以下搜索于 2026-07-21 执行:

1. **Avalonia .NET 桌面应用架构最佳实践** — bool.dev, AvaloniaUI Blog, Reddit r/csharp, Medium 上的 Avalonia+Clean Architecture 指南, GitHub Discussion #16200
2. **C# 桌面应用 CQRS + Event Sourcing** — Azure Architecture Center, DEV Community, Microsoft Learn, DevWithJosh, Dev Genius, C# Corner
3. **CommunityToolkit.Mvvm Messenger vs MediatR vs ReactiveUI** — Microsoft Learn, FormatException 博客, Reddit r/csharp 对比帖, Avalonia Discussion #12540, ReactiveUI 官方比较页, 软件工程 StackExchange
4. **MVVM 替代方案 & 单向数据流 / MVI** — SoftAims, Pixact Technologies, Medium 2026 架构综述, Platform Uno "Is MVVM Dead", 2026 iOS MVVM-C Playbook, YouTube UDF 讲解
5. **Redux 模式在 .NET 桌面的应用** — Fluxor 文档/社区、Redux.NET、Atomic Spin "Explorations in Adapting Redux to C#"
6. **Actor Model (Akka.NET) 桌面端适用性** — Akka.NET 官方文档、Reddit r/csharp 讨论、Software Engineering StackExchange、Medium Akka.NET 生命周期指南
7. **Signals 响应式状态管理在 .NET** — SignalsDotnet (Angular Signals → .NET 移植)、Reddit r/csharp "Why Signals in C# are not a thing"、.NET 官方 Issue #67329 (Blazor Signals)
8. **Redux/Fluxor .NET 桌面状态管理** — DEV Community Fluxor 教程、ABP.io 集成指南、Telerik Blazor 复杂状态、Reddit r/Blazor 经验帖

## 架构方案比较

### 方案 1: Clean Architecture（当前 5 层已接近）

| 维度 | 评估 |
|------|------|
| **适用性** | 高 — Core 层已是零 Avalonia/UI 依赖 |
| **根因消除** | 未直接解决 R1(共享集合) R2(委托汤) R3(弱消息) |
| **增量成本** | 已有 → 缺的是 UseCase 层显式定义 |
| **Go/No-Go** | **No-Go** — 已经够了，额外的层不消除任何根因 |

### 方案 2: Redux/Fluxor（单状态树 + 纯函数 Reducer）

| 维度 | 评估 |
|------|------|
| **适用性** | 中 — `launcher.json` 天然是持久化 Store |
| **根因消除** | 消除 R1(共享集合) R2(委托汤)；R3(消息) 用 Action 取代 |
| **增量成本** | 每个 mutation 需要 Action 类 + Reducer 标记 + Effect 类 → 当前约 20 种 mutation = 60+ 文件样板 |
| **Go/No-Go** | **No-Go** — 本末倒置，小应用背大框架 |

### 方案 3: MVI / 单向数据流

| 维度 | 评估 |
|------|------|
| **适用性** | 低 — WPF/Avalonia 原生没有 Jetpack Compose 的 StateFlow 基础 |
| **根因消除** | 消除 R1(单向流)；R2 和 R3 依实现方式 |
| **增量成本** | 需自己实现状态机框架，约 50% 代码量去管"数据怎么流"而非"做什么" |
| **Go/No-Go** | **No-Go** — 强行实施引入的复杂度 > 解决的问题 |

### 方案 4: CQRS + Event Sourcing

| 维度 | 评估 |
|------|------|
| **适用性** | 低 — 一个 `launcher.json` 已存全部状态，write model / read model 分离无意义 |
| **根因消除** | Event Sourcing 的 append-only log = 把 `launcher.json` 变成 `launcher.events` |
| **增量成本** | 引入 EventStore / 自定义事件存储；查询需建投影 |
| **Go/No-Go** | **No-Go** — 复杂度 >> 收益，当前项目无审计需求 |

### 方案 5: Actor Model (Akka.NET)

| 维度 | 评估 |
|------|------|
| **适用性** | **不适用** — 单进程单线程桌面应用 |
| **根因消除** | Actor 邮箱/监督/生命周期全用不上 |
| **增量成本** | 引入 Akka.NET 依赖 + HOCON 配置 + actor 系统 |
| **Go/No-Go** | **No-Go** — 灾难级过度设计 |

### 方案 6: Signals (SignalsDotnet)

| 维度 | 评估 |
|------|------|
| **适用性** | 低 — 新技术，生态未成熟 |
| **根因消除** | 细粒度响应式更新可消除 R1；R2/R3 仍需要额外框架 |
| **增量成本** | 引入未验证第三方依赖；与 Avalonia 绑定尚未验证 |
| **Go/No-Go** | **No-Go** — 风险 > 收益 |

### 方案 7: StrongReferenceMessenger

| 维度 | 评估 |
|------|------|
| **适用性** | 高 — 同一 API，只改注册方式 |
| **根因消除** | 消除 R3 的"静默丢失"问题（编译器可校验收/发） |
| **增量成本** | 几乎为零（需在 shutdown 时显式注销） |
| **Go/No-Go** | **Go** — 对 R3 的零成本修复 |

### 方案 8: Unified Store + 只读投影（推荐）

| 维度 | 评估 |
|------|------|
| **适用性** | **最高** — 将 `LauncherStoreFile` 同时作为内存单状态源 |
| **根因消除** | 消除 R1(Selector 返回新投影) R2(Feature VM 只依赖 AppStore.Select()) R3(状态变更统一推送) |
| **增量成本** | 新增 3 文件，重写 2 文件，删除 4 文件；~200 行消息/快照基础设施 → ~150 行 Store 核心 + 100 行测试 |
| **Go/No-Go** | **Go** — 推荐方案 |

## 推荐方案：Unified Store Architecture

### 架构图

```
                         ┌─────────────┐
                         │  Launcher   │
                         │  StoreFile  │
                         │ (JSON 持久)  │
                         └──────┬──────┘
                                │ load / save
                         ┌──────▼──────┐
                         │  AppStore   │  ← 新的单状态源
                         │ (immutable) │
                         └──┬───┬───┬──┘
                    ┌───────┘   │   └───────┐
                    ▼           ▼           ▼
              ┌─────────┐ ┌─────────┐ ┌─────────┐
              │Entry    │ │Preset   │ │Layout   │
              │Selector │ │Selector │ │Selector │  ← 只读投影
              └─────────┘ └─────────┘ └─────────┘
                    │           │           │
                    ▼           ▼           ▼
              ┌─────────────────────────────────┐
              │      MainWindowViewModel         │
              │  (orchestration only, no state)  │
              └─────────────────────────────────┘
```

### 核心代码 (AppStore.cs)

```csharp
public sealed class AppStore
{
    private LauncherStoreFile _state;

    public T Select<T>(Func<LauncherStoreFile, T> selector) => selector(_state);

    public void Dispatch(Action<LauncherStoreFile> mutation)
    {
        var before = _state;
        try { mutation(ref _state); Persist(_state); }
        catch { _state = before; }
        NotifySubscribers();
    }
}
```

### 消除的根因

| 当前问题 | 消除机制 |
|----------|----------|
| R1: 共享 ObservableCollection | `Selector` 每次返回新投影，VM 只读使用 |
| R2: EntryManager Func 委托汤 | 所有 Feature VM 只依赖 `AppStore.Select()` |
| R3: WeakReferenceMessenger | 状态变更由 `NotifySubscribers` 统一推送 |

### 文件变更

```
新增文件:  3 (AppStore.cs + IStoreSubscriber.cs + StoreSelector.cs)
重写文件:  2 (MainWindowViewModel.cs, EntryManager.cs → elimination)
删除文件:  4 (LauncherEntryMutationSnapshot.cs, MainWindowViewModel.Messaging.cs,
              LauncherSelectionMutationSnapshot.cs, LauncherDragReorderWorkflow.cs)
测试文件新增: 5 (每个 reducer area 一个纯函数测试)
净变化:     +5 files, -200 行消息/快照代码, +150 行 Store 核心, +100 行测试
保留不变:  Core 层 (0 改动), IEntryService/IEntryCardHost 接口,
           Views 层 (0 改动), 670 测试继续通过
```

> ponytail: 全局 store 锁。若未来有并行后台操作改为 per-section 锁。当前单线程 Avalonia dispatcher 无竞争。

## 不做的方案

- **Full Redux/Fluxor** — 20 种 mutation 需要 60+ Action/Reducer/Effect 文件 → 增加而非减少复杂度
- **Clean Architecture 追加 UseCase 层** — 当前 5 层已够用
- **Actor model** — 不需要 Akka.NET 来管一个 JSON 文件读写
- **Event Sourcing** — 把 `launcher.json` 拆成 `launcher.events` 不解决任何现有问题
- **MVI 单向流** — Avalonia 没有 Jetpack Compose 级别的 StateFlow 基础设施
- **Signals** — 未验证的第三方依赖，生态未成熟

## 建议迁移路线

```
Phase 1: AppStore 核心 + 状态迁移（2-3 小时）
  └── 新建 AppStore + 现有 670 测试继续全绿
Phase 2: 逐 Feature 切换（4-6 小时）
  └── EntryListViewModel → AppStore.Select, LaunchOrchestrator → AppStore.Dispatch
      每切换一个，删除对应旧代码
Phase 3: 删除旧基础设施（1 小时）
  └── 删除 EntryManager, WeakReferenceMessenger 订阅, snapshot 文件
Phase 4: 补测试（2 小时）
  └── Reducer 纯函数测试覆盖 Clone/CommandTemplate/DragReorder
```

每 Phase 结束时执行 `pwsh ./tools/verify.ps1` 确保门禁不破。

## 结论

将"状态散落在 13 个 partial + 一个 EntryManager + 一个消息总线"收拢为**一个 AppStore + 只读 Selector**。不是换一个新架子，而是把 5 层架构的"第 2 层（状态管理）"从委托传递模式改成统一 Store 模式。
