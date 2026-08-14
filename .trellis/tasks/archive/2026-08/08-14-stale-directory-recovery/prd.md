# 失效目录可见化与一键修复

## Goal

让失效条目不再等到点击启动才暴露错误：在卡片/列表/快速启动中可见、可理解，并提供“重新选择路径”或“删除”的一键修复路径。

## Background（已确认事实）

- 当前 `config/launcher.json` 有 4 个条目目录已不存在，用户只能到启动时发现或手工编辑 JSON。
- 项目已有 `existing-directory-contract`：启动前 fail-fast、verify gate 拒绝失效目录；这是正确边界，不应改成本地自动修复。
- 已有 `EntryDetailsWindow`、`DirectoryEditDialog`、`DirectoryManagementWindow` 可复用目录浏览和引用协调逻辑。
- 修改目录/删除条目必须继续维护 presets、customEntryOrder、settings 引用的一致性，并走 snapshot + rollback。

## Requirements

- R1 启动/刷新时扫描条目的 `directory` 存在性，生成只读 stale 状态，不阻塞主窗口正常打开。
- R2 Cards/Details/Names 三种视图都显示失效标记（badge、边框或文字），不能只靠 ToolTip。
- R3 对失效条目提供“重新选择路径”入口，复用目录浏览对话框；保存后同步所有引用并清除失效标记。
- R4 对失效条目提供“删除”入口，带确认、快照和回滚；删除后 presets/customEntryOrder 同步清理。
- R5 顶部/状态区显示失效条目计数摘要，便于用户一次性发现数据健康问题。
- R6 当前失效的 4 个本地条目可作为手工清理验收场景，但代码不得静默修改 `launcher.json`。

## Acceptance Criteria

- [ ] 含失效目录的配置加载后不崩溃，失效条目在三种视图可见标记。
- [ ] “重新选择路径”走完：目录改为有效路径，presets/order 不变，标记消失。
- [ ] “删除”走完：条目删除，presets/customEntryOrder 引用同步，失败可回滚。
- [ ] 失效计数摘要可见且随修复/删除实时更新。
- [ ] 新增测试覆盖 stale 投影、两种修复路径、引用同步和回滚；`tools/verify.ps1` 保持绿。
- [ ] 实现不会自动重写用户配置；只有用户显式修复或删除才写盘。

## Out of Scope

- 自动移动/恢复目录、猜测新路径、静默删除。
- 把目录存在性检查做成网络/UNC 探测或后台常驻监控。
- 导入导出、多机路径映射（属于 `08-14-import-export-migration`）。

## Open Questions

- 失效扫描时机：启动时、Refresh 时、目录对话框返回后，是否需要手动“重新检查”。