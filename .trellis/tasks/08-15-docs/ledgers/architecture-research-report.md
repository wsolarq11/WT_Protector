# 验证台账 — docs/architecture-research-report.md

> 验证日期：2026-08-15 ｜ 协议：.trellis/tasks/08-15-docs/verification-protocol.md
> 文档性质：带日期行业调研报告（2026-07-23）→ 判定为 HISTORICAL

| 声明 | 判定 | 证据（命令+结果） | 处理 |
|---|---|---|---|
| 调研日期 2026-07-23 | VERIFIED | git log --follow → `093d16bc 2026-07-23 [Snow Team] pattern-researcher: auto-commit work` | 日期已存在，保留 |
| 调研范围 "5 个方向，10+ 来源" | VERIFIED | 正文含 5 个方向章节；调研来源汇总表列 10 条 URL | 保留 |
| "项目已使用 Fluxor" / "当前 Fluxor Store 中的数据已经是事实来源" / "项目已在用 Fluxor"（多处） | VERIFIED（时点） | git log --all → Fluxor 骨架 `86cce496 2026-07-23 feat: add Fluxor registration skeleton`（报告同日）；后于 `0c0b8771 2026-07-24 Scheme B` 移除 | 保留（2026-07-23 时点事实）；验证记录注明 Fluxor 已移除 |
| 当前问题映射（LayoutManager.cs / Layout.cs / EntryListViewModel.cs / ReplaceEntries / ReplaceAllEntries / TryRefreshCardSize / _lastApplied* / _isHandlingSettingsChange 等） | HISTORICAL | 报告日期 2026-07-23；所引旧代码已被后续 Scheme B/VTK/CFA/Trinity 重构替换 | 保留原文（时点调研映射） |
| "Avalonia 11+ 才完全支持 ItemsRepeater，需确认项目使用的版本" | UNVERIFIABLE（外部技术事实，且自带"需确认"） | 仓库无法验证；当前 Directory.Packages.props 为 Avalonia 12.1.0 | 保留（带"需确认"措辞的研究记录）；验证记录注明当前 Avalonia 12.1.0 |
| 外部来源引用（React/Redux/Fluxor/CommunityToolkit/Avalonia/Fowler/CQRS/Wikipedia 等 URL） | UNVERIFIABLE（外部研究） | 仓库无证据可查；带 URL 的调研记录 | 保留 |
| 综合建议 3 变革 | HISTORICAL | 提案内容，后续被 CFA/Trinity 落地替代（见 reform-proposal 台账） | 保留 |
| 结构数字（53/53/15/15/24/20/7/121/728/38） | 不适用 | 本文无结构数字 | — |

## 处理总结
- 判定：VERIFIED 3 项 / UNVERIFIABLE 2 项 / HISTORICAL 3 项
- 重写：仅新增 2026-08-15 验证记录块（置于头部引用块之后）；正文原文保留（带日期历史研究记录）
- 无删除；未触碰 AGENTS.md TRELLIS 块、config/launcher.json、源码
