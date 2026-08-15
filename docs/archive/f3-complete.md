> Historical record — completed 2026-07-23; archived 2026-08-15 to docs/archive/. Content is a point-in-time record, not current state.
# F3 完成报告: EntryId 同形字保护

## 安全审计发现
**风险等级**: MEDIUM
**威胁类型**: 同形字攻击 (Homoglyph Attack)
**原问题**: EntryId 使用 FormC 规范化但不处理跨脚本同形字,攻击者可通过视觉相同的不同 ID(Cyrillic 'а' U+0430 vs Latin 'a' U+0061)实现 UI 欺骗。

## trusted-local-config 威胁模型评估
在 trusted-local-config 模型下,配置由用户信任的本地应用管理。然而:
- 用户可能被诱导创建视觉相同的重复条目
- 攻击者获得本地访问后可修改 config,插入同形 ID
- 通过预设/批量操作传播恶意 ID 会造成混淆
- 同形攻击对非技术用户尤其有效

实施 Unicode 脚本白名单是预防性安全措施,成本低、零误报,即使威胁模型变更也能持续提供保护。

## 实现方案

### 方案选择: Latin 脚本白名单
基于 Unicode 脚本类别(Unicode Script Property),仅允许 Latin 和 Common/Inherited 脚本字符。

**关键设计决策:**
1. 使用白名单模式而非黑名单,避免遗漏新添加的 Unicode 脚本
2. 在 `Parse()` 中集成,同一验证路径覆盖所有 EntryId 入口
3. 使用 `Rune.EnumerateRunes()` 正确处理 surrogate pair

### 修改文件

**src/WTLauncher.Core/EntryId.cs**
- 新增 `LatinScriptRanges`: 定义 Latin/Common 脚本的 Unicode 码点范围(含 Basic Latin、Latin-1 Supplement、Latin Extended A-E、IPA Extensions、Combining Diacritical Marks、Latin Extended-F)
- 新增 `EnsureLatinScript()`: 遍历字符验证码点是否在允许范围内,拒绝非 Latin 字符
- 在 `Parse()` 中 `StripUnsafeChars` 之后调用 `EnsureLatinScript`

**tests/WTLauncher.Tests/LauncherEntryIdsTests.cs**
- `Parse_RejectsCyrillicHomoglyph`: Cyrillic 'а'(U+0430)抛出 FormatException
- `Parse_RejectsGreekScript`: Greek alpha(U+03B1)抛出 FormatException
- `Parse_AcceptsExtendedLatinCharacters`: 接受 Latin accented 字符(café, José)
- `Same_DistinguishesLatinFromCyrillicHomoglyph`: Same()正确区分 Latin 'a' 与 Cyrillic 'а'
- 已有 surrogate pair 测试更新为使用 Latin Extended-F 范围(U+10780)

### 验证结果
- 全部 677 个测试通过,0 失败,0 警告
- Core 项目编译零错误

## 影响范围
- 已有的非 Latin EntryId 将通过 Normalize() 返回空字符串,不再可用
- 所有 EntryId 入口(Parse、Normalize、SameNormalized、ToRequiredUniqueList、ToRequiredSet、RequireEntryId)均已覆盖
- JSON 反序列化(EntryIdJsonConverter)亦通过 Parse() 继承保护

## 后续建议
1. 如果将来需要支持非 Latin 脚本,扩展 LatinScriptRanges 数组即可
2. 考虑在迁移文档中说明此变更对已有非 Latin EntryId 的影响
