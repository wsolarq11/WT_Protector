# 重新构建发布产物并按需求矩阵全量验证

## Goal

在 `4811df3`（Avalonia 12.0.3 → 12.1.0, Test.Sdk 18.6 → 18.7, coverlet 6.0.4 → 10.0.1）合入后、当前没有任何 git tag/CHANGELOG 的前提下，对 `origin/main` 当前 HEAD 做一次**端到端发布质量评审**：

1. 重新构建可执行二进制，刷新 `artifacts/publish/win-x64/WTLauncher.App.exe` + `receipt.json`；
2. 跑全量 gate（`tools/verify.ps1`，含 18 checks、xUnit 测试套件、隐藏 GUI smoke）拿到本轮 PASS 凭证；
3. 按 `docs/feature-reality-north-star-audit.md` 的 21-feature 矩阵逐项做实机功能代跑；
4. 同步调研 GitHub Releases 页状态、Avalonia 12.1 / Test.Sdk 18.7 / coverlet 10 的已知升级风险、12 个相似项目的近期动态；
5. 若任何环节红/异常，做根因调查并给出修复路径；
6. 把所有证据归档到一份可复查的最终报告中。

最终交付物只有两类：**绿了的 gate 凭证** + **报告文档**。代码改动不是必需产出（仅在根因调查暴露真实 bug 时才动代码，且要回头补 design）。

## Context

- 当前 HEAD：`1ca3beb chore: record journal`（origin/main 干净）
- 最近变更轴：依赖升级（Avalonia/Test.Sdk/coverlet）、质量收口（pitfalls §3.11-3.12 窄异常 + NuGet audit）、设置单一源 R5 修复、project-wide reorg
- 无 git tag，`build-release.ps1` 的版本号会 fallback 到 `0.1.0-dev`
- `gh` CLI 不在 bash PATH —— GitHub release/issue 调研走 WebFetch
- 21 features 来自 `docs/feature-reality-north-star-audit.md`，是 verify.ps1 的 `feature-objective-boundary` check 强制的不变量

## Scope (in)

| 阶段 | 范围 |
|------|------|
| Build | `tools/build-release.ps1` 重产 `artifacts/publish/win-x64/WTLauncher.App.exe` + `receipt.json` |
| Gate | `tools/verify.ps1` 全量 18 checks + xUnit 全套（~410+ `[Fact]/[Theory]`） + 隐藏 GUI smoke |
| 实机 | `src/avalonia-launcher/run.ps1` 启动 GUI，对 21-feature 矩阵逐项肉眼/操作验证 |
| 调研 | GitHub Releases 页 / Avalonia 12.1 release notes + 已知 issues / coverlet 10 breaking changes / Test.Sdk 18.7 notes / 12 个相似项目（snapshot.json）近 30 天 push/Release 动态 |
| 根因 | 仅当上面任何一项红/异常时启动；定位 + 决策（修 or defer-with-trigger） |
| 报告 | 一份 `release-validation-report.md`（路径见 design.md），含每阶段证据、矩阵结果、调研发现、最终结论 |

## Out of scope (显式)

- 创建首个 git tag 或写 CHANGELOG（这是另一类决策，不在本轮）
- 发布到 GitHub Releases（仅调研页面状态，不实际创建 release）
- 重新审计 21-feature 北星边界（verify.ps1 已在 gate 中强制）
- 代码重构、新功能、Layer 0 契约调整
- 任何会让 `tools/verify.ps1` 退化为非自动化的改动
- 改 `retired/`（字节级保持不变）

## Constraints

- 所有命令从仓库根运行，PowerShell 在 Windows 上用 `-NoProfile -ExecutionPolicy Bypass`
- 不能跳过 verify.ps1 的任何 check；不能 `--no-verify` 提交
- 实机验证如果触发"directory must exist"check，必须用 `config/launcher.json` 里现存的 entry（每个 entry 的 directory 必须在本机存在）
- 报告里的引用必须有 citation（commit hash / URL / 文件路径:行号）
- 全程严守 `docs/feature-reality-north-star-audit.md` 的 claim boundary：**禁止**对外声称 unimprovable / stronger-than-every-GitHub-similar / 0% side-effects

## Acceptance Criteria

- [ ] **AC1** `tools/build-release.ps1` 成功，新 `WTLauncher.App.exe` 产出，`receipt.json` 字段完整（sourceRoot/publishedExe/publishedDll/bytes/sha256/dllBytes/dllSha256/builtAt）
- [ ] **AC2** `tools/verify.ps1` 整体 status=PASS，含 18 个 check 全 PASS，最终 root-verify receipt 的 `checks` 数组每项无异常
- [ ] **AC3** 全量 xUnit 测试套件运行结果记录到报告：通过数 / 失败数 / 跳过数（用 `dotnet test -c Release` 或 gate 内嵌的 mainline-verify 输出）
- [ ] **AC4** 21-feature 矩阵在实机验证中每条都有结论：✅ 验证通过 / ⚠️ 有保留 / ❌ 异常（带复现步骤或截图描述）
- [ ] **AC5** 社区佐证文档至少覆盖：(a) GitHub Releases 页当前状态；(b) Avalonia 12.1 breaking changes 与本项目的相关性判定；(c) coverlet 10 / Test.Sdk 18.7 升级风险；(d) 12 个相似项目近 30 天动态摘要
- [ ] **AC6** 若任何环节红/异常，根因调查文档给出：症状、复现命令、定位文件:行号、修复路径或 defer 触发条件
- [ ] **AC7** 最终报告 `release-validation-report.md` 落盘在 `.trellis/tasks/07-10-release-rebuild-matrix-verify/` 下，含所有阶段的 evidence link、矩阵结果表、调研发现、最终 PASS/PARTIAL/FAIL 判定
- [ ] **AC8** `git status` 在任务结束时干净（或仅有新增的报告文档待 commit），不引入未追迹的二进制/临时文件
- [ ] **AC9** 报告最终判定为 PASS 时，`tools/verify.ps1` 必须能再次独立 PASS（避免"刚好通过一次"的侥幸）

## Notes

- 设计与执行细节见 `design.md` / `implement.md`
- 报告 schema 与 21-feature 矩阵模板见 `design.md`
- 全程严守 `.trellis/spec/guides/pitfalls.md`：开始前 skim，遇坑搜关键词，新坑按 session-id 追加
