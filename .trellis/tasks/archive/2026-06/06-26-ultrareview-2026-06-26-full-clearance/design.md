# Design — UltraReview 2026-06-26 全部发现清缴

## Architecture Boundaries

所有修复严格限制在现有层级内，不跨层引入新依赖：

| 层级 | 修改范围 | 禁止 |
|------|---------|------|
| WTLauncher.Core | `LauncherEntryIds.cs`, `LauncherPresetIds.cs`, `LauncherEntryVisibility.cs`, `FastWrapPanel.cs` 等 | 不可引入 NuGet 依赖 |
| WTLauncher.App.ViewModels | `MainWindowViewModel.*.cs`, `LauncherLaunchWorkflow.cs` 等 | 不可直接引用 View 层类型 |
| WTLauncher.App.Views | `MainWindow.axaml.cs`, `MainWindow.Drag.cs`, XAML | 不可包含业务逻辑 |
| WTLauncher.Tests | 测试文件 | 不可依赖执行顺序 |
| 文档 | README.md, DESIGN.md, spec/*.md | 不可新增规范要求 |
| 项目配置 | csproj, Directory.Build.props | 不可降级目标框架 |

## Key Design Decisions

### 1. 代理对截断修复 (BUG-1)
- **当前**: `if (trimmed.Length > MaxIdLength + 1)` 在 257 字符时条件为 false
- **修复**: 改为 `>= MaxIdLength + 1` 或检查 `trimmed.Length >= MaxIdLength + 1`
- **影响**: `LauncherEntryIds.cs:25-27`, `LauncherPresetIds.cs:44-45`
- **测试**: 新增 257 字符高代理结尾的测试用例

### 2. NaN 传播修复 (BUG-2)
- **当前**: `double.IsInfinity` 对 NaN 返回 false，NaN 污染布局
- **修复**: 加 `double.IsNaN` 守卫，NaN 时回退到默认宽度 900
- **影响**: `FastWrapPanel.cs:40`

### 3. 键盘焦点越界修复 (BUG-3)
- **当前**: visibleCount==0 时 Key.Down/Up 执行 `VisibleEntries[-1]`
- **修复**: 在 HandleKeyboardNavigation 开头统一检查 visibleCount==0，所有按键路径提前返回
- **影响**: `MainWindow.axaml.cs:152-209`

### 4. NuGetAudit 修复 (BUG-4)
- **修复**: 删除 `<NuGetAudit>false</NuGetAudit>` 或改为 `true`
- **影响**: `Directory.Build.props:6`

### 5. 启动时间戳修复 (BUG-5)
- **当前**: `RecordLaunchFromOwner` 在 `SaveConfig` 之前调用
- **修复**: 先 `SaveConfig`，成功后再 `RecordLaunchFromOwner`；失败则不做任何修改
- **影响**: `MainWindowViewModel.Launch.cs:90-94`

### 6. FormKC 统一 (BUG-6)
- **当前**: Entry ID 用 FormC，Preset ID 用 FormKC
- **修复**: 统一为 FormKC（与 LauncherEntryText 一致）
- **影响**: `LauncherEntryIds.cs:18` 改为 `NormalizationForm.FormKC`
- **兼容性**: 已持久化的 FormC ID 在规范化下可能改变，需验证

### 7. Ghost 限界修复 (BUG-7)
- **修复**: `MainWindowDragGhostFactory.Create` 改为调用带窗口限界的 6-参数 `Calculate` 重载
- **影响**: `MainWindowDragGhostFactory.cs:45`

### 8. 临时文件清理修复 (BUG-8)
- **修复**: 扩展 `CleanupStaleTempFiles` 正则，同时匹配 `.stage-*`, `.bak-tmp-*`, `.recovery-*`
- **影响**: `LauncherJson.cs:107-132`

### 9. 多文件一致性 (BUG-9)
- **决定**: 标注为"已知设计取舍"，不修改提交逻辑。
- **理由**: 测试明确接受此行为；在生产中极罕见；修复需要两阶段提交协议，复杂度远超收益

## Rollback Plan

每个修复独立可回滚（单个 commit revert）。父任务负责最终集成验证。

## Compatibility

- 所有 `LaunchEntry` / `LauncherSettings` / `LauncherPresetFile` JSON schema 不变
- FormKC 统一可能改变已有 FormC ID 的序列化形式，属于数据规范化修正
- 其他修复不改变任何公共 API 契约
