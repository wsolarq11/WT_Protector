Status: Archived
Purpose: 记录 2026-07-23 门禁覆盖度扩展评估结论与建议项。
Original scope: 截至评估日 `tools/verify.ps1` 中的 25 项检查。

# Gate 覆盖度扩展评估

## 结论

- 截至评估日的 25 项检查覆盖面已非常全面，对比同类 Avalonia/.NET 项目属行业领先水平。
- 不需要紧急补充新门禁；建议的两项（`.editorconfig` 准入、测试计数退化检测）均未纳入当前门禁。

## 关键证据

### 25 项检查类别分布（2026-07-23 时点）

| 类别 | 数量 |
| --- | --- |
| 配置验证 | 4 |
| 废弃清理 | 3 |
| 编码规范 | 3 |
| 安全审计 | 3 |
| 项目声明 | 2 |
| 基础设施 | 2 |
| 文档完整性 / 项目结构 / 外部基准 / 主线验证 / 架构规则 / 环境配置 / 文档一致性 | 各 1 |

### 安全审计门禁（新增于本评估前）

- `security-audit-f1-path-getfullpath`（verify.ps1:617）
- `security-audit-f2-preset-entryid-validate`（verify.ps1:634）
- `security-audit-f3-homoglyph-protection`（verify.ps1:651）

### 评估结论（无需补充）

- 编译/测试：`avalonia-mainline-verify` 端到端覆盖充分。
- NuGet 一致性：低风险，暂不独立门禁。
- `.editorconfig` 强制：低风险，关键规则已被编码规范门禁覆盖。
- DEAD CODE：已覆盖已知废弃路径，未知死代码靠定期审计，不设 CI 门禁。
- 覆盖率阈值：建议但不强制，避免“为覆盖率而写测试”的扭曲。
- 文档链接/spec 一致性/资源泄漏/新安全类别：均不紧急，按需添加。

## 建议补充（均未纳入）

| 建议 | 内容 | 复核状态 |
| --- | --- | --- |
| A | `.editorconfig` 存在性及关键规则准入检查 | 未纳入（当前 46 项中无 `editorconfig`） |
| B | 测试文件计数退化检测（阈值 60） | 未纳入；退化由 `structure-baseline-consistency` + `coverage-gate` 间接约束 |

## 复核注记（2026-08-16）

- 门禁已由 `scripts/verify.ts` 实现，`tools/verify.ps1` 已退役；步骤数以 `AGENTS.md` 结构基线表为唯一文档源。
- 当前 46 项（含 `aot-toolchain-owned-by-sdk`），原 25 项全部保留（无移除/改名）。
- 测试退化现由结构基线（129 测试文件、749 属性）与 `coverage-gate` 约束。

## 关联路径

- 门禁清单：`scripts/gate-steps.ts`
- 结构基线：`AGENTS.md`
