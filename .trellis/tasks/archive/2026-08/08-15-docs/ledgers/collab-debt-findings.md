# 验证台账 — docs/collab-debt-findings.md

> 验证日期: 2026-08-15 ｜ 判定词: VERIFIED / STALE / UNVERIFIABLE / HISTORICAL
> 全文性质: 2026-07-23 审查报告（2026-07-25 状态同步），点状历史记录——保留全部发现与行动项记录（含已关闭项）。

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 审查时间 2026-07-23；跨度约 369 提交自 2026-07-01 | HISTORICAL | 现 380 提交（2026-07-01 起，HEAD 42ca44c6）；文内 369 与 274（§4.5-A）并存为不同时点统计 | 保留 |
| 2026-07-25 状态同步块: High 清零 / Vogen 值对象迁移为 Core record struct（ValueObjects/ 已删）/ CFA 5 步完成 / 测试 696→716 | PARTIAL→VERIFIED（Vogen/CFA） | Test-Path src/avalonia-launcher/src/WTLauncher.Core/ValueObjects = False；Core 现含 EntryId.cs、DirectoryPath.cs、PresetId.cs、PresetName.cs；.trellis/tasks/archive/2026-07/07-25-closed-flux-arch 存在（Test-Path True）；测试属性现 728（EV2） | 保留 |
| 4.1-A: 32 个 Snow Team 提交集中于 07-22/07-23 两天（6+6 与 14+6） | HISTORICAL（计数） | 现 47 提交，日期仍仅 07-22（12，6 auto+6 merge）与 07-23（35）——两日期集中成立；07-22 的 6 auto + 6 merge 与文中一致 | 保留 |
| 4.2-A: 20 种 distinct 类型 + 12 merge | HISTORICAL | 现 29 auto + 18 merge；建议"建立 Agent 类型目录"已落地（docs/agent-type-directory.md，27 种） | 保留 |
| 4.3 三份审查（core-fixer ffb9cd6 / path-unifier c801a50 / phase-c-arch 5028046）与位置 | UNVERIFIABLE（哈希）；路径可查 | 哈希无法解析；EntryListViewModel.cs、MainWindowViewModel.cs、Views/Controls 现存在（Test-Path True，位于 src/avalonia-launcher/src/ 下） | 保留（时点记录） |
| 4.4-A: 全部 12 个简单 2-parent merge | HISTORICAL | 现 18 个 Snow Team merge | 保留 |
| 4.4-B: 无自动 CI → 已解决（verify.yml full-gate PR 触发） | VERIFIED | verify.yml 行 6 pull_request、行 89-93 full-gate job（if: pull_request）、行 109 运行 tools/verify.ps1 | 保留 |
| 4.5-A: 274 提交 / 32 Agent（11.8%）/ 242 手动 | HISTORICAL | 现 380 / 47；时点比例保留 | 保留 |
| 5.1-A: verify.ps1（609 行）实施 22 项检查 + 22 个检查名清单 | HISTORICAL（行数/数量）；VERIFIED（清单名） | 现 920 行 / 38 项 `^Add-Check `；清单中 22 个检查名全部仍存在于 verify.ps1 Add-Check 列表 | 保留 + 注记 |
| 5.1-B / 5.5-B: 3 个 MEDIUM 安全发现修复提交 4aebd24 + security-audit-f1/f2/f3 gate | PARTIAL | 4aebd24 无法解析；security-audit-f1-path-getfullpath / f2-preset-entryid-validate / f3-homoglyph-protection 现存在 | 保留 |
| 5.2-A F1: 修复前 LaunchEntry.cs 无 GetFullPath；修复落点 DirectoryPath.cs JsonConverter.Read | VERIFIED（现状） | LaunchEntry.cs GetFullPath 0 命中；DirectoryPath.cs GetFullPath 2 命中 | 保留 |
| 5.2-B F2: knownEntryIds 运行时交叉验证（AddPreset/UpdatePreset） | VERIFIED | knownEntryIds 10 处引用；IPresetService.cs:31-38 签名；PresetService.cs:89-91 FindInvalidEntryIds(entryIds, knownEntryIds) | 保留 |
| 5.2-C F3: EntryId.cs LatinScriptRanges + EnsureLatinScript | VERIFIED | EntryId.cs 4 处命中 | 保留 |
| 5.3-A: WeakReferenceMessenger/FeatureMessage 已完全清理（删除提交 77504425） | PARTIAL | 现 0 代码命中，仅 1 处注释提及 WeakReferenceMessenger（ViewModels/Messages.cs:7）；FeatureMessage 0 命中；77504425 无法解析 | 保留 + 注记 |
| 5.3-B: LiteDB 已完全清理 | VERIFIED（现状） | LiteDB 0 命中（Get-ChildItem -Recurse 可靠法） | 保留 |
| 5.3-C: 系统化死代码清理（67827bb、d2fd836、ef978b7、55cf008、89a4650、03e9401、61c1e1c、022f708） | PARTIAL | 哈希均无法解析；快照类删除为协议已知事实（2026-07-21，StoreGrain.Dispatch save-first-then-commit 回滚） | 保留（时点记录） |
| 5.4-A: 3 波现代化（ad9d80f / 1732754 / ac675a4 / c21ec1e / 757b84a） | UNVERIFIABLE（哈希） | 哈希无法解析 | 保留（时点记录） |
| 5.5-A: 5 个针对性复发门禁 | VERIFIED | no-bare-catch-exception、app-raw-hex-zero、no-stale-three-file-refs、settings-single-source-buildstorefile、no-imperative-view-ui 全部现存在 | 保留 |
| 5.5-C → 方法级死代码门禁 no-thin-wrapper-delegation（07-25 新增） | VERIFIED | gate 现存在（Add-Check 列表） | 保留 |
| 严重程度汇总（Critical 0 / High 0 / Medium 2 / Low 9）与 6 项行动项全部关闭 | HISTORICAL | 行动项记录保留（不删除）；CI ✓（verify.yml）、F1-F3 gate ✓（verify.ps1） | 保留 |
| 正面评价（gate 设计 / 死代码习惯 / 11.8% Agent 比例 / UltraReview） | HISTORICAL | 时点评价；Agent 比例现 12.4%（47/380） | 保留 |
| 后续变更表（VTK 07-24、5 个 Analyzer 07-24、.snow 07-25、696→700 + Gate 22→25、CI 07-23） | HISTORICAL | Analyzer WT1001/1003/1004/1005/1006 现存在；其余时点记录 | 保留 |
| 结尾引用 `docs/completion-overview.md` | VERIFIED | Test-Path True | 保留 |
| 2026-08-15 归档移动: 仓库根 → docs/ | VERIFIED | git status --porcelain: `R collab-debt-findings.md -> docs/collab-debt-findings.md`（staged rename）；PRD 验收 [x] 7 个文档 git mv 归档 | 文首注记 |

## 处理汇总

- 保留全部发现、更新块与行动项记录（含已关闭项）。
- 文首新增 2026-08-15 归档 + 核验注记（移动事实、哈希不可解析、可复核现状清单、verify.ps1/测试现计数）。
- 未发现因归档移动失效的路径引用需改写（文档内无 f2/f3/phase*/ultrareview 文件路径引用，仅为 UltraReview 词提及）。
