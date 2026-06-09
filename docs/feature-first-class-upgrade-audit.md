# WT Protector 全功能现实问题能力评估与一等公民改造建议

Reviewed on: 2026-06-09

Scope: 当前 worktree 的 Avalonia 主线、根配置、发布/验证脚本和项目文档。本文不是营销稿，不拿旧 PASS 冒充当前事实。

Current evidence used in this pass:

- `git rev-parse --short HEAD` -> `1c76654`; worktree is dirty, so行号锚点代表当前未提交工作树。
- `config/config.json` -> 19 entries / 19 unique ids / 19 existing directories。
- `config/presets.json` -> 2 presets。
- `config/settings.json` -> 3 commandTemplates, 2 searchHistory entries, `sortMode=custom`, `entryViewMode=cards`。
- `git diff --check` -> PASS。
- `dotnet test .\src\avalonia-launcher\WTLauncher.Avalonia.sln -c Release --no-restore` -> PASS, 542 tests。
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1` -> PASS, 16/16 root checks, 542 tests, hidden temp-config GUI smoke OK, repoHead `1c76654`, worktreeDirty=true.

## 判断标准

一等公民不是“功能多”。一等公民必须满足这五条：

1. 解决真实、高频、当前存在的问题。
2. 数据有唯一权威源，不搞第二套真相。
3. 用户路径显式可见，不靠隐藏 fallback。
4. 失败在契约边界暴露，不能吞错继续跑。
5. 验证能覆盖当前声明，不能靠旧日志和愿望。

本项目的正确战场很窄：**可移植 Windows Terminal 工作空间启动器**。不要把它改成 Flow Launcher、Wox、Raycast、VS Code、tmux 或 Windows Terminal profile manager。那是拿一个小工具去打平台战争，愚蠢。

## 总体结论

【核心判断】✅ 值得继续收口，但不是继续堆功能。

当前 23 项能力里：

- 当前真实代码缺口已经清到零：XAML card radius/padding/shadow 已由 `LauncherDesignTokens` 注入资源统一，不再是第二套真相。
- 剩余只有 future-workload / broad-scope 边界：`selected` 状态迁移、动态命令参数、import/export、run-history、installer/update。
- 绝对“0% 副作用”“强于所有 GitHub 相似项目”“不可再改进”这类声明仍然禁止。启动外部进程之后，app 不能证明 terminal session 健康，也不能消灭任意 shell 命令副作用。

最好的改造方向：**保持门禁新鲜，只在真实痛感出现时处理 future-workload；不要再堆功能。**

## 功能逐项评估

| # | 能力 | 真实问题能力 | 当前级别 | 一等公民证据 | 改造建议 |
|---|------|--------------|----------|--------------|----------|
| 1 | Launch entry object | 极高：保存工作区目录和命令，消灭重复 `cd`/敲命令 | ⭐⭐⭐⭐⭐ | `LaunchEntry.cs:5-30` 七字段；`[JsonRequired]`；`LauncherConfigStore` 内容校验 | 无。数据结构够小、够硬。 |
| 2 | Existing-directory contract | 极高：stale 按钮会直接破坏信任 | ⭐⭐⭐⭐⭐ | `LaunchArgumentBuilder.cs:25-26` runtime 检查；`tools/verify.ps1` gate 检查目录存在 | 无。fail-fast 边界正确。 |
| 3 | Search/filter | 高：19 条目已需要快速定位 | ⭐⭐⭐⭐⭐ | `LauncherEntryVisibility.cs:124-141` 多词匹配；`MainWindow.axaml.cs` Ctrl+F 聚焦；settings 持久化 | 无。fuzzy ranking 等真实痛感出现再说。 |
| 4 | Direct single launch | 极高：核心动作就是找到并启动 | ⭐⭐⭐⭐⭐ | `LaunchArgumentBuilder.cs:11-36` 构造 `wt.exe` plan；`LaunchPlan` 强制文本字段 | 无。不要加“危险命令分类剧场”。 |
| 5 | Persistent checkbox selection & batch launch | 极高：每天一键启动一组工作区 | ⭐⭐⭐⭐ | `config/config.json` `selected`；batch preflight/confirmation/receipt | future-workload：>50 条目且多人协作导致 diff noise 时，把临时选择状态迁出 entry spec。现在别动。 |
| 6 | Named launch groups / presets | 极高：重复启动子集需要命名和审查 | ⭐⭐⭐⭐⭐ | `config/presets.json` 引用 stable entry ids；preset store/gate 拒绝未知 id | 无。这是项目最好的数据结构。 |
| 7 | First-class preset actions | 高：不能要求用户手改 JSON 才维护启动组 | ⭐⭐⭐⭐⭐ | `MainWindowViewModel.Presets*.cs` save/update/delete + rollback | 无。 |
| 8 | Preset-vs-selection preview | 高：应用 preset 会覆盖当前勾选，必须预知变化 | ⭐⭐⭐⭐⭐ | UI 差异预览；已匹配时禁用 no-op apply | 无。 |
| 9 | Preset launch plan preview | 极高：批量启动前必须看到实际命令 | ⭐⭐⭐⭐⭐ | 复用 batch preflight；preview 不启动进程、不改勾选 | 无。这是“启动前零副作用”的正确表达。 |
| 10 | Add/edit/delete launcher buttons | 高：配置维护不能只靠手写 JSON | ⭐⭐⭐⭐⭐ | `MainWindowViewModel.Entries.cs:65-121` clone；edit/delete snapshot + rollback | 无。clone 和 ID 引用提示已补齐。 |
| 11 | Custom order & drag/non-drag reorder | 高：空间记忆比字母序更适合常用按钮 | ⭐⭐⭐⭐⭐ | `customEntryOrder` 单一 owner；drag 与 context menu reorder 共用模型；`MainWindow.Drag.cs:148-157` lazy preview | 无。测试已修复以覆盖新 lazy-preview 合同。 |
| 12 | Sort modes | 高：自定义、最近、更新时间、字母序是四个真实扫描方式 | ⭐⭐⭐⭐⭐ | `LauncherEntryVisibility.cs:5-20` mode 契约；`recent` 已加入 | 无。不要做排序插件系统。 |
| 13 | View modes: Cards / Details / Names | 高：可读操作、紧凑扫描、一眼总览是三种不同任务 | ⭐⭐⭐⭐⭐ | `MainWindow.axaml` 三视图；settings `entryViewMode` | 无。 |
| 14 | Adaptive card sizing & density | 高：窗口尺寸变化不能把卡片挤烂 | ⭐⭐⭐⭐⭐ | `LauncherEntryLayoutCalculator.cs` density/layout 规则；`FastWrapPanel.cs` | 无。视觉大改后补截图 receipt。 |
| 15 | Global launch confirmation | 极高：启动安全规则必须唯一、可见 | ⭐⭐⭐⭐⭐ | `settings.json` `requireConfirmationForLaunches`；per-entry `confirm` 被 schema 拒绝 | 无。一个开关比命令分类安全。 |
| 16 | Portable settings | 极高：偏好必须跟仓库走，不能藏 AppData/registry | ⭐⭐⭐⭐⭐ | `LauncherSettings.cs` / `LauncherSettingsStore.cs:58-172`；根 `settings.json` | 无。 |
| 17 | JSON persistence, backup & rollback | 极高：配置半写比功能缺失更糟 | ⭐⭐⭐⭐⭐ | `LauncherJson.cs:35-60` temp write + cleanup；mutation snapshot/rollback | 无。当前单进程本地工具足够。 |
| 18 | Core/App/Tests separation | 高：启动参数、排序、配置规则必须可测试 | ⭐⭐⭐⭐⭐ | Core 无 Avalonia 依赖；`dotnet test` 542 PASS | 无。 |
| 19 | Self-contained win-x64 publish & native gate | 极高：桌面工具必须能产出可运行 exe | ⭐⭐⭐⭐⭐ | `tools/build-release.ps1:46-58` SHA256 receipt；`tools\verify.ps1` PASS + GUI smoke | 无。注意：worktreeDirty=true，所以这是当前工作树门禁，不是已提交发布声明。 |
| 20 | Retired legacy surfaces | 高：HTA/WinForms 留作活路径就是第二套真相 | ⭐⭐⭐⭐⭐ | `retired/` 归档；root gate 阻止旧路径复活 | 无。 |
| 21 | Design benchmark & giants-shoulders library | 高：设计判断不能靠感觉 | ⭐⭐⭐⭐ | `docs/design-benchmark-digest.md`、`docs/giants-shoulders/` | 只在做 UI/市场声明时刷新外部证据；别让基准库变成流程负担。 |
| 22 | Command templates | 高：长命令重复会制造错误和维护成本 | ⭐⭐⭐⭐⭐ | `LauncherCommandTemplateResolver.cs:7-24`；`settings.json` `commandTemplates`；unknown key fail-fast | 无。动态 `{query}` 是 future-workload，别预铺。 |
| 23 | Design tokens | 中：颜色/间距分散会导致视觉漂移 | ⭐⭐⭐⭐⭐ | `LauncherDesignTokens.cs:7-66`；`App.axaml.cs:23-68` 注入 brush/dimension/shadow resources；`MainWindow.axaml` base card 使用 `CardBorderRadius`/`CardPadding`/`CardBoxShadow` | 无。最后硬编码缺口已关闭并由 XAML 合同测试钉住。 |

## 剩余缺口账本

| 缺口 | 类型 | 是否现在做 | 原因 |
|------|------|------------|------|
| `selected` 与 entry spec 同文件 | future-workload | 否 | 当前 19 entries、单人本地工作流没有真实 diff 痛感；现在拆会制造第二状态文件。 |
| 动态命令模板 `{query}` | future-workload | 否 | 没有真实启动时输入参数工作负载；会引入对话框和替换规则复杂度。 |
| import/export 生态 | broad-scope | 否 | 会把小工具推向平台化。 |
| 持久 run-history / terminal health | external-process-limit | 否 | `wt.exe` handoff 后 session 健康不是本 app 可证明事实。 |
| installer/update/signing | distribution-scope | 否 | 属于分发能力，不是当前启动器正确性；需要时单独立项。 |

## Linus 式方案

1. **先固定数据结构**：`config.json` entries、`presets.json` named groups、`settings.json` UI preferences 三个源已经够清楚；不要新增第四个状态文件，除非 `selected` diff noise 真实发生。
2. **消除特殊情况**：保留全局 confirmation，不恢复 per-entry confirm；command template unknown key 直接 fail-fast，不猜默认命令。
3. **用最笨但清楚的方式实现**：继续让 Core 纯规则、ViewModel 负责 mutation+rollback、View 只管交互边界。
4. **确保零破坏性**：每个改造必须先证明不破坏 `config/*.json` schema、preset 引用、custom order、launch preflight、root native gate。

## 不要做的蠢事

- 不做插件系统。
- 不做 broad launcher 平台。
- 不做隐藏 fallback、默认模板、自动修配置。
- 不把 `selected` 现在就拆出去，只因为理论上 spec/status 更漂亮。
- 不拿“已请求启动”冒充“终端会话健康完成”。

## 当前结论

WT Protector 对“可移植 Windows Terminal 工作空间启动器”这个窄域，当前真实代码缺口已清零。真正值得继续处理的不是新功能，而是：

1. 让本报告成为唯一权威的“全功能一等公民改造建议”文档；
2. 保持 root gate 新鲜，提交/发布前重复 `git diff --check` + `tools\verify.ps1`；
3. 只在真实痛感出现时处理 `selected` 状态迁移；
4. 继续拒绝 broad launcher / 插件平台 / 静默 fallback。
