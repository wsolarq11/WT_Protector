# Implement — 删 LiteDB 完整死链路

## 执行步骤(顺序)

### Step 1: 启动任务
```bash
python ./.trellis/scripts/task.py start 07-09-remove-litedb-deadcode
```

### Step 2: 删除代码文件
```bash
rm src/avalonia-launcher/src/WTLauncher.Core/LiteDbStore.cs
rm src/avalonia-launcher/src/WTLauncher.Core/MigrationTool.cs
rm src/avalonia-launcher/tests/WTLauncher.Tests/MigrationToolTests.cs
```

### Step 3: 改 csproj(删 LiteDB 依赖)
- 编辑 `src/avalonia-launcher/src/WTLauncher.Core/WTLauncher.Core.csproj`
- 删除 `<ItemGroup>` 中 `<PackageReference Include="LiteDB" />`
- 若该 ItemGroup 变空,删整个 ItemGroup

### Step 4: 改 Program.cs
- 删 L54 注释 `// MigrateJsonToLiteDb();  // LiteDB Phase 3 deferred`
- 删 L138-162 `MigrateJsonToLiteDb()` 私有方法及其上方 doc comment
- 删方法后,检查文件顶部 `using` 是否还有未用项(`using WTLauncher.Core;` 等若不再被用也应删,但要小心)

### Step 5: 改 LauncherStore.cs(修文件名 bug)
- L123: `var storePath = Path.Combine(configDirectory, "store.json");`
- 改为: `var storePath = Path.Combine(configDirectory, StoreFileName);`

### Step 6: 改 quality-guidelines.md(删死注释)
- L68-69:`❌ Adding a NuGet reference to Core — Core must stay dependency-free (beyond BCL).` 下面的 ponytail 注释行整行删除

### Step 7: 验证
```bash
# 编译
dotnet build -c Release src/avalonia-launcher/WTLauncher.Avalonia.sln

# 测试
dotnet test -c Release src/avalonia-launcher/WTLauncher.Avalonia.sln

# 残留检查
grep -rn "LiteDb\|MigrationTool" src/avalonia-launcher/src/ src/avalonia-launcher/tests/

# 完整门禁
powershell -NoProfile -ExecutionPolicy Bypass -File ./tools/verify.ps1
```

### Step 8: 提交
```bash
git add -A
git commit -m "$(cat <<'EOF'
refactor: remove LiteDB dead code path (Phase 3 abandoned)

- Delete LiteDbStore.cs (93 lines) + MigrationTool.cs (30 lines)
- Delete MigrationToolTests.cs (50 lines, tested dead code)
- Remove LiteDB NuGet dependency from WTLauncher.Core.csproj
- Remove MigrateJsonToLiteDb() method + commented call site in Program.cs
- Remove stale ponytail comment in quality-guidelines.md that referenced
  the already-deleted IStore abstraction as LiteDB's justification
- Fix LauncherStore.MigrateFromLegacyFiles: store.json → StoreFileName
  constant (was a latent bug for users upgrading from legacy 3-file format)

Total: -180 lines code + 1 NuGet dependency removed. The entire LiteDB
pipeline was dead since Program.cs:54 commented out MigrateJsonToLiteDb();
the JSON store (LauncherJson + LauncherStore) is and remains the only
production persistence path.

Co-Authored-By: glm-5.2 <zai-org@claude-code-best.win>
EOF
)"
```

### Step 9: 归档任务
```bash
python ./.trellis/scripts/task.py finish
python ./.trellis/scripts/task.py archive 07-09-remove-litedb-deadcode
```

## Rollback Points

- 任一 Step 失败 → `git status` 看改动,`git checkout -- .` 回滚
- 若测试失败 → 检查是否有遗漏的 LiteDbStore/MigrationTool 引用(grep)
- 若 verify.ps1 失败 → 看具体检查项,通常是文件数/schemaVersion 检查需要同步
