# Ponytail 局限性评估报告

审查时间: 2026-07-23
审查范围: `tools/verify.ps1` 中所有 `# ponytail:` 注释标注的已知局限性

> 复核（2026-08-15）：文中 6 个门禁名称全部仍在当前 `tools/verify.ps1`（现共 43 项检查）；行号已更新为复核日当前值（原行号与 2026-07-23 当日版本及当前版本均不符，逐项证据见验证台账 `.trellis/tasks/08-15-docs/ledgers/ponytail-assessment.md`）。当日 ponytail 注释共 6 处，复核时为 10 处。

---

## 1. no-stale-three-file-refs (verify.ps1:646)

### 当前实现

```powershell
$activeDocs = @(
    'README.md', 'AGENTS.md', 'DESIGN.md',
    'docs\\feature-reality-north-star-audit.md',
    'docs\\feature-first-class-upgrade-audit.md',
    'docs\\giants-shoulders\\launcher-first-class-patterns.md'
)
```

该门禁检查 `$activeDocs` 列表中的文档是否引用了已废弃的 `config/(config|presets|settings).json` 三文件架构。

### 局限性评估

**1.1 `$activeDocs` 覆盖范围不足**

当前仅覆盖 6 个文档。仓库中还有其他文档可能描述当前单文件 launcher.json 契约，但未被检查：

- `docs/design-benchmark-digest.md` — 包含设计基准，可能引用配置路径
- `docs/architecture-reform-proposal.md` — 架构改造提案，可能引用旧架构
- `docs/architecture-research-report.md` — 架构研究，可能引用旧结构
- `docs/exemplar-gap-refactor-audit.md` — 示例差距审计
- `docs/feature-first-class-upgrade-audit.md` — 功能审计（已在列表中）
- `docs/giants-shoulders/codexswitch-avalonia-native-aot.md` — 技术文档

**评估结论**: 当前 `$activeDocs` 覆盖了最关键的核心文档（README、AGENTS、DESIGN、功能审计、模式文档），这些文档最可能描述配置契约。`docs/research/` 和 `docs/reference/` 中的文档不太可能引用旧三文件架构，因为它们是历史快照或外部参考。**不需要立即扩展 `$activeDocs`**，但有以下建议：

- 新文档描述 launcher.json 契约时，应同时将其加入 `$activeDocs`
- `docs/design-benchmark-digest.md` 可考虑加入，因为它包含验收规则
- `docs/architecture-reform-proposal.md` 作为历史提案，即使引用旧架构也是合理的——但应在 ponytail 注释中显式豁免

**1.2 仅检查 Active Docs 本身**

门禁不检查 `config/launcher.json` 本身（不是文档）或 `src/` 中的注释。但源码中引用旧三文件架构的注释在重构中已被清除，且 `launcher.json` 本身就是单文件格式。**此局限性在当前状态下可接受。**

### 建议

- 在 ponytail 注释中显式列出已评估但未加入 `$activeDocs` 的文档及其理由
- 在代码审查中增加"新文档加入 `$activeDocs`"的检查项

---

## 2. settings-single-source-buildstorefile (verify.ps1:660)

### 当前实现

```powershell
$signatureCount = ([regex]::Matches($vmSource,
    'Entries = _list\\.MutableEntries\\.Select\\(ToLaunchEntry\\)\\.ToImmutableList\\(\\),\\s*\\r?\\n\\s+Settings =')).Count
```

该门禁确保 `MainWindowViewModel.cs` 中恰好有 1 个 `LauncherStoreFile` 手写汇编点（即 `BuildStoreFile` 方法体）。如果计数不为 1，说明存在双路径回归。

### 局限性评估

**2.1 指纹依赖特定代码格式**

正则表达式匹配以下精确模式：

```
Entries = _list.MutableEntries.Select(ToLaunchEntry).ToImmutableList(),
<换行+空白>Settings =
```

如果：
- `_list` 被重命名为其他字段名
- `ToLaunchEntry` 被重命名
- `MutableEntries` 属性被重命名
- 换行格式发生根本变化（虽然 `\r?\n` 已覆盖 CR/LF 差异）
- `BuildStoreFile` 方法被提取到另一个类

则门禁会漏检。

**2.2 加固评估**

- 当前 `BuildStoreFile` 已稳定，上述标识符自 task `07-09-settings-single-source` 以来未变化
- 重构风险中等——如果未来重构涉及 `MainWindowViewModel` 的拆分或 `LauncherStoreFile` 的更改，该门禁可能失效
- **加固方案**: 添加第二扫描模式，匹配 `BuildStoreFile` 方法签名的特征（如 `LauncherStoreFile BuildStoreFile()`），通过语义匹配而非仅内容匹配

### 建议

