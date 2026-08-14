# 关键旅程 E2E 冒烟 - Design

## 流程

```text
创建临时 launcher.json
  -> 启动 WTLauncher.App.exe --config <temp>
  -> 等待主窗口标题出现
  -> 断言进程存活
  -> 正常退出
```

## 方案

- 新增 `tools/smoke-critical-journey.ps1`。
- 复用 `config/launcher.example.json` 或生成最小配置。
- `tools/verify.ps1` 调用该脚本作为 P4 检查。
