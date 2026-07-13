# UX02 命令目录集中管理闭环

## Goal

完成命令与目录集中管理的生产闭环：用户在管理窗口批量修改后，管理表格和主列表立即反映新状态；磁盘提交失败时，entries、templates 与所有可观察投影完整恢复。

## Background

- 已归档 `07-11-command-management-dialog` 与 `07-11-directory-management-dialog` 拥有主体实现，主窗口已提供“管理”和“地址”入口。
- `ApplyCommandManagementEdit` / `ApplyDirectoryManagementEdit` 已执行 snapshot→mutate→save→refresh，并把更新后的 rows 返回给仍打开的管理窗口。
- 现有测试覆盖分析、批量 rename/merge、模板 CRUD、持久化和 dialog callback，但没有直接证明保存失败回滚，也没有同时证明返回 rows 与主列表过滤投影即时更新。

## Requirements

- R1：复用现有两个管理窗口和 edit/result records，不合并窗口、不新增管理中心抽象。
- R2：命令 rename/merge、模板变更及目录 rename/merge 继续批量更新全部引用，并经唯一 `LauncherStore` 保存路径提交。
- R3：成功 result 必须携带重新分析后的 command/template/directory rows，供打开中的窗口即时刷新。
- R4：成功后 `VisibleEntries`/Cards 分组等主列表派生状态立即刷新，不需要关闭窗口或手动 Reload。
- R5：保存失败必须恢复 entries、settings.CommandTemplates、launch workflow template resolver、selection/filter/count 等快照状态，并返回失败 result。
- R6：补齐上述跨层回归测试，不新增依赖或更改配置 schema。

## Acceptance Criteria

- [x] AC1：命令管理成功 result 返回更新后的去重 rows/template rows，主列表过滤投影即时变化。
- [x] AC2：目录管理成功 result 返回更新后的去重 rows，主列表过滤投影即时变化。
- [x] AC3：命令管理保存失败恢复 entry commands 与 templates，result 失败且 rows 显示旧状态。
- [x] AC4：目录管理保存失败恢复 entry directories，result 失败且 rows 显示旧状态。
- [x] AC5：现有批量 mutation、持久化、dialog callback 与完整生产门禁继续通过。

## Verification evidence

- Four new cross-layer regressions cover command/directory success refresh and save-failure rollback.
- Full Release suite passes with 614 tests.
- `tools/verify.ps1` PASS, including win-x64 self-contained publish and hidden-temp-config GUI smoke.
- No production source, schema or dependency change was required.

## Out of scope

- CQ-03 的 CommandManagementWindow XAML 迁移与 V-02 的工具栏降噪。
- UX-03 的已有目录快速创建。
- schema 重构、数据库、全盘扫描或新依赖。
