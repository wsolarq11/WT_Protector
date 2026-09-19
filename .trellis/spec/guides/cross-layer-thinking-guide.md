# Cross-Layer Thinking Guide

> 目的：实现前想透 5 层数据流。多数 bug 发生在层边界，不在层内。

## 实现前

1. 映射数据流 `Source → Transform → Store → Retrieve → Transform → Display`；每箭头问：格式？会错在哪？谁负责校验？
2. 识别边界：API↔Service（类型不匹配/缺字段）、Service↔DB（格式转换/null）、Backend↔Frontend（序列化/日期格式）、组件↔组件（props 形状变化）。
3. 定义契约：每边界的精确输入/输出格式 + 可能错误。

## 常见跨层错误

1. 隐式格式假设 → 边界显式转换。
2. 校验散落多层 → 入口校验一次。
3. 泄漏抽象 → 每层只知邻居。
4. 每消费者解析同一 payload → 事件边界 decode 一次再导出类型化投影。规则：append-only log / JSON stream / RPC payload / config 文件，建一个所有者拥有：payload 类型定义、`unknown` 类型 guard + normalize、UI 命令用的元数据投影、从事实源重放状态的 reducer。渲染可格式化，但不得重定义 payload 契约。

## 跨层特性清单

实现前：映射完整数据流 / 识别全部边界 / 定义每边界格式 / 决定校验点。实现后：edge case（null/empty/invalid）测过 / 每边界错误处理验证 / 数据 round-trip / 消费者导入共享 decoder 非本地 cast / 派生状态回指源事件标识（`seq` / `id` / `version`）不另造游标。

## 跨平台模板一致性

修改命令模板后：`find src/templates/*/commands/trellis/ -name "<command>.*"` 找全部平台 / 更新所有副本（.md + .toml）/ Gemini TOML 适配续行（`\\` vs `\`）与三引号字符串 / 跑 `/trellis:check-cross-layer`。实例：改 `record-session.md` 漏 iFlow/Kilo/OpenCode/Gemini。

## 生成式运行时模板升级一致性

部分生成文件既是文档又是运行时输入（`.trellis/workflow.md` 被 `get_context.py` / `workflow_phase.py` / SessionStart / per-turn hook 解析）。清单：识别每个读模板的运行时解析器（不只安装它的 writer）/ 检查相关语法是否在托管标记区外 / 验证 fresh init 与写旧 `.trellis/.version` 的 upgrade 场景 / 加旧模板 fixture 升级回归断言装出当前形状 / 更新拥有运行时契约的 backend spec。实例：Codex inline mode 改平台标记，`trellis update` 只合并 `[workflow-state:*]` 块，保留区外 stale 标记 → 升级项目旧路由。

## 版本化文档边界

源路径、`docs.json` 版本路由、渲染版本选择器必须描述同一 release line。清单：识别目标线（stable/beta/RC）/ 校验 MDX 路径（stable=`docs-site/{start,advanced,...}` + `zh/`；beta=`docs-site/beta/**` + `zh/beta/**`；RC=`docs-site/rc/**` + `zh/rc/**`）/ 校验 `docs.json` 导航指向同路径 / 提交前 grep 对侧树找 release-line 特定词 / beta 内容出现在 root 路径视为 source-path bug。

## Mode-Detection 探测清单

CLI 探测远程资源自动判定 mode 时。实现前：探测跑在所有用结果的路径（interactive / `-y` / `--flag` 组合）/ 区分 404 vs 瞬态错误 / 瞬态错误 abort 或 retry 不静默换 mode / 上下文变化（切 source）时重置共享状态（cache/prefetch）/ 快捷路径（`--template` 跳过 picker）与探测路径同等错误处理质量。实现后：追踪探测结果→mode 分支每条路径无 fallthrough / 外部格式契约（giget URI/raw URL）测试或注释 / 元数据读完整响应或流式解析（禁固定前缀当完整 JSON）/ 重组复合标识符校验全字段 + 正确位置（`provider:repo/path#ref` 非 `provider:repo#ref/path`）/ 快捷后调用的 action 函数不用旧 catch-all fetch。实例：自定义 registry 8 bug（探测只 interactive、瞬态落错 mode、giget `#ref` 位置错、prefetch 泄漏、`--template` 绕过探测）；agent-session hint 用 `read(4096)` 截断 JSON 解析失败。

## 事件日志 / 投影边界

append-only log 是跨层契约：`CLI → writer → events.jsonl → reader → filter → reducer → display`。新增事件 kind/字段清单：加入中央事件分类 / 事件层加类型化变体或 type guard / 用户输入/JSON 的数组对象字段加 normalize helper / `seq` / `id` 只在 writer 分配 / filter 与 reducer 消费类型化 guard 非本地 cast / display 消费 reducer 输出或类型化事件非 raw JSON / 至少一条回归证明 history replay 与 live filter 同 filter 模型。

## FindUpward 幽灵配置陷阱

`FindUpward` 依赖 `README.md` 作仓库根标记；子目录也有 `README.md` 且下有幽灵 `config/StoreFileName` 时提前停搜。排查（视图为空）：1) 检查 `config/StoreFileName` 存在且条目正确；2) 检查旧 WTLauncher.App 进程锁构建输出；3) 检查上层目录同名配置文件被 `FindUpward` 优先发现。修复：删上层幽灵文件 + 重启。
