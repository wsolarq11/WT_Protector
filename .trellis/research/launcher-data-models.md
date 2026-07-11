# 主流快速启动器核心数据模型调研

调研对象：Raycast / Alfred / ueli / uTools / Fluent Search / Keypirinha。
焦点：**数据模型架构**（不是 UI），尤其是"启动项 vs 命令/目录/参数"的关系组织方式，
以及各工具如何处理"同一命令对多个目录"的重复问题。

调研日期：2026-07-11。
本地背景：当前 `config/launcher.json` 有 20 个 entry，其中 15 个 `"command":"$codex"`、
3 个 `$claude`、1 个 `$opencode`、1 个裸 `codex`。命令已用 `$key` 模板引用做了去重
（`commandTemplates`），但 entry 仍是一对一绑定 `(name, directory, $codex)`。

---

## 1. Raycast（macOS）—— 三件套：Quicklinks / Script Commands / Snippets

Raycast 内部不是一个统一模型，而是三个**并列的第一公民实体**，各自有自己的字段集：

**Quicklinks**（最贴近我们的 entry 概念）。扁平列表，每个 Quicklink 字段固定且少：
`name`、`link`（必填，可以是 URL / 文件路径 / 文件夹路径 / deeplink）、`iconName`、
`openWith`（可选，指定用哪个 app 打开）、`tags`（可选数组）。没有"命令"字段——
`link` 本身既是目标也是动作。**关键去重机制**：`link` 支持 **Dynamic Placeholders**，
例如 `https://google.com/search?q={argument name="query"}`，或
`~/Projects/{argument name="repo"}` + `openWith: VS Code`。一个 Quicklink + 一个 `{argument}`
就能覆盖"N 个项目用同一个 app 打开"——但 Raycast 的现实是用户仍然**为每个常用目录建一个
Quicklink**，因为带 argument 的 Quicklink 每次都要手输路径，不如预制快。Tags 是唯一的
分组维度，没有层级/文件夹。

**Script Commands**（命令为一等公民）。每个脚本是磁盘上的一个文件，顶部用注释声明元数据
（`@raycast.title`、`@raycast.argument1 {"type":"text","placeholder":"..."}`）。
脚本目录被 Raycast 扫描，每个文件 → 一个可搜索命令。**这里命令确实脱离 entry 单独 CRUD**：
你改一个脚本，所有引用它的命令同步。但 Script Command 不绑定"目录"概念——它是一个可执行体，
不是"在某目录下跑某命令"的预设。

**Snippets**。纯文本替换（`@@email` → 你的邮箱），和启动无关，是第三个模型。用户多次在
r/raycastapp 抱怨 Snippets 不能分层、不能嵌套，要求"layers/folders"组织——被官方忽略。

"一键启动多个"（preset 等价物）：**没有原生 Quicklink 级别的批量启动**。Reddit 上
"Opening multiple projects at once"、"How to open multiple folders/files"、"Can Raycast
open multiple apps" 反复出现，官方答案都是"写个 Script Command 里跑多个 shell 命令"——
即把 preset 下沉到脚本里，不是数据模型层的一等公民。

反模式/吐槽：Quicklink 没有 command 概念，导致"15 个项目文件夹各开一个 VS Code Quicklink"
是常态，和我们的 `$codex × 15` 同病。Raycast 没解决，靠 `{argument}` 让用户接受"一个
Quicklink + 手输"或"15 个 Quicklink"二选一。

## 2. Alfred（macOS）—— Workflows = Triggers + Actions 的 DAG，变量是一等公民

Alfred 是这次调研里**模型最成熟、最贴近"command-centric"**的一个。核心是 Workflow Editor
画布上的有向图，对象分七类：**Triggers**（Hotkey / Snippet / File Action / External /
Universal Action）、**Inputs**（Keyword + query）、**Actions**（Open / Run Script /
Terminal Command 等）、**Automations**、**User Interface**、**Utilities**（Arg & Vars、
Filter、Split Arg to Vars）、**Outputs**。

