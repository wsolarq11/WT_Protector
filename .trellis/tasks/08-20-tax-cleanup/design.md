# 税项清理：设计

## 整体策略

将六个维度的改动拆成 6 个独立子任务，每个子任务可独立验证、独立提交/回退。
执行顺序按风险排列：先修 bug（安全网），再删死代码，最后简化架构。

```
 Parent: 08-20-tax-cleanup
   ├── child-1: fix-static-facade      (ConfigExchangeService 接线)
   ├── child-2: fix-runtray-default     (runInTray null bug)
   ├── child-3: remove-dead-code        (AppError + OneOf)
   ├── child-4: clean-schema-fields     (searchHistory/runHistory/lastFilterText)
   ├── child-5: collapse-grain          (Grain<T> → StoreGrain)
   └── child-6: clean-docs-and-gates    (过时文档 + 门禁)
```

## 受影响文件清单

### child-1: fix-static-facade
- `src/WTLauncher.Core/LauncherStore.cs` — 删除静态门面，将 `Load/Save/ResolveStorePath` 移入 `IStorePersistence` 或直接删
- `src/WTLauncher.Core/LauncherJson.cs` — 同上
- `src/WTLauncher.App/Features/ConfigExchangeService.cs` — 改为直接注入 `IStorePersistence` + `ILauncherJsonIO`
- `src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` — 创建 `ConfigExchangeService` 时传入注入的 persistence
- `src/WTLauncher.App/App.axaml.cs` — 创建 `FileStorePersistence` 并传给构造函数链
- `src/WTLauncher.Core/LauncherJsonContext.cs` — 可能不再需要
- `src/WTLauncher.Tests/Support/TestStartup.cs` — 删掉静态字段初始化
- `src/WTLauncher.Core/ILauncherJsonIO.cs` — 保留，`IStorePersistence` 保留

### child-2: fix-runtray-default
- `src/WTLauncher.Core/LauncherSettingsDefaults.cs` — 改 `RunInTray` 行

### child-3: remove-dead-code
- `src/WTLauncher.Core/Errors/AppError.cs` — 删除文件
- `src/WTLauncher.App.csproj` — 检查 `OneOf` 引用，如无其他用途则移除
- `src/WTLauncher.Core.csproj` — 检查 `OneOf` 引用

### child-4: clean-schema-fields
- `src/WTLauncher.Core/LauncherSettings.cs` — 删 `SearchHistory`、`LastFilterText` 属性
- `src/WTLauncher.Core/LauncherStoreFile.cs` — 删 `RunHistory`
- `src/WTLauncher.Core/LauncherSettingsDefaults.cs` — 删对应默认值处理
- `src/WTLauncher.Core/LauncherRunHistory.cs` — 删除文件
- `src/WTLauncher.Core/LauncherSearchHistory.cs` — 删除文件
- `src/WTLauncher.App/ViewModels/MainWindowViewModel.cs` — 删 `RunHistory` 引用
- 各测试文件 — 删对应引用

### child-5: collapse-grain
- `src/WTLauncher.App/Infrastructure/Grain.cs` — 删除文件，逻辑移入 `StoreGrain`
- `src/WTLauncher.App/Infrastructure/StoreGrain.cs` — 内联 `Grain<T>` 的 `_lock` / `ReadState` / `Mutate` / `SetState`

### child-6: clean-docs-and-gates
- `scripts/checks/static.ts` — 删 `checkClaimBoundaryLedger`、`checkFeatureObjectiveBoundary`
- `scripts/verify.ts` — 删对应引用
- `scripts/checks/schema.ts` — 可能调整结构基线
- `docs/feature-first-class-upgrade-audit.md` — 移到 `docs/archive/`
- `docs/architectures/trinity-endgame-architecture.md` — 加过期标记
- `docs/architecture-reform-proposal.md` — 加过期标记
- `src/WTLauncher.Core/LauncherSettings.cs` — 修注释

## 向后兼容

- `config/launcher.json` schemaVersion=2 不变
- 删除 `searchHistory`/`runHistory`/`lastFilterText` 字段后，旧配置仍可读
  （当前 `Loose Read` 策略：未知 key 自动跳过，所以旧配置中的这些字段
   会被静默忽略，不影响新版本）
- 删除 `LauncherStore` 静态门面后，`ConfigExchangeService` 的构造函数签名改变，
  但 `MainWindowViewModel` 的构造函数参数不变（`IConfigExchangeService` 接口不变）