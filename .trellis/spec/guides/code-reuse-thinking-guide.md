# Code Reuse Thinking Guide

> 目的：写新代码前先想——是否已存在？重复代码是不一致 bug 的头号来源。

## 写新代码前

1. 搜索：`grep -r "functionName" .` / `grep -r "keyword" .`。
2. 四问：已有相似函数？→ 复用/扩展；此模式他处已用？→ 跟既有；可共享工具？→ 放对位置；正从别处复制？→ 停，抽共享。

## 常见重复

1. 复制粘贴函数 → 抽共享工具导入。
2. 80% 相似组件 → 扩展现有组件（props/变体）。
3. 重复常量 → 单一源，处处导入。
4. 重复 payload 字段抽取 → 把 decoder/type guard/projection 放数据所有者旁。规则：同一无类型 payload 字段读 2+ 处，先建共享 type guard/normalizer/projection 再加第三读者。

## 何时抽象

抽象：同码 3+ 次 / 逻辑复杂易错 / 多人需用。不抽象：仅 1 次 / 一行琐碎 / 抽象比重复更复杂。

## 批量修改后

审查是否抓全 → grep 查漏 → 考虑抽象。Reducer 用穷尽 `switch` 而非散落 `if/else`（action/kind/status/phase 派生态）。

## 提交前清单

- [ ] 搜过相似代码。
- [ ] 无应共享的复制逻辑。
- [ ] 无共享 decoder 外的重复无类型 payload 抽取。
- [ ] 常量单点定义。
- [ ] 相似模式同结构。
- [ ] reducer/action 转换集中一个 reducer 或 dispatcher。

## Gotcha：Python if/elif/else 无编译期穷尽检查

新增 `Literal` 值（如 `Platform`）时，既有 if/elif/else 会静默落 else 用错默认。预防：新增值搜所有 switch 链加显式分支，不靠 else 兜底新值。

## Gotcha：非对称机制产同输出

两种机制须产同文件集（递归 copy vs 手写 `files.set()`）时，结构变更只走自动机制传播，手写路径静默漂移。预防：首选消除非对称（手写路径调自动路径）；不可避免则加两机制输出比对回归；迁移目录结构搜所有引用旧结构的路径。实例：`trellis update` 手写 11 脚本清单与 `getAllScripts()` 漂移，改 for..of 循环。

## 模板文件注册（Trellis 专用）

新增 `src/templates/trellis/scripts/` 文件：单一注册点 `src/templates/trellis/index.ts`——加 `export const xxxScript = readTemplate("scripts/path/file.py")` 并加入 `getAllScripts()` Map；`commands/update.ts` 直接用 `getAllScripts()`。未注册 → `trellis update` 不向用户项目同步，bug/特性不传播。加新 `.py` 后 `grep -l "newFileName" src/templates/trellis/index.ts` 验证。

模板同步约定：`.trellis/scripts/`（dogfood）与 `packages/cli/src/templates/trellis/scripts/`（template）保持一致，编辑后 `rsync -av --delete --exclude='__pycache__' .trellis/scripts/ packages/cli/src/templates/trellis/scripts/`。Gotcha：rsync 源/目标写反会生成嵌套垃圾目录，先核对路径。
