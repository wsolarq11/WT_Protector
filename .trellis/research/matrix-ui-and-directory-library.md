# 调研报告：命令 × 目录 二维选择 UI & 文件管理器地址库设计

**日期**: 2026-07-11
**动机**: 本 launcher 的 `entries[]` 当前是 `{directory, command}` 扁平列表，已出现同一目录挂多个命令的现象（`Data_Acquisition_and_Analysis` + `CLAUDE_Data_Acquisition_and_Analysis` 共用一个目录，只是命令不同）。这本质是"命令 × 目录"的二维关系被强行压扁成一维。调研业界两件事：(A) 二维选择 UI 怎么做；(B) 文件管理器的"地址库/扫描器"做到哪一步了。

---

## A. 二维选择 UI — "选命令 + 选目录"

### A1. VSCode Multi-root Workspace

**数据模型**: `.code-workspace` 是一个 JSON 文件，顶层 `folders[]` 数组，每项 `{path, name}`。`path` 可绝对可相对；`name` 是显示别名（避免长路径污染 Explorer）。`settings`/`extensions`/`launch` 配置可写在同一文件顶层（workspace 级），也可在子文件夹的 `.vscode/`（folder 级）。变 `${workspaceFolder:FolderName}` 支持在配置里按文件夹名引用各自根。

**UI 模式**: **扁平多根** — Explorer 把每个 folder 当平级根显示，不做嵌套（社区 issue #246729 长期请求层级树，VSCode 一直拒绝，理由是简洁性 > 真实文件系统层级）。多根之间通过 `workspaceFolder` scope 隔离配置。

**关键决策**: VSCode 把"多目录"问题降级成"多根 folder 列表 + 每根独立配置"，**不引入命令维度**。命令由 `launch.json` 的 `configurations[]` / `compounds[]` 处理，是另一条独立轴。两轴正交：folder 决定"在哪"，launch config 决定"跑啥"。这正是我们 launcher 需要学的设计。

### A2. JetBrains Projects — Recent Projects + Project Group

**数据模型**: 每个项目一个 `.idea/` 目录，独立打开 = 独立窗口。Welcome 屏幕的 Recent Projects 是平铺列表，可右键建 **Project Group**（仅是 welcome 屏上的分组容器，不改变项目数据）。组可"All Projects in Group"一键全开。

**UI 模式**: **列表 + 一次性分组** — 组不是工作区，打开后仍是 N 个独立窗口（不像 VSCode 把多 folder 装一个窗口）。JetBrains 的"workspace"概念（IDEA 2024+）才真正支持多项目同窗，但本质是"附加项目"模式，主项目仍是宿主。

**关键决策**: JetBrains 选择"项目 = 窗口"强绑定，组只是启动器层快捷方式。这反例告诉我们：**把"目录"和"命令"绑死成一个对象（=项目=窗口）会让二维选择退化**。我们 launcher 的 entry 已经更灵活：entry = (dir, cmd) 对，可以同目录多 entry。

### A3. tmuxinator — YAML 定义多窗口/多目录/多命令

**数据模型**: 典型 `~/.config/tmuxinator/<name>.yml`:
```yaml
name: dev
root: ~/projects/foo
windows:
  - editor:
      layout: main-vertical
      panes:
        - vim
        - guard
  - server:
      root: ./api        # 可覆盖 root
      commands:
        - bundle exec rails s
```
顶层 `root` 是默认目录，每个 window/pane 可 `root:` 覆盖；每个 pane 可挂多个 command。**这是"目录 + 命令"二维最显式的建模** — pane = (cwd, command) 二元组，windows 是 pane 的容器。

**UI 模式**: 无 GUI，纯声明式。`tmuxinator start <name>` 一键重建整个会话。`tmuxinator edit <name>` 调 `$EDITOR` 改 YAML。配合 fzf 做 `tmuxinator start $(tmuxinator list | fzf)`。

**关键决策**: tmuxinator把二维关系写进 YAML 结构本身（pane 同时持 cwd + command），**不靠矩阵 UI**。可借鉴：entry 的 `(dir, cmd)` 二元组天然存在，UI 可以不暴露矩阵，只要数据模型正交即可。

