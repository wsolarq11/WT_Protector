# Implementation plan

- [x] 为 command management 增加 result rows + filtered main-list immediate refresh 测试。
- [x] 为 directory management 增加 result rows + filtered main-list immediate refresh 测试。
- [x] 为 command management 增加 save-failure complete rollback 测试。
- [x] 为 directory management 增加 save-failure complete rollback 测试。
- [x] 运行完整 Release tests（614 passed）。
- [x] 执行 `trellis-check`；行为复用既有 CQ-02 与 callback 规范，无新增通用契约需写入 spec。
- [x] 运行 `git diff --check` 与完整 `tools/verify.ps1`。
- [ ] 提交并归档 UX-02。
