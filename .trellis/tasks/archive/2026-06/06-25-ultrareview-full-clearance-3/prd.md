# UltraReview 全部发现清零 — 75+ 项修复

## Goal

基于 2026-06-25 完成的 14 维度独立并行 UltraReview，将全部 75+ 项发现清零。目标置信度无限逼近 100%。绝对禁止降低测试标准来迁就产品代码缺陷。

## 审查基线

本次审查完全独立进行，未参考任何历史 UltraReview 结果。14 个代理并行覆盖：
安全、错误处理、文件 I/O、逻辑正确性、ViewModel/MVVM、资源管理、JSON 序列化、
视图层/拖拽、并发/线程、数据验证、预设/设置、测试套件、性能/GC。

## Background — 已知文档化缺口（不计入本次修复范围）

以下缺口在 `docs/feature-first-class-upgrade-audit.md` 中已明确记录为已知限制，
不在本次修复范围内（除非其本身存在代码缺陷）：

- `searchHistory` 无 UI 表面 — 文档化缺口，非代码缺陷
- `LauncherDragReorderWorkflow` 无隔离单元测试 — 文档化测试缺口
- `ParseLaunchedAt`/`ParseUpdatedAt` 不对称 — 文档化设计缺口
- Details 列宽硬编码 — 文档化低优实现缺口
- Clone entry 零测试 — 文档化测试缺口
- Command template resolver 零测试 — 文档化测试缺口

## Requirements

### R1 — 严重 (Critical)：5 项，必须最先修复

| ID | 标题 | 文件 | 行号 |
|----|------|------|------|
| R1.1 | 备份损坏时的无限循环 | `LauncherConfigStore.cs` | 38-54 |
| R1.1b | 备份损坏时的无限循环 | `LauncherPresetStore.cs` | 22-38 |
| R1.1c | 备份损坏时的无限循环 | `LauncherSettingsStore.cs` | 23-39 |
| R1.2 | 代理对拆分：ID 截断产生非法 UTF-16 | `LauncherEntryIds.cs` | 17 |
| R1.2b | 代理对拆分：ID 截断产生非法 UTF-16 | `LauncherPresetIds.cs` | 42 |
| R1.3 | TOCTOU 竞态导致 .bak 恢复被跳过 | `LauncherConfigStore.cs` | 38-54 |
| R1.3b | TOCTOU 竞态导致 .bak 恢复被跳过 | `LauncherSettingsStore.cs` | 23-39 |
| R1.4 | `AssertJsonPropertyNames` 不检测缺失字段 | `RepositoryConfigContractTests.RootConfigTests.cs` | 154-161 |
| R1.5 | `TerminalExecutable` 断言是同义反复 | `RepositoryConfigContractTests.RootConfigTests.cs` | 24 |

### R2 — 高 (High)：15 项，安全漏洞和功能缺陷

#### 安全 (4项)

| ID | 标题 | 文件 | 行号 |
|----|------|------|------|
| R2.1 | UNC 路径 NTLM 凭据中继 | `LaunchArgumentBuilder.cs` | 33,50 |
| R2.1b | UNC 路径 NTLM 凭据中继 | `LauncherProcess.cs` | 14 |
| R2.2 | wt.exe 二进制劫持通过 PATH 回退 | `LaunchArgumentBuilder.cs` | 22-26 |
| R2.3 | 全角百分号规范化触发意外环境变量展开 | `LaunchArgumentBuilder.cs` | 33 |
| R2.4 | BiDi/格式控制字符（Cf 类）未剥离 | `LaunchArgumentBuilder.cs` | 66-75 |

#### 数据完整性 (3项)

| ID | 标题 | 文件 | 行号 |
|----|------|------|------|
| R2.5 | ID 缺少 Unicode 规范化导致假唯一性 | `LauncherEntryIds.cs` | 9-41 |
| R2.6 | `LastUsedPresetId` 是死代码 + 不必要 I/O | `LauncherSettings.cs:42-44`, `MainWindowViewModel.cs:134`, `Presets.Helpers.cs:106-118` |
| R2.7 | `UpdatePreset` 使用值相等 IndexOf 而非 ID 查找 | `LauncherPresetWorkflow.cs` | 50-51 |

