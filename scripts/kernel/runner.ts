import { type Check, createCtx, evaluate } from "./check.js";
import { type Preconditions, TIER_ORDER, TIER_POLICY } from "./policy.js";
import { type Evidence, type TierName, type Verdict } from "./verdict.js";
import type { World } from "./world.js";

/** 一条检查在 receipt 里的记录。保留 {name, detail} 形状,使既有消费者与
 * 证据矩阵无需改口径;但 detail 现在由判词直接投影,不再由检查自行拼字符串。 */
export interface CheckRecord {
  readonly name: string;
  readonly detail: Evidence;
}

export interface GateFailure {
  readonly id: string;
  readonly tier: TierName;
  readonly message: string;
}

export interface GateSkip {
  readonly id: string;
  readonly tier: TierName;
  readonly reason: string;
}

export interface GateOutcome {
  readonly failures: readonly GateFailure[];
  readonly advisory: readonly GateFailure[];
  readonly skipped: readonly GateSkip[];
  readonly checks: readonly CheckRecord[];
}

/** 依赖排序:needs 必须在被依赖项之后。取代此前"数组顺序即依赖"+闭包变量传递。
 * 出现环或引用不存在的 id 时直接抛错——排在运行时,而不是靠一条 order broken 断言兜底。 */
export function sortByDependencies(checks: readonly Check[]): Check[] {
  const byId = new Map(checks.map((check) => [check.id, check]));
  for (const check of checks) {
    for (const need of check.needs ?? []) {
      if (!byId.has(need)) {
        throw new Error(`Gate dependency drift: "${check.id}" needs unknown check "${need}"`);
      }
    }
  }
  const sorted: Check[] = [];
  const state = new Map<string, "visiting" | "done">();
  const visit = (check: Check): void => {
    const status = state.get(check.id);
    if (status === "done") {
      return;
    }
    if (status === "visiting") {
      throw new Error(`Gate dependency cycle detected at "${check.id}"`);
    }
    state.set(check.id, "visiting");
    for (const need of check.needs ?? []) {
      visit(byId.get(need)!);
    }
    state.set(check.id, "done");
    sorted.push(check);
  };
  for (const check of checks) {
    visit(check);
  }
  return sorted;
}

/** 按分层顺序稳定排列(static → local → native → snapshot),层内保持注册顺序。
 * 让"最便宜、最可能抓错的检查先跑"成为结构事实而非手工排列的结果。 */
export function orderByTier(checks: readonly Check[]): Check[] {
  const rank = new Map(TIER_ORDER.map((tier, index) => [tier, index]));
  return [...checks].sort((a, b) => (rank.get(a.tier) ?? 0) - (rank.get(b.tier) ?? 0));
}

function project(check: Check, verdict: Verdict): CheckRecord {
  if (verdict.status === "pass") {
    return { name: check.id, detail: verdict.evidence };
  }
  if (verdict.status === "skip") {
    return { name: check.id, detail: { status: "skipped", reason: verdict.reason } };
  }
  return { name: check.id, detail: { status: "failed", message: verdict.reason } };
}

export async function runGate(
  checks: readonly Check[],
  world: World,
  pre: Preconditions,
): Promise<GateOutcome> {
  const ctx = createCtx(world);
  const ordered = sortByDependencies(orderByTier(checks));

  const failures: GateFailure[] = [];
  const advisory: GateFailure[] = [];
  const skipped: GateSkip[] = [];
  const records: CheckRecord[] = [];
  const verdicts = new Map<string, Verdict>();

  for (const check of ordered) {
    const policy = TIER_POLICY[check.tier];

    // 依赖未通过时显式 skip,而不是拿到半成品输入再抛一句 order broken。
    const blockedBy = (check.needs ?? []).filter((need) => verdicts.get(need)?.status !== "pass");
    let verdict: Verdict;
    if (blockedBy.length > 0) {
      verdict = { status: "skip", reason: `depends on ${blockedBy.join(", ")} which did not pass` };
    } else {
      const tierSkip = policy.skipReason(pre);
      verdict = tierSkip === null ? await evaluate(check, ctx) : { status: "skip", reason: tierSkip };
    }
    verdicts.set(check.id, verdict);
    records.push(project(check, verdict));

    if (verdict.status === "fail") {
      const record: GateFailure = { id: check.id, tier: check.tier, message: verdict.reason };
      // 非阻断层失败只作 advisory:snapshot 描述外部人工数据时效,local 描述本机路径存在性,
      // 二者都不描述代码正确性,不该让一台机器缺一个目录就瘫痪整条验证。
      (policy.blocking ? failures : advisory).push(record);
    } else if (verdict.status === "skip") {
      skipped.push({ id: check.id, tier: check.tier, reason: verdict.reason });
    }
  }

  return { failures, advisory, skipped, checks: records };
}
