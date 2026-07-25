# Implement: 斩根架构迁移 — 5 步跃迁

## 执行总览

```
Step 1: AppState 统一 + 锁内原子化   │ 预计: 3 天
Step 2: DI 化 + union 错误处理       │ 预计: 2 天
Step 3: ViewModelBase + 生命周期     │ 预计: 2 天
Step 4: 强类型值对象 + 验证内联      │ 预计: 2 天
Step 5: Analyzer + ArchUnitNET 守卫  │ 预计: 2 天
─────────────────────────────────────┼──────────
总计                                 │ 11 天
```

## Step 1: AppState 统一 + Grain 锁内原子化

### 前置条件
- 现有测试套件全部通过
- Backup 当前分支

### 文件清单

| 操作 | 文件 | 改动 |
|------|------|------|
| 创建 | `Core/AppState.cs` | 定义 `sealed record AppState` |
| 修改 | `Infrastructure/Grain.cs` | 新增 `Mutate(Func<TState,TState>)` 方法 |
| 修改 | `Infrastructure/StoreGrain.cs` | `Dispatch` 使用 `_grain.Mutate` 在锁内完成 |
| 修改 | `ViewModels/MainWindowViewModel.cs` | 删除 `_settings` 字段，改为 `Select` |
| 修改 | `ViewModels/MainWindowViewModel.Drag.cs` | 删除 5 处 `_settings = _dragReorderWorkflow.Settings` |
| 修改 | `ViewModels/MainWindowViewModel.Layout.cs` | `_settings` 引用改为 `Select` |
| 修改 | `ViewModels/MainWindowViewModel.Presets.cs` | `_settings` 引用改为 `Select` |
| 修改 | `ViewModels/MainWindowViewModel.CommandManagement.cs` | `_settings` 引用改为 `Select` |
| 修改 | `ViewModels/MainWindowViewModel.Discovery.cs` | `_settings` 引用改为 `Select` |
| 修改 | `ViewModels/LauncherDragReorderWorkflow.cs` | 不再持有 `_settings`，改通过 `Func<AppState>` 读取 |
| 修改 | 测试文件 | StoreGrain 测试改为使用 AppState |

### 验证命令

```powershell
# 每次文件修改后运行
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
# 架构规则检查
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~ArchitectureRules"
# 全量测试
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release
```

### 验收条件
- [ ] `_settings` 字段在所有文件中被删除
- [ ] `StoreGrain.Dispatch` 中无 `;` 在 `ReadState` 和 `SetState` 之间
- [ ] 所有 `_settings =` 赋值被替换为 `storeGrain.Select` 或 `storeGrain.Dispatch`
- [ ] 现有测试全部通过

### 回滚方案
- 如果 `dotnet test` 失败超过 5 个测试且无法 10 分钟内修复: `git checkout -- .`

---

## Step 2: DI 化 + union 错误处理

### 文件清单

| 操作 | 文件 | 改动 |
|------|------|------|
| 修改 | `Program.cs` | `ServiceCollection` + DI 容器 |
| 修改 | `Core/LauncherJson.cs` | 删除 `static DefaultIO` |
| 修改 | `Core/LauncherStore.cs` | 删除 `static DefaultPersistence` |
| 修改 | `App/Infrastructure/FileStorePersistence.cs` | 所有 void 返回改为 ErrorOr<Success> |
| 创建 | `Analyzers/WT2004.cs` | `[MustHandleErrors]` analyzer |
| 修改 | `StoreGrain.cs` | `DispatchErrorOr` 改用 `ExceptionFilters.IsRecoverable` |
| 修改 | `FileLauncherJsonIO.cs` | 空 catch 块补日志 + 恢复失败补 ErrorLog |
| 修改 | 多个调用点 | analyzer 修复: 未处理的 ErrorOr<T> 返回值 |

### 关键修复清单

```
FileLauncherJsonIO.cs:71-73   空 catch → ErrorLog.Write
FileLauncherJsonIO.cs:94-97  空 catch → ErrorLog.Write  
FileLauncherJsonIO.cs:211-212 空 catch → 至少加注释
StoreGrain.cs:49             StateChanged?.Invoke → try-catch 包裹
StoreGrain.cs:78-81          静默返回空 → ErrorLog.Write + 用户可见警告
```

### 验证命令

```powershell
dotnet build -warnaserror
# 验证 WT2004 能检测未处理的 ErrorOr
# 验证所有 ErrorOr 返回值被处理
dotnet test --filter "FullyQualifiedName~ArchitectureRules"
dotnet test
```

### 验收条件
- [ ] `LauncherJson.DefaultIO` / `LauncherStore.DefaultPersistence` 不存在
- [ ] `Program.cs` 使用 DI 容器
- [ ] 空 catch 块全部补日志
- [ ] `StoreGrain.LoadInitialState` 在 IOException 时记录错误
- [ ] `DispatchErrorOr` 使用 `ExceptionFilters.IsRecoverable`

---

## Step 3: ViewModelBase + 生命周期

### 文件清单

| 操作 | 文件 | 改动 |
|------|------|------|
| 创建 | `ViewModels/ViewModelBase.cs` | ObservableObject + CompositeDisposable |
| 创建 | `Analyzers/WT1010.cs` | `Subscribe` 返回值必须被追踪 |
| 创建 | Source Generator: `AutoSelectorAttribute` | 标记 selector 属性 |
| 修改 | `LauncherEntryViewModel.cs` | 继承 ViewModelBase, 删除手动事件订阅 |
| 修改 | `LauncherEntryViewModel.Layout.cs` | 删除 `SubscribeToLayoutProvider()`, 改为 AutoSelector |
| 修改 | `EntryListViewModel.cs` | `ReplaceEntries` 中 Dispose 旧 VM |
| 修改 | `MainWindowViewModel.cs` | 继承 ViewModelBase, 订阅通过 AutoSubscribe |
| 修改 | 所有 ViewModel | 逐个继承 ViewModelBase + 改用 AutoSelector |

