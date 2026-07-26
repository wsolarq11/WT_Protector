# Phase F: 文档修复

## 根因

UltraReview 文档审计确认 5 处文档与代码不符。

## 修复清单

### F1: AppStore→StoreGrain 术语统一
- CLAUDE.md、AGENTS.md、README 中 AppStore 引用 → StoreGrain
- MainWindowViewModel.cs 注释中 AppStore. 引用 → StoreGrain.

### F2: SemaphoreSlim 非 static
- CLAUDE.md:72 `private static SemaphoreSlim(1,1)` → `private readonly SemaphoreSlim _saveLock = new(1, 1)`

### F3: 特性数量修正
- CLAUDE.md:128 `21-feature` → `22-feature`

### F4: 门禁计数同步
- CLAUDE.md:25 `16 checks` → 实际 ~29 个 Add-Check

### F5: 窗口几何描述修正
- 澄清 verify.ps1 拒绝 vs 运行时 Math.Max 钳制的区别

## 验收标准
- [ ] 全部 5 处修复完成
- [ ] `powershell -File tools\verify.ps1` 通过
- [ ] `dotnet build -warnaserror` 通过
