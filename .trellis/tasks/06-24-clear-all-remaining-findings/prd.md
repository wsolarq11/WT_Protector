# 清零 ultrareview 全部 87 剩余项 — MEDIUM+LOW+DOC

## Goal

修复 2026-06-24 UltraReview 剩余的全部 87 项发现。目标置信度逼近 100%。
三轮清零：前两轮已修复 34 CRITICAL+HIGH+M，本轮修复剩余 87。

## Context

- UltraReview 总发现: 121
- 已修复: 34 (C1-C2, H1-H12, M1/M4/M5/M8/M9/M13/M15-M19/M26, DOC H12/D1-D11)
- 本轮: 87 (15 MEDIUM + 66 LOW + 6 DOC)
- 测试基准: 573/573, 0 errors, 0 warnings

## Requirements

### P1 — MEDIUM (15) — 全部修复

M2 启动清理残留 tmp, M3 CloneNormalized 剥离 SchemaVersion, M6 PresetDetailsWindow 重复名检查, M7 预设交叉引用验证, M10 finally null MainWindow 处理, M11 窗口位置持久化, M12 CLI --help, M14 $$ 转义, M20 ProcessTests 故障覆盖, M21 PresetStore bak 测试, M22 CollectionBehavior 迁移, M23 设置样板提取, M24 RequireEntryName 统一, M25 Chrome 常量测试, M27 排序缓存规范化名

### P2 — LOW (66) — 全部修复

文件 I/O 6 项, 输入验证 7 项, 异常处理 10 项, ViewModel 10 项, UI 布局 18 项, 死代码 5 项, 测试 4 项, 无障碍/文档 5 项

### P3 — DOC (6) — 全部修复

README 配置示例对齐, 文件计数同步, spec 引用修正

## Acceptance Criteria

- dotnet build 0 errors 0 warnings
- dotnet test >=573 passed, 0 failed
- 零测试标准降低
- 全部 87 项已修复

## Notes

- 决策全按推荐走
- 修复按文件分组并行实施
- 遵循现有代码模式
