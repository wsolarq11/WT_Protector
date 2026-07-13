# Design

## Reuse boundary

不修改管理模型：`CommandManagementEdit/Result` 与 `DirectoryManagementEdit/Result` 已是稳定边界。ViewModel 仍是 observable aggregate 与 CQ-02 transaction owner；窗口使用 callback result 替换自身 rows。

## Missing proof

新增四个跨层测试，分别覆盖 command/directory 的成功即时刷新与 save-failure rollback。测试通过真实 ViewModel、真实 `LauncherStore` 临时文件和真实分析器执行，不 mock persistence。

## Success flow

```text
management callback
  → capture entry mutation snapshot
  → apply batch edit
  → save unified store
  → refresh selection/filter/groups
  → return freshly analyzed rows
```

成功测试预置一个只匹配“新 command/directory”的 filter；编辑前 `VisibleEntries` 为空，编辑后立即出现，从而证明主列表闭环。Result rows 同时断言旧 key 消失、新 key 与引用数正确。

## Failure flow

测试在 ViewModel 构造后把 store 目录替换为同名文件，稳定触发保存失败。断言失败 result、entry/template 恢复、返回 rows 为旧状态、主列表投影不泄漏临时 mutation。

## Rollback

预计只改测试。若测试揭示缺陷，修复应落在 `Apply*ManagementEdit` 的共享 snapshot/save/restore 控制流，不在窗口或每个 edit 分支补丁。