关键点：**Trigger 和 Action 是解耦的**。一个 Action（比如"在 Terminal 跑 codex"）可以被
N 个 Trigger 各自带不同变量触发。机制是 **Workflow Variables**：
- 在 Workflow Configuration 里定义 `repo_path` 等变量；
- 每个 Trigger / Hotkey 对象可以**覆盖**变量的默认值（`{const:myvariable}` 语法）；
- Run Script action 通过环境变量读取，或 Script Filter JSON 里动态 set。

这就是 Alfred 解决"同一命令对多目录"的原生答案：**一个 Action + N 个带不同 env var 的
Trigger**。没有"N 个 entry 各自带 codex 命令"的重复——命令实体一份，变量化的 Trigger 是
薄的入参层。这正好是我们 `commandTemplates.codex` + 每 entry 一个 `directory` 想达到的形态，
差别只在于我们把变量 (`directory`) 和触发点 (entry name) 焊死在 entry 里，Alfred 把它们
拆成了 Trigger 对象和 Action 对象两层。

"一键启动多个"：天然支持——一个 Trigger 连多个 Action 就是 preset。变量在对象间流转是
Utilities（Arg & Vars）的职责，是显式的图边。

反模式：DAG 对非技术用户太重，社区靠 `.alfredworkflow` 包分享。但"重复命令"问题在 Alfred
里几乎不存在，因为模型本身就是 command-centric 的。

## 3. ueli（跨平台开源）—— 扁平 plugin 列表，Custom Commands 是最薄模型

ueli（github.com/oliverschwendener/ueli）是 Electron + React，设置全在 `settings.json`。
核心是**Search Plugin** 概念：Applications / Files / Browser Bookmarks / Commands /
Custom Commands / Simple Folder Search / Search Engines / Web Search 等，每个 plugin 是
一个独立的检索源，扁平并列，没有层级。

**Custom Commands** 字段极简：`name`、`prefix`（触发词）、`executionCommand`（要跑的命令行）、
`icon`。本质上和我们的 entry 几乎同构——`(name, command)` 对。没有"目录"一等公民概念，
没有模板/变量引用。要"同一命令多目录"就是复制 N 份 Custom Command 改 `executionCommand`
里的路径，**和我们 `commandTemplates` 出现前的状态一样**。

"一键启动多个"：没有。ueli 的定位是检索器（搜应用、文件、书签），不是工作集管理器。
反模式：模型太薄，社区 issue（#109 "How to create custom commands"）反复有人问怎么复用，
答案是"自己复制"。

## 4. uTools（跨平台）—— "一切皆插件" + 特征词 + 上下文匹配

uTools 数据模型**以插件为一等公民**，不是 entry。`plugin.json` 声明插件的 `features`：
每个 feature 有 `cmd`（特征词数组）、`args`（接受的输入类型：文本/图片/文件/正则）、
`mode`（list / none / regex）。用户在主输入框打特征词进入插件，插件内部用 React 渲染自己的 UI。

**"快捷命令"插件**最接近我们的模型：JSON 配置匹配 `features.cmds`，每条 = 一个
`(关键词 → 动作)` 映射，动作可以是 shell 命令、打开网址、打开文件。"项目启动器"类插件
（uTools 专题 #14）提供 startup script config + 运行日志 + Git 状态——但这是**第三方插件
自己定义的模型**，uTools 本体不规定。

"同一命令多目录"：uTools 的解法不是数据模型层，而是**上下文匹配**——插件可以声明匹配
窗口标题/文件夹/文件类型（"匹配窗口或文件夹「Claude Code 打开」"是近期更新），让"当前
焦点目录"成为隐式参数。这是 list 里唯一用 **context-as-input** 而非 explicit-arg 的模型。

反模式：插件之间模型不统一，用户得为每种需求装不同插件，且插件数据散落各处。优势是
扩展性极强（更新日志显示正在往"AI Agent 工具平台"演化，插件能力以 `tools` 暴露给 Agent）。

## 5. Fluent Search / Keypirinha / Cerebro

