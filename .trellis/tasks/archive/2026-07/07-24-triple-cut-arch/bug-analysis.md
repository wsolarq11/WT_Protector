## Bug Analysis: 三剪架构 — 系统性状态漂移与持久化绕过

### 1. Root Cause Category
- **Category**: B (Cross-Layer Contract) + E (Implicit Assumption)
- **Specific Cause**: `_settings` 字段在 MWVM、DragWorkflow、AppStore 三个位置存在副本，无仲裁机制。TrySaveWindowBounds 直接调 LauncherStore.Save 绕过 AppStore，破坏单源真理。

### 2. 修复历程
1. Phase 1: 添加 `CurrentSettings` 属性，所有读取从 AppStore → 消除读取漂移
2. Phase 2: WT1003 编译时禁止绕过 → TrySaveWindowBounds 必须走 Dispatch
3. Phase 3: CleanupStaleTempFiles 加 SaveLock → 消除持久化竞态
4. Phase 4: Stateless FSM 拒绝非法拖拽状态转换

### 3. Prevention Mechanisms
| Priority | Mechanism | Specific Action | Status |
|----------|-----------|-----------------|--------|
| P0 | Compile-time | WT1003: Dispatch_Must_Be_Only_Write_Path | DONE |
| P0 | Compile-time | WT1004: catch (Exception) must have when | DONE |
| P1 | Architecture | CurrentSettings 属性替代 _settings 读取 | DONE |
| P1 | Architecture | Stateless FSM 替代手工状态守卫 | DONE |
| P2 | Runtime | SaveLock + 恢复链搜索 | DONE |

### 4. Systematic Expansion
- **Similar Issues**: 所有 `LauncherStore.Save` 直接调用（测试中已允许，生产中由 WT1003 禁止）
- **Design Improvement**: F# Core 可永久消除状态漂移（Phase 5 已验证兼容性）
- **Process Improvement**: `_settings` 字段应逐步替换为 `CurrentSettings` 只读投影