#### 异常安全和崩溃 (6项)

| ID | 标题 | 文件 | 行号 |
|----|------|------|------|
| R2.8 | MutationPersistence 中 InvalidDataException 处理不一致 | 3 个 `*MutationPersistence.cs` |
| R2.9 | Release 构建中备份轮换失败静默（Debug.WriteLine 被剥离） | `LauncherJson.cs` | 66 |
| R2.10 | 空 catch 块吞没清理错误 | `LauncherJson.cs:105-107`, `ErrorLog.cs:88-102` |
| R2.11 | 键盘焦点状态在过滤器清空可见条目时泄漏 | `MainWindow.axaml.cs` | 156-162 |
| R2.12 | 拖拽插入决策跨拖拽会话泄漏 | `MainWindow.Drag.cs` | 23,168-169 |
| R2.13 | async void 拖拽处理器异常安全不完整 | `MainWindow.Drag.cs` | 27 |
| R2.14 | SetKeyboardFocusedEntryId 中 NRE 风险 | `MainWindowViewModel.cs` | 236-250 |
| R2.15 | ErrorLog 并发写入静默丢弃错误（无诊断输出） | `ErrorLog.cs` | 52 |

### R3 — 中 (Medium)：22 项，边界条件和设计缺陷

#### 文件 I/O (5项)

| ID | 标题 | 文件 |
|----|------|------|
| R3.1 | 非原子备份创建（File.Copy → 应使用 temp-file-then-rename） | `LauncherJson.cs:57-68` |
| R3.2 | 非原子备份恢复（File.Copy → 应使用 temp-file-then-rename） | 所有 3 个 Store 文件 |
| R3.3 | 无 fsync — File.WriteAllText 后无 Flush(true) | `LauncherJson.cs:52` |
| R3.4 | NaN 窗口坐标导致未处理的 JsonException | `LauncherSettingsStore.cs:201-206` |
| R3.5 | 多文件保存无原子性（跨文件不一致状态） | `LauncherEntryMutationPersistence.cs:22-56` |

#### 错误处理 (3项)

| ID | 标题 | 文件 |
|----|------|------|
| R3.6 | Refresh() 链无错误处理 | `MainWindowViewModel.cs:125-141` |
| R3.7 | finally 块中 File.Delete 可替换原始异常 | `LauncherJson.cs:77-81` |
| R3.8 | 冗余 catch(InvalidDataException){throw;} | `LauncherSettingsStore.cs:80-83` |

#### 性能 (4项)

| ID | 标题 | 文件 |
|----|------|------|
| R3.9 | 每按键 FilterAndSort 分配 ToList() + AsReadOnly() | `LauncherEntryVisibility.cs:40,86` |
| R3.10 | 双 JSON 解析同一字节数组 | `LauncherConfigStore.cs:31-34` |
| R3.11 | GetSelectedEntryIds 分配两个 List（Select+ToList） | `MainWindowViewModel.Selection.cs:290-298` |
| R3.12 | SyncVisibleEntries 每次调用分配 HashSet+Dictionary | `MainWindowViewModel.Layout.cs:291,299` |

#### 逻辑正确性 (3项)

| ID | 标题 | 文件 |
|----|------|------|
| R3.13 | 命令模板键大小写不匹配（Validate vs Resolve 使用不同比较器） | `LauncherCommandTemplateResolver.cs:54` |
| R3.14 | FailureIndex 属性被覆盖（应存储第一个，实际存储最后一个） | `LauncherBatchLaunchReport.cs:140` |
| R3.15 | RenameEntryId 在规范化后 ID 相同时仍返回 Renamed=true | `LauncherEntryReferences.cs:16-17` |

#### JSON/序列化 (3项)

