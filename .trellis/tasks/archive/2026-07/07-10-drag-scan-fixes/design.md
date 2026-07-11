# Technical Design — 拖拽柄可见性 + Discovery 扫描并行化

## Architecture Decisions

### ADR-1: ~~拖拽柄用 IsVisible 绑定 IsCustomSortMode（而非自动切模式）~~ 【REVERSED 2026-07-10】

> **用户否决**："拖拽要始终可以拖拽，不由排序方式所限定"。原决策作废，改为 ADR-1'。

### ADR-1': 拖拽柄常显 + 非 custom 模式自动切换 (Revision 2026-07-10)

**决策**: 撤销三处 `IsVisible` 绑定（拖拽柄恢复常显）；`OnEntryDragPointerPressed` 守卫只校验类型；`BeginDragReorder` 入口调用 `EnsureDragSortMode()`，非 custom 模式下先切到 custom 再开 session。

**否决的选项**:
- 保留 IsCustomSortMode 守卫但改"切到 custom 后再开 session"：用户在 alphabetical/updated/recent 下看到拖拽柄但按了无反应，仍是体感"坏了"。否。
- 让 LauncherDragReorderWorkflow 自己处理非 custom：要改 4 个方法签名 + workflow 单元测试，违反 ponytail。否。

**实现路径**:
- `MainWindow.axaml` 行 582/759/869 删除 `IsVisible="..."` 三行。
- `MainWindow.Drag.cs:36` 改为 `if (DataContext is not MainWindowViewModel viewModel) return;`。
- `MainWindowViewModel.Drag.cs:BeginDragReorder` 第一行加 `EnsureDragSortMode();`。
- 新增 `private void EnsureDragSortMode()`：`if (IsCustomSortMode) return; SelectedSortMode = _sortModes.First(m => m.Id == LauncherEntryVisibility.CustomSortModeId);`

**风险**:
- `EnsureDragSortMode` 触发 `OnSelectedSortModeChanged` → `TryPersistSettingsChange` → 保存 + `ApplyFilter`。`ApplyFilter` 看到 `HasActiveSession=false`（session 还没 Begin），走非 drag 分支重建 `_visibleEntries`，然后回到 `BeginDragReorder` 调 `_dragReorderWorkflow.Begin(true, ...)` 创建 session。时序安全。
- `FindEntry(draggedEntryId)` 在 `_entries`（全集）中查找，所以切换排序后 entry 必然能找到。✓

### ADR-6: WindowsExtensions 移除 "" (Revision 2026-07-10)

**决策**: `WindowsExtensions` 从 `["", ".exe", ".cmd", ".bat"]` 改为 `[".exe", ".cmd", ".bat"]`，移除 `""`。

**根因**: npm 全局 bin 同时生成无扩展名 Unix shell 脚本 + Windows `.cmd` shim：
```
C:\Users\<USER>\AppData\Roaming\npm\claude      ← Unix shell 脚本
C:\Users\<USER>\AppData\Roaming\npm\claude.cmd  ← Windows 批处理
```
`""` 在首位会先匹中 Unix 脚本，Windows 上 `Process.Start` 无法直接执行 shell 脚本（无 shebang 解释器）→ 启动失败或挂起 → 扫不到任何 npm 装的 CLI。

**否决的选项**:
- 把 `""` 放最后：仍然可能匹中 Unix 脚本（如果 PATH 上有无扩展名二进制）。否。
- 读 `PATHEXT` 环境变量动态构造：增加复杂性，硬编码已覆盖 99% 场景。ponytail: 否。

**验证**:
- `where claude` 同时返回 `claude` 和 `claude.cmd`。修复后 `TryResolveOnPath` 跳过无扩展名 `claude`，匹中 `claude.cmd`。
- `cmd //c "claude.cmd --version"` 输出 `2.1.206 (Claude Code)` → 正则匹配 `2.1.206`。✓
- 既有测试 `TryResolveOnPath_FindsExe_WhenPresentWithoutExtension` / `TryResolveOnPath_FindsCmd_WhenOnlyCmdExtensionPresent` 不依赖 `""`，全部继续 PASS。

