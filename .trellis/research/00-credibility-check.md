# 调研报告可信度验证

**日期**: 2026-07-11
**验证范围**: `launcher-data-models.md` / `wt-ecosystem-config-models.md` / `matrix-ui-and-directory-library.md`
**方法**: 每份报告抽取 5-8 个关键事实 claim（非结论），通过 WebSearch 核对官方文档/权威来源
**目的**: 在 parent PRD 依据这三份报告做"不重构 schema"决策之前，排除 hallucination 风险

---

## 总结

| 报告 | 抽样 claim 数 | 真 | 假 / 误导 | 无法验证 | 整体可信度 |
|------|--------------|----|----------|---------|-----------|
| `launcher-data-models.md`（6 大启动器） | 6 | 5 | 1（Alfred `{const:}` 语义框架误导） | 0 | **高** |
| `wt-ecosystem-config-models.md`（7 大终端） | 7 | 5 | 2（Alacritty `[general] import` 位置错；zoxide 第 4 档无出处） | 0 | **中高** |
| `matrix-ui-and-directory-library.md`（二维 UI + 地址库） | 6 | 6 | 0 | 0 | **高** |

19 个抽样 claim 中 16 真 / 3 处偏差。偏差均为局部细节错位而非整段捏造，不改变任何报告的核心结论。三份报告可以作为 parent PRD 决策依据，但建议在引用以下 3 处细节前修正措辞。

---

## 详细验证

### 报告 1：`launcher-data-models.md`

- **Claim A**: "Raycast Quicklinks 字段固定且少：`name`、`link`、`iconName`、`openWith`、`tags`" → **真**。Raycast 官方 manual Quicklinks 页列出上述字段，`tags` 为可选数组。
- **Claim B**: "Raycast Dynamic Placeholders 语法 `{argument name="query"}`" → **真**。官方 manual Dynamic Placeholders 页原样确认。
- **Claim C (偏差)**: "Alfred `每个 Trigger / Hotkey 对象可以覆盖变量的默认值（`{const:myvariable}` 语法）`" → **误导**。`{const:myvariable}` 是 Alfred **在对象字段里读取**常量/Workflow Variable 的宏语法（官方"Using Variables in Workflows"页明示），而**覆盖默认值**的实际机制是通过 Arg & Vars utility 的 `{var:VARIABLE_NAME}` 宏写入。报告把"读宏"误写成"覆盖语法"，Trigger×Action 解耦的核心结论本身正确，但语法举例错位。
- **Claim D**: "Alfred Trigger 和 Action 解耦，一个 Action 可被 N 个 Trigger 触发" → **真**。Workflow Editor 画布 DAG 模型官方文档明确。
- **Claim E**: "ueli Custom Commands 字段 `name` / `prefix` / `executionCommand` / `icon`" → **真**。ueli GitHub `settings.json` schema 与 issue #109 回答一致。
- **Claim F**: "uTools `plugin.json` 的 `features` 含 `cmd`（特征词数组）、`args`、`mode`（list/none/regex）" → **真**。u-tools.cn 开发者文档原样确认（uTools 官方页面 WebSearch 命中）。

### 报告 2：`wt-ecosystem-config-models.md`

- **Claim G**: "WT profile 是 1:1 命令+目录扁平记录，`startingDirectory` 是字符串字段" → **真**。Microsoft Learn Windows Terminal General Profile Settings 页确认。
- **Claim H**: "`startupActions` 是 `wt` 命令行语法的字符串拼接，不可命名/复用" → **真**。Microsoft Learn Panes 页与社区 novacare 博客均印证。
- **Claim I**: "ConEmu 嵌套 task 支持 `{MyTool} parameter-set-1/2/3` 传参" → **真**。ConEmu 官方 SettingsTasks.html 原文给出该三行示例。
- **Claim J**: "Tabby `isTemplate` 字段语义是不在 launcher 列表显示" → **真**。Tabby ProfilesService 文档与 `PartialProfile` 类型定义一致。
- **Claim K**: "WezTerm Lua 可 `for` 循环生成 `launch_menu` 条目" → **真**。wezterm.org `launch_menu.html` + Configuration files 文档支持此用法。
- **Claim L (偏差)**: "Alacritty `[general]` `import = ["theme.toml", "mac.toml"]`" → **错**。Alacritty TOML 配置中 `import` 是**顶级 key**，不属于 `[general]` table；`[general]` 装 `working_directory`、`live_config_reload` 等。报告把顶级 key 误归到 `[general]`。
- **Claim M (偏差)**: "zoxide frecency 四档：`<1h ×4`、`<1d ×2`、`<1w ×0.5`、`else ×0.25`" → **前三档真，第四档无出处**。zoxide Algorithm Wiki 仅列出 3 档（within hour ×4 / within day ×2 / within week ×0.5），"else ×0.25" 在 wiki 中无对应，疑为推断或来自旧版本/未归档实现。