| ID | 标题 | 文件 |
|----|------|------|
| R3.16 | UnmappedMemberHandling.Disallow 破坏前向兼容性 | `LauncherJson.cs:17` |
| R3.17 | selected 字段预检未验证值类型（仅检查键存在） | `LauncherConfigStore.cs:192-198` |
| R3.18 | Unicode 规范化不匹配（内存 NFKC vs 磁盘原始形式） | `LauncherEntryText.cs:7` |

#### 视图层/拖拽 (4项)

| ID | 标题 | 文件 |
|----|------|------|
| R3.19 | 键盘焦点索引在可见条目变化后过期 | `MainWindow.axaml.cs:41,169-176` |
| R3.20 | 快速视图模式切换时滚动偏移恢复竞态 | `MainWindow.axaml.cs:218-231` |
| R3.21 | 拖拽期间显示模式 ComboBox 仍可交互 | `MainWindow.Drag.cs:42-76` |
| R3.22 | FastWrapPanel 硬编码 900px 回退（非设计令牌） | `FastWrapPanel.cs:36` |

### R4 — 低 (Low)：30+ 项，代码质量和维护性

#### 数据验证 (5项)

| ID | 标题 | 文件 |
|----|------|------|
| R4.1 | ParseTimestamp 使用过于宽松的 TryParse（接受任意日期格式） | `LauncherEntryVisibility.cs:223` |
| R4.2 | RecordLaunchFromOwner 存储时间戳无格式验证 | `LauncherEntryViewModel.cs:114-119` |
| R4.3 | 空 sortMode/viewMode ID 产生混淆的错误消息 | `LauncherEntryVisibility.cs:23`, `LauncherEntryViewModes.cs:24` |
| R4.4 | 错误消息中 [..50] 范围表达式脆弱 | `LauncherSettingsStore.cs:233` |
| R4.5 | RequireNullableText 是死方法 | `LauncherSettingsStore.cs:165-166` |

#### 结构一致性 (5项)

| ID | 标题 | 文件 |
|----|------|------|
| R4.6 | LauncherPresetFiles.Clone 不重置 SchemaVersion | `LauncherPresetFiles.cs:7-15` |
| R4.7 | LauncherSettings.SchemaVersion 缺少 [JsonRequired] | `LauncherSettings.cs:12` |
| R4.8 | LauncherSettingsCopies 注释错误声称 SchemaVersion 是 [JsonIgnore] | `LauncherSettingsCopies.cs:21` |
| R4.9 | LauncherSettings.Window 有 [JsonRequired] 但有默认值（不一致） | `LauncherSettings.cs:46-48` |
| R4.10 | CloneCustomEntryOrder 命名误导（不克隆 ImmutableList） | `LauncherEntryMutationSnapshot.cs:55-58` |

#### 视图/无障碍 (5项)

| ID | 标题 | 文件 |
|----|------|------|
| R4.11 | Space 键未处理（键盘聚焦条目无法用空格激活） | `MainWindow.axaml.cs:121-148` |
| R4.12 | 启动按钮缺少 AutomationProperties.Name | `MainWindow.axaml` 多处 |
| R4.13 | 上下文菜单项缺少 AutomationProperties.Name | `MainWindow.axaml` 多处 |
| R4.14 | 卡片视图拖拽手柄用文本"拖拽"vs 其他视图用"≡"（不一致） | `MainWindow.axaml:552-556` |
| R4.15 | 拖拽幽灵透明度 0.86 硬编码（非设计令牌） | `MainWindowDragGhostFactory.cs:55` |

#### 资源/生命周期 (2项)

| ID | 标题 | 文件 |
|----|------|------|
| R4.16 | ViewModel 委托捕捉 MainWindow 强引用（GC 泄漏风险） | `MainWindow.axaml.cs:52-61` |
| R4.17 | Lambda 事件处理器永不退订 | `MainWindow.axaml.cs:64-65,76-77` |

#### 文件 I/O 和测试 (3项)