**Fluent Search** 用 **Search Apps** 模块化（Apps / Files / Commands / Browser / Web 等），
每个 Search App 是独立检索源，可独立 enable/disable、可分配 global hotkey 做"jump in"。
**Search Tags** 是显式前缀过滤（`files<Tab> query` 只搜文件）。Custom Commands 模块字段和
ueli 类似（name + command），没有目录/模板一等公民。**工作集/聚合概念：没有**——它把"工作集"
外包给 Windows 本身的窗口/标签检索（搜 running apps、browser tabs），而不是用户预定义的 preset。

**Keypirinha** 用 **INI 配置 + Packages**。每个 Package（Apps / Files / WebSearch / Bookmarks /
URL / Calc 等）有自己的 `.ini`，`[item]` 段定义单个条目，`keyword =` 给 item 绑定触发词。
模型扁平、文件级分组（一个 Package 一个 ini），**"Naturally associate a keyword to a specific
item"** 是它的核心——关键词 → item 一对一，没有命令复用。变量要在 `Keypirinha.ini` 的 `[var]`
段集中声明，可被任何配置引用——这是它唯一接近"模板"的机制，但变量是全局常量，不是 per-entry
参数。

**Cerebro**（已基本停滞）：Electron + 插件，模型类似 ueli 但更薄，略过。

---

## 对比表

| 工具 | 模型范式 | 命令是否一等公民 | 目录是否一等公民 | "同命令多目录"机制 | 一键启动多个 |
|------|---------|----------------|----------------|------------------|------------|
| **Raycast** | entry-centric（Quicklink）+ command-centric（Script Command）双轨 | Script Command 是；Quicklink 不是（link 即动作） | 否（路径是 link 字符串的一部分） | `{argument}` 占位符 + 用户手输，或复制 N 个 Quicklink | 无原生（写脚本） |
| **Alfred** | **command-centric DAG**（Trigger×Action 解耦） | 是（Action 可复用） | 否（通过 Workflow Variable 注入） | **Workflow Variables + per-Trigger 覆盖** | 原生（一 Trigger 多 Action） |
| **ueli** | entry-centric（plugin 扁平列表） | 否 | 否 | 复制 N 份 Custom Command | 无 |
| **uTools** | plugin + 特征词 + 上下文 | 插件是；快捷命令条目不是 | 否（靠上下文匹配窗口/文件夹） | **上下文匹配**（当前焦点目录作为隐式输入） | 插件内自行实现 |
| **Fluent Search** | module-centric（Search App） | 否（Custom Commands 薄） | 否 | 复制 N 份 | 无（外包给系统窗口检索） |
| **Keypirinha** | package + INI（keyword→item） | 否（keyword 绑死 item） | 否 | 复制 N 份 item | 无 |
| **WT Protector（当前）** | entry-centric + `$key` 模板引用 | 半个（`commandTemplates` 是命令表，但 entry 仍各带 `$codex`） | 否（`directory` 是 entry 字段） | `commandTemplates` 已做命令去重，**目录仍是每 entry 一份** | 有（preset） |

---

## 关键洞察：是否有人解决了我们的问题？

我们的问题是：**15 个 entry 共享同一个 `codex` 命令，每个 entry 只差 `directory`**。
`commandTemplates` 已经把命令侧去重了（改 `codex` 模板一次，15 个 entry 同步），剩下的重复
是"15 行 entry 各自带一个不同的 `directory` 字段"。

**没有任何调研对象的现成数据模型直接解决"命令复用 + 目录仍要每条列"的组合**，但有两个
可借鉴的方向：

1. **Alfred 路线（Trigger×Action 解耦 + 变量化）**。把 entry 拆成两层：
   - Action 层 = 命令模板（已有 `commandTemplates`）；
   - Trigger 层 = `(name, directory)` 对，引用模板 + 注入 `directory` 作为变量。
   现状我们已经是这个形态了——entry 的 `directory` 就是注入到 `$codex` 的变量。
   **结论：我们当前模型已经等价于 Alfred 的单 Action + N 变量 Trigger，只是没有显式命名。**
   "重复"是表层的（15 行 JSON），不是结构性的（命令实体只有一份）。

