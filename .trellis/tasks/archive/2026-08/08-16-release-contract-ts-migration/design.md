# Design: 中央发布契约与 PS 迁移 TS

## Architecture

```
User / CI
   │
   v
release-contract          ← 中央权威，唯一真实发布入口
   │
   ├── locked restore      ← packages.lock.json 一致性
   ├── Native AOT publish  ← AOT 是郡县，不是裁决者
   ├── clean worktree      ← git status 必须为空
   └── receipt validation  ← receipt.json + update-manifest.json + exe SHA256
```

AOT 只负责 Native AOT 编译、启动、打包资产。中央契约负责整条发布路径是否可复现、是否有隐藏副作用。

## Target Script Stack

- Runtime: Node.js LTS
- Language: TypeScript, `strict`
- Executor: `tsx`
- Process orchestration: `execa`
- Script tests: Vitest
- Package manager: npm
- CLI parsing: Node built-in `util.parseArgs`

`pwsh` 继续作为产品启动 shell 和用户终端命令依赖，但不作为仓库脚本语言。

## Migration Map

| PowerShell | TypeScript |
|---|---|
| `tools/config-lib.ps1` | `scripts/config-lib.ts` |
| `tools/build-release.ps1` | `scripts/build-release.ts` |
| `tools/release-contract.ps1` (new) | `scripts/release-contract.ts` |
| `src/avalonia-launcher/run.ps1` | `scripts/run.ts` |
| `src/avalonia-launcher/verify.ps1` | `scripts/verify-mainline.ts` |
| `tools/test-fast.ps1` | `scripts/test-fast.ts` |
| `tools/test-coverage.ps1` | `scripts/test-coverage.ts` |
| `tools/test-launch-args.ps1` | `scripts/test-launch-args.ts` |
| `tools/smoke-critical-journey.ps1` | `scripts/smoke-critical-journey.ts` |
| `tools/verify.ps1` | `scripts/verify.ts` + `scripts/checks/*.ts` |
| `tools/apply-machine-config.ps1` | `scripts/apply-machine-config.ts` |
| `tools/generate-icon.ps1` | `scripts/generate-icon.ts` |
| `tools/package-friend.ps1` | `scripts/package-friend.ts` |
| `tools/install-portable.ps1` | `scripts/install-portable.ts` + .NET uninstaller |
| `tools/update-github-similar-projects-snapshot.ps1` | `scripts/update-github-similar-projects-snapshot.ts` |
| `tools/add-test-category-traits.ps1` | `scripts/add-test-category-traits.ts` |
| `tools/layer-test-directories.ps1` | `scripts/layer-test-directories.ts` |

## Contract

- `release-contract` 必须执行真实发布命令。
- 发布后 `git status --porcelain --untracked-files=all` 必须为空。
- 所有 restore 使用 `--locked-mode`。
- AOT publish 使用 `--no-restore`，禁止隐式 restore 改写锁文件。
- `receipt.json` 校验字段：`schemaVersion`, `kind`, `status`, `repoHead`, `publishedExe`, `bytes`, `sha256`。
- `update-manifest.json` 校验字段：`schemaVersion`, `version`, `fileName`, `sha256`, `bytes`, `publishedAtUtc`。
- CI `release-contract` 必须 required，不允许 `continue-on-error`。

## Migration Boundary

- 双轨期间 TS 和 PS 并存，但同一条命令只能有一个权威入口。
- 每条命令迁移前在干净 checkout 对比新旧 exit code、receipt、生成文件。
- 只有 `npm run release:contract` 通过后，才允许进入下一阶段。
- 删除 PS 必须在 TS 等价验证通过之后。
- 生成式卸载器改为小型 .NET 自包含卸载器，避免最终用户依赖 Node 或 PowerShell。

## Rollback

- 每个阶段保留旧 PS 直到该阶段 TS 验证通过。
- 任一阶段失败只回滚该阶段。
- 中央 `release-contract` 始终作为安全网，不允许“先删 PS 再补 TS”。
