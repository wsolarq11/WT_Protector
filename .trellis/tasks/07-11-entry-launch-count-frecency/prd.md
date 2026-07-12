# Entry launchCount + frecency 排序

## Goal

在现有 sort 模式（custom/recent/updated/alphabetical）之外新增 `frequent` 模式：按 zoxide 风格 frecency（frequency × recency 衰减）排序，让"最常用"的 entry 排到顶部。比 `recent`（纯最近启动时间）更能体现真实使用频率 —— 一个频繁使用的 entry 即便 2 天没启动，也应当排在 1 小时前用过一次的 entry 之上。

## Background

- 用户在父任务 `07-11-centralize-declutter` 中明确提出需要"快速启动常用 entry"。`recent` 模式仅反映"最近一次"启动时间，不反映"累积频率"。frecency 通过衰减函数把两者融合。
- 父任务原始诉求："比启动组更为快速简单"的常用入口 —— frecency 排序就是这条诉求的最小落地形态。
- 现有基础：`LaunchEntry.LaunchedAtUtc` 已存在（ISO 8601 UTC，空串表示从未启动），`RecordLaunchFromOwner` 是 launch 记录的单一入口（`Launch.cs:99` 单启 + `Batch.cs:166` 批启），`LauncherEntryVisibility.FilterAndSort` 是 sort 的单一入口。

## Requirements

### R1 — 新增 `LaunchEntry.LaunchCount` 字段（schema 前向兼容）

- 在 `LaunchEntry` 添加 `LaunchCount` int 字段，JSON key `launchCount`，默认 0。
- Optional（无 `[JsonRequired]`）。空/null/缺失 → 0。
- 现有 schemaVersion=2 不变（未知/新增 optional 字段已被 `LauncherJson` 静默接受）。

### R2 — launch 记录时同步递增 LaunchCount

- `RecordLaunchFromOwner` 既要更新 `LaunchedAtUtc` 也要 +1 `LaunchCount`。
- 单一 mutation point：所有 launch 路径（单启 + 批启）都走这个方法，不会遗漏。
- 撤销/回滚路径不变：快照机制已捕获整个 `_entries`，count 字段自动随快照恢复。

### R3 — 新增 `frequent` sort 模式（frecency 评分）

- `LauncherEntryVisibility.FrequentSortModeId = "frequent"`。
- `NormalizeSortModeId` 接受 `frequent`。
- `LauncherSortMode` enum 新增 `Frequent` 项。
- 在 `FilterAndSort` 添加 `frequent` 分支：计算每条 entry 的 frecency score，按 score desc 排序。
- frecency 公式（zoxide 风格，时间衰减）：
  - `count = LaunchCount`，`launchedAt = ParseLaunchedAt(LaunchedAtUtc)`
  - `delta = now - launchedAt`（now 取 `DateTimeOffset.UtcNow`，调用时一次性采样）
  - `score = count * decay`，decay 满足：
    - `delta < 1h` → `4.0`
    - `delta < 24h` → `2.0`
    - `delta < 7d` → `0.5`
    - `else` → `0.25`
  - count=0 → score=0（始终排在最后，不论时间桶）
  - launchedAt=MinValue（从未启动）→ 落入 else 桶，score=count*0.25；若 count 也是 0 → score=0
- Tiebreaker：score 相等时按 name OrdinalIgnoreCase asc，再按 id Ordinal asc（与现有 recent/updated 模式一致）。

### R4 — `frequent` sort 模式持久化

- `LauncherSettings.SortMode` 已是 string，`NormalizeSortModeId` 自动处理。无需 schema 变更。
- `LauncherEntryVisibility.NormalizeSortModeId` 新增 `frequent` 分支（不抛 UnknownLauncherSortMode）。

### R5 — UI：sort 模式 ComboBox 新增"按使用频率"项

- `MainWindow.axaml` 中 sort 模式 ComboBox（已绑定 `SortModeOptions`）新增一项。
- 标签：`按使用频率`（与现有 `按最近使用 / 按更新时间 / 按名称 / 自定义` 同风格）。
- `SelectedSortMode` 推断 `IsFrequentSortMode` 等 projection 不需要新增（现有代码不依赖每个 mode 一个 bool projection，只是 ComboBox SelectedItem 绑定）。

### R6 — Gate 通过

- `tools/verify.ps1` 16 检查全绿，含新增的 frecency 单元测试。
- `-warnaserror` 干净。
- GUI smoke 通过。
- `config/launcher.json` 现有 entries 无 `launchCount` 字段时也能正常加载（前向兼容验证）。

## Acceptance Criteria

- [ ] AC-1：`LaunchEntry.LaunchCount` 字段存在，optional，默认 0；现有 `config/launcher.json`（无此字段）加载后所有 entry LaunchCount=0，gate 通过。
- [ ] AC-2：调用 `RecordLaunchFromOwner(timestamp)` 后，目标 entry 的 `LaunchedAtUtc` 更新且 `LaunchCount` +1（单元测试覆盖单启路径）。
- [ ] AC-3：批启后，所有被启动 entry 的 `LaunchCount` 各自 +1（单元测试覆盖批启路径）。
- [ ] AC-4：sort mode 设为 `frequent` 后，`VisibleEntries` 按 frecency score desc 排序（构造 3 条 entry：A=高频+1h前, B=低频+10min前, C=零频+1天前 → 顺序应为 A > B > C）。
- [ ] AC-5：score 相等时按 name asc（构造 2 条同 score entry，name "B" 在 "A" 之后）。
- [ ] AC-6：从未启动的 entry（count=0 + launchedAt=""）在任何 frecency 排序中 score=0，排到列表末尾（与有 score 的 entry 混排时不浮起）。
- [ ] AC-7：sort mode `frequent` 在 `LauncherStore.Load` 后正确还原；持久化 JSON 中 `sortMode="frequent"` 可被下次启动解析。
- [ ] AC-8：UI sort 模式 ComboBox 显示"按使用频率"选项；选中后 `VisibleEntries` 立即按 frecency 排序，且持久化。
- [ ] AC-9：撤销/回滚（save 失败）路径下 `LaunchCount` 与 `LaunchedAtUtc` 一并回滚（已有快照机制覆盖，无需新增逻辑，但要有一行测试断言）。
- [ ] AC-10：`tools/verify.ps1` 通过。

## Out of Scope

- 在卡片上显示 LaunchCount / frecency score（仅排序用，不暴露数字给用户）。
- 手动重置 LaunchCount 的 UI（重置 entry 时一并清零，作为 edit-clone 路径的一部分自然处理）。
- 跨多机同步 LaunchCount（单机本地状态，不进云）。
- frecency 公式可配置（写死 4.0/2.0/0.5/0.25，与 zoxide 一致）。
- 直方图/统计视图（YAGNI）。

## Open Questions

无。公式采用 zoxide 标准，UI 标签与现有 sort 模式同风格。
