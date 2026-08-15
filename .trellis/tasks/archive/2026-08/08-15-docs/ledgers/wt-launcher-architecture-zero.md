# 验证台账 — docs/research/wt-launcher-architecture-zero.md

> 验证日期：2026-08-15 ｜ 协议：.trellis/tasks/08-15-docs/verification-protocol.md
> 文档性质：带日期退路方案记录（研究 2026-07-06 / 状态头 2026-07-10）→ HISTORICAL（提案保留）

| 声明 | 判定 | 证据（命令+结果） | 处理 |
|---|---|---|---|
| 研究日期 2026-07-06、状态头 2026-07-10 | VERIFIED | git log --follow → 61c6bc64 2026-07-06 创建；2af3be2d 2026-07-10 reposition as contingency | 日期已存在 |
| 退路方案定位（非行动方案） | VERIFIED | AGENTS.md：research/ 含 wt-launcher-architecture-zero.md 退路方案，非行动方案；文档自身状态头也声明 | 保留 |
| "1 solution, 3 projects, 1 CI pipeline" | VERIFIED | git ls-tree 61c6bc64 → WTLauncher.Avalonia.sln + WTLauncher.App/Core/Tests 3 个 csproj + .github/workflows/verify.yml | 保留 |
| "~10,000 source lines, ~9,000 test lines" | STALE（数字不符） | 61c6bc64 实测：非测试 .cs ≈ 7,981 行、测试 .cs ≈ 7,509 行 | 已按实测改写（保留日期） |
| "2 NuGet dependencies" | STALE（数字不符） | 61c6bc64 实测 PackageReference：App=5、Core=1、Tests=6（6 个声明） | 已按实测改写 |
| "457 tests, 49 files" | STALE（数字不符） | 61c6bc64 实测：[Fact]/[Theory] 属性 418、测试文件 63 | 已按实测改写（出现 2 处） |
| "Current launcher.json has 19 entries" + "core-projects preset (2 entries)" | UNVERIFIABLE（时点条目数） | 61c6bc64 无被跟踪的 config/launcher.json（git show → 'path exists on disk, but not in 61c6bc64'）；当时被跟踪配置为 retired/config/config.json（18 行平铺结构，无 id 键）；2026-08-15 实测当前 config/launcher.json：12 entries / 2 presets（core-projects 在列） | 已标注不可核实并给出当前实测 |
| 表格行数分项（~8,000 src / ~4,000 VM / ~500 VO / ~1,000 持久化 / ~200 构建 / ~9,000 测试 / ~22,000 总计） | UNVERIFIABLE（估算） | 分项拆分无法逐项核实；实测总量：src ≈ 7,981、测试 ≈ 7,509（合计 ≈ 15,490，非 ~22,000） | 表格加脚注标注估算并给出实测总量；Tests 行已按实测改写 |
| Option A/B/C（WT Profiles / PowerShell / Console App）与启动命令示例 | HISTORICAL | 提案内容（退路方案），按 AGENTS.md 非行动方案 | 保留完整 |
| Windows Terminal 能力表（wt -d / new-tab / split-pane / startupActions 等） | UNVERIFIABLE（外部产品事实） | 仓库无证据可查；2026-07-06 调研记录 | 保留（研究记录）；验证记录注明 |
| 风险表（模板变量/多选/迁移/搜索 UI 损失） | HISTORICAL | 时点评估 | 保留 |
| 结构数字（53/53/15/15/24/20/7/121/728/38） | 不适用 | 本文无结构数字 | — |

## 处理总结
- 判定：VERIFIED 4 项 / STALE 4 项 / UNVERIFIABLE 4 项 / HISTORICAL 3 项
- 重写：① 新增 2026-08-15 验证记录块（置于状态头之后）；② 正文数字按 61c6bc64 实测改写（源码/测试行数、NuGet 声明数、测试文件与属性数、19 entries 标注）；③ 表格加脚注
- 提案（Option A/B/C）完整保留；未触碰 AGENTS.md TRELLIS 块、config/launcher.json、源码