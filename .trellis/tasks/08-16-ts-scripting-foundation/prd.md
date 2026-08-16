# TS 脚本系统骨架

## Goal

建立 Node/TypeScript 脚本系统，并把发布链路迁移到 TS，作为中央发布契约的新执行体。

## Requirements

- 新增 `package.json`、`tsconfig.json`、`scripts/config-lib.ts`。
- 新增 `scripts/build-release.ts`，与 `tools/build-release.ps1` 行为等价。
- 新增 `scripts/release-contract.ts`，替代 `tools/release-contract.ps1`。
- 使用 Node LTS、TypeScript、tsx、execa、Vitest。
- `.wt-protector.dsc.yaml` 增加 Node LTS 安装项。
- CI release job 切换为 `npm ci && npm run release:contract`。

## Acceptance Criteria

- [ ] `npm run release:contract` 在干净 checkout 上 PASS。
- [ ] TS 与 PS receipt 关键字段一致。
- [ ] TS 发布不修改 `packages.lock.json`。
- [ ] `scripts/tests/` 覆盖 config-lib 和 release contract 关键路径。

## Out of Scope

- 根门禁全量迁移。
- 产品运行 shell 替换。
