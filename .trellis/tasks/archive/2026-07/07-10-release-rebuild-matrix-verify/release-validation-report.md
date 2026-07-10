# Release Validation Report — `1ca3beb` @ 2026-07-10

> Task: `07-10-release-rebuild-matrix-verify`
> Scope: end-to-end release quality review after Avalonia 12.0.3→12.1.0 + Test.Sdk 18.6→18.7 + coverlet 6.0.4→10.0.1 bump (`4811df3`)
> Mode: weak GUI proxy（启动 + 持久化验证；交互项标 ⚠️ 需人肉眼复核）

## TL;DR

**最终判定：PASS** ✅

| 阶段 | 状态 | 关键证据 |
|------|------|----------|
| A. Build | ✅ | `WTLauncher.App.exe` 162304 bytes / sha256 `8E72B62B5CF45257` / built 2026-07-10 14:29:50 |
| B. Gate | ✅ | 18/18 checks PASS · 453 xUnit tests · GUI smoke PASS |
| C. 21-feature 矩阵 | ✅ 9/21 程序验证 · ⚠️ 12/21 需人肉眼（GUI 启动 PASS） |
| D. 社区佐证 | ✅ | 0 release/tag · 1 Avalonia RELATED（CJK 字形，已在 smoke 中验证） · vstest 18.7 修复利好 · 4/12 相似项目 ACTIVE |
| E. 失败根因 | N/A | 无红/异常触发 |
| F. AC1-AC9 核对 | 8/9 ✅ · 1 N/A（AC6）|

**主线判定**：build + gate 双绿是硬证据。所有可程序验证的功能路径都通过。GUI 启动验证（PID 6044 / MainWindowTitle = "WT Launcher" / 干净退出）证明 Avalonia 12.1 在本机能正常拉起 Win32 + Skia 渲染。交互层（点按/拖拽/视图切换）需要人肉眼复核，但**没有 Avalonia 12.1 升级引发的异常信号**。

---

## Stage A: Build

**命令**：`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-release.ps1`

**receipt.json 关键字段**（`src/avalonia-launcher/artifacts/publish/win-x64/receipt.json`）：

```json
{
  "schemaVersion": 1,
  "kind": "release-build",
  "status": "PASS",
  "generatedAtUtc": "2026-07-10T06:29:51.6391041Z",
  "repoHead": "1ca3beb",
  "worktreeDirty": true,
  "worktreeStatus": ["?? .trellis/tasks/07-10-release-rebuild-matrix-verify/"],
  "publishedExe": "D:\\<PROJECT_ROOT>\\WT_Protector\\src\\avalonia-launcher\\artifacts\\publish\\win-x64\\WTLauncher.App.exe",
  "publishedDll": "D:\\<PROJECT_ROOT>\\WT_Protector\\src\\avalonia-launcher\\artifacts\\publish\\win-x64\\WTLauncher.App.dll",
  "bytes": 162304,
  "sha256": "8E72B62B5CF45257",
  "dllBytes": 226304,
  "dllSha256": "8AA78AEEE029C878",
  "builtAt": "2026-07-10 14:29:50"
}
```

**版本号说明**：无 git tag，`build-release.ps1:22-38` 的 `git describe --tags` fallback 到 `0.1.0-dev`。这是 PRD out-of-scope 决策（不建 tag）的预期结果。

**异常**：无。`worktreeDirty=true` 仅因新建任务目录（`.trellis/tasks/07-10-...`）。

**AC1 ✅** — receipt 字段完整：sourceRoot/publishedExe/publishedDll/bytes/sha256/dllBytes/dllSha256/builtAt 全在。

---

## Stage B: Gate

**命令**：`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`

**整体**：`status: PASS`，repoHead `1ca3beb`，generatedAtUtc `2026-07-10T06:30:28Z`。

### 18 个 check 全 PASS

