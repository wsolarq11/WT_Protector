# Add SingleInstanceLock (Mutex + Named Pipe)

## Goal

调查并定案：是否需要添加 Named Mutex + Named Pipe IPC 来切断 S11（多进程文件竞争）。

## 调查结论：Mutex 已存在且充分，无需 Named Pipe IPC（S11 已切断）

审查 `src/avalonia-launcher/src/WTLauncher.App/Program.cs` 后确认：单实例锁**已经实现**并切断 S11。

### 当前实现事实（`Program.cs:71-105`）

```csharp
_instanceMutex = new Mutex(false, @"Local\WTLauncher_ConfigMutex", out bool createdNew);
if (!_instanceMutex.WaitOne(0))
{
    _instanceMutex.Dispose();
    _instanceMutex = null;
    ShowAlreadyRunningMessage();
    return false;
}
// AbandonedMutexException 处理：前实例崩溃后当前进程接管
// UnauthorizedAccessException / WaitHandleCannotBeOpenedException：best-effort 容错
```

| 特性 | 状态 |
|------|------|
| Named Mutex（`Local\WTLauncher_ConfigMutex`） | ✅ 已实现 |
| 非阻塞尝试获取（`WaitOne(0)`） | ✅ 已实现 |
| AbandonedMutexException 处理（前实例崩溃接管） | ✅ 已实现 |
| 权限/命名空间异常容错（best-effort 不阻断启动） | ✅ 已实现 |
| 第二实例 MessageBox 提示 | ✅ 已实现 |
| 进程退出时释放 mutex（finally + ReleaseMutex + Dispose） | ✅ 已实现 |

### S11 切断判定

**S11（多进程文件竞争）已由 mutex 完全切断**：第二实例无法获取 mutex → 立即退出 → 不会调用 `LauncherStore.Save` → 不会与第一实例产生文件交错。

### Named Pipe IPC 的现代实践调研（2025）

| 来源 | 共识 |
|------|------|
| [Medo64 — Single Instance Application for .NET 6/7](https://medo64.com/posts/single-instance-application-for-net-6-or-7) | Mutex 用于检测；Named Pipe 用于**转发命令行参数**到第一实例 |
| [Microsoft Learn — Named Pipes for IPC](https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication) | Named Pipe 是 IPC 的标准机制 |
| [AutoIt Consulting — Single Instance WinForm](https://www.autoitconsulting.com/site/development/single-instance-winform-app-csharp-mutex-named-pipes) | Mutex + Pipe 用于参数转发 + 窗口激活 |

**关键判断**：Named Pipe IPC 在所有现代实践中都是用于**转发命令行参数**（如双击文件打开、CLI 命令转发）。WT Launcher 当前**没有任何需要转发的命令行参数**：

- `Main(string[] args)` 直接把 args 透传给 Avalonia `StartWithClassicDesktopLifetime`（处理标准桌面生命周期，无自定义参数解析）
- 没有 `--open <file>` / `--launch <id>` / `--config <path>` 等需要转发的参数
- `LaunchPlan.CommandLine`（grep 命中的）是 wt.exe 子进程命令，不是 WTLauncher.exe 自身的参数

**结论**：没有参数转发需求 → 没有 IPC 业务驱动 → 添加 Named Pipe 是投机性抽象（ponytail ultra 禁止）。

### ROI 分析

**全量 Named Pipe IPC 实现成本：**
- 新增 `NamedPipeServerStream` 异步监听循环
- 新增 `NamedPipeClientStream` 在第二实例中发送
- 跨线程 UI 调度（收到 IPC 消息后激活第一实例窗口）
- 协议定义（payload schema、错误处理、超时）
- 至少 5 个新测试
- 引入跨平台兼容性顾虑（medo64 文章指出 `PipeOptions.CurrentUserOnly` 在 .NET 6+ 才能跨平台工作）

**实际收益：**
- ❌ 不切断任何当前 bug（mutex 已经切断 S11）
- ❌ 没有参数需要转发
- ❌ 没有"打开文件"工作流
- ⚠️ "激活已有窗口" 是唯一潜在收益（当前 MessageBox 提示用户手动切换）—— ROI 极低，用户已经在屏幕上看到自己的窗口

**Ponytail ultra: 不做。**

## Requirements

- **无代码变更**。
- 更新 spec 文档：明确 SingleInstanceLock 由 Mutex 实现，IPC 暂不引入。
- 更新父任务 PRD：将 S11 标记为"已由现有 Mutex 切断，无 IPC 需求"。

## Acceptance Criteria

- [x] Program.cs 现有 mutex 实现审查完成（7 项特性全部具备）
- [x] 命令行参数需求审查完成（无可转发参数）
- [x] 2025 现代 IPC 实践调研完成（Mutex + Pipe 用于参数转发，本项目无此需求）
- [x] spec 文档更新：记录 mutex 单实例锁设计 + IPC 拒绝理由
- [x] 父任务 `prd.md` 更新：S11 标记为已切断
- [x] `dotnet build` 0 警告 0 错误
- [x] `dotnet test` 451 测试通过
- [x] `tools/verify.ps1` 16 项检查通过

## Notes

- 这是"调查并拒绝扩展"型任务：mutex 充分，IPC 投机。
- 真正需要 IPC 的场景：未来若加入"双击 `.wtlauncher` 文件打开"或"命令行 `WTLauncher.exe --launch <id>`"工作流，届时再添加 Named Pipe。当前不引入。
- 现有 mutex 的 `Local\` 前缀是正确的选择（单用户单会话）；`Global\` 仅在跨会话场景下才需要，本项目不适用。