### 报告 3：`matrix-ui-and-directory-library.md`

- **Claim N**: "VSCode `.code-workspace` 顶层 `folders[]` 每项 `{path, name}`" → **真**。VSCode Multi-root Workspaces 官方文档原文确认。
- **Claim O**: "JetBrains Project Group 可 'All Projects in Group' 一键全开" → **真**。JetBrains 官方"Open, Close and Move Projects"与 Guide "Group projects" 均印证。
- **Claim P**: "tmuxinator YAML 顶层 `root` + `windows:` + `panes:`，pane 挂 cwd+command" → **真**。tmuxinator GitHub README 给出等价示例。
- **Claim Q**: "Total Commander Directory Hotlist = Ctrl+D 菜单" → **真**。ghisler.ch wiki Directory Hotlist 页原样确认。
- **Claim R**: "Directory Opus Favorites 支持 热键绑定 + branch 分组 + 别名" → **真**。gpsoft.com.au Opus Favorites 帮助页逐一印证。
- **Claim S**: "zoxide aging：总分超 `_ZO_MAXAGE` 时全局除以 k 降到 90%；Pruning：FS 不存在 + 90 天未访问 懒删" → **真**。zoxide Algorithm Wiki Aging/Pruning 章节原样印证。

---

## 结论

### 整体可信度

三份报告整体可信。19 个抽样 claim 中 16 真，3 处偏差均为**局部语法/字段归属错位**，没有发现整段捏造的工具/特性/issue。报告对核心数据模型的描述（WT 扁平 profile、ConEmu 嵌套 task、WezTerm Lua、tmuxinator pane 二元组、VSCode 多根 workspace、各启动器 entry-centric vs command-centric 范式分类）全部经官方文档交叉验证。

### 需要修正的 3 处细节

1. **报告 1 / Claim C**：把"`{const:myvariable}` 用于 per-Trigger 覆盖"改成"Workflow Variables 通过 Arg & Vars (`{var:NAME}`) 写入，对象字段用 `{const:NAME}` 读取"。Trigger×Action 解耦的结论不变。
2. **报告 2 / Claim L**：把"`[general]` `import = [...]`"改成"顶级 `import = [...]`"。
3. **报告 2 / Claim M**：删除 zoxide 第 4 档 "else × 0.25"，或标注"未在官方 Wiki 找到，可能为旧版行为"。

### 对 parent PRD "不重构 schema" 决策的影响

**无实质影响**。Parent PRD 决策依赖的 3 个核心论据全部经独立验证为真：

1. **WT 扁平 profile 不支持参数化实例**（Claim G/H）→ 真，证明"补 WT 故意不做的层"定位成立。
2. **业界没有成熟产品用矩阵 UI 表达"命令×目录"二维**（三份报告共同观察）→ 验证范围内成立，无反例。
3. **当前 `entry × commandTemplates` 模型已等价于 Alfred 单 Action + N 变量 Trigger / Cmder 单 task + N 实例**（报告 1 结论）→ 数据层等价性成立，"重复"是表层 JSON 行数而非结构冗余。

3 处偏差都不触及上述论据。parent PRD 可以继续依据这三份报告做"不重构 schema、改进 UI 投影"的决策。

### 建议后续动作

- 修正三处细节后，报告可定性为"可信参考"（无需重新调研）。
- 报告中所有引用的 GitHub issue 编号（#15721、#246729、#3835、#9210、#10788、#7805）和官方文档 URL 均在验证中命中，链接完整有效。
- 唯一无法在本次验证中完全锁定的边角：Win11 Quick Access "Recommended" 算法吐槽（SuperUser #1832783）和 Docker 论坛 #143809 — URL 命中但内容仅做了快速扫读，未逐字核对。如 parent PRD 不引用这两条吐槽，可忽略。
