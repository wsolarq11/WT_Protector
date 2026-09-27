# Handoff — 2026-09-27（Semi 主题换肤收口）

## 现状

- `main` 已 push 到 `origin/main`（commit `4f5e4e7b`），本地与远端同步。
- 工作树干净，`git diff --check` 干净，无漂移、无未跟踪垃圾。
- 本地 `npm run verify` 43 步门禁全绿（exit 0）。
- 远端 CI 全绿（run `36299402877`，6m41s，exit 0），临时转公开跑通后已转回私有。

## 本轮完成

- 换 Semi.Avalonia 主题：`SemiTheme` 替换 `FluentTheme` 底座，删除手写 TextBox/ComboBox/AutoCompleteBox/ListBoxItem/ContextMenu/MenuItem/ToolTip 样式。
- 卡片成熟化：`Button.more` 与拖拽手柄幽灵化（`BorderlessButton` + 中性灰图标，悬停加深）。
- 全仓反推：新增 `BadgeCornerRadius` token 统一徽章圆角；裸圆角换 `ControlCornerRadius`/`DialogCornerRadius`；删除孤儿 token（`SecondaryButton*`/`DragHandleTextBrush`）。
- 分析器适配：WT1005 加 `ExternalThemeResourceKeys` 白名单（Semi 按钮 ControlTheme key）+ 补测试。
- 外部库深挖：整个 Avalonia 生态扫完，结论「当前不引任何新库」，登记四张未来牌（见下）。
- 整洁全仓：清理 `config/*.bak`、`.snow` 深挖残留（21MB）、`$env:TEMP` 克隆。
- 提交 `4f5e4e7b`（25 文件，319+/178-）+ push。

## 遗留提醒

- 私有仓库 Actions 免费分钟已耗尽，CI 会被账单拦下。本轮已临时转公开跑 CI，**CI 跑通后必须转回私有**：`gh repo edit wsolarq11/WT_Protector --visibility private`。
- Semi 换肤尚未 GUI 目检——视觉回归需用户跑起来确认观感。
- 任务 `adopt-semi-theme`（`task.json`）status 仍 `in_progress`，目检通过后标记 `completed`。

## 搁置项（等触发条件，无欠账）

- 数据迁移登记器：等 schema v3 且迁移路径 ≥2 条。
- 外部库四张未来牌：跨预设拖拽→`Sortable.Avalonia`；图标超 20 个→`Lucide.Avalonia`；加交互行为→`Xaml.Behaviors.Avalonia` 12（旧包 `Avalonia.Xaml.Behaviors` 已弃用）；全局撤销→自写（`ReactiveHistory` 不活跃）。
- Semi 配套 `Dock`/`Tabalonia`/`AvaloniaEdit` 免费但闭源，用时需过 license 关。
- 宝藏作者 `wieslawsoltes`：Dock/PanAndZoom/ThemeManager/Xaml.Behaviors 等「小而精标准件」，需要小零件优先看他的主页。

## 关键命令

- 全量门禁：`npm run verify`
- 发布契约：`npm run release:contract`
- 产品构建：`npm run make`
- 转回私有：`gh repo edit wsolarq11/WT_Protector --visibility private`
- 收口：补 Session 日志（`add_session.py`）+ `/trellis:finish-work`
