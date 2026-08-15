# 执行计划:朋友分发就绪(friend-handoff)

## 前置(已完成)

- [x] P1 实测 `tools/build-release.ps1`:**失败,NETSDK1207**(Analyzer 项目收到全局 PublishAot)。根因与修复见 design.md R0。
- [x] P2 读 `.trellis/spec/{core,views,guides}/index.md` 相关层规范(Views 层改动前必读)

## 实施顺序(按依赖排列)

0. **R0 修复发布路径(阻塞前置)**
   - [x] `WTLauncher.App.csproj`:加 `<PublishAot Condition="'$(EnableNativeAot)' == 'true'">true</PublishAot>`
   - [x] `tools/build-release.ps1`:`-p:PublishAot=true` → `-p:EnableNativeAot=true`
   - [x] `verify.yml` aot-verify:publish 目标改 App 项目 + `-p:EnableNativeAot=true`
   - [x] **验证门**:`tools/build-release.ps1` 成功;实测 AOT 输出是否带 .pdb,据此定 R2 清理逻辑
1. **R1 应用身份**
   - [x] `tools/generate-icon.ps1`:System.Drawing 渲染 lucide terminal 字形 → `assets/app.ico`(多尺寸)
   - [x] 生成并提交 `assets/app.ico`
   - [x] `WTLauncher.App.csproj`:加 `<ApplicationIcon>`、`<Product>`、`<AssemblyTitle>`
2. **R2 干净分发物**
   - [x] `tools/build-release.ps1`:publish 后清理 `*.pdb`/`*.dll`/陈旧 `config/` 目录(按 P1 实测)
3. **R3 zip 打包**
   - [x] `tools/package-friend.ps1`:校验输入齐全 → 组装 `WTLauncher-<version>.zip` → 输出 `artifacts/friend/`
4. **R8 示例配置 + 门禁**
   - [x] 写 `config/launcher.example.json`(空 entries + blank-manual 预设)
   - [x] `tools/verify.ps1` 增加 example 检查(可解析 / entries 空 / 无绝对路径)
5. **R6 朋友文档**
   - [x] `docs/使用说明.txt`(UTF-8 BOM,三步上手 + SmartScreen + FAQ)
6. **R7 README 修正**
   - [x] README 内容清单修正 launcher.json 条目;新增「首次使用」小节链接使用说明
7. **R4 空状态面板**
   - [x] VM 只读投影 `IsEmptyEntries`(派生自条目计数,走 CountChanged 刷新)
   - [x] `LauncherEntriesView.axaml` 空状态面板(欢迎文案 + 两个大按钮,全 token 样式)
8. **R5 添加对话框轻量改造**
   - [x] `EntryDetailsWindow.axaml`:目录行加「浏览...」按钮
   - [x] `EntryDetailsWindow.axaml.cs`:浏览逻辑(OpenFolderDialog 模式)+ 新增模式 ID 自动派生 + 命令默认 `pwsh`
   - [x] 编辑模式回归验证(手动 + 既有测试)

## 验证门(每个阶段合上再进下一步)

- [x] 步骤 0 后:`tools/build-release.ps1` 成功(AC0),普通构建测试仍绿(Analyzer 未受影响)
- [x] `dotnet test src/avalonia-launcher/WTLauncher.Avalonia.sln -c Release`(410+ 测试全绿)
- [x] `dotnet build src/avalonia-launcher/WTLauncher.Avalonia.sln --no-restore -warnaserror`
- [x] 步骤 7/8 完成后:`tools/verify.ps1` 全绿
- [x] `tools/build-release.ps1` → 检查:无 pdb/dll、exe 属性版本+图标
- [x] `tools/package-friend.ps1` → 检查 zip 结构
- [x] 手动冒烟:全新空目录解压 zip → 双击 exe → 空状态面板 → 浏览目录添加工作区 → 启动成功
- [x] 编辑模式冒烟:编辑现有条目,字段预填/保存行为不变

## 风险与回滚点

- R0 修复:三处小改动(csproj + build-release + verify.yml),回滚 = git revert;`EnableNativeAot` 是全新无害属性,不影响任何既有构建
- `build-release.ps1` 清理步骤:实施前先备份当前产物目录(或直接依赖 git,产物目录是 ignored 本地构建输出,回滚=重新 publish)
- 对话框改动:回归面 = 编辑模式 + 既有 410 测试;冲突时回退 EntryDetailsWindow 两个文件即可
- 空状态面板:纯 UI + 只读投影,不触碰数据流;若投影刷新时机有误,回退面板 IsVisible 为静态即可

## 完成前提

- [x] 全部 AC0-AC8 达成
- [x] 最后迭代全量 `tools/verify.ps1` 绿 + 手动冒烟通过
