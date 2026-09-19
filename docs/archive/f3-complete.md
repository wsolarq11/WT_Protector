Status: Archived
Purpose: 记录安全审计 F3（EntryId 同形字保护）的发现、方案与验收。
Original scope: 2026-07-23 完成，677 个测试通过。

# F3 完成报告: EntryId 同形字保护

## 安全审计发现

- 风险等级：MEDIUM，同形字攻击（Homoglyph Attack）。
- 原问题：EntryId 用 FormC 规范化但不处理跨脚本同形字（Cyrillic `а` U+0430 vs Latin `a` U+0061），可实现 UI 欺骗。

## 结论

实施 Unicode 脚本白名单（Latin + Common/Inherited）作为预防性措施，成本低、零误报，即使威胁模型变更仍持续有效。

## 关键证据

| 文件 | 改动 |
| --- | --- |
| `src/WTLauncher.Core/EntryId.cs` | 新增 `LatinScriptRanges`（Basic Latin、Latin-1 Supplement、Latin Extended A-E、IPA、Combining Diacritical Marks、Latin Extended-F）；新增 `EnsureLatinScript()`，在 `Parse()` 中 `StripUnsafeChars` 之后调用 |
| `tests/WTLauncher.Tests/LauncherEntryIdsTests.cs` | 新增 Cyrillic/Greek 拒绝、extended Latin 接受、Latin/Cyrillic 区分测试；surrogate pair 测试改用 Latin Extended-F（U+10780） |

## 设计决策

1. 白名单而非黑名单，避免遗漏新增 Unicode 脚本。
2. 在 `Parse()` 集成，同一验证路径覆盖所有 EntryId 入口。
3. 使用 `Rune.EnumerateRunes()` 正确处理 surrogate pair。

## 影响范围

- 已有非 Latin EntryId 经 `Normalize()` 返回空字符串，不再可用。
- 所有入口（Parse、Normalize、SameNormalized、ToRequiredUniqueList、ToRequiredSet、RequireEntryId）已覆盖。
- JSON 反序列化（`EntryIdJsonConverter`）经 `Parse()` 继承保护。

## 验证

- 全部 677 个测试通过，0 失败、0 警告；Core 编译零错误。

## 后续建议

如需支持非 Latin 脚本，扩展 `LatinScriptRanges` 数组即可；迁移文档应说明对已有非 Latin EntryId 的影响。
