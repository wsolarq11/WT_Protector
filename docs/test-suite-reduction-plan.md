Status: Active
Purpose: 测试体系从约 800 收敛到约 200 的执行方案，供新会话接手。
Update when: 削减进度、判据、已知难点或验收标准变化时。

# 测试套件收敛方案（选项 B）

## 目标

把测试从当前 898（811 个 `[Fact]`+`[Theory]`）收敛到约 200，符合设计源 [test-structure.md](test-structure.md) 的取舍模型：每条测试都在守护一个"静默且昂贵"的漂移，其余删掉。

## 现状

- 设计源：[test-structure.md](test-structure.md)（漂移矩阵 + 四层 + 五红线 + 删测试三问）。
- 当前规模：898 测试 / 811 属性 / 139 测试文件 / 26 Views C# 文件。
- 选项 C 已完成：删 12 个零风险测试（2 个确认冗余 + 10 个重复/纯文案/纯转发），`npm run verify` 全绿，已提交。
- C 的诚实结论：零风险可删的只有这 12 个，剩下的削减必须进"有判断争议"的 B。

## 判据：五红线（命中即删）

1. **搬运工/转发器测试**——只测一个类把调用转发给另一个类，无自己的分支或计算。
2. **实现测试**——锁死实现细节，不测行为契约。
3. **复制/文案测试**——只断言字符串字面量，无行为验证。
4. **UI 结构测试**——测 XAML/布局结构，不测决策。
5. **手写架构规则**——编译器/分析器能表达的规则，手写一份重复一份。

## 删测试三问（逐条过）

1. 删了它，有没有别的测试覆盖同样的行为？
2. 它测的是行为契约，还是实现细节？
3. 它的漂移是"静默昂贵"，还是"响亮便宜"？

## 方法

- 分批删：每批 20-30 个测试文件，删完跑 `npm run verify` 确认不丢覆盖。
- 宁缺毋滥：拿不准的一律不删，误删真实回归比多留一条测试更贵。
- 目标不是数字好看，是让每条留下的测试都在守一个真实漂移。
- 每个删除要留记录：为什么删、谁覆盖，写进 commit message。

## 已知难点（按命中红线的层排查）

- `UiContract/`：`MainWindowXamlModularityContractTests`、`PanelsTests`、`DialogConsistencyContractTests` 命中红线 4。
- `Architecture/`：已整个删除。手写架构规则（ArchitectureRules R4/R7-R14 + ServiceContract 源码锁）全部升格为 `LayerDependencyAnalyzer`（WT1008）编译器强制，ArchUnitNET 包与 architecture-rule-check 门禁一并移除。
- `ViewModelBehavior/`：`Launcher*WorkflowTests` 系列可能重复覆盖同一接线（红线 1）。
- `CoreUnit/`：`ServiceResult`、`ErrorOr` 等纯 getter/纯转发测试（红线 1）。

## B 执行进度（已提交 4 批）

- batch 1（e6c9d260）：删 UiContract 8 文件（红线 4 UI 结构）+ CoreUnit 10 文件（红线 1 转发器 + 红线 3 文案），811→754 属性。
- batch 2（054366f2）：删 Architecture R1/R2/R3/R6 + ServiceContract 2 实现测试 + StatusTextContract 5 文案 + 5 个 XAML 结构测试，754→738 属性。
- batch 3（664a3306）：删 7 个 Launcher*WorkflowTests（红线 2 锁实现 + 红线 1 层间重复），738→728 属性。
- batch 4（28db73b7）：删 LauncherBatchLaunchReceiptTextTests（红线 3 文案）+ PresetService 2 转发器测试（红线 1），728→720 属性。
- batch 5（4ee895a9）：新增 `LocalizationKeyAnalyzer`（WT1007），文案 key 漂移升格编译期强制（补删文案测试后欠的账），+3 属性。
- batch 6（69982f53）：新增 `LayerDependencyAnalyzer`（WT1008），Architecture 手写规则（R4/R7-R14 + ServiceContract 源码锁）升格编译期强制，删 Architecture 目录 + ArchUnitNET 包 + architecture-rule-check 门禁，723→717 属性。
- batch 7（待提交）：WT1001（LayerViolationAnalyzer，语法级 Core→UI 检查）合并进 WT1008 语义级规则表，砍掉重复的层依赖分析器，717→713 属性。

合计：139→112 文件 / 811→713 属性 / 898→785 展开测试。每批 `npm run verify` 全绿 + 基线更新。

## B 的诚实结论

忠实执行五红线 + 删测试三问 + 宁缺毋滥后，明确命中红线的测试已全部删除（UI 结构、手写架构规则、转发器、实现测试、纯文案）。剩余 720 属性测试是真实漂移守护：

- 契约层（Persistence 配置往返 + CoreUnit 数据契约 + 端到端黄金用例）：约 330 属性。
- 决策层（CoreUnit 纯函数 + PropertyTest 属性测试）：约 130 属性。
- 编译器层（Analyzer 分析器行为）：约 35 属性。
- VM 行为层（ViewModelBehavior 命令决策：回滚/快照/通知 + 黄金用例）：约 220 属性。

"约 200" 需要五红线之外的激进削减（合并回滚分支、削减 VM 命令分支覆盖、把成功路径归冒烟层），这已超出"宁缺毋滥：拿不准的一律不删"的约束。剩余削减需人工决策，不在自动收敛范围内。

## 验收

- `npm run verify` 全绿。
- 结构基线更新（`npm run gen:structure-baseline`）。
- 每个删除可回溯：commit message 说明"为什么删、谁覆盖"。

## 关键参考

- 设计源：[test-structure.md](test-structure.md)
- 结构基线：`AGENTS.md` 派生表（勿手改，用 `npm run gen:structure-baseline`）
- 命令机器源：`package.json` 脚本工具箱
