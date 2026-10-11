# Dialogs — Views Layer

> 确认窗口、entry/preset 编辑器：对话框契约。

## Wiring Contract（2026-08-15）

- `IDialogService` 禁暴露 `Func<...>` / `Action<...>` 桥参数。
- 命令/目录管理对话框收 `ICommandEditApplier` / `IDirectoryEditApplier`；对话框 VM 经端口应用编辑，绝不自己持久化。
- `EntryDetailsWindow` / `PresetDetailsWindow` 收 `IEntryDraftValidator`；code-behind 禁直接调 Core 规范化。
- Views 只经 `DialogService` 建窗；业务校验与文件 IO 留在 feature 端口后。

## 对话框清单

| 对话框 | 文件 | 返回 | 用途 |
|--------|------|------|------|
| Confirm Launch | `ConfirmLaunchWindow.cs` | `bool` | 单启动确认 |
| Confirm Batch Launch | `ConfirmBatchLaunchWindow.cs` | `bool` | 批量确认 |
| Confirm Entry Delete | `ConfirmEntryDeleteWindow.cs` | `bool` | 删条目确认 |
| Confirm Preset Delete | `ConfirmPresetDeleteWindow.cs` | `bool` | 删预设确认 |
| Entry Details | `EntryDetailsWindow.cs` | `EntryDraft?` | 增/改条目 |
| Preset Details | `PresetDetailsWindow.cs` | `string?` | 保存/更新预设 |
| Directory Management | `DirectoryManagementWindow.cs` | `null` | 浏览/合并目录 |
| Directory Edit | `DirectoryEditDialog.cs` | `string?` | 编辑目录路径 |
| Command Edit | `CommandEditDialog.cs` | `string?` / `(string,string)` | 编辑命令/模板 |
| Discover Agents | `DiscoverAgentsWindow.cs` | `DiscoveredAgent[]?` | 自动发现 CLI agent |
| Command Management | `CommandManagementWindow.axaml` | `null` | 管理命令/模板 |

## 对话框模式

1. 构造收类型化参数对象（确认收 target；编辑器收 draft，null=新建）。
2. `ShowDialog<T>` 返回有意义结果（`bool`=确认/取消；`EntryDraft?` null=取消）。
3. 只收集输入返回；VM 决定处置，绝不直接调业务逻辑。

共享 helper：`LauncherDialogButtons.cs` / `LauncherDialogText.cs`。两个对话框按钮布局/文本相似即抽到 helper，不复刻 chrome。注入在 `MainWindow.axaml.cs`（VM 只知 delegate，View 知 `ShowDialog<>`，此处是 seam）。

## 一致性 Token（V03）

| Token | 值 | 消费方 |
|-------|----|--------|
| `DialogTitleFontSize` | 20 | `LauncherDialogText.Title()`、`CommandManagementWindow.axaml` `{StaticResource DialogTitleFontSize}` |
| `DialogOuterPadding` | 24 | 全部程序化对话框、`CommandManagementWindow.axaml` `{StaticResource DialogOuterPadding}` |
| `DialogFooterTopMargin` | 14 | `LauncherDialogButtons.Footer()`、`CommandManagementWindow.axaml` `{StaticResource DialogFooterMargin}`（Margin `0,14,0,0`） |
| `DialogFooterSpacing` | 10 | `LauncherDialogButtons.Footer()` 按钮水平间距 |

规则：`Title()` 用 `DialogTitleFontSize`，禁每对话框 FontSize 覆盖；外容器用 `DialogOuterPadding`，禁字面量 20/22/24；`Footer()` 收 `DialogFooterTopMargin`，除非父 Grid `RowSpacing=14` 已提供（用 0 防双倍）；`CommandManagementWindow.axaml` 消费三个 `{StaticResource}`，禁 XAML 字面量。

## 破坏性对话框语义（V03）

危险删除/移除对话框偏向安全：Cancel `IsDefault=true` + `IsCancel=true` + `Opened` 初始焦点；破坏按钮 `IsDefault=false` + `LauncherDialogButtons.Destructive()`。受影响：`ConfirmEntryDeleteWindow`、`ConfirmPresetDeleteWindow`。安全/肯定对话框（launch/save/confirm）继续 Primary 为默认 + 初始焦点主操作。

Design token 用法：`DialogMinWidth=420`、`DialogFooterSpacing=10`、`DialogTitleFontSize=20`、`DialogOuterPadding=24`、`DialogFooterTopMargin=14`。

## Forbidden Patterns

- ❌ 对话框直调 `LauncherStore.Save()`；返回数据让 VM 持久化。
- ❌ 对话框改 `MainWindowViewModel` 状态；返回结果让 VM 处置。
- ❌ `ShowDialog<object>` + 强转；用正确类型化返回。
- ❌ 取消无 null 返回；取消编辑必须返回 `null` 非默认对象。
