# Gate 覆盖度扩展评估

评估时间: 2026-07-23
评估范围: 截至评估日 `tools/verify.ps1` 中的 25 项检查

> 复核（2026-08-15）：门禁现为 **40 项检查**（与 AGENTS.md 结构基线一致）。原 25 项检查全部保留（无移除/改名），另新增 15 项（见文末「当前门禁状态」）。下表行号为 2026-07-23 当日版本（与提交 ee2a85a9 逐行一致）；当前行号见文末对照。

---

## 现有检查清单（截至 2026-07-23，共 25 项）

`tools/verify.ps1` 在评估日包含以下 25 项检查（按出现顺序）：

| 编号 | 检查名称 | 行号 | 类别 |
|------|---------|------|------|
| 1 | line-ending-policy | 41 | 基础设施 |
| 2 | snow-ignored-local-state | 55 | 基础设施 |
| 3 | retired-paths-removed | 65 | 废弃清理 |
| 4 | no-bare-catch-exception | 87 | 编码规范 |
| 5 | app-raw-hex-zero | 135 | 编码规范 |
| 6 | mainline-layout | 148 | 项目结构 |
| 7 | readme | 171 | 文档完整性 |
| 8 | feature-objective-boundary | 197 | 项目声明 |
| 9 | claim-boundary-ledger | 237 | 项目声明 |
| 10 | github-similar-projects-snapshot | 336 | 外部基准 |
| 11 | json | 363 | 配置验证 |
| 12 | config-schema | 378 | 配置验证 |
| 13 | presets-schema | 416 | 配置验证 |
| 14 | settings-json | 435 | 配置验证 |
| 15 | shortcut-backup-retirement | 478 | 废弃清理 |
| 16 | winforms-retired-design | 491 | 废弃清理 |
| 17 | hta-retired-config | 496 | 废弃清理 |
| 18 | avalonia-mainline-verify | 515 | 主线验证 |
| 19 | no-stale-three-file-refs | 550 | 文档一致性 |
| 20 | settings-single-source-buildstorefile | 564 | 架构规则 |
| 21 | winget-config-schema-valid | 583 | 环境配置 |
| 22 | no-imperative-view-ui | 604 | 编码规范 |
| 23 | security-audit-f1-path-getfullpath | 617 | 安全审计 |
| 24 | security-audit-f2-preset-entryid-validate | 634 | 安全审计 |
| 25 | security-audit-f3-homoglyph-protection | 651 | 安全审计 |

**类别分布**:

| 类别 | 数量 | 占比 |
|------|------|------|
| 配置验证 | 4 | 16% |
| 废弃清理 | 3 | 12% |
| 编码规范 | 3 | 12% |
| 安全审计 | 3 | 12% |
| 项目声明 | 2 | 8% |
| 基础设施 | 2 | 8% |
| 文档完整性 | 1 | 4% |
| 项目结构 | 1 | 4% |
| 外部基准 | 1 | 4% |
| 主线验证 | 1 | 4% |
| 架构规则 | 1 | 4% |
| 环境配置 | 1 | 4% |
| 文档一致性 | 1 | 4% |

---

## 评估：是否存在需要补充的领域

### 1. 编译和测试覆盖

当前门禁通过 `avalonia-mainline-verify` 间接验证构建和测试结果（该门禁运行 `restore -> build -> test -> publish -> GUI smoke` 并生成 receipt）。这是端到端验证，覆盖面充分。

**结论**: 不需要补充。

### 2. NuGet 包一致性

当前没有检查 NuGet 包版本一致性（如 `Directory.Packages.props` 中的 Central Package Management 版本）。虽然 `avalonia-mainline-verify` 中的 `dotnet restore/build` 会暴露版本冲突，但不会暴露"使用了过时/不安全的包版本"。

**评估**: 低风险。如果引入安全检查（如 NuGet 漏洞扫描），可考虑添加。但当前阶段不需要独立门禁。

### 3. .editorconfig 强制

当前没有检查 `.editorconfig` 是否存在或生效。目前 `.editorconfig` 已存在于仓库根目录，但未被门禁保护。

**评估**: 低风险。`no-imperative-view-ui`、`app-raw-hex-zero`、`no-bare-catch-exception` 等编码规范门禁已覆盖了 .editorconfig 中最关键的规则。但 `.editorconfig` 本身可能被意外删除或修改而未引起注意。

**结论**: 可以考虑添加一个轻量门禁检查 `.editorconfig` 的存在性和基本规则。

### 4. DEAD CODE 门禁

参考 `deadcode-gate-assessment.md`（如果存在）的结论。当前没有独立的 DEAD CODE 门禁。项目通过 `retired-paths-removed`、`shortcut-backup-retirement`、`winforms-retired-design`、`hta-retired-config` 等门禁检查已标记为废弃的路径和文件。

**评估**: 当前门禁覆盖了"已知废弃代码"的清理验证，但不覆盖"未知死代码"的检测。后者更适合通过定期审计或 IDE 分析进行，而非 CI 门禁。死代码门禁容易产生噪音，且维护成本高。

**结论**: 不需要添加独立的 DEAD CODE 门禁。当前方式（手动清理 + 废弃路径门禁）更适合此项目规模。

### 5. 测试覆盖率阈值

当前没有测试覆盖率百分比门禁（如 `dotnet test --collect:"XPlat Code Coverage"` 并检查覆盖率 > 80%）。

**评估**: 中低风险。`avalonia-mainline-verify` 要求所有测试通过（零失败），但允许测试数量减少或覆盖率下降。如果团队希望防止测试覆盖率退化，可考虑添加。

**结论**: 建议添加但不强制。92 个测试文件（2026-07-23 当日复核值；现为 122 个）是良好的测试基础，但覆盖率门禁可能产生"为覆盖率而写测试"的激励扭曲。

