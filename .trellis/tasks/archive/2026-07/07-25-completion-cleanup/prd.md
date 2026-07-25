# 完成度总览实施：文档更新 + 遗留项清理

## Goal

完成 `docs/completion-overview.md` 中记录的"剩余"、"遗留"和"后续"中立即可以做的文档更新工作。

## Scope

本次只做**文档层面**的更新，不涉及代码重构、CI 配置或架构变更。
中期/长期项（Ponytail 升级评估、Agent 目录、死代码门禁、Gate 扩展、架构验证）留待后续讨论。

## Deliverables

1. **collab-debt-findings.md 更新** — 反映当前真实状态：
   - 严重程度汇总 High 清零（F1/F2/F3 已修复，5.1-B/5.2-A/5.5-B 的 High 等级应移除或降级）
   - 主要行动项全部标记为已完成（第 1-5 项均已修复）
   - 补充 VTuber-Killer 架构落地说明（AppStore 已删除）
   - 补充 .snow/ 跟踪结论
   
2. **architecture-reform-proposal.md 标记过期** — 添加过期声明：
   - 说明该提案的 Fluxor 路线已被 VTuber-Killer (Event Sourcing + Grain) 架构取代
   - 指向当前架构的参考位置
   
3. **verify.ps1 ponytail 注释检查/补充** — 确认 3 个安全审计回归门禁的 ponytail 注释是否完整

4. **completion-overview.md 同步更新** — 反映本次变更后的最新状态

## Acceptance Criteria

- [ ] collab-debt-findings.md 的严重程度汇总准确反映当前状态（High 为 0）
- [ ] collab-debt-findings.md 的主要行动项全部标记完成，无未关闭的待办
- [ ] architecture-reform-proposal.md 顶部有过期标记，指向 VTuber-Killer
- [ ] verify.ps1 中 3 个安全审计 gate（security-audit-f1/f2/f3）都有 ponytail 注释（或确认已有）
- [ ] completion-overview.md 的"遗留/待更新"章节同步为已更新状态
- [ ] gate 检查仍通过（dotnet build -warnaserror）
