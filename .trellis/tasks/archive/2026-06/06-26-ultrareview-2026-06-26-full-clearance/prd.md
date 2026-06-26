# UltraReview 2026-06-26 全部发现清缴

## Goal

清缴 UltraReview 2026-06-26 发现的全部 67 项问题，清零所有未解决项。最终经完整验证门禁（verify.ps1 + 全量测试套件），置信度无限逼近 100%。

## Background

2026-06-26 独立深度审查产出 67 项发现，分为：

- 🔴 严重 Bug: 9 项
- 🟡 中等: 14 项
- 🟢 低/代码质量: 12 项
- 📋 文档差异: 4 项
- 📋 测试覆盖缺口: 14 项
- 📋 脆弱测试: 10 项
- 📋 项目配置: 4 项

所有发现附带精确文件路径和行号，均已独立验证为真实存在的问题（非推测）。

## Task Structure

采用父任务 + 子任务结构，按层级拆分：

| 子任务 | 范围 | 预估发现数 |
|--------|------|-----------|
| `core-fixes` | Core 库 Bug（代理对截断、规范化不一致、NaN 传播等） | ~8 |
| `viewmodel-fixes` | ViewModel 状态 Bug（启动时间戳、事件泄漏、去抖缺失等） | ~10 |
| `view-fixes` | View/XAML Bug（Ghost 限界、焦点索引越界、FastWrapPanel 等） | ~8 |
| `persistence-fixes` | 持久化修复（文件泄漏清理、PathTooLongException 捕获、冗余 I/O 等） | ~8 |
| `config-fixes` | 项目配置修复（NuGetAudit、包版本、config 可移植性等） | ~6 |
| `doc-fixes` | 文档修正（README 矛盾、drag-drop spec 类名、pitfalls 过时等） | ~4 |
| `test-fixes` | 测试修复（脆弱测试重构、新测试覆盖缺口） | ~24 |

## Non-Negotiable Rules

1. **绝对禁止降低测试标准来迁就产品代码缺陷** — 如果测试暴露了产品 bug，修产品代码，不修测试
2. **禁止跳过 hook 或 verify.ps1 门禁**
3. **每个 commit 必须独立可验证**，不产生回归
4. **所有公共 API 变更保持向后兼容**

## Acceptance Criteria

- [ ] 所有 9 项严重 Bug 通过单元测试验证已修复
- [ ] 所有 14 项中等问题已修复或明确标注为设计取舍
- [ ] 所有 12 项低级别问题已修复
- [ ] 4 项文档差异已修正，文档与代码一致
- [ ] 14 项测试缺口已补充测试
- [ ] 10 项脆弱测试已重构或删除
- [ ] 4 项项目配置问题已修复
- [ ] `dotnet test` 全量通过（零失败、零跳过）
- [ ] `tools/verify.ps1` PASS
- [ ] 无新增 warning

## Out of Scope

- 新增功能（本次仅修复，不增加特性）
- 大范围架构重构
- 退休代码（retired/）的修复

## Decisions

1. **任务结构**: 父任务 + 7 个子任务，按层级拆分（core/viewmodel/view/persistence/config/docs/tests）
2. **脆弱测试策略**: 删除 7 项纯变更探测器测试（XAML 字符串匹配、对话框签名检查），重构 3 项反射类测试为行为断言

## Open Questions

- 无。全部关键决策已确认。
