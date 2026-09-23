# 税项清理：实施计划

## 执行顺序

按风险降序排列：先修 bug（安全网），再删死代码，最后简化架构。
每个子任务独立提交，任一子任务失败只回退该子任务。

## 子任务 1: fix-static-facade

### 改动
1. `ConfigExchangeService` 构造函数改为接收 `IStorePersistence` 和 `ILauncherJsonIO`
2. `MainWindowViewModel` 构造函数创建 `ConfigExchangeService` 时传入 `FileStorePersistence` 和 `FileLauncherJsonIO`
3. `App.axaml.cs` 创建 `FileStorePersistence` 实例并传递
4. `LauncherStore` 静态门面中保留 `ResolveStorePath` 作为类方法，删掉 `DefaultPersistence`
5. `LauncherJson` 静态门面删掉 `DefaultIO`
6. `TestStartup.cs` 删掉静态字段初始化，改为直接构造

### 验证
```
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore --filter "FullyQualifiedName~ConfigExchange"
```

## 子任务 2: fix-runtray-default

### 改动
`LauncherSettingsDefaults.cs:84` 改为：
```
RunInTray = !presentKeys.Contains(RunInTrayJsonKey) || settings.RunInTray
```
替换为：
```
RunInTray = presentKeys.Contains(RunInTrayJsonKey) ? settings.RunInTray : RunInTray,
```
或者更简单的：`NormalizeRunInTray(settings.RunInTray, presentKeys)` 用 `value ?? DEFAULT` 模式。

### 验证
```
dotnet test --filter "FullyQualifiedName~LauncherSettingsDefaults"
```

## 子任务 3: remove-dead-code

### 改动
1. 删 `src/WTLauncher.Core/Errors/AppError.cs`
2. 删 `Errors/` 目录（如果空）
3. 两个 `.csproj` 文件中的 `OneOf` 包引用 — 先确认 `dotnet build` 不会报缺失引用，再删

### 验证
```
dotnet build -warnaserror
dotnet test -c Release --no-restore
```

## 子任务 4: clean-schema-fields

### 改动
1. `LauncherSettings.cs` — 删 `SearchHistory` / `LastFilterText`
2. `LauncherStoreFile.cs` — 删 `RunHistory`
3. `LauncherSettingsDefaults.cs` — 删对应 `Normalize*` 方法
4. `LauncherRunHistory.cs` — 删文件
5. `LauncherSearchHistory.cs` — 删文件
6. `MainWindowViewModel.cs` — 删 `_runHistory` 和 `RunHistoryCount` 属性
7. 各测试文件 — 删对应引用

### 验证
```
dotnet build -warnaserror
dotnet test -c Release --no-restore
```

## 子任务 5: collapse-grain

### 改动
1. 删 `Grain.cs` 文件
2. `StoreGrain.cs` — 将 `Grain<T>` 的 `_lock`、`ReadState`、`Mutate`、`SetState` 内联

### 验证
```
dotnet build -warnaserror
dotnet test -c Release --no-restore --filter "FullyQualifiedName~StoreGrain"
```

## 子任务 6: clean-docs-and-gates

### 改动
1. `scripts/checks/static.ts` — 删 `checkClaimBoundaryLedger`、`checkFeatureObjectiveBoundary`
2. `scripts/verify.ts` — 删对应 import 和调用
3. `docs/feature-first-class-upgrade-audit.md` — 移到 `docs/archive/`
4. `docs/architectures/trinity-endgame-architecture.md` — 顶部加过期标记
5. `docs/architecture-reform-proposal.md` — 顶部加过期标记
6. `LauncherSettings.cs` 注释 — 修正 `UnmappedMemberHandling=Disallow` 为 `Loose Read`

### 验证
```
$env:CI='true'; npm run verify
```

## 最终验证

```
$env:CI='true'; npm run verify
git diff --check
dotnet build -warnaserror
dotnet test -c Release --no-restore
```

## 回滚策略

每个子任务独立提交。如果一个子任务失败，`git revert` 该提交即可。
不叠加修复 — 失败时先回退，查明原因后再重试。