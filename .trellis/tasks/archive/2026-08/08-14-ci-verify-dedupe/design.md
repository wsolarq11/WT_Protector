# CI 与 verify 去重 - Design

## 调整

- `build-and-test`：build + `tools/test-fast.ps1`。
- `full-gate`：继续运行 `tools/verify.ps1`，承担全量测试。
- `verify.ps1` 保持现状，避免本地降级。
