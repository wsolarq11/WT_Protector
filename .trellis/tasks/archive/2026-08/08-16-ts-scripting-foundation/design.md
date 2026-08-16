# Design: TS 脚本系统骨架

## Goal

建立 Node/TypeScript 脚本运行层，并把发布链路迁移为 TS 权威入口。旧 PowerShell 发布脚本继续保留到 P2-P4 等价验证通过，但在 P1 后 `npm run release:contract` 是 CI 的发布契约执行体。

## Scope

- 新增 Node 工程：`package.json`、`tsconfig.json`、`scripts/`、`scripts/tests/`。
- 新增 `scripts/config-lib.ts`，提供路径、receipt、hash、git、进程执行等共享能力。
- 新增 `scripts/build-release.ts`，行为等价 `tools/build-release.ps1`。
- 新增 `scripts/release-contract.ts`，行为等价 `tools/release-contract.ps1`。
- `.wt-protector.dsc.yaml` 增加 Node LTS。
- CI `release-contract` 切换到 `npm ci && npm run release:contract`。

## Target Script Stack

| Item | Choice |
|---|---|
| Runtime | Node.js LTS |
| Language | TypeScript strict |
| Executor | `tsx` |
| Process orchestration | `execa` |
| Tests | Vitest |
| Package manager | npm |
| CLI parsing | Node built-in `util.parseArgs` |

## Module Contracts

### `scripts/config-lib.ts`

| Export | Contract |
|---|---|
| `repoRoot` | `path.resolve(__dirname, "..")` |
| `avaloniaRoot` | `src/avalonia-launcher` |
| `solutionPath` | `WTLauncher.Avalonia.sln` |
| `appProjectPath` | `src/WTLauncher.App/WTLauncher.App.csproj` |
| `publishDir` | `artifacts/publish/win-x64` |
| `runStep(name, fn)` | execute command; non-zero exit throws with step name |
| `readJson<T>(path)` | parse UTF-8 JSON, throw on null/invalid |
| `sha256File(path)` | full uppercase SHA-256 |
| `shortSha256(path)` | first 16 chars of SHA-256 |
| `makeReceipt(kind, fields)` | schemaVersion 1, kind, status PASS, generatedAtUtc, repoHead, worktreeDirty, worktreeStatus |
| `gitStatus(repoRoot)` | `git status --short --untracked-files=all` lines |
| `resolveExecutablePath(path)` | `fs.realpath` or full path fallback |

### `scripts/build-release.ts`

Parity with `tools/build-release.ps1`:

1. Prepend Visual Studio Installer path when present (AOT needs `vswhere.exe` on the PATH).
2. Compute solution/app/publish paths.
3. Reject running `WTLauncher.App.exe` from publish dir.
4. Derive version with `git describe --tags --always --dirty` and the same fallback rules.
5. `dotnet restore <solution> -r win-x64 -p:EnableNativeAot=true [--locked-mode]`.
6. `dotnet publish <app> -c Release -r win-x64 -p:EnableNativeAot=true --no-restore -o <publishDir> -p:Version=<v> -p:InformationalVersion=<v>`.
7. Clean `*.pdb`; remove stale `config/`; keep all `*.dll`.
8. Write `receipt.json` and `update-manifest.json` with UTF-8 no BOM.
9. Print `receipt.json`.

### `scripts/release-contract.ts`

Parity with `tools/release-contract.ps1`:

1. Run `scripts/build-release.ts --locked`.
2. Assert `git status --porcelain --untracked-files=all` empty.
3. Validate `receipt.json` fields and hashes.
4. Validate `update-manifest.json` fields and hashes.
5. Print contract receipt.

## Receipt Parity

| Field | PS | TS |
|---|---|---|
| `schemaVersion` | 1 | 1 |
| `kind` | `release-build` / `release-contract` | same |
| `status` | `PASS` | `PASS` |
| `repoHead` | short HEAD | same |
| `worktreeDirty` | status count > 0 | same |
| `worktreeStatus` | git status lines | same |

## Out of Scope

- Mainline/root gate migration (P2/P3).
- Auxiliary scripts and PowerShell removal (P4).
- Product launch shell replacement.
