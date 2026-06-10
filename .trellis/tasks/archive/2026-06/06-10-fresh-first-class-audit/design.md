# 审计方法论设计

## 审计边界

```
输入：HEAD commit 22f57a9 的 src/、config/、docs/、tools/
输出：docs/feature-first-class-upgrade-audit.md（覆盖）
不碰：源码逻辑、配置 schema、门禁逻辑
```

## 评估维度（五条铁律）

每项功能必须逐条回答：

1. **真实问题** — 它解决什么问题？这个问题在当前用户工作流里真实存在吗？频率多高？
2. **数据所有权** — 谁拥有状态？有没有第二套真相（dual source of truth）？
3. **用户路径** — 用户怎么触发这个能力？路径是否显式可见？有没有隐藏 fallback？
4. **失败契约** — 异常/错误在哪里暴露？会吞错继续跑吗？失败后有可见回执吗？
5. **验证覆盖** — 当前测试/门禁是否覆盖了这个能力的声明？有没有只靠旧日志证明的"鬼魂功能"？

## 星级标度

```
⭐       占位/未实现/已退化
⭐⭐      有严重缺口
⭐⭐⭐    勉强可用但有可见问题
⭐⭐⭐⭐  好，有小缺口
⭐⭐⭐⭐⭐ 一等公民——当前窄域内的正确实现
```

## 审计流程

```
Phase 1: 收集证据
  - git diff --check
  - tools\verify.ps1 (full)
  - 扫描 src/avalonia-launcher/src/ 全部 .cs 文件
  - 扫描 config/*.json schema
  - 读取测试文件确认覆盖范围
  - GitHub 快照时效性检查

Phase 2: 逐功能审计
  - 每个功能：读关联源码 → 查测试覆盖 → 评星级 → 写证据锚点
  - 不偷懒：不因为昨天审计给了5星就跳过源码验证

Phase 3: 缺口分析
  - 区分 future-workload / broad-scope / external-process-limit / distribution-scope
  - 检查昨天报告中的缺口是否仍然真实存在

Phase 4: 写报告
  - 覆盖 docs/feature-first-class-upgrade-audit.md
  - 更新审查日期和 HEAD hash
  - 更新关联引用
```

## 关键审视点（易遗漏处）

这些是昨天审计可能没充分覆盖的面积：

- `LauncherDesignTokens.cs` — XAML 资源注入是否正确消除了硬编码样式？
- `FastWrapPanel.cs` — 名称墙多列布局的边界条件（空列表、单条目、极窄窗口）
- `searchHistory` — settings.json 新增字段的 schema 契约和 fail-fast
- `commandTemplates` runtime resolver — unknown key 的 fail-fast 是否完整？
- `recent` sort mode — `launchedAtUtc` 时间戳的写入时机和排序正确性
- `updated` sort mode — `updatedAtUtc` 的更新时机（是否每次 mutation 都刷新）
- Drag lazy preview — `MainWindow.Drag.cs:148-157` 的 lazy preview 合同是否被测试钉住
- `LauncherEntryIds` — 新增工具类的 blank/duplicate rejection
- `LauncherSettingsCopies` — snapshot 合同完整性
