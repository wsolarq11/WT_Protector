Status: Archived
Purpose: 记录测试体系结构改进路线图 P0-P7 的目标、交付与最终结果。
Original scope: `src/avalonia-launcher` 测试体系、CI、Avalonia 门禁。

# 测试体系结构改进路线图

## 结论

- 状态：已完成（P0-P7 已实现并归档）。
- 目标：把“单测试项目 + 全量跑”升级为分层、快速、可维护的测试体系，同时保持完整验收能力。

## 最终结果

- 804 个测试按 7 个业务分类（`CoreUnit`、`Persistence`、`ViewModelBehavior`、`Analyzer`、`Architecture`、`UiContract`、`PropertyTest`）+ `Support/` 物理分层，带 xUnit `Category` Trait。
- XAML 契约改为 XML 语义断言；关键旅程冒烟固定 2 条。
- 覆盖率门禁覆盖 9 条高风险路径；CI 快速层与完整门禁去重。
- 最终指标：120 测试源文件、721 个 `[Fact]`/`[Theory]`、804 用例；快速层约 12.4s、全量约 16.7s。

## 阶段与验收

| 阶段 | 目标 | 关键交付 |
| --- | --- | --- |
| P0 | 测试地图与基线 | `docs/test-structure.md` + 分类定义 |
| P1 | 层级标记与快速反馈 | `[Trait("Category", ...)]` + `tools/test-fast.ps1` |
| P2 | 目录/命名空间分层 | 物理边界（Support/CoreUnit/…/PropertyTest） |
| P3 | 语义化 UI 契约 | 去掉整段 XAML 字符串断言 |
| P4 | 关键旅程 E2E 冒烟 | 独立 E2E 目录，数量 ≤3 |
| P5 | 覆盖率与质量门禁 | coverlet 报告 + 关键路径清单 |
| P6 | CI 与 verify 去重 | 全量测试只执行一轮 |
| P7 | 文档与验收 | 更新 spec/README + 全绿验收 |

## 原则

- 先分类再分层，优先用目录/命名空间分层，不急着拆项目。
- E2E 只保护关键旅程，数量必须少；覆盖率用于关键路径，不设全仓 100% 目标。
- 任何阶段结束 `tools/verify.ps1` 必须仍能 PASS。

## 依赖关系

`P0 -> P1 -> P2 -> P3/P4 -> P5 -> P6 -> P7`；P3、P4 可并行；P1 必须在 P6 前完成。

## 风险与对策

- 批量加 Trait：脚本机械化处理，单独提交；拆目录保留 `Support/`，不拆项目。
- Avalonia E2E 可能 flaky：数量压到 1-3 条，CI 独立 job。
- 覆盖率阈值假安全感：只对关键路径设阈值，全仓只报告不强制。
- XAML 契约迁移暴露规则不清：先把规则写进 `docs/test-structure.md` 再写测试。

## 核验注记（2026-08-15）

- 当前基线以 `AGENTS.md` 结构基线为准：129 测试文件、749 个 `[Fact]`+`[Theory]`、门禁实测 832 用例。
- `-SkipMainlineTests` 参数未加入 `src/avalonia-launcher/verify.ps1`；CI 去重由 `build-and-test` 快速层 + `ci-fast-lane-no-publish-duplication` 门禁实现。

## 关联路径

- 测试结构与文件归属：`docs/test-structure.md`
- 任务归档：`.trellis/tasks/archive/2026-08/`
