# 中央发布契约

## Goal

先用 PowerShell 把中央发布契约落地，AOT 从 informational 改为 required，发布后工作区必须干净，锁文件一致性进入本地 gate。

## Requirements

- `tools/build-release.ps1` 增加 `-LockedMode` 和 `--locked-mode` restore。
- AOT publish 增加 `--no-restore`。
- 新增 `tools/release-contract.ps1`，执行真实发布并校验 receipt、manifest、clean worktree。
- CI `aot-verify` 改为 required `release-contract`。
- `tools/verify.ps1` 新增 `release-lockfile-consistency`，检查数 43 到 44。
- AGENTS.md 结构基线同步更新。

## Acceptance Criteria

- [ ] `tools/release-contract.ps1` 在干净 checkout 上 PASS。
- [ ] AOT 或 lockfile 漂移会让 `release-contract` 失败。
- [ ] 发布后 `git status --porcelain --untracked-files=all` 为空。
- [ ] `tools/verify.ps1` 包含 44 项 Add-Check。
- [ ] CI `release-contract` 无 `continue-on-error`。

## Out of Scope

- TS 迁移。
- 业务功能改动。
