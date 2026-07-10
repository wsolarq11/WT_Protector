# Design — 重新构建发布产物并按需求矩阵全量验证

## 设计原则

1. **顺序优先并行**：build/gate 是后续实机的依赖；社区调研可后台并行（不阻塞主线）
2. **证据可复查**：每个判定都有 citation（commit hash / 文件路径:行号 / URL / 命令输出）
3. **零代码改动是默认**：除非发现真实 bug，否则任务结束时 `git diff` 应只含新增的报告/prd/design/implement 文档
4. **不重新发明轮子**：21-feature 矩阵直接复用 `docs/feature-reality-north-star-audit.md` 的章节标题；测试套件直接复用 gate；二进制构建直接复用 `build-release.ps1`

## 执行流水线

```
[Stage A] build-release.ps1  ──┐
                                ├─► [Stage C] run.ps1 + 21-feature 矩阵 ──► [Stage F] report
[Stage B] verify.ps1 (18 ck) ──┤                                            ▲
                                │                                            │
[Stage D] 社区调研 (WebFetch) ──┴────────────────────────────────────────────┘
                                
[Stage E] 失败根因（条件触发，仅当 A/B/C/D 红时）
```

A 与 B 不并行（都吃 dotnet，并行可能踩 IO/锁；先 build，gate 内会自己再做一次 verify-publish 是独立的）。
D 全程后台并行，不阻塞。

## 21-feature 矩阵 → 实机动作映射

> 每条都要在 `release-validation-report.md` 中产出一条结论：✅ / ⚠️ / ❌。
> 实机动作的"代码层"已由 xUnit 覆盖，"GUI/交互层"必须肉眼/操作验证。

| # | Feature | 实机动作（GUI 端） | 关键代码层覆盖（仅 reference，不在实机重复） |
|---|---------|---------------------|---------------------------------------------|
| 1 | Launch entry object | 启动后核对 entry 列表 = `config/launcher.json` 的 entries[]（数量、名称、目录） | LauncherStore / LaunchEntry schema |
| 2 | Existing-directory contract | 故意把一条 entry 的 directory 改成不存在（编辑对话框）→ 期望被 reject；或者新增 entry 时填不存在的 dir → reject | verify.ps1:307 `Test-Path ... Container` check |
| 3 | Search/filter | 在搜索框输入多 token（"w wt" / "codex"），期望列表 filter 收敛且 token 都 match | MainWindowViewModel search predicate |
| 4 | Direct single launch | 点一条 entry 卡片启动 → 期望 wt.exe 拉起，`launchedAtUtc` 被写回 launcher.json | Direct launch handler |
| 5 | Persistent checkbox + batch | 勾若干 → 重启 app → 期望勾选保留；"启动当前组"批量拉起 | LauncherEntryMutationSnapshot 持久化路径 |
| 6 | Named launch groups | 切换 preset → 期望选中状态被替换（不是合并） | Preset apply handler |
| 7 | First-class preset actions | "保存当前选择为新组" / "更新当前组" / "删除当前组" 三个 action 各跑一次 | Preset CRUD |
| 8 | Preset-vs-selection preview | 选中与当前组一致 → "应用"按钮期望 disabled；不一致 → 期望显示 diff | Preset diff preview |
| 9 | Preset launch plan preview | "预演当前组" → 期望显示生成的 wt.exe 命令行但不启动进程 | Preview handler |
| 10 | Add/edit/delete/clone | 4 个 action 各跑一次；"复制副本"克隆出 deep copy | Entry CRUD |
| 11 | Custom order + drag reorder | 拖一条 entry 换位 → 期望 ghost cursor-following；drop 后 customEntryOrder 写回 | Drag session |
| 12 | Sort modes | 切 4 种排序（custom/recent/updated/alphabetical）各一次，期望列表顺序变化 | Sort calculator |
| 13 | View modes Cards/Details/Names | 切 3 种视图各一次，期望 UI 切换 | view mode projection |
| 14 | Adaptive card sizing + density | 切 Normal/Compact/Micro 三种密度 + 调窗口大小，期望卡片 shrink-to-fit | Layout density |
| 15 | Global launch confirmation | 开关 requireConfirmationForLaunches → 启动时分别确认/直接启动 | Confirmation dialog |
| 16 | Portable settings | 关 app 后查 launcher.json：lastFilterText/sortMode/entryViewMode/customEntryOrder/window 都已持久化 | Settings persistence |
| 17 | JSON persistence + backup + rollback | 手动破坏 launcher.json（备份原文件）→ 启动期望可见失败；恢复 | LauncherJson atomic write + File.Replace |
| 18 | Core/App/Tests separation | 验证证据来源：xUnit 测试套件 PASS | Layer 分层 |
| 19 | Self-contained win-x64 publish | 验证证据来源：Stage A 的 receipt.json | build-release.ps1 |
| 20 | Retired legacy surfaces | 验证证据来源：verify.ps1 的 retired-paths-removed + winforms-retired-design + hta-retired-config check | retired/ hygiene |
| 21 | Design benchmark + giants-shoulders | 检查 `docs/giants-shoulders/` 与 `docs/feature-first-class-upgrade-audit.md` 存在且引用有效 | docs reference |

** Ponytail 简化**：#18/#19/#20 的实机动作就是"引用 Stage A/B 的凭证"，不重复 GUI 操作（避免与自动化测试冗余）。

## 报告 schema（release-validation-report.md）

固定结构（违反结构 → 不算 AC7 通过）：