**否决的选项**:
- 自动切到 custom：会偷换用户排序意图（用户切到字母序就是为了字母序）。否。
- 只改 config `sortMode=custom`：一次性修复，用户切走又坏，治标。否。
- 加 `IsHitTestVisible="False"` 而非隐藏：用户看到按钮但不能点，比隐藏更糟。否。

**实现路径**:
- `MainWindow.axaml` 行 ~579 (cards)、~756 (details)、~864 (names) 三处 Border 加 IsVisible 绑定。
- 绑定 source：ItemsControl 的 DataContext 就是 MainWindowViewModel，所以 `{Binding $parent[ItemsControl].DataContext.IsCustomSortMode}` 应该工作。但 details 模式的 drag handle 在 ListBox.ItemTemplate 的 StackPanel 里，需要 `$parent[ListBox]` 或更稳妥地用 `$parent[Window].DataContext.IsCustomSortMode`。
- 最稳妥：`{Binding DataContext.IsCustomSortMode, RelativeSource={RelativeSource AncestorType=Window}}`。

**风险**:
- DataTemplate 内的 Binding 祖先查找在不同 ItemsControl 子类（ItemsControl / ListBox）上行为可能不同。用 `RelativeSource AncestorType=Window` 规避。
- IsCustomSortMode 是只读 projection，OnPropertyChanged 在 `OnSelectedSortModeChanged` 里手动触发（`Layout.cs:133`），绑定会正确刷新。

### ADR-2: 扫描并行化用 Parallel.ForEachAsync + 同步外壳

**决策**: `Scan` 保持 sync 签名（VM 契约 `Func<AgentDiscoveryResult>` 不变），内部用 `Parallel.ForEachAsync` 并行 spawn catalog entries。每个 entry 独立调用 `TryResolveOnPath` + `TryDetectVersion`。

**否决的选项**:
- 改 Scan 签名为 `async Task<AgentDiscoveryResult>`：要改 VM `Func<AgentDiscoveryResult>` → `Func<Task<AgentDiscoveryResult>>`，连带改 `DiscoverAgentsViewModel`（已经在 Task.Run 里再包一层 Task）。改动面变大，违反 ponytail 最小 diff。否。
- 用 `Parallel.ForEach`（同步）：`TryDetectVersion` 内部用 `Process.WaitForExit` 是同步阻塞，能跑，但 maxDegreeOfParallelism 控制不如 ForEachAsync 直观。可选，但 ForEachAsync 更现代。
- 用 `Task.WhenAll` + `Wait()`：手动管理并发度，不如 ForEachAsync 内置。

**实现路径**:
- 用 `ConcurrentBag<DiscoveredAgent>` + `ConcurrentBag<AgentDiscoveryFailure>` 替代 `ImmutableArray.CreateBuilder`，线程安全。
- `Parallel.ForEachAsync(catalog, maxDegreeOfParallelism: 4, async (entry, ct) => { ... })`，内部 `await Task.Run(() => TryResolveOnPath(...))` 和 `await Task.Run(() => TryDetectVersion(...))`。
- 外部 `Scan` 方法 `.GetAwaiter().GetResult()` 转 sync。

**风险**:
- Process.Start 本身是 OS 调用，4 并发没问题；如果 catalog 涨到 20+ 可能要降并发，但当前 8 个无所谓。
- `GetAwaiter().GetResult()` 在 UI 线程会死锁——但 `Scan` 已经被 `DiscoverAgentsViewModel.ScanAsync` 的 `Task.Run` 包裹，运行在线程池，不存在同步上下文死锁。

### ADR-3: ReadLine 超时——调换 WaitForExit 与 ReadLine 顺序

**决策**: 不用 `ReadLineAsync().WaitAsync(timeout)`（要包 async/try-catch），直接调换调用顺序：先 `WaitForExit(timeout)`，超时 kill，再读 `StandardOutput` buffer。

