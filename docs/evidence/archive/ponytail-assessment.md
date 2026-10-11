Status: Archived
Purpose: 记录 2026-07-23 对 `tools/verify.ps1` 全部 ponytail 局限性的评估结论。
Original scope: 6 个已知局限性门禁（当日 6 处 ponytail 注释，复核时 10 处）。

# Ponytail 局限性评估报告

## 结论

- 6 个门禁的 ponytail 注释均准确记录已知局限性，升级路径清晰，无紧急改进需求。
- 全部为基于字符串匹配的回归门禁，非语义验证——这是简单、快速、不易误报的设计权衡。

| 门禁 | 风险等级 | 是否立即改进 |
| --- | --- | --- |
| no-stale-three-file-refs | 低 | 否 |
| settings-single-source-buildstorefile | 中低 | 否 |
| winget-config-schema-valid | 低 | 否 |
| security-audit-f1-path-getfullpath | 低 | 否 |
| security-audit-f2-preset-entryid-validate | 低 | 否 |
| security-audit-f3-homoglyph-protection | 低 | 否 |

## 关键证据

| 门禁 | 局限 | 升级路径 |
| --- | --- | --- |
| no-stale-three-file-refs | 只检查 `$activeDocs` 列表，新文档不会自动纳入 | 新文档描述 launcher.json 契约时加入 `$activeDocs` |
| settings-single-source-buildstorefile | 正则指纹依赖 `_list.MutableEntries.Select(ToLaunchEntry)` 等标识符 | 重构 `MainWindowViewModel` 时同步更新扫描模式 |
| winget-config-schema-valid | 无 DSC v3 时只能做结构标记验证 | 本地用 `apply-machine-config.ps1 -DryRun` |
| F1 path-getfullpath | 扫描 `DirectoryPath.cs` 的 `Path.GetFullPath(`，换位置会漏检 | 新增扫描模式指向新位置 |
| F2 preset-entryid-validate | 检查 `IPresetService.cs` 的 `knownEntryIds`，重命名/迁移会绕过 | 新增接口名到扫描模式 |
| F3 homoglyph-protection | 检查 `EntryId.cs` 的 `LatinScript`，白名单改名/提取会绕过 | 新增类/成员名到扫描模式 |

## 复核注记（2026-08-15）

- 文中 6 个门禁名称全部仍在当前门禁清单中（门禁已迁至 `scripts/verify.ts`）。
- 行号与 PowerShell 代码块为 2026-07-23 当日版本；逐项证据见 `.trellis/tasks/archive/2026-08/08-15-docs/ledgers/ponytail-assessment.md`。

## 关联路径

- 升级评估：`docs/archive/ponytail-upgrade-assessment.md`
- 门禁清单：`scripts/gate-steps.ts`
