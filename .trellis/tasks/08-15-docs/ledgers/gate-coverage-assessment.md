# 验证台账 — docs/gate-coverage-assessment.md

> 复核日期: 2026-08-15（只读验证；未运行 dotnet/verify.ps1）
> 文档日期: 2026-07-23（创建提交 ee2a85a9）

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 评估时间 2026-07-23 | VERIFIED | git log: ee2a85a9 2026-07-23 | 保留 |
| 评估范围: verify.ps1 中 25 项检查 | VERIFIED(当日) | git show ee2a85a9:tools/verify.ps1 恰 25 个 Add-Check | 保留；改述为"截至评估日" |
| 25 项检查名称 | VERIFIED | 25 个名称全部仍存在于当前 verify.ps1（38 项）；无移除/改名 | 保留 |
| 25 项行号表（41…651） | VERIFIED(当日) | 与 ee2a85a9 版逐行一致（41/55/65/87/135/148/171/197/237/336/363/378/416/435/478/491/496/515/550/564/583/604/617/634/651） | 保留为当日记录；另附当前行号对照 |
| 类别分布（4/3/3/3/2/2/1×8） | VERIFIED(当日) | 基于当日 25 项计算，数值自洽 | 保留 |
| avalonia-mainline-verify 端到端（restore→build→test→publish→GUI smoke + receipt） | VERIFIED | Add-Check @ verify.ps1:581；根 verify.ps1 调 src/avalonia-launcher/verify.ps1 | 保留 |
| NuGet 一致性: Directory.Packages.props 存在 | VERIFIED | Test-Path src/avalonia-launcher/Directory.Packages.props OK | 保留 |
| .editorconfig 存在于仓库根目录 | VERIFIED | Test-Path .editorconfig OK | 保留 |
| 62 个测试文件 | STALE | 当日(ee2a85a9) tests/WTLauncher.Tests 下 *.cs = 92 个（git ls-tree）；当前 121 个 | 更正为 92（当日）+ 注明当前 121 |
| deadcode-gate-assessment.md 参考（如果存在） | 文档不存在 | Test-Path docs/deadcode-gate-assessment.md MISS | 保留条件式表述；台账注明不存在 |
| 建议 A: .editorconfig 准入检查 | 未采纳 | 当前 38 项中无 'editorconfig' 检查 | 保留为当日建议；注明未纳入 |
| 建议 B: 测试计数退化检测 | 未采纳 | 当前 38 项中无 'test-file-count' 检查 | 保留为当日建议；注明未纳入 |
| 结论"当前 25 项覆盖面全面" | 改述 | 当日 25 项；当前 38 项（新增 13 项） | 加"截至当日"限定 + 新增当前状态节 |
| 新增 13 项检查（相对当日） | VERIFIED | config-example-schema(487)、portable-installer-contract(537)、update-manifest-generation(547)、test-structure-layering(560)、ci-fast-lane-no-publish-duplication(567)、coverage-gate(603)、architecture-rule-check(616)、analyzer-wiring(623)、no-thin-wrapper-delegation(775)、vtk-architecture-integrity(799)、trinity-architecture-integrity(824)、vtk-no-appstore-residue(849)、structure-baseline-consistency(910) | 新增"当前状态"节 |

处理汇总: "62 个测试文件"更正为 92（当日）/121（当前）；"当前 25 项"改为"截至评估日 25 项"并新增 38 项当前状态节；其余保留。
