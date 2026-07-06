# Fix UltraReview CRITICAL/HIGH findings

## Goal

修复 2026-07-05 UltraReview #8 中发现的 3 个 CRITICAL、2 个 HIGH 级别缺陷及其相关的 MEDIUM 问题，确保应用正常启动、正确加载配置、持久化路径一致。

## Background

UltraReview #8 使用 10 个并行子代理进行的全量审查，发现以下最紧急的问题。

### 需修复的缺陷

| 级别 | ID | 问题 | 文件 |
|------|----|------|------|
| 🔴 CRITICAL | C1+C2 | `LauncherStore.StoreFileName = "store.json"` 与磁盘文件名 `launcher.json` 不匹配，应用空配置启动，`run.ps1` 无法运行 | `LauncherStore.cs:9`, `config-lib.ps1:68`, `run.ps1:12-20` |
| 🔴 CRITICAL | C3 | `LiteDbStore.Open()` 异常直接传播到启动流程最外层，完全不触发 JSON 后备，应用永久启动失败 | `App.axaml.cs:126-128`, `LiteDbStore.cs:17` |
| 🟠 HIGH | H1 | `Refresh()` 仅重载 entries，不重载 `_settings` 和 `_launchWorkflow.CommandTemplates`，外部设置编辑不生效 | `MainWindowViewModel.cs:136-185` |
| 🟠 HIGH | H2 | `.trellis/spec/` 下 14 个文件引用已删除的类（`LauncherConfigStore`, `LauncherPresetStore`, `MainWindowDragGhostFactory` 等），测试计数过时 | `quality-guidelines.md`, `persistence.md`, `drag-drop.md`, `directory-structure.md` 等 |
| 🟡 MEDIUM | M1 | `LiteDbStore.Load` 返回前不调用 `LauncherStore.Normalize`，绕过重复 ID 检测、空字段拒绝、长度限制 | `LiteDbStore.cs:42-49` |
| 🟡 MEDIUM | M2 | `LiteDbStore.Save` 的 `DeleteAll()+Insert()` 无事务保护，崩溃可能导致数据完全丢失 | `LiteDbStore.cs:65-76` |
| 🟡 MEDIUM | M5 | `ExceptionFilters.IsRecoverable` 认为 NRE/IndexOutOfRange 可恢复，吞没逻辑错误 | `ExceptionFilters.cs:31` |
| 🟡 MEDIUM | M7 | 全局互斥体使用 `Global\` 命名空间，本地拒绝服务面大 | `Program.cs:76` |
| 🟡 MEDIUM | M8 | PR CI 缺少 `dotnet publish` 步骤，发布破坏在合并后才被发现 | `.github/workflows/verify.yml` |
| 🟡 MEDIUM | M12-M14 | `EntryDirectory`, `MigrationTool`, 5 个 JSON 转换器零测试覆盖 | `EntryDirectory.cs`, `MigrationTool.cs`, 各 `JsonConverter` |

## Decisions

- **C1/C2**: 改代码——`StoreFileName` → `"launcher.json"`，不改文件名
- **M2**: 使用 LiteDB `BeginTrans/Commit` 事务保护写操作
- **M5**: `ExceptionFilters.IsRecoverable` 在 DEBUG 时重新抛出 NRE/IndexOutOfRange
- **H2**: 包含在当前任务中，一次性修复 14 个 spec 文件

## Requirements

### R1: 修复 store.json 文件名不匹配
- 将 `LauncherStore.StoreFileName` 改为 `"launcher.json"`
- 同步更新 `config-lib.ps1` 和 `run.ps1` 中的路径引用
- 验证 `dotnet test` 通过且应用正确加载配置

### R2: 修复 LiteDB 异常兜底
- `LiteDbStore.Open` 调用包裹 try/catch，失败时回退到 `new JsonStore()`

### R3: Refresh() 重载设置
- `Refresh()` 中同步 `storeFile.Settings` 到 `_settings` 和 `_launchWorkflow.CommandTemplates`

### R4: LiteDB 路径调用 Normalize
- `LiteDbStore.Load` 返回前调用 `LauncherStore.Normalize`

### R5: LiteDB Save 事务
- 用 `BeginTrans/Commit` 包裹写操作

### R6: 互斥体安全加固
- `Global\` → `Local\`

### R7: PR CI 添加发布验证
- `build-and-test` 作业中添加 `dotnet publish` 步骤

### R8: 测试覆盖
- 新增 `EntryDirectory` 测试
- 新增 `MigrationTool` 测试
- 新增 JSON 转换器测试

### R9: Spec 腐烂修复
- 更新 14 个 spec 文件中过时的类名引用和测试计数

## Out of Scope

- LOW 级别问题、非紧急 MEDIUM（M3-M4-M6-M9）、功能新增或重构

## Acceptance Criteria

- [ ] C1/C2: 应用启动后正确加载 `config/launcher.json` 全部条目和设置
- [ ] C1/C2: `run.ps1` 从仓库根目录成功执行
- [ ] C3: 损坏 `store.db` 后应用通过 JSON 后备成功启动
- [ ] H1: `Refresh()` 后设置和模板反映外部修改
- [ ] M1: LiteDB 路径拒绝重复 ID 和空字段
- [ ] M2: LiteDB Save 有事务保护
- [ ] M12-M14: 新增测试覆盖三个缺口
- [ ] H2: 14 个 spec 文件无残留过时引用
- [ ] 全部现有 455 个测试通过
- [ ] `tools/verify.ps1` 通过