2. **Raycast 路线（占位符 + 动态输入）**。一个 entry + `{argument}` 让用户运行时输目录。
   代价是每次手输路径，收益是 entry 数量从 15 降到 1。适合"偶尔切换的项目"，不适合
   "每天高频固定项目"——这正是我们 15 个 entry 存在的理由。

3. **uTools 路线（上下文匹配）**。不维护 entry 列表，让 launcher 感知"当前窗口/焦点目录"
   自动决定在哪跑 `codex`。最激进、最省 entry，但依赖系统能暴露焦点目录（Windows 上不总是
   可靠），且对"从冷启动开一个不在焦点里的项目"无能为力。

**对我们最实际的结论**：当前 `entry × commandTemplates` 模型在数据层已经是
**Alfred 单 Action + N Trigger 的等价物**，去重已发生在命令侧。剩下能做的不是改数据模型
（会破坏 preset / 搜索 / 视图排序），而是 **UI 侧的命令分组投影**——让 15 个 `$codex` entry
在 Cards/Details 视图里能按 `command` 维度折叠成"codex (15)"组，减少视觉重复，但底层
JSON 保持一对一。这是 Raycast 用 tags + 过滤解决的问题，Alfred 用画布折叠解决的问题，
不需要动我们的 schema。

---

## 参考来源

- [Quicklinks | Raycast Manual](https://manual.raycast.com/quicklinks)
- [Script Commands | Raycast Manual](https://manual.raycast.com/script-commands)
- [Inputs for Script Commands - Raycast Blog](https://www.raycast.com/blog/inputs-for-script-commands)
- [Dynamic Placeholders | Raycast Manual](https://manual.raycast.com/dynamic-placeholders)
- [Alfred Workflows Overview](https://www.alfredapp.com/help/workflows/)
- [Alfred Workflow Triggers](https://www.alfredapp.com/help/workflows/triggers)
- [Alfred Workflow Inputs](https://www.alfredapp.com/help/workflows/inputs)
- [Using Variables in Workflows - Alfred](https://www.alfredapp.com/help/workflows/advanced/variables)
- [Workflow Script Environment Variables - Alfred](https://www.alfredapp.com/help/workflows/script-environment-variables)
- [ueli GitHub repo](https://github.com/oliverschwendener/ueli)
- [ueli: How to create custom commands (issue #109)](https://github.com/oliverschwendener/ueli/issues/109)
- [uTools 基础文档 / 开发者文档](https://www.u-tools.cn/docs/developer/docs.html)
- [uTools 快捷命令插件](https://www.u-tools.cn/plugins/detail/%E5%BF%AB%E6%8D%B7%E5%91%BD%E4%BB%A4)
- [uTools 项目启动器专题 #14](https://www.u-tools.cn/plugins/topic/14)
- [uTools 更新日志](https://www.u-tools.cn/docs/guide/changelog.html)
- [Fluent Search Getting Started](https://fluentsearch.net/docs/Getting%20started)
- [Fluent Search Search Apps General](https://fluentsearch.net/docs/Search%20apps/General)
- [Keypirinha Configuration](https://keypirinha.com/configuration.html)
- [Keypirinha Glossary](https://keypirinha.com/glossary.html)
- Reddit 用户反馈：
  - [r/raycastapp: Opening multiple projects at once](https://www.reddit.com/r/raycastapp/comments/1rbvvn7/opening_multiple_projects_at_once)
  - [r/raycastapp: How to open multiple folders/files?](https://www.reddit.com/r/raycastapp/comments/1iofc59/how_to_open_multiple_foldersfiles)
  - [r/raycastapp: Can Raycast open multiple apps?](https://www.reddit.com/r/raycastapp/comments/18sa6qk/can_raycast_open_multiple_apps)
  - [r/raycastapp: Better snippets (layers/folders 请求)](https://www.reddit.com/r/raycastapp/comments/1h14mv9/better_snippets)