### A4. zellij layouts — KDL 版的 tmuxinator

**数据模型**: KDL 格式（非 YAML），`layout { tab { pane split_direction="vertical" { ... } } }`。pane 可挂 `command="foo"` + `args` + `cwd`，也可纯 shell。`default_tab_template` 支持复用，社区在推 `include` 指令做跨文件共享（issue #3835）。

**UI 模式**: 同 tmuxinator，纯声明式 + `zellij --layout foo.kdl`。

**关键决策**: 与 tmuxinator 等价，验证了"目录+命令二元组 + 容器嵌套"是 CLI 工具界共识。

### A5. Git GUI 客户端（GitKraken / Sourcetree / Fork / Tower）

**数据模型**: 一个 repo = 一个目录（含 `.git`）。GitKraken 有"Profiles"切换多账号；Sourcetree 把 repo 列表存自己的 `~/.config/...`；Fork 内置 repo 标签页 + 最近列表。**没有一个把"多命令"作为独立维度** — 每个 repo 的"动作"（fetch/pull/commit/push）是固定工具栏按钮，不是用户自定义命令。

**UI 模式**: **左 sidebar = repo 树/列表 + 主区 = 单 repo 详情 + 顶部固定 action 工具栏**。GitKraken 左侧还分"Local"/"Remote" 分组。Sourcetree 支持把多个 repo 加 Bookmarks 分组。

**关键决策**: Git GUI 把 action 钉死成工具栏，因为 git 动作集合固定。我们 launcher 的命令是**用户自定义**（`$codex` / `$claude` / `$opencode`），所以不能学这套，但"sidebar 分组 + 主区单选"的导航骨架可借鉴。

### A6. Docker Desktop — Container List + Action 矩阵

**数据模型**: Containers/Containers 视图是列表，每行 = 容器，列：Name / Image / Status / Ports / Actions。Actions 是图标按钮组（Start/Stop/Restart/Logs/Inspect/Delete）。

**UI 模式**: **行尾 action 图标组**（不是真正的矩阵）— 每行尾部一排小圆按钮，对应固定动作集。社区吐槽（论坛 #143809）：点 action 按钮边缘会误触发行点击跳到 Logs，是反模式。

**关键决策**: Docker 的"action"是**全局固定集合**，所以行尾按钮组成立。我们命令集合不固定，不能直接搬。但启示是：**行尾动作按钮 vs 行点击导航 必须视觉隔离**，否则误触。

### A7. VSCode launch.json — compounds 多配置

**数据模型**: `launch.json` 的 `configurations[]` 是独立调试配置；`compounds[]` 引用一组 configuration name 并行/串行启动。multi-root workspace 下 `${workspaceFolder:FolderName}` 让每个 config 指向各自根。第三方扩展（Launch Configs）把 config 绑快捷键、组合成数组一键启动。

**UI 模式**: 配置选择器下拉（debug 视图顶部），不暴露矩阵。compounds = "批量启动预设"，本质上就是我们 launcher 的 `presets[]`。

**关键决策**: **compounds = preset 是成熟模式** — 不需要矩阵 UI，用"配置 + 预设组合"两个独立概念即可表达二维。我们已有 `presets[]`，方向正确。

---

## B. 文件管理器的"地址库"设计

### B1. Directory Opus — Favorites / Folder Tree / Quick Folders

**设计**: Favorites 是命名的快捷目录列表，可在 Lister 里按 `Favorites` 菜单或工具栏按钮访问。每条 favorite 可：重命名（避免长路径显示）、绑热键、排序、分组（branch）。Favorites 可同步显示在 Folder Tree 里作为独立节点。设置页是 CRUD 表格：Add/Edit/Delete/Sort/Branch。

**亮点**: **热键绑定** 是 Opus 杀手锏 — 每条 favorite 可独立热键，秒级跳转。**别名** 解决"路径丑"问题。**branch**（分支分组）防止列表膨胀。

### B2. Total Commander — Directory Hotlist (Ctrl+D) + Dir History + Directory Menu

**设计**: 三套机制并存：(1) **Directory Hotlist** (Ctrl+D) — 用户主动收藏的目录，菜单弹出，可子菜单分组；(2) **Dir History** (Alt+Down) — 自动记录访问历史，按时间倒序；(3) **Button Bar** 可拖目录上去做永久快捷。

