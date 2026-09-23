# CLAUDE.md

导航、命令人工视图、项目级不变量唯一源在 `AGENTS.md`；文档索引在 `docs/README.md`；五层架构唯一源在 `src/avalonia-launcher/README.md`；代码层规范在 `.trellis/spec/`；踩坑见 `.trellis/spec/guides/pitfalls.md`。

## 本地迭代命令

```bash
npm ci
npm run typecheck
npm test
```

产品/门禁入口命令见 `AGENTS.md` 脚本工具箱（机器源 `package.json`）。

## Raw dotnet（本地迭代）

```bash
dotnet restore src/avalonia-launcher/WTLauncher.Avalonia.sln -r win-x64 -p:EnableNativeAot=true
dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release --no-restore
dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror
dotnet publish src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release -r win-x64 --self-contained --no-restore
```

`-warnaserror` is real in CI and the gate. C# 构建/测试一律 `--no-restore`，先显式完整参数还原。

## Trellis

改层前读 `.trellis/spec/{core,views,tests,guides}/index.md`；非平凡工作走 `.trellis/workflow.md` Plan → Execute → Finish。
