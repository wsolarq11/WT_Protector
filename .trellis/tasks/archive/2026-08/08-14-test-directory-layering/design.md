# 测试目录命名空间分层 - Design

## 目录

```text
WTLauncher.Tests/
├── Support/      # 共享 fixture，namespace WTLauncher.Tests.Support
├── CoreUnit/     # namespace WTLauncher.Tests.CoreUnit
├── Persistence/  # namespace WTLauncher.Tests.Persistence
├── ViewModelBehavior/
├── Analyzer/
├── Architecture/
├── UiContract/
└── PropertyTest/
```

## 迁移策略

- 使用脚本移动文件并重写 namespace。
- 被迁移文件需要 `using WTLauncher.Tests;` 或 `using WTLauncher.Tests.Support;` 引用共享类型。
- 保持 `InternalsVisibleTo` 不变。