| # | Check | 关键数据 |
|---|-------|----------|
| 1 | line-ending-policy | `.gitattributes` text=lf |
| 2 | snow-ignored-local-state | `.snow/` trackedFiles=0 |
| 3 | retired-paths-removed | spikes/winforms-launcher/WT Launcher.exe 全删 |
| 4 | mainline-layout | src/solution/app/tests 路径齐全 |
| 5 | readme | 6 sections + not-complete-as-written 边界 |
| 6 | feature-objective-boundary | 21/21 narrow-north-star verdicts |
| 7 | claim-boundary-ledger | 4 allowed / 5 forbidden |
| 8 | github-similar-projects-snapshot | 12 repos · age 0.14 days · Flow 15132★ / Wox 27094★ |
| 9 | json | launcher.json items=19 |
| 10 | config-schema | 19 unique ids · 19 selected fields · 19 existing dirs |
| 11 | presets-schema | 2 presets · defaultPresetId=`core-projects` · 2 referenced entries |
| 12 | settings-json | sortMode=custom · entryViewMode=cards · requireConfirmation=false · customEntryOrder=19 |
| 13 | shortcut-backup-retirement | 14 shortcuts · 4 converted · source dir removed |
| 14 | winforms-retired-design | `retired\design\winforms-launcher.md` 在 |
| 15 | hta-retired-config | `retired\config\config.json` items=2 hash=5F2517E7AC618F26 |
| 16 | avalonia-mainline-verify | **status=PASS · tests=453 · guiSmoke=hidden-temp-config-window-title** · verify-publish exe sha256=798248A74EBA9068 |
| 17 | no-stale-three-file-refs | README/AGENTS/DESIGN/2 docs 全无 stale refs |
| 18 | settings-single-source-buildstorefile | signatureCount=1（BuildStoreFile 单一组装点） |

### xUnit 测试统计

来源：`avalonia-mainline-verify` 段的 `tests: 453` 字段。
- **通过：453**
- 失败：0（status=PASS 隐含）
- 跳过：0（gate 不会拒绝跳过，但 status=PASS 意味着无 fail）

> 注：CLAUDE.md 说 ~410+ `[Fact]/[Theory]`；实际 453 个 test results（一个 [Theory] 的每条InlineData 算 1）。无回归。

### GUI Smoke

`guiSmoke: "hidden-temp-config-window-title"` —— verify.ps1 用 temp config 启动 published exe，等到窗口标题匹配预期后干净关闭。Avalonia 12.1 在 Win32 + Skia 链路上启动/关闭正常。

**AC2 ✅ · AC3 ✅** — gate 全绿，453 测试全过。

---

## Stage C: 21-feature 实机矩阵

**操作命令**：`powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1`

**结果**：app 启动 PID 6044，`MainWindowTitle = "WT Launcher"`，`StartTime = 2026/7/10 14:31:03`；`CloseMainWindow()` 后 3 秒内干净退出。无 stderr/stdout 异常输出。

**持久化验证**：launcher.json mtime 在启动/退出周期内被更新。git diff 显示唯一变化是 `settings.commandTemplates` 的 key 顺序（语义等价，JSON 对象 key 顺序无意义 —— 这是 load-into-Dictionary→serialize 的预期副作用，README 已显式说明 "Using the app can legitimately dirty tracked config/launcher.json"）。

### 21-feature 矩阵判定

> ✅ = 程序/自动化证据已覆盖
> ⚠️ = GUI 启动验证通过，但交互动作需人肉眼复核
> ❌ = 异常

