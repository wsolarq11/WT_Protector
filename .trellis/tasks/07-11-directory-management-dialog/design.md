# Technical Design — Directory Management Dialog

## Architecture Decisions

### ADR-1: 独立 dialog,不与 CommandManagementWindow 合并

**决策**: 新建 `DirectoryManagementWindow`,与 `CommandManagementWindow` 独立。

**否决**:
- 合并到一个"集中管理" mega-dialog: 单 dialog 复杂度爆炸,3 tab + 不同的 mutation 类型,违反单一职责。
- 在 `CommandManagementWindow` 加第 3 tab: 同上,且 command 有 templates 而 directory 无,字段不对称。

**理由**: 两个 dialog 共享 mutation 路径(snap/rollback/save)但字段语义不同。复用代码通过共享 helper(CommandUsageAnalyzer 模式)而非共享 UI。

### ADR-2: DirectoryUsageAnalyzer 复用 CommandUsageAnalyzer 模式

**决策**: 新建 `DirectoryUsageAnalyzer` 静态类,纯函数。`Analyze(entries) → IReadOnlyList<DirectoryRow>`。`CanDelete(directory, entries)`。

**否决**:
- 泛型 `FieldUsageAnalyzer<T>`: 抽象层次错误,directory 和 command 都是 string 但概念不同,且未来可能有非 string 字段。
- 把 directory 处理塞进 CommandUsageAnalyzer: 违反单一职责。

**理由**: 与 CommandUsageAnalyzer 同模式,代码读者一眼能识别。

### ADR-3: 浏览按钮用 Avalonia `OpenFolderDialog`

**决策**: edit dialog 内嵌"📁 浏览..."按钮,Click 调 `OpenFolderDialog.ShowAsync(this)`,返回 path 填到 TextBox。

**否决**:
- 手输路径: 已支持,但浏览按钮是 PRD R-3 显式要求。
- 自实现文件夹树: Avalonia 原生 dialog 跨平台且无代码成本。

**理由**: Avalonia 原生组件,零依赖。

### ADR-4: 合并语义 = 失效方所有 entry.directory 改成保留方

**决策**: 合并两个 directory A 和 B(用户选 A 保留)→ 遍历 `_entries`,把 `entry.Directory == B` 的全部改成 `entry.Directory == A`。

**否决**:
- 强删失效方 directory: directory 不是顶级资源,没有"删除"概念,只是 entry 字段值。

**理由**: 与 Child 3 合并 command 同语义,对称设计降低用户认知负担。

### ADR-5: 重命名 = cascade 所有引用 entry

**决策**: 改 directory D → D2 → 遍历 `_entries`,把所有 `entry.Directory == D` 改成 `entry.Directory == D2`。

**否决**:
- 仅改当前选中 entry: PRD R-3 要求"全部同步"选项。

**理由**: 与 Child 3 rename command 同语义。

### ADR-6: 引用 > 0 时禁止删除

**决策**: 删 directory → 若有 entry 引用,弹"无法删除:N 个 entry 仍在引用此地址。" → 阻止。

**理由**: 与 Child 3 R-4 同。删除是危险操作。

## Data Flow

```
[用户点击"管理地址"按钮]
    ↓
[MainWindowViewModel.OpenDirectoryManagementCommand]
    ↓
[MainWindowViewModel 构造 DirectoryManagementViewModel 并注入:
 - 初始 directory rows (去重 + 引用计数)
 - 编辑回调 Func<DirectoryManagementEdit, DirectoryManagementResult>]
    ↓
[MainWindow.axaml.cs 实例化 DirectoryManagementWindow(vm) → ShowDialog]
    ↓
[用户编辑 / 双击 / 浏览 / 右键]
    ↓
[VM 调 editCallback(edit)]
    ↓
[MainWindowViewModel.ApplyDirectoryManagementEdit:
  1. CaptureEntryMutationSnapshot()
  2. 应用 edit (修改 _entries 的 directory 字段)
  3. SaveEntryCollectionChanges(snapshot)
  4. 若失败 → RestoreEntryMutationSnapshot → 返回失败结果
  5. 若成功 → 返回成功结果 + 新的 directory rows]
    ↓
[VM 更新本地行数据 + 状态消息]
```

## Contracts

### DirectoryManagementEdit

```csharp
public abstract record DirectoryManagementEdit
{
    private DirectoryManagementEdit() { }
    public sealed record RenameDirectory(string OldDirectory, string NewDirectory) : DirectoryManagementEdit;
    public sealed record MergeDirectories(string LoserDirectory, string KeeperDirectory) : DirectoryManagementEdit;
    public sealed record DeleteDirectory(string Directory) : DirectoryManagementEdit;
}
```

### DirectoryManagementResult

```csharp
public sealed record DirectoryManagementResult(
    bool Success,
    string StatusMessage,
    IReadOnlyList<DirectoryRow> UpdatedDirectoryRows);
```

### DirectoryRow

```csharp
public sealed record DirectoryRow(
    string Directory,
    int ReferenceCount,
    IReadOnlyList<string> ReferencingEntryNames);
```

## Compatibility / Rollback

- 不改 `config/launcher.json` schema(schemaVersion 保持 2)
- 不改 `_entries` 字段
- 新 dialog 是纯加法,不影响现有路径
- Rollback: 整个 dialog 是新增文件 + 3 个 mutation 方法注入 MainWindowViewModel。`git revert` 即可。

## Risks

- **风险 1**: 重命名 directory 时漏更新某个 entry。**缓解**: 单元测试覆盖 `_entries` 全集替换 + case-insensitive 比较(Windows 路径不区分大小写)。
- **风险 2**: 浏览 dialog 选了不存在/无权限的目录。**缓解**: 现有 `LauncherStore` 验证 directory 存在(参见 gate `existingDirectories` 检查),保存失败时 rollback。
- **风险 3**: 用户在大目录(如 D:\)误选 → cascade 改一大批 entry。**缓解**: edit dialog 在确认前显示"将影响 N 个 entry"。

## Test Strategy

- `DirectoryUsageAnalyzer` 单元测试: 去重 / 引用计数 / ordinal case-insensitive(Windows 路径) / 空集合。
- `MainWindowViewModel.ApplyDirectoryManagementEdit` 集成测试:
  - RenameDirectory 单引用 / 多引用
  - MergeDirectories → 失效方 entry.directory 更新
  - DeleteDirectory 引用 > 0 → 由 VM 阻止(实际 ApplyEdit 不接受此情况,因为 VM 已验证 refCount == 0)
  - save 失败 → snapshot/rollback 还原
- 现有 530 测试保持通过(8+ 新增 → 538+)。
- Gate 全 PASS。
