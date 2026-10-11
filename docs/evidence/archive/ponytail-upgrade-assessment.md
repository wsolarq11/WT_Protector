Status: Archived
Purpose: 记录 2026-07-25 对 3 个 ponytail 已知局限性的升级评估结论。
Original scope: verify.ps1 中 3 个标记 `ponytail:` 的已知局限性。

# Ponytail 升级评估

## 结论

3 个 ponytail 局限均不需要升级修复，保持在预期风险接受范围内。

| # | Gate 名称 | 风险等级 | 建议 |
| --- | --- | --- | --- |
| 1 | no-stale-three-file-refs | 低 | 保持现状 |
| 2 | settings-single-source-buildstorefile | 低 | 保持现状 |
| 3 | winget-config-schema-valid | 低 | 保持现状 |

## 关键证据

| Gate | 局限 | 评估 |
| --- | --- | --- |
| no-stale-three-file-refs | 只检查 `$activeDocs` 列表，新文档不自动纳入 | 文档添加伴随 PR review，白名单手动维护是合理权衡 |
| settings-single-source-buildstorefile | 依赖代码模式指纹，重构签名后静默失效 | `BuildStoreFile` 是核心方法，任何重构都经测试与 review；正则指纹足够捕获意外回归 |
| winget-config-schema-valid | 只能结构验证，完整 winget 验证需 DSC v3 | 结构验证覆盖常见错误；`apply-machine-config.ps1 -DryRun` 提供完整验证路径 |

## 复核注记

- 3 个门禁名称仍在当前门禁清单中（门禁已由 `tools/verify.ps1` 迁至 `scripts/verify.ts`）。
- 步骤数以 `AGENTS.md` 结构基线表为唯一文档源。
- 下方行号为 2026-07-25 当日 PowerShell 版本历史记录（`9df5ceec` 版本实际为 550/564/583）；逐项证据见 `.trellis/tasks/archive/2026-08/08-15-docs/ledgers/ponytail-upgrade-assessment.md`。

## 关联路径

- 完整 ponytail 评估：`docs/archive/ponytail-assessment.md`