**亮点**: **Hotlist + History 分离** 是经典设计 — 人为收藏 vs 自动记录用两套数据，避免互相污染。Hotlist 支持子菜单嵌套分组。键盘过滤（输入字母快速定位）。

### B3. Files (Windows) / Files app

**设计**: 标准 Windows Quick Access 模式 — sidebar 顶部"Pinned"（用户固定）+"Recent"（自动），下面是标准文件夹 + This PC。Win11 把 Home 混合了 Recent + Favorites + Recommended，被吐槽（SuperUser #1832783）：分类消失、推荐算法不可控。

**反模式**: **把 Pinned 和 Recent 混一个面板** 让用户失去控制感。Win11 的退步验证了 TC 的"分离"哲学正确。

### B4. One Commander — Sidebar + Favorites Groups + Miller Columns

**设计**: Sidebar 是核心，含 Drives / Favorites / Volumes 多分组。Favorites 支持无限层级 group（点 "New Group" 嵌套），拖拽入组、Ctrl+拖 = 复制文件到该 favorite。Miller Columns（每级一列）做路径导航。

**亮点**: **Favorites Groups 支持任意嵌套** + 拖拽即收藏 + 拖拽即复制到目标。组是用户自定义树，不是固定分类。

### B5. xplorer² / SpeedCommander

**设计**: xplorer² 的 **Folder Groups**（Customize 菜单）= 一组文件夹 tab 的快照，保存命名后可整体召回（"多书签"概念）。普通 Bookmarks 是单目录快捷。Dual bookmarks = 左右两窗各记一个目录，一起召回。

**亮点**: **Folder Group = "workspace 快照"** — 不是单目录而是"一组目录 + 当前布局"。这比单层 favorite 更接近 launcher 的 preset 概念。

### B6. zoxide / fzf / autojump — CLI 频率排序

**zoxide 算法（frecency，精确）**:
- 每个目录有 `score`，首次访问 = 1，每次访问 +1
- 查询时按"最后访问时间"加权：`<1h × 4`、`<1d × 2`、`<1w × 0.5`、`else × 0.25`
- **Aging**: 总分超 `_ZO_MAXAGE` 时全局除以 k，降到 90% MAXAGE；单项 <1 则剔除
- **Pruning**: 文件系统不存在的条目 + 90 天未访问 = 懒删

**fzf 模式**: 不存库，实时 `fd`/`find` 管道喂入 + 模糊匹配 UI。无状态、无排名，靠文件系统本身排序。

**autojump**: zoxide 前身，类似 frecency 但纯 Python 实现，无 aging 机制（库会膨胀）。

**关键洞察**: **zoxide 的 aging + pruning 是"地址库不膨胀"的标准答案**。fzf 的"无状态 + 实时扫描"是"扫描器"思路的另一极。

---

## 对比表

| 设计点 | 可借鉴 | 反模式/避免 |
|---|---|---|
| **二维数据模型** | tmuxinator/zellij 的 `(cwd, command)` 二元组 + 容器嵌套（A3/A4）；VSCode folder ⊥ launch config 正交（A1/A7） | JetBrains 项目=窗口强绑定（A2）；Git GUI 把 action 钉死（A5） |
| **二维 UI 呈现** | compounds/preset 批量启动（A7）；Opus 热键 + 别名（B1）；TC 的 Hotlist ⊥ History 分离（B2） | Win11 混合 Pinned+Recent（B3）；Docker 行尾 action 误触（A6）；矩阵 UI 过于密集 |
| **地址库防膨胀** | zoxide aging + 90 天 pruning（B6）；Opus 的 branch 分组（B1）；One Commander 无限嵌套 group（B4）；TC Hotlist 子菜单（B2） | autojump 无 aging（B6）；扁平列表无分组（当前 launcher 状态） |
| **扫描器/自动推荐** | zoxide 自动从 shell hook 累积（B6）；fzf 实时 `fd` 扫描（B6）；TC Dir History 自动记录（B2） | Win11 "Recommended" 不可控算法（B3） |
| **批量管理 CRUD** | Opus 设置页表格 Add/Edit/Delete/Sort（B1）；xplorer² 统一 list manager 管书签/命令/macro（B5） | Docker 只能逐容器操作（A6） |

