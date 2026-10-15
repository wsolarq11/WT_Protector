import type { TierName } from "./verdict.js";

/** 门禁运行的前置事实。一次性探测,供所有分层共享——避免每条 native 检查各自
 * 再 shell 一次 `dotnet --version`。 */
export interface Preconditions {
  readonly isCi: boolean;
  /** 本次构建实际会用的 SDK 版本(null = 不可用)。 */
  readonly dotnetSdk: string | null;
}

export interface TierPolicy {
  /** 该层失败是否阻断。 */
  readonly blocking: boolean;
  /** 前置不满足时,该层如何留痕;返回 null 表示可运行。
   * 理由文本是结构化政策的一部分,不再散落在执行循环的 if 分支里。 */
  readonly skipReason: (pre: Preconditions) => string | null;
}

/** 分层语义的唯一权威源。此前这些规则硬编码在 verify.ts 的执行循环里
 * (native 缺 SDK 跳过、local 在 CI 跳过、snapshot/local 只作 advisory),
 * 加一层或改一条理由都要去改循环;现在集中在一张表上。 */
export const TIER_POLICY: Record<TierName, TierPolicy> = {
  static: { blocking: true, skipReason: () => null },
  local: {
    blocking: false,
    skipReason: (pre) => (pre.isCi ? "CI overlays config/launcher.ci.json, so entry directories are not applicable" : null),
  },
  native: {
    blocking: true,
    skipReason: (pre) => (pre.dotnetSdk === null ? "requires .NET 10 SDK; none installed on this machine" : null),
  },
  snapshot: { blocking: false, skipReason: () => null },
};

export const TIER_ORDER: readonly TierName[] = ["static", "local", "native", "snapshot"];