- 当前指纹足够鲁棒，因为正则匹配了跨行模式，降低了意外误报
- 建议在重构 `MainWindowViewModel` 时，同步更新此门禁的正则模式
- 长期加固：增加 `BuildStoreFile` 方法级别的扫描（如检查方法名出现次数），降低对内部变量名的依赖

---

## 3. winget-config-schema-valid (verify.ps1:679)

### 当前实现

该门禁检查 `.wt-protector.dsc.yaml` 存在性及其包含 4 个必需标记。

### 局限性评估

**3.1 无 DSC v3 时无法执行完整 winget 验证**

如 ponytail 注释所述，`winget configure validate` 需要 DSC v3，这是新机配置依赖而非开发依赖。当前仅做结构标记验证。

**3.2 是否需要增强**

当前验证水平对于 CI 门禁是合理的，因为：
- 结构标记验证可捕获大多数常见错误（缺失 schema、错误版本、缺 ID）
- 完整 winget validate 需要 DSC v3，不适合在普通 dev/CI 环境中运行
- `apply-machine-config.ps1 -DryRun` 提供了带完整验证的替代路径

**潜在增强方案**:

1. **YAML 结构深度验证** — 使用 PowerShell 解析 YAML 并验证层次结构，不仅做字符串匹配
2. **资源类型验证** — 检查 resources 数组不为空且每个 resource 有合理的 type/properties
3. **依赖顺序验证** — 检查 `dependsOn` 不引用不存在的 resource

但这些增强会增加门禁维护成本。**当前水平已足够防止常见的配置漂移。**

### 建议

- 保持当前水平，因为 YAML 结构验证在 PowerShell 中需要额外依赖
- 如果未来 DSC v3 变得普遍，可考虑添加条件性完整验证（检测到 DSC v3 时运行 `winget configure validate`）

---

## 4. 安全审计门禁 (F1/F2/F3) 的 ponytail 注释评估

### F1: security-audit-f1-path-getfullpath (verify.ps1:713)

**注释内容**: 扫描 `DirectoryPath.cs` 中 `Path.GetFullPath(` 的调用。如果规范化逻辑移至其他类或方法，门禁会漏检。

**评估**: 注释准确。当前 `DirectoryPathJsonConverter.Read` 是目录路径规范化的唯一入口。如果未来：
- 添加新的 JSON converter（如 `DirectoryPathListConverter`）
- 在 `LaunchEntry.cs` 中添加环境变量展开
- 将规范化提取到公共工具方法

则需扩展扫描模式。

**充分性**: 充分。注释明确指出升级路径（"add a second scan pattern for the new location"）。

### F2: security-audit-f2-preset-entryid-validate (verify.ps1:730)

**注释内容**: 检查 `IPresetService.cs` 中 `knownEntryIds` 字符串。参数重命名或接口迁移会绕过门禁。

**评估**: 注释准确。`knownEntryIds` 是 F2 安全审计的核心契约。如果：
- 参数重命名为 `validEntryIds` 或 `allowedEntryIds`
- 验证逻辑移至 `IPresetValidator` 新接口
- 签名改为 params 或集合类型

则门禁需更新。

**充分性**: 充分。注释指出升级路径（"add the new interface name to the scan pattern"）。

### F3: security-audit-f3-homoglyph-protection (verify.ps1:747)

**注释内容**: 检查 `EntryId.cs` 中 `LatinScript` 字符串。白名单重命名或移入 helper 类会绕过门禁。

**评估**: 注释准确。如果：
- `LatinScriptRanges` 重命名为 `AllowedScriptRanges`
- `EnsureLatinScript` 方法提取到 `ScriptValidator` 工具类
- 使用不同的脚本白名单策略

则门禁需更新。

**充分性**: 充分。注释指出升级路径（"add the new class/member name to the scan pattern"）。

### 综合评估

三个安全审计门禁的 ponytail 注释遵循了统一模式：
1. 明确指出扫描目标
2. 标识重构时可能绕过门禁的变更类型
3. 提供升级路径

这种模式足够充分。所有三个门禁都是基于字符串匹配的回归门禁，而非语义验证——这是设计权衡，使门禁简单、快速、不易误报。ponytail 注释准确记录了此权衡。

---

## 总结

| 门禁 | 风险等级 | 是否需要立即改进 |
|------|---------|----------------|
| no-stale-three-file-refs | 低 | 否，保持当前覆盖面 |
| settings-single-source-buildstorefile | 中低 | 否，可考虑添加方法级备份扫描 |
| winget-config-schema-valid | 低 | 否，当前水平足够 |
| F1 安全审计门禁 | 低 | 否，注释充分 |
| F2 安全审计门禁 | 低 | 否，注释充分 |
| F3 安全审计门禁 | 低 | 否，注释充分 |

所有 ponytail 注释准确记录了已知局限性，升级路径清晰。无紧急改进需求。
