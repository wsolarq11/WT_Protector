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
- `Architecture/`：`ArchitectureRules.cs` 命中红线 5（已删 R5，其余规则逐个评估是否与分析器重复）。
- `ViewModelBehavior/`：`Launcher*WorkflowTests` 系列可能重复覆盖同一接线（红线 1）。
- `CoreUnit/`：`ServiceResult`、`ErrorOr` 等纯 getter/纯转发测试（红线 1）。

## 验收

- `npm run verify` 全绿。
- 结构基线更新（`npm run gen:structure-baseline`）。
- 每个删除可回溯：commit message 说明"为什么删、谁覆盖"。

## 关键参考

- 设计源：[test-structure.md](test-structure.md)
- 结构基线：`AGENTS.md` 派生表（勿手改，用 `npm run gen:structure-baseline`）
- 命令机器源：`package.json` 脚本工具箱
