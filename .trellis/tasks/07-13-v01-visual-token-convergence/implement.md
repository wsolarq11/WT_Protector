# Implement — V01 Visual Token Convergence

## Steps

- [ ] 新增最小颜色/阴影/字号 token 与 App resource 注入。
- [ ] 替换 EntryDetails、drag ghost 和 MainWindow transparent shadow raw hex。
- [ ] 将重复 12/14/16 字号迁移到语义资源。
- [ ] 新增 token/raw-hex 结构契约测试。
- [ ] 在 tools/verify.ps1 增加 App raw-hex 零漂移检查。
- [ ] build、聚焦测试并确认所有新增 token 有生产消费者。
- [ ] trellis-check、完整 Release tests、git diff --check、tools/verify.ps1。
- [ ] 更新 core/views/tests spec，提交并归档。

## Rollback Points

- 颜色/阴影替换与字号替换分开 build，避免 XAML 类型转换错误堆叠。
- 门禁若误报，只收窄扫描路径或正则；不建立不断增长的颜色 allowlist。