| ID | 标题 | 文件 |
|----|------|------|
| R4.18 | Temp 文件清理使用宽泛 glob 模式 `*.tmp-*` | `LauncherJson.cs:99` |
| R4.19 | 搜索历史排序假设未强制执行 | `LauncherSettingsStore.cs:159` |
| R4.20 | TempDirectory.Dispose 静默吞没清理错误 | `TempDirectory.cs:25-31` |

#### 其他低优项 (10+)

| ID | 标题 | 文件 |
|----|------|------|
| R4.21 | LauncherPresetDeleteTarget 验证 entryIds 后丢弃结果 | `LauncherPresetDeleteTarget.cs:7-12` |
| R4.22 | UpdateSelectedPreset() 缺少 HasChanges defense-in-depth 检查 | `MainWindowViewModel.Presets.cs:112-143` |
| R4.23 | 严格 schema version 检查无迁移路径 | `LauncherPresetStore.cs:17-18`, `LauncherSettingsStore.cs:17-18` |
| R4.24 | 构造函数直接设置 _requireConfirmationForLaunches 字段不一致 | `MainWindowViewModel.cs:89` |
| R4.25 | _visibleEntries 字段缺少 readonly | `MainWindowViewModel.cs:19` |
| R4.26 | 预检和反序列化器选项不一致 | `LauncherConfigStore.cs:25-34 vs 182-186` |
| R4.27 | 大小写变体字典键可能静默冲突 | `LauncherSettings.cs:35` |
| R4.28 | IsSelected 公共 setter 无重入守卫 | `LauncherEntryViewModel.cs:33-41` |
| R4.29 | RecordSearchTerm 中冗余的 if/else 分支 | `MainWindowViewModel.Layout.cs:268-275` |
| R4.30 | 备份恢复重新从磁盘读取（而非用内存数据） | `LauncherConfigStore.cs:51` |

### R5 — 测试套件补充

| ID | 标题 | 文件 |
|----|------|------|
| R5.1 | 补充 null 输入验证测试（Capture/RestoreCustomEntryOrder） | `LauncherSettingsSnapshotTests.cs` |
| R5.2 | Commit(nonCustom) 测试需验证 HasActiveSession 仍为 true | `LauncherDragReorderWorkflowTests.cs:146-157` |
| R5.3 | Begin() 返回值不应被丢弃 | `LauncherDragReorderWorkflowTests.cs:72` |
| R5.4 | "RoundTrip" 测试需真正执行 save+reload | `RepositoryConfigContractTests.SettingsTests.cs:9` |
| R5.5 | LaunchPlan with 表达式验证测试缺失 | `LaunchPlanTests.cs` |
| R5.6 | LauncherProcess.Start(null) 测试缺失 | `LauncherProcessTests.cs` |

## Acceptance Criteria

- [ ] R1: 全部 5 项严重问题修复，有隔离单元测试证明
- [ ] R2: 全部 15 项高严重度问题修复，有隔离单元测试证明
- [ ] R3: 全部 22 项中严重度问题修复
- [ ] R4: 全部 30+ 项低严重度问题修复（或明确记录为有意保留）
- [ ] R5: 全部 6 项测试补充完成
- [ ] `dotnet test` 全部通过（当前基线：542 tests PASS），新增测试不少于 15 个
- [ ] `tools\verify.ps1` 全部通过
- [ ] 零破坏性：config/presets/settings JSON schema 向后兼容
- [ ] 零降低测试标准：不接受删除测试或弱化断言来"通过"测试

## 技术约束

1. **只读 → 读写**: 本次可以修改代码，可以构建和运行测试
2. **不可降低测试标准**: 绝对禁止删除或弱化现有测试来迁就代码缺陷
3. **向后兼容**: config.json / presets.json / settings.json 格式不得破坏
4. **保持不可变模式**: 所有数据修改通过 `with { }` 表达式
5. **纯函数核心**: Core 层零 Avalonia 依赖

## 排除范围

- 不新增功能（仅修复缺陷）
- 不进行大规模重构（保持现有架构）
- 不添加文档化缺口（searchHistory UI 等已在 audit 中记录）
- 不修改 `retired/` 目录内容
- 不修改 `.trellis/` 下的工作流文件