**否决的选项**:
- `await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(2))`：要改 `TryDetectVersion` 为 async，连带改 `Scan`（已经被 ADR-2 改成并行 async 了，可以接受）；但 `ReadLineAsync + WaitAsync` 涉及异常 unwrap（`OperationCanceledException` vs `TimeoutException`），代码可读性下降。
- 把 Process 整个改成 `Process.StartAsync`（.NET 7+）：现有代码用 `Process.Start()` 同步，没必要为这一处升级。

**实现路径**:
```csharp
using var process = new Process { StartInfo = psi };
if (!process.Start()) return null;

if (!process.WaitForExit((int)VersionTimeout.TotalMilliseconds))
{
    try { process.Kill(entireProcessTree: true); } catch { }
    return null;
}

// 进程已退出，stdout 已 buffer 完毕，ReadLine 立即返回
string? firstLine;
try { firstLine = process.StandardOutput.ReadLine(); }
catch (IOException) { return null; }

if (firstLine is null) return null;
var match = VersionPattern.Match(firstLine);
return match.Success ? match.Value : null;
```

**风险**:
- 如果 cli 退出码非 0 但 stdout 有内容（比如打印到 stderr 然后退出），仍然能读到 stdout 第一行——版本检测成功。这是好行为。
- 如果 cli 写完 stdout 但 `--version` 后 hang（比如等待 stdin），`WaitForExit(2s)` 超时 kill，return null。可接受。
- Kill 之后 stream 可能被 OS 强制关闭，`ReadLine` 抛 IOException——已经在 catch 里。

### ADR-4: PATH 扫描保留现状

**决策**: 不替换 `TryResolveOnPath` 的逐目录 File.Exists 为 Windows `where` 命令。

**理由**:
- 并行化（ADR-2）已经把 catalog 的 spawn 并发跑，PATH 扫描作为 spawn 前的同步步骤，每个 cli 内部串行扫描几十个 PATH 目录 × 4 扩展名 = ~200 次 `File.Exists`，单次 < 1ms，总计 < 200ms，可接受。
- 引入 `where` 跨平台问题（Linux/macOS 没有 `where`，要用 `which`），且 spawn `where` 也是一个进程开销。不值。
- ponytail: 并行化已经吃到 80% 收益，剩 20% 不值得增加跨平台风险。

### ADR-5: 测试策略——fixture 脚本而非 mock

**决策**:
- 并行测试：写一个 PowerShell/cmd 输出固定版本字符串的 fixture（已有 `AgentDiscoveryScannerTests` 的 temp dir 模式），构造 catalog 里 1 个慢 cli（`timeout /t 1` 输出延迟）+ 1 个快 cli，断言总耗时 < 2 × 单 cli 耗时。
- ReadLine 超时测试：写一个 fixture cli 启动后 `Console.ReadKey()` 阻塞（永远不输出 stdout），断言 `TryDetectVersion` 在 `VersionTimeout + 500ms` 内 return null。

**否决的选项**:
- Mock `Process`：`Process` 是 sealed 类不好 mock；用 `ProcessStartInfo` 注入 + 测试 hook 是过度设计。否。

**风险**:
- 测试 fixture 跨平台：cmd `timeout` 在 Windows 上有，Linux 上没有；用 PowerShell 脚本统一（项目已经是 win-x64 self-contained，测试也假设 Windows）。

## Compatibility / Rollout

- 向后兼容：无 schema 改动，无 config 改动，无 API 改动。
- Rollback：纯代码改动，git revert 即可。
- 不影响 `LauncherStore` 原子写、不影响其他 VM partial。

## Test Strategy

- 现有 `AgentDiscoveryScannerTests` 6 个测试保持通过。
- 新增：
  - `Scan_ParallelSpawn_CompletesInMaxPerCliTime`（并行）
  - `TryDetectVersion_KillsUnresponsiveCli_WithinTimeout`（ReadLine 超时）
- 现有 `DiscoverAgentsViewModelTests` + `MainWindowViewModelTests.DiscoveryTests` 保持通过（并行化对 VM 调用契约透明）。
- Gate：`tools/verify.ps1` 16 项全 PASS，测试数 504/505 → 506/507。