### 6. 文档链接完整性

当前 `no-stale-three-file-refs` 检查文档中的过期架构引用，但不检查其他类型的文档问题（如断链、过时的说明）。

**评估**: 低风险。该项目的文档数量有限且核心文档由门禁直接检查。

**结论**: 不需要补充。

### 7. spec 文档一致性

当前没有检查 `.trellis/spec/` 下的 spec 文档是否与代码实现一致。

**评估**: 低风险。Spec 文档是契约和设计记录，而非精确的代码规范。强制同步会大幅增加维护成本。项目通过 trellis-check 等工具在开发工作流中验证一致性，而非 CI 门禁。

**结论**: 不需要补充。

### 8. 窗口/视图资源泄漏

当前 `no-imperative-view-ui` 防止命令式 UI 控件构造，但不检查 `.axaml` 文件中的资源泄漏（如未使用的 StaticResource 引用）。

**评估**: 极低风险。该检查更适合 IDE/分析器，而非 CI 门禁。

**结论**: 不需要补充。

### 9. 安全审计后的持续验证

F1/F2/F3 安全审计门禁已覆盖了最新的安全发现。但如果未来发现新的安全漏洞类别，应添加新的门禁。

**评估**: 当前三个安全门禁覆盖已实施的安全审计。新的安全发现应伴随新的门禁。

**结论**: 不需要预先添加。按需添加即可。

---

## 建议补充的检查

基于上述评估，**建议补充 2 项轻量检查**（非紧急）：

### 建议 A: .editorconfig 准入检查

```powershell
# 轻量门禁：检查 .editorconfig 存在性及关键规则
$editorconfigPath = Join-Path $root '.editorconfig'
if (-not (Test-Path -LiteralPath $editorconfigPath)) {
    throw 'Missing .editorconfig coding-style policy'
}
$editorconfig = Get-Content -Raw -Encoding UTF8 -LiteralPath $editorconfigPath
foreach ($required in @('indent_style = space', 'indent_size = 4', 'charset = utf-8')) {
    if (-not $editorconfig.Contains($required)) {
        throw ".editorconfig missing required rule: $required"
    }
}
Add-Check 'editorconfig' @{
    path = '.editorconfig'
    indent_style = 'space'
    indent_size = '4'
    charset = 'utf-8'
    pass = $true
}
```

**理由**: `.editorconfig` 是编码风格的基础设施，当前未被任何门禁保护。

### 建议 B: 测试计数退化检测

```powershell
# 轻量门禁：记录测试计数，防止无意的测试删除
# （仅记录，不阻断——由审查判断退化是否合理）
$testProject = Get-AvaloniaTestsProjectPath
$testFileCount = @(Get-ChildItem -LiteralPath (Split-Path $testProject -Parent) -Recurse -Filter '*Tests.cs').Count
Add-Check 'test-file-count' @{
    count = $testFileCount
    threshold = 60
    pass = $testFileCount -ge 60
}
```

**理由**: 防止测试文件被无意识地删除。60+ 的测试文件基线是项目成熟度的体现。

> 复核（2026-08-15）：上述两项建议均**未纳入**门禁——当前 40 项中不存在 `editorconfig` 或 `test-file-count` 检查。测试数量退化现由 `structure-baseline-consistency`（129 测试文件、749 属性基线）与 `coverage-gate` 间接约束。

---

## 结论

**截至评估日的 25 项检查覆盖面已经非常全面**。对比类似规模的 Avalonia/.NET 项目，此门禁体系是行业领先水平：

- 基础设施（2 项）: .gitattributes, .gitignore
- 废弃清理（3 项）: 废弃路径、快捷方式、WinForms/HTA 配置
- 编码规范（3 项）: catch 异常、颜色字面量、命令式 UI
- 配置验证（4 项）: JSON schema、entries、presets、settings
- 安全审计（3 项）: 路径规范化、Preset entryId 验证、同形字保护
- 架构规则（1 项）: BuildStoreFile 单源
- 外部基准（1 项）: GitHub 相似项目快照
- 项目声明（2 项）: 功能审计、声明边界账本
- 主线验证（1 项）: 端到端构建+测试+发布

**不需要紧急补充新门禁**。建议的两项补充（`.editorconfig`、测试计数）属于"锦上添花"，可在下次门禁重构时一并考虑。

---

## 附：当前门禁状态（2026-08-15 复核，40 项）

原 25 项全部保留，新增 15 项：

| 新增检查名称 | 当前行号 | 类别 |
|------|---------|------|
| config-example-schema | 487 | 配置验证 |
| portable-installer-contract | 537 | 分发契约 |
| update-manifest-generation | 547 | 更新清单 |
| test-structure-layering | 560 | 测试结构 |
| ci-fast-lane-no-publish-duplication | 567 | CI 去重 |
| coverage-gate | 603 | 覆盖率门禁 |
| architecture-rule-check | 616 | 架构规则 |
| analyzer-wiring | 623 | 分析器连线 |
| dialog-service-no-func-action | 712 | 视图契约 |
| views-no-direct-core-business-calls | 728 | 视图契约 |
| no-thin-wrapper-delegation | 775 | 编码规范 |
| vtk-architecture-integrity | 799 | 架构完整性 |
| trinity-architecture-integrity | 824 | 架构完整性 |
| vtk-no-appstore-residue | 849 | 残留清理 |
| structure-baseline-consistency | 910 | 结构基线 |

原 25 项在当前版本中的行号对照：41 / 55 / 65 / 87 / 135 / 148 / 171 / 197 / 237 / 336 / 363 / 378 / 416 / 435 / 505 / 518 / 523 / 581 / 646 / 660 / 679 / 700 / 713 / 730 / 747。