---

## 关键洞察（回答调研重点）

### 1. 二维选择：矩阵 vs 双列表 vs 折叠组，哪种最好用？

**业界答案：没有成熟产品用矩阵 UI**。理由：
- 矩阵（命令为列、目录为行、单元格=组合）只在两个维度都是**固定小集合**时成立（如 Docker 的固定 actions）。当命令是用户自定义、目录会增长时，矩阵迅速变稀疏且难维护。
- **真正成熟的设计是"正交两轴 + 预设组合"**：
  - VSCode：folder（目录轴）独立配置，launch.json（命令轴）独立配置，compounds 做批量组合
  - tmuxinator：pane 数据模型持 `(cwd, command)` 二元组，UI 不暴露矩阵
  - 我们 launcher：entry 已经是 `(directory, command)` 二元组，preset 是组合，方向正确

**结论**: 不要做矩阵 UI。entry 列表 + preset 批量 = 业界标准。如果未来要让"同目录多命令"更顺，应做的是**目录轴的折叠/分组显示**（同目录的 entry 折叠成一组，点开看不同命令），而不是矩阵。

### 2. "扫描器自动推荐目录"有没有成熟方案？

**有，但分两派**：
- **被动累积派（zoxide/autojump/TC History）**: hook 进 shell/文件管理器，每次访问自动 +1 分。**零打扰、零决策**，库自然增长。需要时间累积，新机器冷启动空。
- **主动扫描派（fzf + fd）**: 不存库，运行时 `fd` 扫磁盘，fzf 模糊匹配。**瞬时全量**，但无排序、无个性化。磁盘大时慢。
- **混合派（少数）**: Win11 的 "Recommended" = 系统记录 + 算法推荐，但用户骂不可控，是反模式。

**对 launcher 的建议**: 被动累积派（zoxide 模式）是正路 — 我们已经有 `launchedAtUtc` 时间戳和 `updatedAtUtc`，加个 `launchCount` 字段就能算 frecency。主动扫描磁盘的"扫描器"除非用户主动触发"扫描 D:\<PROJECT_ROOT> 加候选"，否则全自动扫描 = 不可控 = 反模式。**真要做扫描器，做成用户点按钮的"导入向导"，不是后台自动跑**。

### 3. 我们想做的"地址录入器"在文件管理器界是否早有标准做法？

**是，标准做法已经 30 年成熟**：
- **Directory Opus Favorites** (1990s-): 命名 + 别名 + 热键 + 分组(branch) + CRUD 表格
- **Total Commander Hotlist** (1990s-): Ctrl+D 菜单 + 子菜单分组 + 键盘过滤
- **One Commander Sidebar Groups** (2010s-): 无限嵌套 group + 拖拽即收藏
- **xplorer² Folder Groups** (2000s-): 多目录快照（最接近 launcher preset）

**共同标准**:
1. **命名 + 别名** — 路径丑不是问题，显示名解决
2. **分组/分支** — 防止列表扁平膨胀，必须支持层级
3. **CRUD 表格管理** — 一个统一的"管理所有收藏"面板，不是只能逐个加
4. **热键/快捷** — 高频项秒级触达
5. **收藏 ⊥ 历史** — 人为决策 vs 自动记录，两套数据不混

**对 launcher 的建议**:
- 当前 `entries[]` 是扁平列表，已 20 项，会继续涨。**至少应加 entry 分组字段**（类似 Opus branch / One Commander group），让 preset 之外有第二层组织
- **目录别名** 已有（`name` 与 `directory` 分离），方向对
- **查重/合并 UI** 是空白：`Data_Acquisition_and_Analysis` 和 `CLAUDE_Data_Acquisition_and_Analysis` 同目录，应该有"按目录折叠"视图暴露重复
- **frecency 排序** 可低成本加：现有 `launchedAtUtc` + 新增 `launchCount` → 直接套 zoxide 公式

---

## 来源

