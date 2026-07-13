# CQ05 状态文案类型化所有权

## Goal

让主窗口状态的文字与语义色调由同一个类型化值原子运输，消除依赖中文关键词推断 severity 的脆弱路径，同时保持现有用户文案、状态栏权威性和 CQ-06 service-result 分支行为。

## Confirmed Facts

- LauncherStatusText.cs 已按 main/refresh/persistence/entry/preset/selection/settings/launch/drag 工作流分组，但 formatter 仍只返回 string。
- UX-04 当前通过 LauncherStatusPresentation.Classify(StatusText) 的中文 marker 猜测 tone；新增或改写文案可能静默改变视觉语义。
- CQ-06 的 ServiceCanceled/ServiceFailure 用 string Status 运输取消与失败文案，ViewModel 再把字符串写入 StatusText。
- MainWindow 状态栏是唯一权威反馈面；UX-06 历史、toast、队列和持久化明确延期。

## Requirements

- R1：引入最小 LauncherStatusContent 值类型，原子包含非空 Text 与 LauncherStatusTone。
- R2：工作流 formatter 返回明确 tone 的 LauncherStatusContent，不再要求调用方拼接或猜测 severity。
- R3：ServiceCanceled/ServiceFailure 运输 LauncherStatusContent；成功数据、CQ-02 persistence transaction 和服务所有权不变。
- R4：MainWindowViewModel 以单一 StatusContent 为可写权威，并继续提供 StatusText、StatusTone、indicator 与布尔投影供现有 XAML 绑定。
- R5：删除生产路径中的 LauncherStatusPresentation.Classify marker heuristic；IndicatorFor 可保留为纯 tone 映射。
- R6：迁移主窗口相关的直接状态字符串拼接到对应工作流 formatter；窗口内非权威辅助文本和完整 launch receipt 不强制包装。
- R7：保持所有现有用户可见文案和 tone 语义，取消为 warning、失败为 error、完成为 success、环境/进行中信息为 neutral。

## Acceptance Criteria

- [ ] AC1：LauncherStatusContent 验证空文本并提供 Neutral/Success/Warning/Error 明确构造边界。
- [ ] AC2：MainWindowViewModel 不再从 StatusText 文本分类 tone，文字与 tone 在一次赋值中同步通知。
- [ ] AC3：ServiceCanceled/ServiceFailure 的 Status 为 LauncherStatusContent，所有调用方穷尽处理仍通过。
- [ ] AC4：LauncherStatusText 工作流 formatter 和剩余主窗口状态写入均使用类型化内容；无调用点中文 marker 分类。
- [ ] AC5：测试覆盖内容验证、四种 tone、service result、通知顺序及代表性取消/部分成功/失败重叠文案。
- [ ] AC6：没有 toast/history/event bus/schema/dependency/Core-Avalonia 泄漏或持久化变化。
- [ ] AC7：Release tests、git diff --check、tools/verify.ps1、publish 与 GUI smoke 全绿。

## Out of Scope

- UX-06 回执历史、通知中心、timer 或状态持久化。
- 重写 launch receipt 的多行展示模型。
- 将 LauncherStatusTone 下沉到 Core。
- 全局异常 taxonomy 或通用 Result 框架。
