# WT 生态多启动配置数据模型调研

日期：2026-07-11
范围：Windows Terminal、Cmder/ConEmu、Tabby、Alacritty、WezTerm、Fluent Terminal、Hyper
核心问题：**"同一 CLI（codex/claude）× N 个工作目录"** 怎么建模？

---

## 1. Windows Terminal — profile + startupActions

**数据模型**：`profiles.list[]` 每项是一个 profile（`name` / `commandline` / `startingDirectory` / `guid` / `icon` / `fontFace` …），`profiles.defaults` 提供继承基线。`schemes[]` 是配色盘。顶层 `defaultProfile` 是 GUID 引用。第三方可通过 **JSON Fragment Extensions**（`%LOCALAPPDATA%\Microsoft\Windows Terminal\Fragments\<App>\*.json`）向用户配置注入 profile；Dynamic Profile Generators 内建扫描 WSL 发行版 / Azure Cloud Shell / PowerShell（`disabledProfileSources` 可关）。

**能否"一命令 N 目录"？** **否。** WT 的 profile 是 **1:1 命令+目录** 的扁平记录，`startingDirectory` 是字符串字段，不支持参数化模板。要做"codex 在 3 个项目目录"必须复制 3 个 profile 实例，每个硬编码不同 `startingDirectory`。这是 [#15721](https://github.com/microsoft/terminal/issues/15721)（200+ profile 用户求"分组"）和 fragment 扩展的真实痛点。

**批量启动？** **半解。** `startupActions` 是 `wt` 命令行语法的字符串拼接（`new-tab -p ProfileA -d C:\dir1 ; split-pane -p ProfileB -d C:\dir2 ; ...`）。这确实能在**单次启动**里拼出"同一 CLI 多目录多 pane"的布局，但它：
- 是**启动时命令行**，不是可命名的可复用 profile；
- 字符串拼接，无校验，无模板变量；
- 不能在 GUI 里选一个"preset"就展开。

**复用机制**：`defaults` 继承 + fragment 注入。无 template / mixin / 参数。

## 2. Cmder / ConEmu — task + 嵌套 task（最接近我们要的）

**数据模型**：`<key name="TaskN">` 在 `user-conemu.xml` 的 `<key name="Tasks">` 下。每个 task = `{name}` + `Hotkey` + `Task parameters`（`/dir "C:\..." -icon ... -single`）+ `Commands`（多行，每行一个 console，空行分隔）。`/dir` 就是启动目录参数，`-new_console:t:"title"` 控制标签名。

**一命令 N 目录？** **是，且是七者中唯一原生支持。** ConEmu 支持**单层 task 嵌套 + 参数透传**（[官方文档](https://conemu.github.io/en/SettingsTasks.html)原话）：

```
{MyTool} parameter-set-1
{MyTool} parameter-set-2
{MyTool} parameter-set-3
```

定义 `{MyTool}` 一次，在 `{ToolSet}` 里引用 N 次传不同参数。这正是"模板 + N 实例"。`/dir` 通过 Task parameters 注入，**参数化目录**成立。

**批量启动**：一行 task 就是批量；多行 = 多 console（可分屏）。task 可绑 hotkey、可设为默认 task、可进 Windows 任务栏 jump list。

**复用机制**：单层嵌套（一层深度限制）+ `-new_console` 开关透传 + `Default Tasks` 自动生成器（扫描已知 shell）。

## 3. Tabby — profile + group + isTemplate

**数据模型**：`PartialProfile` 含 `group`（字符串分组名）、`isTemplate`（bool）、`isBuiltin`、`options`（自由对象）。`ProfileGroup` 支持嵌套（[#9210](https://github.com/Eugeny/tabby/issues/9210) 长期求嵌套树，[#10788](https://github.com/Eugeny/tabby/issues/10788) 求 profile 树视图）。`getProfileDefaults` 返回 global + group defaults 的合成视图（**双层继承**：global → group → profile）。

**一命令 N 目录？** **部分。** `isTemplate` 字段存在但**语义是"不在 launcher 列表里显示"**，不是真正的"参数化实例工厂"。要做 N 目录仍需复制 N 个 profile 放进同一 group。group 做的是**组织**（文件夹折叠），不是**参数化**。

**批量启动**：可"保存当前 tab 布局为 profile"，下次一键恢复 — 这是**布局快照**，不是声明式 group 启动。

**复用机制**：group defaults 继承。无参数透传。

## 4. Alacritty — 极简，无 profile

**数据模型**：单个 TOML/YAML 配置（`alacritty.toml`），`[general]` `import = ["theme.toml", "mac.toml"]` 做文件级合并（后者覆盖前者）。`working_directory` 是单值。**没有 profile 概念**，没有 task。

**一命令 N 目录？** **否。** 要多目录要么开多个进程传不同 `--working-directory` CLI 参数，要么靠外层 shell multiplexer（tmux/zellij）。`import` 是文件级 mixin，不是实例化。

**批量启动**：无内建。靠外部脚本。

**复用机制**：`import` 文件合并。

## 5. WezTerm — Lua 脚本 = 任意参数化

**数据模型**：`wezterm.lua` 返回 config table。`launch_menu` 是 `[{label, args, cwd, set_environment_variables}]` 列表。workspace / window / tab / pane 四层。`SwitchToWorkspace { name=..., spawn={args, cwd} }` 可绑定快捷键一键切到命名工作区并启动其默认程序。

**一命令 N 目录？** **是，最灵活。** Lua 是图灵完备的：

```lua
for _, proj in ipairs({"proj-a", "proj-b", "proj-c"}) do
  table.insert(config.launch_menu, {
    label = "codex:" .. proj,
    args = {"codex"},
    cwd = "/home/me/" .. proj,
  })
end
```

一行 `for` 循环 = N 个目录实例。参数化完全交给脚本。

**批量启动**：`wezterm cli spawn --workspace X --cwd Y PROG`（CLI），或 `Multiple{SpawnTab..., SplitPane...}` 键绑定（Lua）。可脚本化任何启动编排。

**复用机制**：Lua 函数 + `wezterm.config_builder` + 文件拆分。无声明式模板，但脚本即模板。

## 6. Fluent Terminal — profile 列表

**数据模型**：JSON 配置，`profiles[]` 含 `name` / `commandline` / `startingDirectory` / `args` / `tabTitle` / `icon` / `colorScheme`。结构接近 WT 但更扁平。无 group、无 template、无 nested。

**一命令 N 目录？** **否。** 同样需要 N 个 profile 实例。

**批量启动**：无内建。外部 `fluentterm.exe` + 参数。

## 7. Hyper — 极简 + 插件

**数据模型**：`.hyper.js` 单文件，`config.shell` + `config.shellArgs` 单值。无内建 profile/task。会话由**插件**提供：`hypercwd`（新 tab 继承当前目录）、`hyper-pane`（分屏）、`hyperterm-visor`（全局 hotkey）。

**一命令 N 目录？** **否**（核心）。靠插件或外部脚本。

**批量启动**：无。

**复用机制**：npm 插件生态。配置是 JS，理论上可循环生成，但无 profile 抽象。

---

## 对比表

| 工具 | 模型 | 一命令 N 目录 | 批量启动 | 目录作参数 | 复用机制 |
|------|------|--------------|---------|-----------|---------|
| Windows Terminal | profile-centric（扁平） | **否** | startupActions 字符串 | `startingDirectory` 字段 | defaults 继承 + fragment |
| Cmder/ConEmu | **task-group + 嵌套** | **是**（嵌套 task 传参） | task 多行命令 | `/dir` task 参数 | 单层嵌套 + 透传 |
| Tabby | profile + group + template flag | 部分（需复制） | 布局快照 | `cwd` 字段 | group defaults 继承 |
| Alacritty | flat config + import | 否 | 无 | `working_directory` | import 文件合并 |
| WezTerm | **script-driven（Lua）** | **是**（循环生成） | Lua/CLI spawn | `cwd` 字段 | Lua 函数 |
| Fluent Terminal | profile-centric | 否 | 无 | `startingDirectory` | 无 |
| Hyper | shell 单值 + 插件 | 否 | 无 | 无 | 插件 |

三种范式：**profile-centric**（WT/Fluent/Tabby/Alacritty）、**task-group**（Cmder）、**script-driven**（WezTerm）。

---

## 关键洞察

### 我们是否在重复造轮子？

**部分是，部分否。** 三个分层：

1. **WT 的 `startupActions` + `splitPane` 已能做"批量启动 + 同一 CLI 多目录多 pane 的单次布局"** — 这是 WT 内建的解。但它**不是可命名、可复用、可参数化的 profile**，而是**一次性命令行字符串**。用户痛点正在此：每次启动要手敲或写 `.bat`（见 [novacare 博客](https://blog.novacare.no/advanced-setup-for-windows-terminal) 的 `start-windows-terminal.bat` 模式）。

2. **Cmder 的嵌套 task 是"模板 + N 实例"的正解**，但 Cmder 的 UI 和配置生态（XML、ConEmu 内部）对 2026 年的用户是负担。WT 抛弃了 task 模型回到扁平 profile，**丢掉了参数化能力** — 这是回归。

3. **WezTerm 的 Lua 是终极灵活**，但门槛极高，且整个配置变成代码（不可视化、难分享）。

**我们 launcher 的定位空位**：在 WT 之上提供 **"Cmder 的参数化 task 模型 + WT 的现代 UI"** — 即一处定义命令模板（`codex`），N 个工作目录实例，一键批量启动展开成 `wt new-tab ... ; split-pane ...`。这不是重复造轮子，是**补 WT 故意不做的层**（profile 分组 + 参数化 + 预设批量启动）。我们的 `commandTemplates`（`$key` 引用）+ `presets`（批量勾选集合）数据模型正是这个空位的声明式表达。

### "目录扫描器"概念

调研中**未发现任何终端有"扫描项目目录自动生成 profile"的内建功能**。最接近的：
- WT 的 **Dynamic Profile Generators** 只扫描**已安装的 shell**（WSL/Azure/PowerShell/Git Bash/Anaconda/Cygwin），**不扫工作目录**。
- ConEmu 的 **Default Tasks** 同理，扫工具不扫项目。
- 社区方案都是**外部脚本**：PowerShell 扫 `D:\projects\*` 生成 `settings.json` profile 列表（Stack Overflow 有多个问答）。

**结论：用户提的"地址扫描器"（扫目录自动生成 launcher 条目）在生态里是空白**，是我们 launcher 可差异化的真功能。现有做法是手写脚本，无产品化、无可视化、无原子写入。我们的 `LauncherStore`（原子 JSON）+ 扫描器 + 去重校验，是有价值的非重复发明。

### 设计启示给我们的 launcher

- **保留 `commandTemplates` + `$key` 引用** — 这是 Cmder task 嵌套的现代化等价物，WT 没有的层。
- **保留 `presets`（批量集合）** — 这是 Cmder "一行 task 多 console" 的声明式等价，WT `startupActions` 字符串的可维护替代。
- **扫描器作为差异化功能** — 生态空白，值得做，但要明确是"项目目录扫描"而非"shell 扫描"（WT 已占后者）。
- **不要做**：另一套配色 / 字体 / shell 检测（WT defaults + fragment 已覆盖）。专注"命令模板 × 目录实例 × 批量预设"三角。

---

## 参考来源

- [Windows Terminal Panes — Microsoft Learn](https://learn.microsoft.com/en-us/windows/terminal/panes)
- [Windows Terminal General Profile Settings — Microsoft Learn](https://learn.microsoft.com/en-us/windows/terminal/customize-settings/profile-general)
- [Windows Terminal Dynamic Profiles — Microsoft Learn](https://learn.microsoft.com/en-us/windows/terminal/dynamic-profiles)
- [Windows Terminal JSON Fragment Extensions — Microsoft Learn](https://learn.microsoft.com/en-us/windows/terminal/json-fragment-extensions)
- [Ability to group profiles #15721 — microsoft/terminal](https://github.com/microsoft/terminal/issues/15721)
- [Split Pane Startup Profiles #19502 — microsoft/terminal Discussion](https://github.com/microsoft/terminal/discussions/19502)
- [Advanced setup for Windows Terminal — novacare blog](https://blog.novacare.no/advanced-setup-for-windows-terminal)
- [Settings › Tasks page — ConEmu](https://conemu.github.io/en/SettingsTasks.html)
- [Tasks: Set up your favourite shells — ConEmu](https://conemu.github.io/en/Tasks.html)
- [Default Tasks — ConEmu](https://conemu.github.io/en/DefaultTasks.html)
- [How can I specify multiple startup tasks in Conemu? — Super User](https://superuser.com/questions/669563/how-can-i-specify-multiple-startup-tasks-in-conemu)
- [ProfilesService — Tabby docs](https://docs.tabby.sh/classes/ProfilesService.html)
- [Nested groups in connection manager #9210 — Eugeny/tabby](https://github.com/Eugeny/tabby/issues/9210)
- [Tabby features](https://tabby.sh/about/features)
- [Alacritty TOML configuration](https://alacritty.org/config-alacritty.html)
- [Alacritty — ArchWiki](https://wiki.archlinux.org/title/Alacritty)
- [Multiplexing — WezTerm](https://wezterm.org/multiplexing.html)
- [launch_menu — WezTerm](https://wezterm.org/config/lua/config/launch_menu.html)
- [spawn CLI — WezTerm](https://wezterm.org/cli/cli/spawn.html)
- [default_cwd — WezTerm](https://wezterm.org/config/lua/config/default_cwd.html)
- [Configuration files — WezTerm](https://wezterm.org/config/files.html)
- [SwitchToWorkspace — WezTerm](https://wezterm.org/config/lua/keyassignment/SwitchToWorkspace.html)
- [Allow custom dynamic profile generator sources #7805 — microsoft/terminal](https://github.com/microsoft/terminal/issues/7805)
- [Hyper™ — 官网](https://hyper.is)
- [awesome-hyper — 插件列表](https://github.com/bnb/awesome-hyper)
- [4 reasons Windows Terminal profiles are better than old-school shortcuts — XDA](https://www.xda-developers.com/reasons-windows-terminal-profiles-better-old-school-shortcuts)
