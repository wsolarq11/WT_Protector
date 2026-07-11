# Fix drag-mode-guard visibility + discovery scan parallelization

## Goal

修复两个真实用户报告的回归：

1. **拖拽柄"按了没反应"**：在非 custom 排序模式下，XAML 中三个 view mode（cards / details / names）的 `entry-drag-handle` 始终可见，但 `OnEntryDragPointerPressed` 第一行 `if (DataContext is not MainWindowViewModel { IsCustomSortMode: true } viewModel) return;` 静默 return。用户长按按钮无任何视觉反馈，体感"坏了"。
2. **AI Agent CLI 扫描慢/卡**：`AgentDiscoveryScanner.Scan` 串行 spawn catalog 中 8 个 cli 的 `--version` 进程，每个最多 2s 串行 await；同时 `TryDetectVersion` 内 `process.StandardOutput.ReadLine()` 没有超时——任意 cli 不立即向 stdout 输出第一行即整体 hang，`IsScanning` 永不 reset，dialog 永远显示"扫描中…"。

## Background

- 当前 `config/launcher.json: sortMode = "updated"`，`IsCustomSortMode` 只读投影（`MainWindowViewModel.Layout.cs:73`）= false。
- 拖拽柄从 commit `7777bb1 label drag reorder handles` 起就是常显的；commit `4c67183 feat(discovery): AI Agent CLI auto-discovery` 引入了扫描器，`f6154cd feat(branding)` 之后没人改这两块。
- 参考实现 `farion1231/cc-switch`：commit `f8c1f17` 揭示其 `get_tool_versions` 也是 `for tool in requested { ... .await }` 串行；它没有"1 秒扫到"的魔法，仅在 Windows 上整体禁用了 env check（因协议处理器副作用）。我们要做到亚秒级靠**并行化**，不是抄它的实现。
- Invariant 不变量：`AgentDiscoveryScanner` 是 `WTLauncher.Core` 层 pure function，不能引用 Avalonia；扫描失败要继续而不是 abort（已由 `AgentDiscoveryResult.Failures` 保留）。
- Invariant 不变量：所有 `catch (Exception)` 必须 `when (ExceptionFilters.IsRecoverable(ex))`（PRD-only 任务 `07-10-discovery-scan-narrow-catch` 已经把 `DiscoverAgentsViewModel.ScanAsync` 的 catch 收窄，本任务的并行化不得破坏这点）。

## Requirements

### 拖拽可见性

- **R1** — 三个 view mode 的 `entry-drag-handle` Border（`MainWindow.axaml` 行 579 / 756 / 864 附近）增加 `IsVisible` 绑定，使其仅在 `IsCustomSortMode = true` 时可见。这样按下行为与视觉一致；用户体感"按钮坏了"消失。
- **R2** — 不在 VM 层做"按下时自动切到 custom"——会破坏 alphabetical/recent/updated 排序模式的语义（用户切到字母序就是为了字母序，自动回 custom 是把意图偷换掉）。
- **R3** — 不修改 `config/launcher.json` 的 `sortMode`。用户当前选择 `updated` 是有意的；本任务是修 UX 一致性，不是改默认行为。

### 扫描并行化

- **R4** — `AgentDiscoveryScanner.Scan` 内部把串行 `foreach (catalog)` 改成并行 spawn `TryDetectVersion`。总耗时从 `Σ(per-cli)` 降到 `max(per-cli)`。不引入新 NuGet，使用 BCL（`Parallel.ForEachAsync` 或 `Task.WhenAll` + `Wait`）。
- **R5** — `Scan` 方法签名保持 sync（`Func<AgentDiscoveryResult>` 是 VM 调用契约，`DiscoverAgentsViewModel.ScanAsync` 已经在 `Task.Run` 里调用）；内部并行用 `Task.Run` + `Wait` 或 `Parallel.ForEach` 即可。
- **R6** — 每个 catalog entry 的失败隔离保持不变（`IOException | UnauthorizedAccessException | Win32Exception | InvalidOperationException` → `Failures`）；并发写入 `found` / `failures` builder 必须线程安全（用 lock 或 `ConcurrentBag`）。

### ReadLine 超时

- **R7** — `TryDetectVersion` 中 `process.StandardOutput.ReadLine()` 必须有超时保护。任意 cli 不立即输出 stdout 第一行（`.cmd` shim 启动慢 / 走 stderr / 卡 stdin）不能让整个扫描 hang。
- **R8** — 实现选择：先 `process.WaitForExit(timeout)` 等进程退出，超时则 `Kill(entireProcessTree: true)` 并 return null；进程正常退出后再 `process.StandardOutput.ReadLine()` 读已 buffer 的首行。这样 `ReadLine` 在进程已退出后调用，永远不会阻塞。
- **R9** — 保留现有 `VersionPattern` 正则不变（ponytail: 一个 regex 没必要换）。

### 测试

- **R10** — 扩 `AgentDiscoveryScannerTests`：加一个并行场景测试，证明多个 catalog entry 同时被检测（通过 mock 慢 cli 验证总耗时 ≈ 单个最慢，而非串行和）。如果 mock spawn 不实际可行，则加一个 smoke test：catalog 有 1 个慢 cli + 1 个快 cli，扫描能在 `VersionTimeout × 1.5` 时间内完成（而非 `2 × VersionTimeout`）。
- **R11** — 加一个测试验证 `ReadLine` 超时路径：catalog cli 启动后不输出 stdout（写一个测试 fixture 进程），扫描在 `VersionTimeout` 内 return null version，整体扫描不 hang。

