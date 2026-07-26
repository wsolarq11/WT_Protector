# Phase C: 死代码清理

## 根因

UltraReview 发现 3 个完整类/接口为死代码（从未被任何代码引用）。清理可减少源码约 230 行。

## 清理清单

### C1: AppState record（CR-6）
- 文件：`AppState.cs`（43 行）
- 原因：设计为"单一不可变状态树"但从未落地。StoreGrain 使用 `Grain<LauncherStoreFile>` 而非 `Grain<AppState>`
- 删除或标记 `[Obsolete]` 并引用 StoreGrain

### C2: ErrorSupervisor（CR-7）
- 文件：`ErrorSupervisor.cs`（145 行）
- 原因：断路器模式实现，但从未被任何代码实例化或使用
- 删除整个文件

### C3: ServiceResult 未使用抽象（BS-7）
- 文件：`Features/ServiceResult.cs`（41 行）
- 原因：定义在 Features 命名空间但流程中不使用
- 评估是否保留（接口契约）或删除

### C4: ProcessLaunchFailed 目录项
- 文件：`LauncherStatusText.cs:89-90`
- 原因：方法无调用者

### C5: LauncherPresetValidator.Validate 死代码（PR-CR-1）
- 文件：`LauncherPresetValidator.cs`（118 行）
- 原因：从未被调用——预设加载路径不走此验证
- 修复：在 `PresetManager.Load` 或 `StoreGrain` 加载路径中调用验证

## 验收标准
- [ ] 清理后 `dotnet build -warnaserror` 通过
- [ ] 清理后 `dotnet test` 全部通过
- [ ] 清理后 `powershell -File tools\verify.ps1` 通过
- [ ] 每个被删除的类/方法确保无引用残留
