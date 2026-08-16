# Design: 中央发布契约

## Boundary

- 本阶段只改 PowerShell 工具链和 CI，不开始 TS 迁移。
- AOT 是发布契约内部的一个步骤。
- 当前未提交的 lockfile、build-release restore、CI restore 对齐是基线，不回滚。

## Release Contract Flow

```
release-contract.ps1
   │
   ├── build-release.ps1 -LockedMode
   │     ├── dotnet restore -r win-x64 -p:EnableNativeAot=true --locked-mode
   │     └── dotnet publish -c Release -r win-x64 -p:EnableNativeAot=true --no-restore
   │
   ├── git status --porcelain --untracked-files=all
   ├── receipt.json validation
   └── update-manifest.json validation
```

## Files

- `tools/build-release.ps1`: add `-LockedMode`, locked restore, publish `--no-restore`.
- `tools/release-contract.ps1`: new central wrapper.
- `.github/workflows/verify.yml`: required release contract job.
- `tools/verify.ps1`: add `release-lockfile-consistency`, bump count.
- `AGENTS.md`: update gate count and command references.
- `README.md` / `CLAUDE.md` / `src/avalonia-launcher/README.md`: document `release-contract`.

## CI Contract

- Job name: `release-contract`.
- `continue-on-error: false`.
- Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\release-contract.ps1`.
- Upload `artifacts/publish/win-x64` and receipt.
- Branch protection requires this job.

## Rollback

- `release-contract.ps1` can be deleted if CI needs an emergency revert.
- Existing `aot-verify` can be restored, but only as temporary rollback.
- Lockfile update stays because it fixes stale AOT dependencies.