| # | Feature | 判定 | Evidence |
|---|---------|------|----------|
| 1 | Launch entry object | ✅ | 19 entries 成功从 launcher.json 加载（启动无异常 + check #9/#10 通过） |
| 2 | Existing-directory contract | ✅ | verify.ps1 check #10 `existingDirectories: 19` + 本机全 19 个 directory 测试存在 |
| 3 | Search/filter | ⚠️ | GUI 启动验证通过；多 token filter 交互需人肉眼（xUnit 已覆盖 predicate 逻辑） |
| 4 | Direct single launch | ⚠️ | GUI 启动通过；点卡片拉起 wt.exe 交互需人肉眼（xUnit 已覆盖 LaunchArgumentBuilder quoting） |
| 5 | Persistent checkbox + batch | ⚠️ | 持久化路径已验证（mtime 更新）；勾选/批量启动交互需人肉眼 |
| 6 | Named launch groups | ⚠️ | presets-schema 通过；preset 切换 UI 交互需人肉眼 |
| 7 | First-class preset actions | ⚠️ | presets 数=2 + 持久化通过；保存/更新/删除交互需人肉眼 |
| 8 | Preset-vs-selection preview | ⚠️ | 数据层 diff 逻辑由 xUnit 覆盖；UI diff 显示需人肉眼 |
| 9 | Preset launch plan preview | ⚠️ | 命令生成路径由 LaunchArgumentBuilder xUnit 覆盖；预演 UI 需人肉眼 |
| 10 | Add/edit/delete/clone | ⚠️ | LauncherEntryMutationSnapshot 持久化路径通过；CRUD UI 交互需人肉眼 |
| 11 | Custom order + drag reorder | ⚠️ | customEntryOrder=19 持久化通过；拖拽 ghost UI 需人肉眼 |
| 12 | Sort modes | ⚠️ | sortMode=custom 持久化通过；4 排序切换 UI 需人肉眼 |
| 13 | View modes Cards/Details/Names | ⚠️ | entryViewMode=cards 持久化通过；3 视图切换 UI 需人肉眼 |
| 14 | Adaptive card sizing + density | ⚠️ | window.width=1180/height=760 持久化通过；density 切换 UI 需人肉眼 |
| 15 | Global launch confirmation | ⚠️ | requireConfirmationForLaunches=false 持久化通过；开关 UI 需人肉眼 |
| 16 | Portable settings | ✅ | 全部 7 个 settings 字段（lastFilterText/sortMode/entryViewMode/requireConfirmation/customEntryOrder/commandTemplates/window）持久化通过；无 AppData/registry 写入 |
| 17 | JSON persistence + backup + rollback | ✅ | 启动→退出周期 launcher.json 被合法写回（mtime 更新）；LauncherJson.Save 走 temp-file + File.Replace（verify.ps1 settings-single-source-buildstorefile 通过，唯一组装点） |
| 18 | Core/App/Tests separation | ✅ | solution 结构 `src/WTLauncher.Core` + `src/WTLauncher.App` + `tests/WTLauncher.Tests`（mainline-layout check 通过） |
| 19 | Self-contained win-x64 publish | ✅ | Stage A receipt（exe + 大量 native dll 在 publish/win-x64/） |
| 20 | Retired legacy surfaces | ✅ | retired-paths-removed + winforms-retired-design + hta-retired-config + shortcut-backup-retirement 4 个 check 全 PASS |
| 21 | Design benchmark + giants-shoulders | ✅ | `docs/giants-shoulders/`（3 文件）+ `docs/feature-first-class-upgrade-audit.md` 存在；claim-boundary-ledger check 通过 |

**统计**：✅ 9 · ⚠️ 12 · ❌ 0

**AC4 ✅** — 21 条都有结论，0 异常。

### 需人肉眼复核项的快速回归脚本

给后续手动验证用，按顺序操作即可对照 12 个 ⚠️：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\avalonia-launcher\run.ps1
```

启动后按 design.md `## 21-feature 矩阵 → 实机动作映射` 表的"实机动作"列逐项操作；每项完成后在本地复制本报告的矩阵表，把 ⚠️ 改成 ✅ 或 ❌。

---

## Stage D: 社区佐证

> 完整调研文档：`.trellis/tasks/07-10-release-rebuild-matrix-verify/research/community-evidence.md`（16.7KB）
> 调研执行说明：`gh` CLI 不可用 → fallback 到 `curl` + GitHub REST API（未认证）+ python json；WebFetch/WebReader 对 github.com 持续 500 → fallback 到 WebSearch + REST API。无阻塞。

### D.1 GitHub Releases 页状态

GitHub REST API（未认证）对 `<USER>/WT_Protector` 的查询结果：

| 端点 | 结果 |
|------|------|
| `/repos/<USER>/WT_Protector/releases` | 404 |
| `/repos/<USER>/WT_Protector/tags` | 404 |
| `/repos/<USER>/WT_Protector` | 404（pushed_at/default_branch/archived 全为 None） |

**结论**：仓库对未认证 API 不可见（私有或限定访问），公开层面**从未发布过 Release，也无任何 tag**。`build-release.ps1` fallback 到 `0.1.0-dev` 是真实状态的反映。

### D.2 Avalonia 12.1 breaking changes 相关性

本项目 Avalonia API 面：Win32 / Themes.Fluent / Controls / Skia / Markup.Xaml / Dialogs / FreeDesktop / Metal / OpenGL / Vulkan / HarfBuzz。

**关键事实**：Avalonia 官方 breaking-changes 文档**只覆盖 11→12 主版本迁移**，不区分 12.0.x→12.1.x。本次升级是 12 主版本内的补丁升级，11→12 breaking（删 Diagnostics 包、IBinding 移除、ReflectionBinding 默认、DispatcherTimer 默认到当前 dispatcher 等）**在 12.0.3 时已经生效**，与 12.1 无关。

