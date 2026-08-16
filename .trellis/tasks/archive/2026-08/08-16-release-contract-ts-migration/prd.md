# 中央发布契约与 PS 迁移 TS

## Goal

建立一条不可绕过的中央发布契约：真实执行 Native AOT 发布路径，发布后工作区必须干净，receipt 必须可验证。然后把仓库 active 工具脚本从 PowerShell 全量迁移到 TypeScript，AOT 始终作为局部郡县，不作为全局裁决者。

## Background / Confirmed Facts

- 产品是 Windows Terminal workspace launcher，Avalonia/.NET 10，win-x64 Native AOT。
- `pwsh` 是产品启动项使用的 shell，也是用户终端命令运行时依赖，不属于脚本系统迁移范围。
- 当前未提交变更已有 3 项：
  - `src/avalonia-launcher/src/WTLauncher.App/packages.lock.json` 补出 `Microsoft.DotNet.ILCompiler`。
  - `tools/build-release.ps1` restore 已对齐 `EnableNativeAot`。
  - `.github/workflows/verify.yml` CI `aot-verify` restore 已对齐 `EnableNativeAot`。
- 根门禁 `tools/verify.ps1` 当前 43 项 `Add-Check`，计划新增 `release-lockfile-consistency` 后为 44 项。
- CI 的 `aot-verify` 当前 `continue-on-error: true`，会被文档标为 informational，失败可能被掩盖。
- active PowerShell 脚本包括但不限于：
  - `tools/config-lib.ps1`
  - `tools/verify.ps1`
  - `tools/build-release.ps1`
  - `tools/apply-machine-config.ps1`
  - `tools/generate-icon.ps1`
  - `tools/install-portable.ps1`
  - `tools/package-friend.ps1`
  - `tools/smoke-critical-journey.ps1`
  - `tools/test-coverage.ps1`
  - `tools/test-fast.ps1`
  - `tools/test-launch-args.ps1`
  - `tools/add-test-category-traits.ps1`
  - `tools/layer-test-directories.ps1`
  - `tools/update-github-similar-projects-snapshot.ps1`
  - `src/avalonia-launcher/run.ps1`
  - `src/avalonia-launcher/verify.ps1`

## Requirements

- R1: 中央权威是 `release-contract`，不是 AOT。
- R2: `release-contract` 必须执行真实发布命令，而不是只做 AOT 编译。
- R3: 发布后 `git status --porcelain --untracked-files=all` 必须为空。
- R4: `packages.lock.json` 必须通过 `--locked-mode` 校验，锁文件漂移必须失败。
- R5: `receipt.json` 和 `update-manifest.json` 必须被解析并校验关键字段。
- R6: AOT 失败必须红 CI，不再 `continue-on-error`。
- R7: 所有 active 工具脚本迁移为 TypeScript，CI 不再调用 PowerShell 执行仓库脚本。
- R8: `pwsh` 只保留为产品运行 shell 和用户终端命令，不再是仓库脚本语言。
- R9: 迁移过程采用双轨，旧 PS 只保留到 TS 等价验证通过。
- R10: 每个迁移阶段必须有可对比的 parity 验收，不允许“先删 PS 再补 TS”。

## Acceptance Criteria

- [ ] CI 中 AOT publish 失败会导致 `release-contract` 失败。
- [ ] CI 中 lockfile 漂移会导致失败。
- [ ] CI 中发布后任何 tracked 或 untracked 非忽略文件变化会导致失败。
- [ ] `release-contract` 校验 receipt、manifest、exe SHA256。
- [ ] `npm run release:contract` 是 TS 中央入口。
- [ ] `npm run verify` 等价覆盖原根门禁检查集。
- [ ] active `.ps1` 数量为零，排除 docs/retired 示例和明确保留的生成式用户卸载器决策。
- [ ] `.github/workflows/verify.yml` 不调用 PowerShell。
- [ ] 根门禁结构基线与 AGENTS.md 同步更新。

## Out of Scope

- 让应用支持 macOS/Linux。
- 替换 `pwsh` 作为产品启动 shell。
- 重写 Avalonia/Core/ViewModels 业务代码。
- 把所有检查逻辑塞进 AOT。
- 在迁移完成前删除全部 PowerShell 回退路径。

## Child Tasks

- `central-release-contract`: 先用 PowerShell 落地中央契约和锁文件一致性 gate。
- `ts-scripting-foundation`: Node/TypeScript 骨架、`build-release.ts`、`release-contract.ts`。
- `ts-mainline-scripts`: `run.ts`、mainline verify、测试、覆盖率、冒烟脚本迁移。
- `ts-root-gate`: `verify.ps1` 44 项 Add-Check 按模块迁移为 TS。
- `ts-aux-cleanup`: 辅助脚本迁移、生成式卸载器决策落地、active PS 清理、文档与基线更新。

## Open Questions

- 生成式卸载器边界：`tools/install-portable.ps1` 生成的 `uninstall.ps1` 是给终端用户的，不能假设用户装有 Node。要达成“仓库内零 active PS”，需要决定保留生成式 PS 卸载器，还是改造成小型 .NET 自包含卸载器。
