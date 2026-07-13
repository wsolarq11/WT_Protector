# Implementation plan

## Preparation

- [x] 启动任务后加载 App/ViewModel 与 tests specs。
- [x] 通过 codebase-memory `trace_path` 审计 `SaveStoreFile` 全部调用方及四类 snapshot/restore 路径。
- [x] 记录哪些调用方具有真实 rollback 语义，哪些只是纯保存。

## Implementation

- [x] 在最接近现有 ViewModel mutation 所有权的 App 层位置新增一个 internal、无状态的泛型 transaction helper。
- [x] 先迁移 settings 路径并运行其聚焦回滚测试。
- [x] 迁移 entries、presets、selection，保持各自 restore 后派生状态刷新顺序。
- [x] 删除被共享 helper 取代的重复 success/failure 分支；不改 UI 文案和 public command API。
- [x] 审计其余 `SaveStoreFile` 调用方，避免为没有 snapshot 的操作制造虚假事务保证。

## Tests

- [x] 新增 helper contract test：成功时不 restore，失败时只 restore 一次并原样返回失败。
- [x] 运行 mutation rollback 相关测试：entry、preset、selection、settings、drag、search history、launch save failure（19 项聚焦测试通过）。
- [x] 运行 `dotnet test` 完整测试集（574 项通过）。

## Quality gates

- [x] 使用 `trellis-check` 检查 spec compliance、复用、跨层数据流与测试覆盖。
- [x] 把 transaction ownership 规则写入 `.trellis/spec/views/mvvm-pattern.md`。
- [x] `git diff --check`。
- [x] `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1`（PASS，574 tests + publish + GUI smoke）。
- [ ] 独立提交并归档 CQ-02，然后继续 CQ-01。

## Rollback points

- helper contract test 通过后为第一检查点。
- 每迁移一个 mutation 家族立即运行其现有回滚测试；失败则只回退该调用点并修正设计，不批量掩盖。
