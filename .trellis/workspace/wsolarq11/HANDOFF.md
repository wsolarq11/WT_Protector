# Handoff — 2026-09-30（拖拽命中区整卡化 + CI 公开流程固化收口）

## 最新现状

- `main` 主体 commit `b460cfa9`（拖拽整卡化）+ 文档收口 commit，均已 push 到 `origin/main`，工作树 clean。
- 本地 `npm run verify:mainline` PASS（**819 测试** / nativeAot / guiSmoke），`git diff --check` 干净，`*.lock.json` 零污染。
- 仓库当前为 **PUBLIC**（为触发 CI 按规则转公开）；远端 CI 绿后必须转回私有（见「CI 流程规则」）。

## 本轮（09-30 收口）完成——严格占优决策链

核心决策：**拖拽命中区 = 整卡，删独立手柄 well 与六点 grip 图标**。证据链闭环：

1. 旧 `entry-drag-handle` well 在卡片/详情是 32×32，名称墙 18×18（`NameTileHeight 24 − 2 边框 − 4 padding` 硬编码），18 在命中尺寸上被 32 严格占优、仅密度胜出 → 帕累托非占优，且制造「绘制框≠命中框」隐形环。
2. 整卡命中 + 4px 阈值（`MainWindowDragThreshold.ThresholdPx`）严格占优：更大统一命中区、无双框漂移、点击/双击保留、名称墙行高仍 24、净删代码。
3. grip 图标（六点）是「把手」隐喻，与「整卡即热区」矛盾——任何固定位置 grip 都不可能是整卡热区中心，故**删除**而非调位置；affordance 由整卡 hover 上浮 + `Cursor=SizeAll` 单一承担（光标位置==热区位置）。
4. 三域合一等式（`MainWindowCardGestureTests.CursorRegion_EqualsGestureRegion` 锁定）：`CursorRegion == DragRegion == DoubleTapLaunchRegion == card − ExcludedControlTypes`。排除集单一源 `MainWindowCardGesture.ExcludedControlTypes = {Button, CheckBox}`，双击与拖拽都走 `IsGestureEligible`。
5. 堵住继承泄漏：Avalonia `Cursor` 是 `inherits:true`，卡片内 `Button`/`CheckBox` 显式 `Cursor=Arrow` 钉回点击语义，否则「光标说可拖、手势说不拖」。

死代码清除：`LauncherDialogText.cs`（名存实亡，SafeParseBrush 内联进 `MainWindow.Drag.cs` 的 `ParseBrush`）、`DragHandleTextColor`、`DragSortLabel`、`entry.dragSort`、`IconGripVertical`/`IconGripVerticalSmall`、`IconStroke12`、`MainWindowEntryRowDoubleTap`（语义偷换，被拖拽复用「双击启动」判定）、`MainWindowEntryRowDoubleTapTests.cs`。

## CI 流程规则（已固化，权威源 AGENTS.md 核心规则 + pitfalls §2.27）

- **私有仓库触发 CI 前必须先转公开**：`gh repo edit wsolarq11/WT_Protector --visibility public --accept-visibility-change-consequences`，再 push/rerun。
- **禁止**先私有 push 触发 CI、失败后才发现账单拦截再转公开重跑。
- 账单拦截识别：job 的 `runner_id=0` 且 `steps` 为空（runner 从未分配），失败与代码无关。
- CI 绿后转回私有：`gh repo edit wsolarq11/WT_Protector --visibility private --accept-visibility-change-consequences`。

## 遗留提醒

- 任务 `adopt-semi-theme`（`task.json`）status 仍 `in_progress`，拖拽/图标/控件均需用户 GUI 目检后标记 `completed`。
- 拖拽整卡化的最终目检点：悬停卡片空白区 → 四向箭头 + 上浮；悬停「启动/更多」按钮与复选框 → 保持箭头（验证继承泄漏已堵）。
- 编译/门禁前若 exe 被运行中的「WT Launcher」锁定，`Stop-Process -Name WTLauncher.App -Force` 后重跑。

## 关键命令

- 全量门禁：`npm run verify`
- 主线门禁：`npm run verify:mainline`
- 转回私有：`gh repo edit wsolarq11/WT_Protector --visibility private --accept-visibility-change-consequences`
- 查 CI：`gh run list --limit 1 --json databaseId,status,conclusion`
- 收口：补 Session 日志 + `/trellis:finish-work`
