# Implementation Plan

1. 用 codebase-memory 获取 WT Launcher 与 CodexSwitch benchmark 的架构、热点、代表性调用链和源码片段。
2. 读取主窗口 XAML、样式、关键 ViewModel partial、服务边界和测试覆盖。
3. 构建运行 WT Launcher，采集当前界面截图和关键实操路径证据。
4. 将发现与现有 Trellis 任务树去重映射。
5. 写入 `docs/exemplar-gap-refactor-audit.md`，执行证据和一致性复核。
6. 运行 `git diff --check` 与 `tools/verify.ps1`。
