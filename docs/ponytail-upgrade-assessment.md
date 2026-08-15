# Ponytail 升级评估

**生成日期**: 2026-07-25
**范围**: verify.ps1 中 3 个标记了 `ponytail:` 的已知局限性

> 复核（2026-08-15）：3 个门禁名称仍在当前 `tools/verify.ps1`（现共 40 项检查）；行号已更新为复核日当前值（2026-07-25 当日 9df5ceec 版本实际为 550/564/583，见验证台账 `.trellis/tasks/08-15-docs/ledgers/ponytail-upgrade-assessment.md`）。

---

## 评估总览

| # | Gate 名称 | 位置 | 风险等级 | 建议 |
|---|-----------|------|----------|------|
| 1 | no-stale-three-file-refs | verify.ps1:646 | 低 | 保持现状 |
| 2 | settings-single-source-buildstorefile | verify.ps1:660 | 低 | 保持现状 |
| 3 | winget-config-schema-valid | verify.ps1:679 | 低 | 保持现状 |

**结论**: 3 个 ponytail 局限均不需要升级修复。保持在预期风险接受范围内。

---

## 详细评估

### 1. no-stale-three-file-refs

**局限**: 只检查 `$activeDocs` 列表中的文档。新文档不会自动纳入检查。

**风险**: 如果有人添加了新文档并引用了旧的三文件路径，gate 不会捕获。

**升级成本**: 高 — 需要检测项目中所有 Markdown 文件的引用。

**评估**: 低风险。文档添加通常伴随 PR review，且 `$activeDocs` 列表可轻松扩展。手动维护白名单是合理的权衡。

**建议**: 保持现状。在 `$activeDocs` 中添加注释提醒维护者扩展列表。

### 2. settings-single-source-buildstorefile

**局限**: 依赖特定代码模式指纹（`Entries = _list.MutableEntries.Select(…), Settings =`），重构签名后失效。

**风险**: 如果 `BuildStoreFile` 方法签名变更，gate 会静默通过，可能重新引入双路径漂移。

**升级成本**: 高 — 需要语义理解方法内容而非正则匹配。Roslyn analyzer 可实现但过于重量级。

**评估**: 低风险。`BuildStoreFile` 是 `MainWindowViewModel` 中的核心方法，任何重构都会经过测试和 review。正则指纹已足够捕获意外回归。

**建议**: 保持现状。如果未来重构 `BuildStoreFile`，同步更新此 gate 的扫描模式。

### 3. winget-config-schema-valid

**局限**: 只能做结构验证；完整 winget 验证需要主机安装 DSC v3。

**风险**: 结构验证无法捕获 YAML 语义错误（如无效资源类型、包名拼写）。

**升级成本**: 中 — 需要在 CI 环境中安装 DSC v3 模块。CI 运行在 `windows-latest`，但 DSC v3 不是预装组件。

**评估**: 低风险。`apply-machine-config.ps1` 的 DryRun 模式可供开发者本地测试。CI 中 gate 的目的是检测结构问题而非语义正确性。

**建议**: 保持现状。文档已明确升级路径：`.\tools\apply-machine-config.ps1 -DryRun`。
