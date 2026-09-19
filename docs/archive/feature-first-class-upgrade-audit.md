Status: Archived
Purpose: 记录 2026-06-10 全功能一等公民审计的星级结论与缺口账本。
Original scope: HEAD `22f57a9` 的 Avalonia 主线、根配置、发布/验证脚本与文档。

# WT Protector 全功能现实问题能力评估与一等公民改造建议

## 核心结论

- 窄域一等公民，不堆积功能：23 项能力全部 3 星以上，17 项 5 星、5 项 4 星、1 项 3 星、0 项严重缺口。
- 真实缺口不是新功能，而是 6 项 3-4 星功能的测试不足或用户路径不完整，数据结构本身正确。

| 星级 | 数量 | 功能 |
| --- | --- | --- |
| 5 星 | 17 | 1,2,4,5,6,7,8,9,10,14,16,17,18,19,20,21,23 |
| 4 星 | 5 | 11,12,13,15,22 |
| 3 星 | 1 | 3 |
| ≤2 星 | 0 | — |

## 6 项降星原因（相对昨日全 5 星审计）

| 功能 | 星级 | 关键证据 |
| --- | --- | --- |
| F3 Search/filter | 3 | `searchHistory` 当时仅被收集未渲染（死数据）；无去抖、无 Escape 取消聚焦 |
| F11 Drag reorder | 4 | `LauncherDragReorderWorkflow` 只有构造函数测试，Preview/Commit/Cancel 仅集成覆盖 |
| F12 Sort modes | 4 | `ParseLaunchedAt`（宽松）与 `ParseUpdatedAt`（严格）不对称；recent 无 `ValidateSortProjection` |
| F13 View modes | 4 | Details 列宽硬编码、滚动条容器不一致导致焦点/滚动位置丢失 |
| F15 Global confirmation | 4 | 专项测试仅 1 个、不覆盖 preset 路径、默认回调 false 无法区分“未注入”与“用户拒绝” |
| F22 Command templates | 4 | `LauncherCommandTemplateResolver` 零专用单测，4 条路径仅间接覆盖 |

## 关键证据

- `config/launcher.json`：19 entries / 19 唯一 id / 19 存在目录；2 presets（`core-projects`、`blank-manual`）；`sortMode=custom`、`entryViewMode=cards`、`requireConfirmationForLaunches=false`、3 commandTemplates、2 searchHistory。
- `git diff --check` PASS；`dotnet test` 542 PASS；`npm run verify` 16/16 root checks structural PASS（GUI smoke 非 Windows 跳过）。
- 原子写入：`LauncherJson` temp-file → `.bak` rotation → `File.Replace`；save-first-then-commit 回滚在 `StoreGrain.Dispatch`（快照类 2026-07-21 移除）。

## 剩余缺口账本

| 缺口 | 类型 | 建议 |
| --- | --- | --- |
| `searchHistory` 无 UI 表面 | 实现 | 加 autocomplete |
| `LauncherDragReorderWorkflow` 无隔离测试 | 测试 | 补 Preview/Commit/Cancel 单测 |
| ParseLaunchedAt/ParseUpdatedAt 不对称 | 设计 | 统一严格度 |
| Details 列宽硬编码 + 滚动条不一致 | 实现 | 低优，从 DesignTokens 引用 |
| Clone entry 零测试 | 测试 | 补 Clone 专用测试 |
| Command template resolver 零测试 | 测试 | 补 xUnit 合约测试 |

## 不要做的蠢事

不做插件系统；不做 broad launcher 平台；不做隐藏 fallback/默认模板/自动修配置；不拆 `selected`；不冒充终端会话健康完成；不给 searchHistory 加复杂搜索。

## 复核注记（2026-08-15）

快照哈希 `22f57a9` 在当前 git 历史中不可解析（`git cat-file -t 22f57a9` 失败），保留为当日记录；同期提交 `61f7716`（2026-06-10）仍可解析。

## 关联路径

- 功能声明账本：`docs/feature-reality-north-star-audit.md`
- 设计基准：`docs/design-benchmark-digest.md`
- 数据结构：`src/avalonia-launcher/src/WTLauncher.Core/LauncherStoreFile.cs`
