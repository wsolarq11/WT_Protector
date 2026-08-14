# 测试路线图验收 - PRD

## Goal

完成 P0-P6 后的文档、规格和整体验收。

## Requirements

- 更新 `.trellis/spec/tests/test-quality.md` 和 README。
- 记录最终结构基线。
- 执行最终全量门禁。

## Acceptance

- 文档反映新测试地图、命令、CI 分层。
- `dotnet build -warnaserror` PASS。
- `dotnet test -c Release` PASS。
- `tools/verify.ps1` PASS。
