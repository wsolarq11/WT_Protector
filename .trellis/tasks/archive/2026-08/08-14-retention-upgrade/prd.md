# WT Launcher 留存体验升级：快速调用 / 数据健康 / 迁移状态 / 分发

## Goal

把 2026-08-14 的“值得做”评估收敛成一个可独立规划、实现、验证的 Trellis 任务树。本任务作为父任务只拥有需求总账、子任务映射和跨子任务验收约束；具体交付在子任务中实现，不在父任务直接编码。

## Background（已确认事实）

- 当前 `config/launcher.json` 有 16 个条目，11 个有启动时间，累计启动 15 次，说明不是空壳项目。
- 当前 4 个条目目录已失效：`phoenix-crawler`、`Data_Acquisition_and_Analysis` 及其 claude 对应项。
- `dotnet test -c Release` 当前 752/752 通过；发布 exe 可启动并渲染窗口。
- 仓库已有 README、feature-reality-north-star-audit、feature-first-class-upgrade-audit、design-benchmark-digest、giants-shoulders，作为本任务树的证据来源。
- 既有架构不变量：单文件 `config/launcher.json` 权威源、Core 无 Avalonia 依赖、StoreGrain/rollback、无裸 catch、设计 token、`tools/verify.ps1` 门禁。

## Child Task Map

| 逻辑组 | 子任务 | 一句话范围 |
|---|---|---|
| 即时调用 | `08-14-global-quick-launch` | 托盘常驻 + 全局热键 + 输入即启动 |
| 数据健康 | `08-14-stale-directory-recovery` | 失效目录可见化 + 一键改路径/删除 |
| 可移植与可观测 | `08-14-import-export-migration` | 配置导出导入 + 路径映射 |
| 可移植与可观测 | `08-14-run-history-status` | 持久化最近启动/失败回执 |
| 分发留存 | `08-14-distribution-update-signing` | 安装器/签名/更新通道 |

## Cross-Child Requirements

- 每个子任务保持“单文件配置 + 原子写入 + 回滚”边界；不新增 AppData/registry/DB 作为第二真相。
- 启动行为必须继续复用 preflight/confirmation/receipt 边界；不把“已请求启动”升级成“终端健康完成”。
- 任何 UI 改动遵守现有 token 与单源投影规则；任何持久化改动经过 `LauncherStore`。
- 建议执行顺序：`stale-directory-recovery` → `global-quick-launch` → `import-export-migration` / `run-history-status` → `distribution-update-signing`。后三项可并行，但依赖前两项的稳定体验。

## Acceptance Criteria

- [ ] 父任务 PRD 存在且列出子任务映射、跨任务约束、推荐顺序。
- [ ] 五个子任务均有 PRD，包含 Goal、Requirements、Acceptance Criteria、Out of Scope、Open Questions。
- [ ] 所有任务状态保持 `planning`，未运行 `task.py start`。
- [ ] 子任务之间无重复可交付物；依赖关系写入各自 PRD。

## Out of Scope

- 本轮不实现任何产品代码。
- 不把 WT Launcher 做成 broad launcher/plugin/cloud 平台。
- 不自动修复目录、不静默迁移配置、不强制更新。