```markdown
# Release Validation Report — <commit-hash> @ <date>

## TL;DR
最终判定：PASS | PARTIAL | FAIL
Gate：✅/❌  Build：✅/❌  矩阵：N/21  社区佐证：✅/❌

## Stage A: Build
- 命令：`powershell ... tools\build-release.ps1`
- receipt.json 关键字段：bytes/sha256/builtAt
- 异常：无 / <描述>

## Stage B: Gate
- 命令：`powershell ... tools\verify.ps1`
- checks 数：N（全 PASS）
- xUnit 测试：通过 N / 失败 N / 跳过 N
- GUI smoke：PASS/SKIP

## Stage C: 21-feature 实机矩阵
| # | Feature | 判定 | Evidence |
|---|---------|------|----------|
| 1 | Launch entry object | ✅ | <复现步骤简述 + 实际观察> |
... 21 行 ...

## Stage D: 社区佐证
### D.1 GitHub Releases 页状态
### D.2 Avalonia 12.1 breaking changes 相关性
### D.3 coverlet 10 / Test.Sdk 18.7 升级风险
### D.4 相似项目（12 个）近 30 天动态

## Stage E: 失败根因（如触发）
- 症状：
- 复现命令：
- 定位：
- 决议：fix / defer-with-trigger

## AC 核对
[AC1..AC9 全部 ✅/❌]

## 引用清单
- commit / URL / 文件:行号
```

## 社区佐证调研来源与判定阈值

| 主题 | 来源（query） | 相关性判定阈值 |
|------|---------------|----------------|
| GitHub Releases 页 | WebFetch `https://github.com/<USER>/WT_Protector/releases` | 列出最新 release / 标签页是否有内容；若空 → 标记"项目从未发布过 release" |
| Avalonia 12.1 breaking changes | WebSearch `Avalonia 12.1 release notes breaking changes` | 若命中本项目用到的 API（Compositor / Binding / Skia / Win32），标 RELATED，否则 UNRELATED |
| Avalonia 12.1 已知 issues | WebSearch `Avalonia 12.1 issue regression site:github.com/AvaloniaUI/Avalonia` | 若 issue 影响 Avalonia.Win32 / Themes.Fluent / Controls.DataGrid（本项目用的），标 RELATED |
| coverlet 10 breaking | WebSearch `coverlet 10.0 release breaking changes` | 重点关注 collector / format / MSBuild integration |
| Test.Sdk 18.7 | WebSearch `Microsoft.NET.Test.Sdk 18.7 release notes` | 重点关注 xUnit/jUnit 报告器、framework resolver |
| 12 个相似项目动态 | WebFetch 各 repo 的 commits/activity（用 snapshot.json 里的 url） | 摘要近 30 天 push/Release；标 ACTIVE/DORMANT/ARCHIVED |

## 失败根因诊断决策树

```
build-release.ps1 红
├── "Release publish target is running" → 杀掉 WTLauncher.App.exe 进程后重试
├── restore 失败 → NuGet 源 / 网络问题，查 .nuget 缓存
├── publish 失败 → 看 csproj/AssemblyInfo 是否被 reorg 改坏
└── Avalonia 12.1 编译错 → 进 Stage E，对照社区调研的 RELATED API

verify.ps1 红
├── 单个 check 红 → 读 verify.ps1 对应 Add-Check 段，定位阈值
├── directory must exist 红 → 检查 config/launcher.json entry.directory 在本机是否存在
├── 测试套件失败 → dotnet test 拿详细输出，定位 failing test
├── GUI smoke 红 → 单独跑 run.ps1 看实际启动情况
└── stale three-file refs 红 → 文档引用了 retired 的三文件架构

实机异常
├── 启动崩溃 → Event Viewer / run.ps1 stderr
├── 操作无响应 → 检查 RelayCommand binding
└── 持久化丢失 → 检查 LauncherStore.BuildStoreFile 路径
```

## 兼容性 / 回滚形态

- 本任务默认零代码改动，**没有 rollback 概念**（不会修改运行时行为）
- 若 Stage E 触发并改了代码：每个改动必须是独立 commit，回滚 = `git revert <hash>`
- 中间产物（receipt.json）在 `artifacts/` 下，gitignored，不需要清理

## 风险

| 风险 | 概率 | 缓解 |
|------|------|------|
| build-release 被 stale 进程阻塞 | 中 | 事前 `Get-Process WTLauncher.App`；若有，先 kill 或换 verify-publish |
| verify.ps1 因为 `config/launcher.json` 的 directory 在本机不存在而红 | 中 | 事前检查每个 entry.directory 存在；不存在的修复后再启 |
| Avalonia 12.1 在本机驱动/字体上有 regression | 低 | Stage D 调研对照；若 RELATED，进 Stage E |
| GUI smoke 在 hidden 模式下偶发挂 | 低 | 重试 1 次；持续挂才进 Stage E |
| WebFetch GitHub Releases 页被限速 | 低 | 失败时 fallback 到 WebSearch |

## Ponytail 简化记录

- 跳过自定义"质量分"打分系统：直接 ✅/⚠️/❌ 三态判定（少一个维度）
- 跳过自动化截图比对：实机操作由人判定（增加自动化会过工程化）
- 跳过为 21 features 写独立的 .ipynb 验证脚本：报告里直接 inline 表格
- 跳过生成 PDF/HTML 报告：单一 markdown 就够，Trellis 归档标准就是 md
