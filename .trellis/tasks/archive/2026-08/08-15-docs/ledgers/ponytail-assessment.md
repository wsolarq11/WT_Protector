# 验证台账 — docs/ponytail-assessment.md

> 复核日期: 2026-08-15（只读验证；未运行 dotnet/verify.ps1）
> 文档日期: 2026-07-23（创建提交 ee2a85a9）

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 审查时间 2026-07-23 | VERIFIED | git log: ee2a85a9 2026-07-23 创建本文档 | 保留 |
| 范围: verify.ps1 中所有 # ponytail: 注释 | VERIFIED | verify.ps1 现有 10 处 ponytail 注释（609/625/651/663/702/719/736/754/781/831）；文中 6 项全部存在 | 保留；补注当日为 6 处、复核时 10 处 |
| no-stale-three-file-refs 检查存在 | VERIFIED | Add-Check 'no-stale-three-file-refs' @ verify.ps1:646 | 保留 |
| 行号 verify.ps1:529 | STALE | 2026-07-23 当日(ee2a85a9)实际 550；当前 646；529 两处均不符 | 更新为 646 |
| $activeDocs = 6 个文档 | VERIFIED | verify.ps1:629-634 与文中列表逐字一致（README/AGENTS/DESIGN/feature-reality/feature-first-class/launcher-first-class-patterns） | 保留 |
| 门禁检查文档中废弃三文件架构引用 | VERIFIED | verify.ps1:625-628 注释 + :640 正则 (?<!retired[/\])config[/\](?:config|presets|settings)\.json | 保留 |
| 候选未纳入文档 6 个均存在 | VERIFIED | Test-Path: design-benchmark-digest / architecture-reform-proposal / architecture-research-report / exemplar-gap-refactor-audit / feature-first-class-upgrade-audit / giants-shoulders/codexswitch-avalonia-native-aot 全部 OK | 保留 |
| 源码中旧三文件引用已在重构中清除 | VERIFIED | 扫描 src/**/*.cs 匹配 (?<!retired[/\])config[/\](?:config|presets|settings)\.json = 0 处 | 保留 |
| settings-single-source-buildstorefile 存在 | VERIFIED | Add-Check 'settings-single-source-buildstorefile' @ verify.ps1:660 | 保留 |
| 行号 verify.ps1:555 | STALE | 当日 564；当前 660 | 更新为 660 |
| 正则指纹逐字一致 | VERIFIED | verify.ps1:656 与文中第 58-59 行一致；MainWindowViewModel.cs:360 含 Entries = _list.MutableEntries.Select(ToLaunchEntry).ToImmutableList(), | 保留 |
| 恰好 1 个汇编点 | VERIFIED | verify.ps1:657 signatureCount -ne 1 抛错 | 保留 |
| 标识符自 task 07-09-settings-single-source 未变化 | VERIFIED | 任务归档 .trellis/tasks/archive/2026-07/07-09-07-09-settings-single-source；当前源码仍匹配指纹 | 保留 |
| winget-config-schema-valid 存在 | VERIFIED | Add-Check 'winget-config-schema-valid' @ verify.ps1:679 | 保留 |
| 行号 verify.ps1:567 | STALE | 当日 583；当前 679 | 更新为 679 |
| 检查 .wt-protector.dsc.yaml 存在 + 4 个必需标记 | VERIFIED | Test-Path OK；4 标记在第 1/30/40/74 行（configuration-dsc-schema/0.2、configurationVersion: 0.2.0、id: Microsoft.WindowsTerminal、id: Microsoft.PowerShell） | 保留 |
| winget configure validate 需 DSC v3，仅结构验证 | VERIFIED | verify.ps1:663-666 ponytail 注释原文 | 保留 |
| apply-machine-config.ps1 -DryRun 提供完整验证替代路径 | VERIFIED | tools/apply-machine-config.ps1:2 [switch]$DryRun；:18 if ($DryRun) | 保留 |
| security-audit-f1-path-getfullpath 存在 | VERIFIED(name) | Add-Check @ verify.ps1:713；当日 617 | 行号更新为 713 |
| F1 扫描 DirectoryPath.cs 的 Path.GetFullPath( | VERIFIED | verify.ps1:708-712；DirectoryPath.cs:39 System.IO.Path.GetFullPath(expanded) | 保留 |
| DirectoryPathJsonConverter.Read 是规范化入口 | VERIFIED | DirectoryPath.cs:63 class DirectoryPathJsonConverter : JsonConverter<DirectoryPath>；:65 Read | 保留 |
| security-audit-f2-preset-entryid-validate 存在 | VERIFIED(name) | Add-Check @ verify.ps1:730；当日 634 | 行号更新为 730 |
| F2 扫描 IPresetService.cs knownEntryIds | VERIFIED | verify.ps1:725-729；IPresetService.cs:32-38 AddPreset/UpdatePreset 含 knownEntryIds | 保留 |
| security-audit-f3-homoglyph-protection 存在 | VERIFIED(name) | Add-Check @ verify.ps1:747；当日 651 | 行号更新为 747 |
| F3 扫描 EntryId.cs LatinScript | VERIFIED | verify.ps1:742-746；EntryId.cs:31 LatinScriptRanges、:148 EnsureLatinScript | 保留 |
| 综合评估与总结表 | VERIFIED | 表中 6 个门禁名全部复核存在 | 保留（结论为当日评估） |

处理汇总: 6 处行号更新为复核日当前值；其余保留。无删除。
