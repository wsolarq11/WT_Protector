# docs 任务验收记录 (2026-08-15)

- 结构基线：AGENTS.md 10 行与 `tools/verify.ps1` 常量一致，当前为 Core 53 / ViewModels 54 / partials 15 / Features 16 / Views 24cs+20axaml / Analyzers 7 / Tests 121 / attributes 737 / checks 40。
- 文档同步：README、CLAUDE、`.trellis/spec/core/directory-structure.md` 及 2026-08-15 复核注记均已按当前基线校正；`docs/gate-coverage-assessment.md` 补入 `dialog-service-no-func-action` 与 `views-no-direct-core-business-calls`。
- 验证：`post-rewrite-check.ps1` 全部通过；`tools/verify.ps1` 40 项检查全绿；门禁实测 820 用例；`git diff --check` 通过。
