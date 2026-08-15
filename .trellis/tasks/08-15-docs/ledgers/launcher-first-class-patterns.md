# 台账：docs/giants-shoulders/launcher-first-class-patterns.md（验证日期 2026-08-15）

> 协议：.trellis/tasks/08-15-docs/verification-protocol.md。本文件属于 no-stale-three-file-refs 门禁 activeDocs（tools/verify.ps1 行630-646），不得出现 `config/(config|presets|settings).json`（除非 retired/ 前缀）——重写已遵守（无此类引用）。
> 判定词：VERIFIED / STALE / UNVERIFIABLE / HISTORICAL / EXTERNAL。

| 声明 | 判定 | 证据（命令 + 结果） | 处理 |
| --- | --- | --- | --- |
| Reviewed on: 2026-05-17 | HISTORICAL | `git log --oneline --follow -- …launcher-first-class-patterns.md` → 2c1ec666 2026-05-17（创建）；最后改动 a5e1d757 2026-07-09（三文件引用清理，新增负向门禁） | 保留带日期；注明 07-09 清理 |
| 门禁命令 verify.ps1 | VERIFIED | `Test-Path tools/verify.ps1` → True | 保留 |
| config/launcher.json 统一存储（entries + presets + settings + defaultPresetId） | VERIFIED | `Get-Content -Raw config/launcher.json | ConvertFrom-Json` → keys: schemaVersion,settings,entries,presets,defaultPresetId,runHistory；presetCount=2；defaultPresetId=core-projects | 保留（补充 runHistory 事实） |
| "A preset only rewrites checkbox state… 显式『一键启动已勾选』动作" | VERIFIED | MainWindowViewModel.Presets.cs 行242 ApplyPreset；Batch.cs 行131 `entry.SetSelectionFromOwner(selectedIds.Contains(entry.Id))`；LauncherControlBar.axaml 行42 `Button Content="一键启动已勾选" Command="{Binding LaunchSelectedCommand}"`（独立按钮） | 保留 |
| 旧 selected 复选框为 HTA 兼容桥 | VERIFIED | LauncherEntriesView.axaml 行173/387/551 `CheckBox IsChecked="{Binding IsSelected, Mode=TwoWay}"`；协议已知 HTA 已退役（825ecb5b 2026-07-06） | 保留 |
| 12 产品教训 / 14 行表格 | VERIFIED | 表格逐行计数 = 14 行；注释自述 12 教训 14 行（VS Code、Windows Terminal 各 2 条） | 保留 |
| 表格各翻译列（条目按 id 寻址、presets 按 entryIds 选子集等） | VERIFIED（仓库侧）；EXTERNAL（来源侧） | EntryId.cs/PresetId.cs 存在；config/launcher.json presets.entryIds 结构存在（示例 config presets 含 entryIds）；来源 URL 齐备 | 保留 |
| Kubernetes 行 "keep launch config/presets separate from future last-run status/receipts" | STALE（措辞） | runHistory 现已在 config/launcher.json 内（keys 含 runHistory）；LauncherRunHistory.cs 存在；无隐藏外部数据库 | 改写：run history 作为同一统一存储内的元数据，不落入隐藏外部库 |
| "What we intentionally did not copy"（无插件市场/无隐藏状态库/无工作流引擎/无新依赖/选择启动组不自动启动） | VERIFIED（现状一致） | 无扩展运行时代码（Features/ 15 文件清单无插件系统）；选择组需显式 `应用启动组`（ApplyPresetCommand，CanApplyPreset 行109）而非自动执行 | 保留 |
| 本地证明五步（add presets / validate in xUnit+verify.ps1 / UI selector + 应用启动组 / 三个维护动作 / 保留 selected） | STALE（计划态措辞 → 已全部落地） | ① config/launcher.json 含 presets+defaultPresetId ✓；② verify.ps1 `presets-schema`（行416）与 `security-audit-f2-preset-entryid-validate`（行730，IPresetService.AddPreset/UpdatePreset 须校验 knownEntryIds）；xUnit：LauncherPresetIdsTests/LauncherPresetWorkflowTests/PresetServiceTests/PresetSelectionTests 等 12 个 preset 测试文件 ✓；③ `应用启动组`：LauncherPresetSelectionPreview.cs 行9 DefaultApplyButtonText，测试 MainWindowViewModelTests.PresetSelectionTests.cs 行22 ✓；④ LauncherControlBar.axaml 行144-152 保存为新启动组/更新当前启动组/删除当前启动组 ✓；⑤ selected CheckBox ✓ | 改写为"已落地（verified 2026-08-15）"并逐条附证据 |
| 回滚点（删除 presets/defaultPresetId、LauncherPreset*、preset UI/测试；旧复选框流完好） | VERIFIED | LauncherPreset.cs/LauncherPresetWorkflow.cs 存在（Core 53 文件清单）；config keys 含 presets/defaultPresetId；复选框流仍在（上述 CheckBox 证据） | 保留 |

## 处理总结
- VERIFIED：10 项；HISTORICAL：1 项；STALE：2 项（Kubernetes 行措辞、本地证明计划态）；UNVERIFIABLE：0
- 重写点："Current local proof"改为已落地+证据；Kubernetes 行改写；其余保留。无 config/(config|presets|settings).json 引用（门禁合规）。

## 重写
文档已写回 docs/giants-shoulders/launcher-first-class-patterns.md。
