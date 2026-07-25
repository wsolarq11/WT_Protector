# Phase 3: CQRS + Grain

## Goal

用 actor 模型(Grain)替代 monolithic `AppStore`,把命令派发与状态查询分离,使所有写入路径单线程化、可推演。

## Requirements

1. **`Grain<T>`** — 轻量级 actor 基类,单线程派发命令,持有私有 `_state`。
2. **`StoreGrain`** — 替代 `AppStore` 的事件驱动 actor:命令 → reducer → 事件 → EventJournal → 状态投影。
3. **`UiStateProjection`** — 把 StoreGrain 状态变更自动 dispatch 到 UI 线程,VM 无需手动同步。
4. **`ErrorSupervisor`** — 断路器模式隔离故障域,防止单个错误传导到全局。
5. **保留向后兼容 API** — 调用方迁移期内可继续使用 `AppStore` 风格调用,`StoreGrain` 提供等价 Dispatch* 方法。

## Acceptance Criteria

- [x] `StoreGrain.Dispatch` 写盘后必须 `SetState`,使后续 `Select` 读到最新状态(pitfalls §2.5.5)。
- [x] `AppStore.cs` / `StoreReducers.cs` / `StoreSelectors.cs` 已删除,生产代码无 `AppStore` 引用。
- [x] `MainWindowViewModel` 与 `EntryListViewModel` 通过 `StoreGrain` 派发所有写入。
- [x] 全部测试通过(迁移后修复 35 个失败测试,根因见 pitfalls §2.5.5)。
- [x] `dotnet build -warnaserror` 通过。

## Implementation Evidence

- Landing commit: `d9fa243`(与 Phase 2 同提交)。
- 关键文件: `WTLauncher.App/Infrastructure/Grain.cs` (84 lines)、`StoreGrain.cs` (152 lines)、`UiStateProjection.cs` (52 lines)、`ErrorSupervisor.cs` (144 lines)。
- 删除文件: `AppStore.cs` (165 lines)、`StoreReducers.cs` (89 lines)、`StoreSelectors.cs` (47 lines)。
- Journal #54 记录。

## Notes

- 测试文件 `AppStoreTests.cs` 已迁移为对 `StoreGrain` 的测试,类名保留以减少 diff。
- CQRS 查询侧未独立拆分(无独立 Query 层),`Select` 直接读 Grain 状态;未来若查询复杂度上升可作为独立 phase。
