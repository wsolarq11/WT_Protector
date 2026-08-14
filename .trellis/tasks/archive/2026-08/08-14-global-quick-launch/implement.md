# 全局快速调用 — Implement

## Order

1. Core: hotkey parser + quick match score + settings fields。
2. Core tests。
3. QuickLaunchViewModel + QuickLaunchWindow。
4. Win32HotkeyService + Win32TrayIcon。
5. App wiring（热键/托盘/关闭即隐藏）。
6. XAML/VM/settings contract tests。
7. build + dotnet test + Avalonia verify。

## Verification Gates

- `dotnet build ... -warnaserror`
- `dotnet test ... -c Release --no-restore`
- `src/avalonia-launcher/verify.ps1`
- 手动冒烟：热键唤起、Enter 启动、Esc 关闭、托盘打开/退出。

## Rollback

- 新功能全部增量；回滚 = 删除新增文件/窗口接线/设置字段。
- 不改现有启动链路的预检、确认、回执契约。