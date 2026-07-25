# Phase 3 tail: Migration & cleanup

## Goal

清理 VTuber-Killer 架构迁移后残留的旧路径引用、过期测试桩、未使用依赖,使代码库无Dead Code、无悬空引用。

## Requirements

1. **扫除 `AppStore` 残留引用** — grep 全仓(src/ + tests/),确认无未迁移调用方。
2. **`DispatchMustBeOnlyWritePathAnalyzer` 同步更新** — analyzer 现在识别 `StoreGrain.Dispatch*` 为唯一合法写入路径,而不是旧的 `AppStore.Dispatch*`。
3. **`App.axaml.cs` DI 容器配置更新** — 注册 `StoreGrain`、`ErrorSupervisor`,移除 `AppStore` 注册。
4. **测试桩修复** — `MainWindowEntryCardTestFactory` 等 test fixtures 同步到新 API。
5. **依赖包清理** — `OneOf 3.0.271` + `Immutype 1.0.16` 进 `Directory.Packages.props`。

## Acceptance Criteria

- [x] `grep -r AppStore src/` 零命中(不含注释和 StoreGrain 内部)。
- [x] `WT1003` analyzer 在所有写入路径上对 `StoreGrain.Dispatch*` 放行、对其它路径报错。
- [x] `dotnet build -warnaserror` 通过。
- [x] 全部测试通过,无 skip/ignore。

## Implementation Evidence

- 与 Phase 2/3 同提交 `d9fa243` 落地,无独立提交。
- 修改: `App.axaml.cs` (DI)、`MainWindowViewModel.cs` (+20/-20)、`MainWindowViewModel.Layout.cs` (+40/-40)、`EntryListViewModel.cs` (+40/-40)、`DispatchMustBeOnlyWritePathAnalyzer.cs` (+12/-12)。
- 测试: `AppStoreTests.cs` (+28/-28)、`EntryServiceTests.cs`、`MainWindowEntryCardTestFactory.cs`、`MainWindowViewModelTests.Shared.cs` 同步。
- Journal #54 记录。

## Notes

- 本次迁移与 Phase 2/3 在同一 commit 完成,未拆独立提交;PRD 文件在归档时回填(2026-07-25)。
