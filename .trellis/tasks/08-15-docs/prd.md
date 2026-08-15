# PRD — 全仓一致性统一与集中管理

## 背景

AGENTS.md / CLAUDE.md / .trellis/spec 中的结构数字（文件数、partial 数、测试数、门禁检查数）与
实际严重漂移：Core 24→53、ViewModels 28→53、partials 7→15、Views 13→44、测试文件 61→121、
门禁 31/32→37。README/retired/README 对 2026-07-06 已删除的 HTA 仍有过时声明。无强制机制防复发。

## 目标

1. 全部当前状态文档（AGENTS.md、CLAUDE.md、README.md、retired/README.md、.trellis/spec/*/directory-structure.md）与实际结构一致。
2. 根目录历史文档（phase*-complete ×4、ultrareview ×2、collab-debt-findings）集中归档到 docs/（archive/）。
3. 集中管理：AGENTS.md 新增「结构基线」表（唯一文档事实源），verify.ps1 新增
   `structure-baseline-consistency` 检查做机器强制——结构变化必须同时更新两处，否则门禁失败。

## 验收标准

- [x] 7 个历史文档 git mv 归档（docs/archive/ + docs/collab-debt-findings.md）
- [x] AGENTS.md 结构基线 10 行与 verify.ps1 基线常量一致
- [x] CLAUDE.md / README.md / retired/README.md / spec 数字全部校正
- [x] verify.ps1 全绿（38 项检查，含新增 structure-baseline-consistency）
- [x] git diff --check 通过
