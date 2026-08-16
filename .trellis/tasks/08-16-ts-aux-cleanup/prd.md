# TS 辅助脚本与 PS 清理

## Goal

迁移剩余辅助脚本，处理安装器与生成式卸载器，删除所有 active PowerShell 脚本，完成文档与 CI 清理。

## Requirements

- 迁移 `apply-machine-config`、`generate-icon`、`package-friend`、`update-github-similar-projects-snapshot`、`add-test-category-traits`、`layer-test-directories`。
- `install-portable.ts` 生成 `.NET` 自包含卸载器，不再生成 `uninstall.ps1`。
- 删除或归档所有 active `.ps1`。
- `.github/workflows/verify.yml` 不调用 PowerShell。
- 更新 README、AGENTS.md、CLAUDE.md、src README、docs 和结构基线。

## Acceptance Criteria

- [ ] active `.ps1` 数量为零。
- [ ] 安装器生成的卸载器不依赖 Node，也不生成 PS。
- [ ] CI 无 PowerShell 调用。
- [ ] `npm run verify` 和 `npm run release:contract` 为唯一权威入口。
- [ ] `pwsh` 只保留为产品运行 shell 和用户终端命令。

## Out of Scope

- 产品跨平台化。
- 替换应用启动 shell。