### Gate

- **R12** — `tools/verify.ps1` 全 16 检查 PASS（包含 `-warnaserror` + 19 项 test 套件）。
- **R13** — 不引入新依赖；不动 `LauncherStore` / `LauncherJson` 原子写路径；不动 `IsCustomSortMode` 投影定义。

## Acceptance Criteria

- [ ] `MainWindow.axaml` 三处 `entry-drag-handle` 都有 `IsVisible` 绑定到 `IsCustomSortMode`（cards/details/names 三个 view mode 都修），绑定路径正确（`$parent[ItemsControl].DataContext.IsCustomSortMode` 或 `Window.DataContext.IsCustomSortMode`，按实际 visual tree 验证）。
- [ ] `AgentDiscoveryScanner.Scan` 内部并行 spawn；测试证明 8 个 cli 扫描时间 ≈ 单 cli 而非 8 × cli。
- [ ] `TryDetectVersion` 的 `ReadLine` 路径不再无超时阻塞；测试证明卡 stdout 的 cli 不会让扫描 hang。
- [ ] `DiscoverAgentsViewModel.ScanAsync` 的 `catch (Exception ex) when (ExceptionFilters.IsRecoverable(ex))` 保持不变。
- [ ] `tools/verify.ps1` PASS，测试数从当前 504/505 上升（至少 +1）。
- [ ] Grep `catch (Exception)` (无 `when`) 在 `src/avalonia-launcher/src` 仍然 0 命中。
- [ ] 用户体感回归消除：sortMode=updated 时拖拽柄消失，custom 模式下出现且可拖；AI Agent CLI 扫描对话框在亚秒到 2s 内完成（取决于 PATH 上 cli 数量）。

## Out of scope

- 任何 `MainWindow.Drag.cs` / `MainWindowViewModel.Drag.cs` 拖拽工作流逻辑改动（拖拽逻辑本身完好，只是被 mode 守卫静默 return）。
- 任何 cc-switch 风格的"用 Windows `where` 命令替代手动 PATH 扫描"——并行化已解决主要慢点，`where` 跨平台问题不值。
- 给 catalog 增加新 entry、改 brand color、改 discovery dialog UX。
- 任何 `MainWindow.axaml.cs` 的 PointerMoved/Released/CaptureLost 绑定改动。

## Revision (2026-07-10) — 方向调整

**用户反馈否决了 R1-R3 旧方向**："拖拽要始终可以拖拽，不由排序方式所限定，另外依旧扫描不出东西"。

**旧方向 (R1-R3) 作废**：隐藏拖拽柄 + 不自动切模式 → 撤销。

**新方向**：

- **R1'** — 撤销 `MainWindow.axaml` 三处 `entry-drag-handle` 的 `IsVisible` 绑定（已在中间实现中加上，现在删除），拖拽柄恢复**常显**。
- **R2'** — `MainWindow.Drag.cs:OnEntryDragPointerPressed` 的 `IsCustomSortMode: true` 硬守卫移除（改为只校验类型）；`MainWindowViewModel.Drag.cs:BeginDragReorder` 入口新增 `EnsureDragSortMode()`，非 custom 模式下先切到 custom 再开 session。`LauncherDragReorderWorkflow` 签名零改动。
- **R3'** — `EnsureDragSortMode` 触发 `SelectedSortMode = custom`，走 `OnSelectedSortModeChanged` 正常流程（保存 + ApplyFilter），下游 workflow 看到 `IsCustomSortMode=true`。

**新发现（扫描 bug 根因）**：

- **R14** — `AgentDiscoveryScanner.WindowsExtensions` 原 `["", ".exe", ".cmd", ".bat"]` 把 `""` 放首位。npm 全局装的 CLI 同时有无扩展名 Unix shell 脚本（`claude`）和 Windows `.cmd` shim（`claude.cmd`）。`""` 先匹中 Unix 脚本 → Windows 上 `Process.Start` 无法执行 → 扫不到。修复：`WindowsExtensions = [".exe", ".cmd", ".bat"]`，移除 `""`，遵循 PATHEXT 顺序。

**Acceptance Criteria (修订)**：

- [x] `MainWindow.axaml` 三处 `entry-drag-handle` 无 `IsVisible` 绑定（撤销）。
- [x] `MainWindow.Drag.cs:36` 守卫只校验 `MainWindowViewModel` 类型，无 `IsCustomSortMode: true`。
- [x] `MainWindowViewModel.Drag.cs:BeginDragReorder` 第一行调用 `EnsureDragSortMode()`。
- [x] `AgentDiscoveryScanner.WindowsExtensions = [".exe", ".cmd", ".bat"]`（移除 `""`）。
- [x] `where claude` 返回的 `claude.cmd` 能被扫到 + 版本 `2.1.206` 正确解析（已用 `cmd //c "claude.cmd --version"` 验证输出 `2.1.206 (Claude Code)`）。
- [x] `tools/verify.ps1` PASS，测试数 507。
- [x] 用户体感：任意排序模式下长按拖拽柄 → 立即开始拖拽（非 custom 模式自动切到 custom）；Discover 对话框扫到 claude/codex。
