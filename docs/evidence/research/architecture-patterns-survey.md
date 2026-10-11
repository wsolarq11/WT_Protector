Status: Evidence
Purpose: 2026-07-21 架构模式调研记录，非行动方案。
Update when: 不更新（研究快照；结论变化时归档或另立文档）。

# 架构模式调研（2026-07-21）

目标：找到能从根源剪掉问题源头、而非打补丁的架构方案。

> 时点说明：本报告为 2026-07-21 快照。文中"推荐 Unified Store / AppStore"在当次提交已实现，但 AppStore
> 随后被 `StoreGrain`（单一容器，Dispatch/DispatchErrorOr，save-first-then-commit）取代；当前无 AppStore、
> 无 EntryManager、无快照类。方案结论是历史记录，不代表当前代码。

## 时点根因诊断

| # | 根因 | 表现 |
| --- | --- | --- |
| R1 | 共享可变 `ObservableCollection`（`EntryListViewModel._entries` 与 `MainWindowViewModel` 引用同一集合） | 任一方修改对另一方"可见但不可控" |
| R2 | EntryManager 委托聚合（10+ `Func<>` 注入） | 编译期无法追踪依赖，Feature VM 需把全部 Func 传一遍 |
| R3 | `WeakReferenceMessenger` 全局消息总线，无编译期订阅校验 | `Send(EntryUpdatedMessage)` 时不知道谁在收 |

## 搜索覆盖（2026-07-21 执行）

1. Avalonia .NET 桌面架构最佳实践
2. C# 桌面 CQRS + Event Sourcing
3. CommunityToolkit.Mvvm Messenger vs MediatR vs ReactiveUI
4. MVVM 替代与单向数据流 / MVI
5. Redux 在 .NET 桌面（Fluxor 等）
6. Actor Model（Akka.NET）桌面适用性
7. Signals 响应式状态（SignalsDotnet 等）
8. Redux/Fluxor .NET 桌面状态管理

## 方案比较结论

| 方案 | Go/No-Go | 理由 |
| --- | --- | --- |
| Clean Architecture（追加 UseCase 层） | No-Go | 现有 5 层已够，额外层不消除任何根因 |
| Redux/Fluxor | No-Go | 约 20 种 mutation 需 60+ Action/Reducer/Effect 样板，小应用背大框架 |
| MVI / 单向数据流 | No-Go | Avalonia 无 Jetpack Compose 级 StateFlow 基础，自建框架成本大于收益 |
| CQRS + Event Sourcing | No-Go | 单 `launcher.json` 已存全部状态，读写分离无意义 |
| Actor Model（Akka.NET） | No-Go | 单进程单线程桌面应用用不上 Actor |
| Signals（SignalsDotnet） | No-Go | 未验证依赖，生态未成熟 |
| StrongReferenceMessenger | Go | 对 R3 的零成本修复（同 API 改注册方式） |
| Unified Store + 只读投影 | Go（当时推荐） | Selector 每次返回新投影，Feature VM 只依赖 Store；消除 R1/R2/R3 |

## 时点推荐：Unified Store

把散落在多个 partial + EntryManager + 消息总线的状态收拢为一个 store + 只读 selector。不是换架子，
是把状态管理层从委托传递改成统一 store 模式。

> 当前事实：该方向后续落为 `StoreGrain`，不再叫 AppStore。调研里"删除 LauncherDragReorderWorkflow.cs"
> 未发生（该文件至今存在，生产使用 Stateless）。
