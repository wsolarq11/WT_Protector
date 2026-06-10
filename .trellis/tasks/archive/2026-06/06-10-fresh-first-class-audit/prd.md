# 重新全功能一等公民审计

## Goal

从当前 HEAD (`22f57a9`) 源码出发，逐功能重新验证 WT Protector 每一项能力的真实问题解决力、数据所有权、失败契约和一等公民证据，产出新的独立审计报告。不与昨天 (`2026-06-09`) 的审计结论合并，不用旧断言冒充新证据。

## Requirements

1. 覆盖当前 Avalonia 主线全部活跃功能（23+ 项），包括昨天审计可能遗漏的新增代码（`LauncherDesignTokens`、`FastWrapPanel`、`searchHistory`、`commandTemplates` runtime resolver 等）。
2. 每项功能必须从源码、测试、配置文件、门禁脚本中提取当前证据，不能抄旧审计结论。
3. 审计维度沿用五条一等公民标准：
   - 解决真实、高频、当前存在的问题
   - 数据有唯一权威源，不搞第二套真相
   - 用户路径显式可见，不靠隐藏 fallback
   - 失败在契约边界暴露，不能吞错继续跑
   - 验证能覆盖当前声明，不能靠旧日志和愿望
4. 重新验证 GitHub 相似项目快照时效性（`capturedAtUtc` 是否在 30 天窗口内）。
5. 重新运行 `git diff --check` + 全量门禁确认当前状态。
6. 对每个功能给出星级评级（1-5）和改造建议（包括"无"——不需要改）。
7. 产出独立审计报告到 `docs/feature-first-class-upgrade-audit.md`，覆盖旧版本。
8. 更新关联文档引用（`README.md`、`docs/feature-reality-north-star-audit.md` 中审计日期引用等）。

## Acceptance Criteria

- [ ] `git diff --check` PASS
- [ ] `tools\verify.ps1` PASS（含 542+ tests）
- [ ] 每项功能评级有独立源码证据锚点（文件:行号）
- [ ] 审计报告更新至 `docs/feature-first-class-upgrade-audit.md`
- [ ] GitHub 快照时效性已验证或已刷新
- [ ] 遗留缺口账本更新（只含真实存在的缺口）
- [ ] 报告明确标注审查日期为 2026-06-10，HEAD commit hash
- [ ] 旧审计的 "Close first-class launcher upgrade" commit 内容被本次审计重新覆盖验证

## Out of Scope

- 不新增功能实现
- 不修改源码（报告文档除外）
- 不做 broad launcher 平台对比分析
- 不生成 installer/update/signing 方案

## Notes

- 这是复杂任务：需要 `design.md`（审计方法论）和 `implement.md`（执行步骤）。
- 审计不是"找茬"——功能已经很好的，结论就是"无改造建议"。
