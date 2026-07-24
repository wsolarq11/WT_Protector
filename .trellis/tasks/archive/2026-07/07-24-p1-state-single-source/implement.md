# P1: 状态单源化 — 实施步骤

## 执行顺序

### Step 1: 添加 CurrentSettings 计算属性（安全引入）
修改 `MainWindowViewModel.cs`，将 `_settings` 改为计算属性
- 保留 `_settings` 字段（编译期间逐步消除引用）
- 新增 `CurrentSettings` 属性，从 `_appStore.Select(s => s.Settings)` 读取
- 逐步替换引用

### Step 2: 替换 TrySaveWindowBounds
修改 `MainWindowViewModel.Layout.cs:185-210`
- `LauncherStore.Save(nextStore, _storePath)` → `_appStore.Dispatch(_ => nextStore)`
- 保留 `_settings = nextSettings` 直到 Step 4

### Step 3: 替换 Preset/Command 中的 direct Save 引用
- 确保所有写入路由到 `Dispatch`

### Step 4: 删除 _settings 字段
- 删除字段声明
- 所有读取替换为 `CurrentSettings`
- 所有写入替换为 `Dispatch`

### Step 5: 重构 DragWorkflow
- 删除 `DragWorkflow._settings`，通过 AppStore 读写
- 删除 5 处 `_settings = _dragReorderWorkflow.Settings`

### Step 6: 删除 Subscription 覆盖
- `_settings = state.Settings;` → 删除该行

## 验证
```bash
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```
