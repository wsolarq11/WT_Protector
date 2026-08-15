# 验证台账 — docs/ponytail-upgrade-assessment.md

> 复核日期: 2026-08-15（只读验证；未运行 dotnet/verify.ps1）
> 文档日期: 2026-07-25（创建提交 9df5ceec）

| 声明 | 判定 | 证据 | 处理 |
|------|------|------|------|
| 生成日期 2026-07-25 | VERIFIED | git log: 9df5ceec 2026-07-25 | 保留 |
| 范围: verify.ps1 中 3 个 ponytail: 局限 | VERIFIED | 3 处 ponytail 注释均在：625(no-stale)/651(settings-single-source)/663(winget) | 保留 |
| 行号 no-stale-three-file-refs verify.ps1:529 | STALE | 当日(9df5ceec)实际 550；当前 646 | 更新为 646 |
| 行号 settings-single-source-buildstorefile verify.ps1:555 | STALE | 当日实际 564；当前 660 | 更新为 660 |
| 行号 winget-config-schema-valid verify.ps1:567 | STALE | 当日实际 583；当前 679 | 更新为 679 |
| CI 运行在 windows-latest | VERIFIED | .github/workflows/verify.yml 4 个 job（build-and-test/nuget-audit/aot-verify/full-gate）均 runs-on: windows-latest | 保留 |
| .\tools\apply-machine-config.ps1 -DryRun 为完整验证路径 | VERIFIED | tools/apply-machine-config.ps1:2 [switch]$DryRun；:18 分支 | 保留 |
| 3 项局限均无需升级的结论 | VERIFIED(基座) | 3 个门禁名均存在且 ponytail 注释内容与文中描述一致 | 保留（当日评估结论） |
| 升级成本/风险评估内容 | 保留 | 当日评估叙述 | 保留 |

处理汇总: 3 处行号更新为复核日当前值；其余保留。
