# 测试体系结构路线图落实 - PRD

## 需求

按 `docs/roadmap-testing-structure.md` 落实 P0-P7：

1. P0 测试地图与基线
2. P1 测试层级标记与快速反馈
3. P2 测试目录/命名空间分层
4. P3 语义化 UI 契约
5. P4 关键旅程 E2E 冒烟
6. P5 覆盖率与质量门禁
7. P6 CI 与 verify 去重
8. P7 文档与整体验收

## 约束

- 不改变启动器产品行为。
- 不降低现有测试覆盖质量。
- `config/launcher.json` 不作为测试目标，测试只使用临时配置。
- 每个子任务独立可验证、可归档。
- 全仓门禁 `tools/verify.ps1` 最终必须 PASS。

## 验收

- 所有子任务已按 Trellis 创建、实现、验证、归档。
- 测试地图、层级标记、快速反馈脚本、覆盖率门禁、CI 去重均落地。
- `dotnet build -warnaserror` PASS。
- `dotnet test -c Release --no-restore` PASS。
- `tools/verify.ps1` PASS。