### 验证命令

```powershell
dotnet build -warnaserror
# WT1010 生效: 未追踪的 Subscribe 编译警告
dotnet test
```

### 验收条件
- [ ] 所有 ViewModel 继承 `ViewModelBase`
- [ ] `LauncherEntryViewModel` 没有 `PropertyChanged +=` 订阅
- [ ] `ReplaceEntries` 中调用被替换 VM 的 `Dispose()`
- [ ] `Subscribe` 返回值全被 `CompositeDisposable` 追踪
- [ ] 测试无泄漏（长时间运行 SMOKE 测试）

---

## Step 4: 强类型值对象 + 验证内联

### 文件清单

| 操作 | 文件 | 改动 |
|------|------|------|
| 修改 | `Core/EntryId.cs` | 删除 Vogen 包装，验证在主构造器 |
| 修改 | `Core/EntryName.cs` | 删除 Vogen 包装，验证在主构造器 |
| 修改 | `Core/PresetId.cs` | 删除 Vogen 包装，验证在主构造器 |
| 修改 | `Core/PresetName.cs` | 删除 Vogen 包装，验证在主构造器 |
| 修改 | `Core/LauncherPreset.cs` | Id→PresetId, Name→PresetName, EntryIds→ImmutableList<EntryId> |
| 修改 | `Core/LauncherStoreFile.cs` | DefaultPresetId 保持 string（JSON 边界），添加构造器验证 |
| 删除 | `Core/ValueObjects/` | Vogen 生成类型全部移除 |
| 删除 | `Core/EntryDirectory.cs` | 合并到 DirectoryPath |
| 删除 | `Core/EntryDirectoryJsonConverter.cs` | 死代码 |
| 创建 | `Analyzers/WT2003.cs` | 外部 Validate 方法警告 |
| 修改 | `LauncherPresetValidator.cs` | 验证逻辑移入 LauncherPreset/LauncherStoreFile 构造器 |

### 验证命令

```powershell
dotnet build -warnaserror
dotnet test
# 特别注意 LauncherJsonContextRegistrationTests 通过
# 特别注意 EntryDirectoryTests 通过
```

### 验收条件
- [ ] `ValueObjects/` 目录被删除
- [ ] `EntryId.EntryIdJsonConverter` 保留在 Core 层
- [ ] `LauncherPreset` 所有 ID 字段是强类型
- [ ] `LauncherPresetValidator.Validate` 被删除或标记 Obsolete
- [ ] 测试全部通过

---

## Step 5: Analyzer + ArchUnitNET 守卫

### 文件清单

| 操作 | 文件 | 改动 |
|------|------|------|
| 创建 | `Analyzers/WT1007.cs` | `ViewNoBusinessLogic` |
| 创建 | `Analyzers/WT1008.cs` | `VmNoManualSetter` |
| 创建 | `Analyzers/WT1009.cs` | `SelectorReadonly` |
| 创建 | `Analyzers/WT2001.cs` | `CoreNoSystemIo` |
| 创建 | `Analyzers/WT2002.cs` | `NoStaticMutable` |
| 修改 | `Tests/ArchitectureRules.cs` | 新增 R13-R18 共 6 条规则 |
| 修改 | `README.md` / `CLAUDE.md` | 更新文档匹配新架构 |

### 新增 ArchUnitNET 规则

```csharp
// R13: View 层必须不包含 if/switch 语句
// R14: ViewModel 必须继承 ViewModelBase
// R15: Core 层必须不引用 App 命名空间
// R16: IObservable.Subscribe 返回值必须被追踪
// R17: 静态非 readonly 字段必须被标记 [Obsolete]
// R18: 所有 ID 字段必须是值对象类型
```

### 验证命令

```powershell
# 架构规则必须全部通过
dotnet test --filter "FullyQualifiedName~ArchitectureRules"
# 新增 analyzer 全部生效
dotnet build -warnaserror  # 必须零警告
```

### 验收条件
- [ ] 所有 6 条新增 ArchUnitNET 规则通过
- [ ] 所有 6 个新增 Roslyn Analyzer 在违规代码上产生编译错误
- [ ] `CLAUDE.md` 和 `README.md` 中的架构描述更新
- [ ] 代码库中零 `ponytail` 标记（合法兼容性除外）

---

## 完整执行管线

```powershell
# 1. 每步之后的验证
cd <PROJECT_ROOT>\WT_Protector
dotnet restore src/avalonia-launcher/WTLauncher.Avalonia.sln
dotnet build  src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
dotnet test   src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --filter "FullyQualifiedName~ArchitectureRules"
dotnet test   src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release

# 2. 最终门禁
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

## 进度追踪

- [ ] Step 1: AppState + 锁内原子化
- [ ] Step 2: DI + union 错误处理
- [ ] Step 3: ViewModelBase + 生命周期
- [ ] Step 4: 强类型 + 验证内联
- [ ] Step 5: Analyzer + ArchUnitNET 守卫
- [ ] 最终 verify.ps1 通过
- [ ] 所有 `ponytail` 标记清理完毕
- [ ] 文档更新