**A. 二维选择 UI**
- [VSCode Multi-root Workspaces](https://code.visualstudio.com/docs/editing/workspaces/multi-root-workspaces)
- [VSCode Workspace 概念](https://code.visualstudio.com/docs/editing/workspaces/workspaces)
- [VSCode issue #246729 — 层级 folder 请求（被拒）](https://github.com/microsoft/vscode/issues/246729)
- [VSCode debug configuration (launch.json)](https://code.visualstudio.com/docs/debugtest/debugging-configuration)
- [VSCode issue #38134 — compounds 跨 folder 支持](https://github.com/microsoft/vscode/issues/38134)
- [Launch Configs 扩展（第三方）](https://marketplace.visualstudio.com/items?itemName=ArturoDent.launch-config)
- [IntelliJ Workspace 文档](https://www.jetbrains.com/help/idea/workspaces.html)
- [IntelliJ Open/Move/Close Projects + Project Group](https://www.jetbrains.com/help/idea/open-close-and-move-projects.html)
- [JetBrains Guide — Group projects](https://www.jetbrains.com/guide/java/tips/group-projects)
- [tmuxinator GitHub](https://github.com/tmuxinator/tmuxinator)
- [tmuxinator 实战 blog](https://thoughtbot.com/blog/templating-tmux-with-tmuxinator)
- [Zellij Layouts 文档](https://zellij.dev/documentation/layouts.html)
- [Zellij Creating a Layout](https://zellij.dev/documentation/creating-a-layout.html)
- [Zellij 0.32 — YAML→KDL + 新 layout 系统](https://zellij.dev/news/config-command-layouts)
- [Zellij issue #3835 — include 指令请求](https://github.com/zellij-org/zellij/issues/3835)
- [Git GUI 对比 2025](https://dev.to/_d7eb1c1703182e3ce1782/best-git-gui-clients-in-2025-gitkraken-sourcetree-fork-and-more-compared-4gjd)
- [Docker Desktop Dashboard 文档](https://docker-docs.uclv.cu/desktop/dashboard)
- [Docker 论坛 #143809 — 行尾 action 误触吐槽](https://forums.docker.com/t/slight-tweak-to-the-docker-desktop-ui-container-list-view-action-buttons-would-improve-usability/143809)

**B. 文件管理器地址库**
- [Directory Opus — Favorites 帮助](https://www.gpsoft.com.au/help/opus10/Documents/Favorites1.htm)
- [Directory Opus — Favorites 设置页](https://www.gpsoft.com.au/help/opus10/Documents/Prefs/Favorites.htm)
- [Directory Opus — Folder Tree](https://www.gpsoft.com.au/help/opus11/documents/Folder_Tree.htm)
- [Total Commander — Directory Hotlist Wiki](https://www.ghisler.ch/wiki/index.php/Directory_hotlist)
- [Total Commander — Ctrl+D / Alt+Down 讨论](https://www.ghisler.ch/board/viewtopic.php?t=76057)
- [Double Commander — Directory Hotlist](https://doublecmd.github.io/doc/en/directoryhotlist.html)
- [One Commander — Sidebar 文档](https://onecommander.com/help2/Sidebar.html)
- [One Commander — Folder Columns](https://onecommander.com/help2/FolderColumnsandNavigationPane.html)
- [xplorer² — Folder Groups 帮助](https://www.zabkat.com/x2h_8.htm)
- [xplorer² — 多 bookmark blog](https://www.zabkat.com/blog/27Mar11-creative-bookmarks.htm)
- [xplorer² — starting at particular folders](https://www.zabkat.com/blog/01Jul11-starting-folders.htm)
- [zoxide — Algorithm Wiki](https://github.com/ajeetdsouza/zoxide/wiki/Algorithm)
- [zoxide — Arch man page](https://man.archlinux.org/man/zoxide.1.en)
- [fzf GitHub](https://github.com/junegunn/fzf)
- [Red Hat — fzf 实战](https://www.redhat.com/en/blog/fzf-linux-fuzzy-finder)
- [SuperUser #1832783 — Win11 Quick Access 退化吐槽](https://superuser.com/questions/1832783/quick-access-favorite-recent-sections-disappeard-in-windows-11-explorer-home-folder)
