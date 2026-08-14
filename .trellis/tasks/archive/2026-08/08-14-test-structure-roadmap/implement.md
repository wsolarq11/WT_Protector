# 测试体系结构路线图落实 - Implement

## 顺序

1. P0 test-map-baseline
2. P1 test-level-traits-fast-path
3. P2 test-directory-layering
4. P3 ui-contract-semantics
5. P4 critical-journey-smoke
6. P5 coverage-gates
7. P6 ci-verify-dedupe
8. P7 test-roadmap-acceptance

P1 是 P2/P6 的前置；P3/P4 可在 P2 后并行推进。

## 验证命令

```powershell
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore -warnaserror
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\verify.ps1
```

## 回滚

每个阶段独立：如 P2 移动文件导致门禁失败，先恢复移动并保留 Trait；如 P3 迁移不收敛，保留旧契约测试并记录技术债。
