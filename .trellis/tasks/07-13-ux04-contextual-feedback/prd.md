# UX04 操作反馈强化

## Goal

让用户在执行高频操作后立即看见结果的语义强弱，同时保持底部状态栏为唯一权威结果面，不建立 toast 队列、通知中心或历史记录。

## Confirmed Facts

- `MainWindowViewModel.StatusText` 是当前全局结果权威，MainWindow 底部状态区已有自动化名称“状态信息”。
- 成功、取消、验证失败和持久化失败目前都只以同一前景色显示，容易在连续操作中被忽略。
- `LauncherStatusText` 已集中拥有大量文案；CQ-05 将继续收敛文案所有权，本任务不复制 formatter。
- UX-06 通知历史明确延期；本任务不得引入持久化、队列、弹窗或系统通知。

## Requirements

- R1：为状态结果增加最小类型化语义：neutral、success、warning、error；文本仍由现有 formatter 生成。
- R2：MainWindow 状态区根据语义使用现有 design tokens 提供克制的前景/边框强调，并保留可读文本，不能只靠颜色传达结果。
- R3：成功强调不抢焦点、不弹窗、不自动消失；下一条权威状态自然替换上一条。
- R4：错误与 warning 必须保留完整 `StatusText`，并通过 AutomationProperties 暴露稳定的状态区域语义。
- R5：仅覆盖用户可行动的高频边界：entry/preset mutation、launch、management、discovery、save/load；纯筛选、排序、选择计数保持 neutral。
- R6：不新增依赖、计时器、通知中心、历史 schema 或第二套状态文本。

## Acceptance Criteria

- [ ] AC1：状态栏仍绑定唯一 `StatusText`，没有新增并行消息集合或持久化字段。
- [ ] AC2：成功、warning、error 和 neutral 可由 ViewModel 的类型化状态属性确定，且变更通知稳定。
- [ ] AC3：MainWindow 状态区对四种语义有结构化视觉状态，并保留文本/自动化名称，不只使用颜色。
- [ ] AC4：至少覆盖新增/编辑/删除、launch success/failure、save failure、dialog cancel 的语义映射测试。
- [ ] AC5：状态替换不使用延迟、动画或后台任务，不产生竞态和过期消息覆盖。
- [ ] AC6：Release 测试、`git diff --check`、`tools/verify.ps1`、publish 与 GUI smoke 全部通过。

## Out of Scope

- toast/snackbar 队列、通知中心、系统通知、声音、自动消失计时器。
- UX-06 回执历史与任何新持久化。
- 本轮全面重写所有状态文案；该所有权由 CQ-05 继续完成。
