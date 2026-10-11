Status: Evidence
Purpose: 放弃 GUI 的退路方案审计（2026-07-10），非行动方案。
Update when: 不更新（仅当"放弃 GUI"重新成为真实选项时）。

# Architecture Zero：彻底去掉启动器

> 定位（2026-07-10）：应急记录，非行动建议。v2 AI Agent 发现落地后 GUI 产品价值上升；本文保留为
> "若放弃 GUI"的退路审计，不作为迁移计划。

## 根因

WT Protector 是 C#/Avalonia GUI 应用，其唯一职能是拼装 `wt.exe` 命令行，Windows Terminal 已原生具备该能力。
所有 bug 与过度设计都源自一个根决策：围绕命令行任务建 GUI 框架。

## Windows Terminal 已原生提供

| 能力 | WT Launcher | Windows Terminal 原生 |
| --- | --- | --- |
| 不同目录启动标签页 | 有 | 有 `wt -d <dir>` |
| 单窗口多标签 | 有 | 有 `wt new-tab -d <dir>` |
| 分屏 | 无 | 有 `wt split-pane` |
| 记忆目录的 profile | 无 | 有 `profiles[].startingDirectory` |
| 启动工作区布局 | 无 | 有 `startupActions` |
| 命名窗口 | 无 | 有 `wt -w <name>` |

## 三个备选方案

### A：仅用 Windows Terminal Profiles（零代码）

每条启动项映射为一个 profile，启动用 `wt -p "<name>"`。
优点：零代码、零维护。缺点：无运行时多选批量启动、无模板变量、无 GUI。

### B：PowerShell 脚本启动器（约 80 行）

单个 `launch.ps1` 读 `launcher.json` 拼装 `wt.exe` 命令：

```powershell
param([string]$Preset = "core-projects")
$config = Get-Content "config/launcher.json" | ConvertFrom-Json
$preset = $config.presets | Where-Object { $_.id -eq $Preset }
$entries = $config.entries | Where-Object { $preset.entryIds -contains $_.id }
$wtArgs = foreach ($entry in $entries) {
    $cmd = $config.settings.commandTemplates."$($entry.command -replace '^\$','')"
    "-d `"$($entry.directory)`" pwsh -NoExit -Command `"$cmd`""
}
Start-Process -NoNewWindow wt.exe -ArgumentList ($wtArgs -join " ")
```

保留 `config/launcher.json` 与 `launch.ps1`；删除 `src/`、`tools/`、`tests/`。

### C：最小 .NET 控制台（约 200 行）

单个 `.cs`，无 Avalonia/XAML/NuGet；读 JSON、打印 `wt.exe` 命令行，交给 `Invoke-Expression` 或 `Start-Process`。

## 消除与保留

| 项 | 规模（时点估计） | 复杂度来源 |
| --- | --- | --- |
| Avalonia UI | 约 8000 行 | XAML、视图、code-behind、绑定 |
| ViewModel | 约 4000 行 | MVVM、partial、可观察集合 |
| 值对象（EntryId 等） | 约 500 行 | 校验、规范化、Result<T,E> |
| 持久化（JSON + LiteDB） | 约 1000 行 | 双后端、迁移、快照/回滚 |
| 构建系统 | 约 200 行 | csproj、sln、restore、CI |
| 测试 | 时点实测 7509 行 | xUnit、418 属性、63 文件 |

保留：`config/launcher.json`（约 100 行）+ `launch.ps1`（约 80 行），总量约 180 行。

> 规模为 2026-07-06 粗略估计，仅测试行为实测；不逐项复核。

## 风险

| 风险 | 缓解 |
| --- | --- |
| 模板变量展开（`$codex` → 完整命令） | B 方案 PowerShell 替换，或 A 方案 profile commandline |
| 运行时多选批量启动 | B 方案 `-Preset` 参数；`startupActions` 覆盖固定工作区 |
| 配置迁移 | 配置已存在，只需转换 |
| 失去搜索/过滤/卡片 UI | 这是启动器的功能而非启动本身的功能；若不可少则保留 C# 应用 |