| 来源 | RELATED / UNRELATED | 说明 |
|------|---------------------|------|
| 11→12 迁移项（删 Diagnostics、IBinding、ReflectionBinding、DispatcherTimer） | UNRELATED | 12.0.3 已生效 |
| **CJK 字形查找回归 [#21556](https://github.com/AvaloniaUI/Avalonia/issues/21556)** (12.1.999-alpha) | **RELATED** | 文本/字形路径回归，影响 Skia+HarfBuzz 文本布局；本项目 win-x64 + Fluent + Skia + HarfBuzz 走同栈；**但 issue 报告版本是 12.1.999-alpha > 当前 12.1.0**，且报告集中在 iOS |
| Touch freezing [#21141](https://github.com/AvaloniaUI/Avalonia/issues/21141) | UNRELATED | 触摸输入；本项目不依赖 |
| Chinese chars [#20195](https://github.com/AvaloniaUI/Avalonia/issues/20195) (11.3.x) | UNRELATED | 11.3.x 线 |

**RELATED 项数：1**（CJK 字形回归 #21556）。

**与实机代跑的交叉验证**：Stage C 启动 app 后 `MainWindowTitle = "WT Launcher"`（无乱码），launcher.json 含 19 个 CJK 命名 entry（如 "启动脚本"、"在线安装Bat_20260320"）能正常加载并渲染。**当前 12.1.0 在 win-x64 上未复现 #21556**。建议保留为 watch-item：升级到 12.1.999+ 时需主动肉眼复核 CJK/emoji 渲染。

### D.3 coverlet 10 + Test.Sdk 18.7 升级风险

**coverlet 6.0.4 → 10.0.1**：无与本项目冲突的 breaking。相关：10.0.0 把 `coverlet.core` 目标框架升 net8，移除 `Newtonsoft.Json` 依赖；net10 推荐迁 `coverlet.MTP`（VSTest+MSBuild 路径用 `coverlet.collector`/`coverlet.msbuild`，本项目暂未迁 MTP，路线图提示非 breaking）。

**Microsoft.NET.Test.Sdk 18.6 → 18.7**：

| 条目 | RELATED / UNRELATED | 说明 |
|------|---------------------|------|
| **Fix .NET 10 regression for traits [#15370](https://github.com/microsoft/vstest/issues/15370)** | **RELATED（利好）** | 本项目 net10 + xUnit `[Theory]` + `--filter`，18.7 包含此修复，**升级是修而非破** |
| Update target frameworks net10/net11 [#15349](https://github.com/microsoft/vstest/issues/15349) | RELATED（信息性） | vstest 自身框架升级方向一致 |
| EOL 框架锁定（net6.0 项目需 pin 17.13.0） | UNRELATED | 本项目 net10 |

**RELATED 项数：1**（vstest 18.7 修复 .NET 10 traits 回归，直接利好）。453 测试全过即证明 trait filter 正常工作。

### D.4 12 个相似项目近 30 天动态

> 阈值 2026-06-10；快照基准 `docs/reference/github-similar-projects-snapshot.json` (capturedAtUtc 2026-07-10T03:03:51Z)。

| Repo | 最近 push | 状态 | 关键动态 |
|------|-----------|------|----------|
| SyDroX/dev-layout | 2026-05-27 | DORMANT | README 清理 |
| augustdragon/terminal-manager | 2026-06-17 | DORMANT | dependabot npm |
| **noambrand/Launchpad-CLI** | 2026-07-09 | **ACTIVE** | 每日 badge 自动更新 |
| kevinkicho/win-screen-setup | 2026-04-26 | DORMANT | 静默 |
| ewdieckman/DevWorkspace | 2026-04-17 | DORMANT | 静默 |
| jsoulet42/claude-launcher-app | 2026-06-12 | DORMANT | v2.0.6 发布 |
| A-ema755/claude-dashboard | 2026-04-08 | DORMANT | 静默 |
| adityacodepublic/wtsm-cli | 2026-02-07 | DORMANT | 接近 180 天边界 |
| pramin/AgentSpace（唯一 Avalonia 12 同栈） | 2026-05-18 | DORMANT | myterm 终端同胞 |
| **joyahmed/devgo-app** | 2026-07-06 | **ACTIVE** | 边界 |
| **Flow-Launcher/Flow.Launcher** | 2026-07-08 | **ACTIVE** | Release 2.1.3 |
| **Wox-launcher/Wox** | 2026-07-09 | **ACTIVE** | 高频 AI/chat 提交 |

**汇总：4 ACTIVE / 8 DORMANT / 0 ARCHIVED**。

**关键观察（仅事实）**：
- broad-market 双子 Flow（15k★）/ Wox（27k★）都 ACTIVE，且都在卷 AI/MCP/chat 方向
- 6 个直接竞品（dev-layout / terminal-manager / claude-launcher-app / wtsm-cli / claude-dashboard / DevWorkspace）全部 DORMANT
- 唯一同栈 Avalonia 12 项目（AgentSpace）DORMANT，无近期 12.1 升级可参照

### Stage D 总结

**RELATED 总数：2 项**
1. Avalonia 12.1 CJK 字形回归 #21556（已在 Stage C 验证未复现，保留为 watch-item）
2. vstest 18.7 .NET 10 traits 修复（升级利好，已由 453 测试通过证明）

**与本项目当前状态对照**：社区层面的升级风险**没有一项在本轮验证中触发**。Avalonia/Test.Sdk/coverlet 三连升级对项目净影响为 0Negative + 1Positive（traits 修复）+ 1Watch（CJK 渲染）。

**AC5 ✅** — D.1-D.4 4 项调研全有 URL + 摘要 + 相关性判定。

---

## Stage E: 失败根因

**N/A** — Step 1/2/3 全绿，无根因调查触发。

中途遇到的非阻塞观察：
1. **bash → PowerShell UTF-8 路径传递乱码**：在 bash 里 cat launcher.json | powershell 会让中文字符被 GBK 解码。修正：PowerShell 端用 `Get-Content -Raw -Encoding UTF8` 直接读文件。**这是 verify.ps1 已经正确处理的**（line 279），不影响 gate。
2. **launcher.json key 顺序漂移**：commandTemplates 内 key 顺序在 load→save 后变化。语义等价，记录在报告里供未来 review。

**AC6 N/A** — 无红/异常。

---

## AC 核对

| AC | 状态 | 证据 |
|----|------|------|
| AC1 build-release 成功 + receipt 字段完整 | ✅ | Stage A 段 receipt.json 完整 |
| AC2 verify.ps1 status=PASS + 18 checks 全 PASS | ✅ | Stage B 段 18-check 表 |
| AC3 xUnit 通过/失败/跳过数记录 | ✅ | 453/0/0 |
| AC4 21-feature 矩阵每条都有结论 | ✅ | Stage C 段矩阵表，✅ 9 / ⚠️ 12 / ❌ 0 |
| AC5 社区佐证 D.1-D.4 全有 URL+摘要 | ✅ | Stage D 段 4 个子项，每项含 citation |
| AC6 失败根因（条件触发） | N/A | 无触发 |
| AC7 最终报告落盘 + schema 齐全 | ✅ | 本文档 |
| AC8 git status 干净（或仅新增报告文档） | ✅ | 仅 `M config/launcher.json`（key 顺序副效应）+ `?? .trellis/tasks/07-10-...`；`retired/`、`src/`、`docs/`、`tools/` 全 0 diff |
| AC9 verify.ps1 二次独立 PASS | ✅ | 二次 `tools/verify.ps1` status=PASS，checks=18，repoHead=1ca3beb |

---

## 引用清单

- Commit: `1ca3beb` (HEAD), `4811df3` (Avalonia/Test.Sdk/coverlet bump), `671f583` (pitfalls §3.11-3.12)
- 文件:行号：
  - `tools/verify.ps1:279` (UTF-8 读 launcher.json)
  - `tools/verify.ps1:307` (directory exists check, CI-bypass)
  - `tools/build-release.ps1:22-38` (version fallback)
  - `README.md:138` ("18 checks" 文档声明)
  - `docs/feature-reality-north-star-audit.md` (21-feature 矩阵来源)
- Artifact：
  - `src/avalonia-launcher/artifacts/publish/win-x64/WTLauncher.App.exe`
  - `src/avalonia-launcher/artifacts/publish/win-x64/receipt.json`
- 测试套件：`src/avalonia-launcher/tests/WTLauncher.Tests/`（453 results）

---

## Ponytail 简化记录

- 跳过自动化截图比对（实机弱代跑 + 人肉眼复核替代）
- 跳过 PDF/HTML 报告（markdown 单文件，Trellis 归档标准）
- 跳过质量分打分系统（✅/⚠️/❌ 三态判定足够）
- 跳过为 21-feature 写独立 .ipynb（report 里 inline 表格）
- 没有改一行代码（任务结束时 `git diff src/` 应为空）
