# Technical Design — Command Management Dialog

## Architecture Decisions

### ADR-1: Dialog 用纯 C# code-behind（不用 XAML）

**决策**: `CommandManagementWindow` 用纯 C# code-behind 写 UI，与 `DiscoverAgentsWindow`/`ConfirmBatchLaunchWindow` 一致。

**否决**:
- 用 .axaml：现有 dialog 都是 code-behind，统一风格。XAML 会引入混合模式。
- 用 ReactiveUI：项目用 CommunityToolkit.Mvvm，加 ReactiveUI 是依赖倒退。

**理由**: Dialog 是简单一次性 UI，code-behind 比 XAML 编译更快、修改更直接，与现有 dialog 风格一致。

### ADR-2: 双 tab 共享一个 ViewModel，子状态用 partial class 拆

**决策**: `CommandManagementViewModel` 是单 VM，含两个 ObservableCollection（`CommandRows` / `TemplateRows`）+ 当前选中 tab 状态。Tab 切换通过 VM 内 `_activeTab` 字段 + `OnPropertyChanged`。

**否决**:
- 两个独立 sub-VM（`CommandUsageTabViewModel` + `CommandTemplatesTabViewModel`）：增加 indirectness，没有实质好处。
- 把每个 tab 做成单独 Window：用户切换成本高。

**理由**: 两个 tab 共享 snapshot/rollback/save 路径，单 VM 最自然。

### ADR-3: 编辑行为通过 event 回调到 owner，不直接 mutate store

**决策**: `CommandManagementViewModel` 接收一个 `Func<CommandManagementEdit, CommandManagementResult?>` 回调（owner = `MainWindowViewModel`）。用户点 OK → VM 收集 edits → 调 callback → owner 做 snapshot/mutate/save/rollback → 返回结果（成功/失败 + 状态消息）。VM 不直接访问 `_entries` / `_settings` / `LauncherStore`。

**否决**:
- VM 直接 mutate `_entries`：违反 parent invariant "所有 mutations 通过 SaveStoreFile"。VM 没有这个路径。
- 在 VM 里注入 `MainWindowViewModel` 全引用：循环依赖 + 难测试。

**理由**: 与 `DiscoverAgentsDialogAsync` 同模式（dialog 是 `Func<Task<IReadOnlyList<DiscoveredAgent>?>>`，owner 处理实际 mutation）。一致 + 可测。

### ADR-4: 去重用 ordinal ignore-case 比较

**决策**: `CommandUsageAnalyzer` 用 `StringComparer.OrdinalIgnoreCase` 去重 command。`$codex` 和 `$CODEX` 视为同 command。

**否决**:
- ordinal case-sensitive：Windows 命令行不区分大小写，会漏检重复。
- 当前 culture：性能差 + 跨 culture 行为不一致。

**理由**: Windows `cmd.exe` / PowerShell 对命令名 case-insensitive。OrdinalIgnoreCase 性能好且语义匹配。

### ADR-5: 合并语义 = 失效方的所有 entry 引用更新到保留方

**决策**: 合并两个 command A 和 B（用户选 A 保留）→ 遍历 `_entries`，把 `entry.Command == B` 的全部改成 `entry.Command == A`。失效方 B 若是裸命令则消失；若是模板引用 `$B`，模板 `$B` 是否一并删除？

→ **不一致**：模板 `$B` 删除是 Tab 2 的操作。Tab 1 合并不删模板。仅替换 entry.command 字段值。

**否决**:
- 合并时自动删模板：跨 tab 副作用，违反单一职责。
- 阻止模板 command 合并：限制太严，用户失去批量替换能力。

**理由**: Tab 1 只管 entry.command 字段值，Tab 2 只管模板字典。两个 tab 独立操作。

### ADR-6: Tab 2 模板 key 重命名是原子操作，自动同步

**决策**: Tab 2 改 `$codex` 的 key 为 `$codex2` → 所有 `entry.Command == "$codex"` 自动改为 `"$codex2"`。无需同步确认 dialog。

**否决**:
- 弹同步确认：每次改 key 都弹，烦。Key 重命名天然是"全部跟着改"语义。

**理由**: 模板 key 重命名 = 引用重命名。语义明确，加确认是噪音。

### ADR-7: Tab 2 删除模板时，引用 > 0 阻止

**决策**: Tab 2 删 `$codex` → 若有 entry 引用，弹"无法删除：N 个 entry 仍在引用此模板。请先修改这些 entry 的 command 或合并到其他模板。" → 阻止。

**否决**:
- 强删 + 把引用 entry 的 command 改成空：破坏数据。
- 强删 + 把引用 entry 的 command 改成裸命令（如 `$codex` → `codex`）：用户没要求这个语义，可能 surprise。

**理由**: 删除是危险操作。引用 > 0 必须先解引用。与 R-4 一致。

## Data Flow

```
[用户点击 Cards 工具栏"管理"按钮]
    ↓
[MainWindow.axaml.cs 调 MainWindowViewModel.OpenCommandManagementCommand]
    ↓
[MainWindowViewModel 构造 CommandManagementViewModel 并注入:
 - 初始 command rows (去重 + 引用计数)
 - 初始 template rows
 - 编辑回调 Func<CommandManagementEdit, CommandManagementResult?>]
    ↓
[MainWindow.axaml.cs 实例化 CommandManagementWindow(vm) → ShowDialog]
    ↓
[用户编辑 / 双击 / 右键]
    ↓
[VM 收集 edits]
    ↓
[用户点"应用"或单次操作触发]
    ↓
[VM 调 editCallback(edit)]
    ↓
[MainWindowViewModel:
  1. CaptureEntryMutationSnapshot()
  2. 应用 edit (修改 _entries 的 command 字段 或 _settings.CommandTemplates)
  3. SaveEntryCollectionChanges(snapshot)
  4. 若失败 → RestoreEntryMutationSnapshot → 返回失败结果
  5. 若成功 → 返回成功结果 + 新的 command rows]
    ↓
[VM 更新本地行数据 + 状态消息]
```

## Contracts

### CommandManagementEdit (输入到 owner)

```csharp
public abstract record CommandManagementEdit;

public sealed record RenameCommandEdit(string OldCommand, string NewCommand) : CommandManagementEdit;
public sealed record MergeCommandsEdit(string LoserCommand, string KeeperCommand) : CommandManagementEdit;
public sealed record DeleteCommandEdit(string Command) : CommandManagementEdit;

public sealed record UpdateTemplateKeyEdit(string OldKey, string NewKey, string ExpandedValue) : CommandManagementEdit;
public sealed record UpdateTemplateValueEdit(string Key, string NewExpandedValue) : CommandManagementEdit;
public sealed record AddTemplateEdit(string Key, string ExpandedValue) : CommandManagementEdit;
public sealed record DeleteTemplateEdit(string Key) : CommandManagementEdit;
```

### CommandManagementResult (owner 返回)

```csharp
public sealed record CommandManagementResult(
    bool Success,
    string StatusMessage,
    IReadOnlyList<CommandRow> UpdatedCommandRows,
    IReadOnlyList<TemplateRow> UpdatedTemplateRows);
```

### CommandRow / TemplateRow (VM 内展示)

```csharp
public sealed record CommandRow(
    string Command,
    int ReferenceCount,
    IReadOnlyList<string> ReferencingEntryNames);

public sealed record TemplateRow(
    string Key,
    string ExpandedValue,
    int ReferenceCount);
```

## Compatibility / Rollback

- 不改 `config/launcher.json` schema（schemaVersion 保持 2）
- 不改 `_entries` 字段
- 不改 `_settings.CommandTemplates` 字段类型（仍是 `IImmutableDictionary<string, string>`）
- 新 dialog 是纯加法，不影响现有 view mode / sort / preset / drag 路径
- Rollback: 整个 dialog 是新增文件 + 4 个 mutation 方法注入 MainWindowViewModel。`git revert` 即可。

## Risks

- **风险 1**: 模板 key 重命名同步更新所有 entry 时漏掉某些。**缓解**: 单元测试覆盖 `_entries` 全集替换。
- **风险 2**: editCallback 在 dialog 仍打开时多次调用，每次都 snapshot/mutate/save。**缓解**: 每次 edit 独立 transaction（snapshot/mutate/save）。多次连续 edit = 多次 transaction，可接受。
- **风险 3**: dialog 打开期间，外部（其他 mutation）修改了 `_entries` → 关闭时 refresh command rows 会显示新数据。**缓解**: dialog 是 modal（阻塞主窗口），不存在并发 mutation 路径。
- **风险 4**: tab 切换丢失状态。**缓解**: 两个 tab 的 rows 都是 VM 内 ObservableCollection，tab 切换不重建。

## Test Strategy

- **CommandUsageAnalyzer 单元测试**: 去重 / 引用计数 / ordinal case-insensitive / 空集合 / 单 entry 多命令。
- **CommandManagementViewModel 单元测试**: 模拟 callback，验证各种 edit 类型 → 正确 callback 调用 + UI 状态正确刷新。
- **MainWindowViewModel.OpenCommandManagementCommand 集成测试**: 通过注入 mock callback，验证 snapshot/mutate/save/rollback 路径，覆盖：
  - 重命名 command → 多 entry 同步
  - 合并 command → 失效方 entry 引用更新
  - 删除模板（引用 > 0）→ 阻止
  - 模板 key 重命名 → 引用 entry 自动更新
  - save 失败 → snapshot/rollback 还原 commandTemplates
- 现有 504 测试保持通过（+10 新测试 → 总 514+）。
- Gate `tools/verify.ps1` 16 项全 PASS。
