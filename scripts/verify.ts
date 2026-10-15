import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { execa } from "execa";
import { makeReceipt, repoRoot, type ToolReceipt } from "./config-lib.js";
import type { Check } from "./kernel/check.js";
import { type Preconditions, TIER_ORDER } from "./kernel/policy.js";
import { type GateFailure, type GateOutcome, type GateSkip, runGate } from "./kernel/runner.js";
import type { TierName } from "./kernel/verdict.js";
import { createRealWorld } from "./kernel/world.js";
import { assertRegistryIntegrity, gateChecks } from "./registry.js";

export type { Check, GateFailure, GateSkip, TierName };
export { TIER_ORDER };

export interface GateCheckRecord {
  readonly name: string;
  readonly detail: Record<string, unknown>;
}

export interface VerifyReceipt extends ToolReceipt {
  readonly failures: readonly GateFailure[];
  readonly advisory: readonly GateFailure[];
  readonly skipped: readonly GateSkip[];
  readonly dotnetSdk: string | null;
  readonly checks: readonly GateCheckRecord[];
  /** 本次实际评估的分层,便于 --tier 快车道自证"只跑了这一层"。 */
  readonly tiersRun: readonly TierName[];
}

/** 判定 `dotnet --version` 的输出是否是本次构建可用的 SDK。global.json 钉的是
 * 10.x,非 10.x 说明解析到了不兼容的 SDK,视同缺失——让 native 层显式跳过,
 * 而不是拿一个不兼容的 SDK 去构建、再报成代码失败。 */
export function isSupportedDotnetSdk(versionOutput: string): boolean {
  return versionOutput.trim().startsWith("10.");
}

/** 探测本机实际会用于构建的 .NET SDK 版本。`dotnet --version` 尊重 global.json
 * (含 rollForward),返回的正是 MSBuild 会用的那个 SDK;此前用 `--list-sdks` 取
 * 第一个 10.x,在装有多个 SDK 的镜像上会报出没参与构建的版本。 */
async function detectActiveDotnetSdk(): Promise<string | null> {
  const result = await execa("dotnet", ["--version"], { cwd: repoRoot, reject: false });
  if (result.exitCode !== 0) {
    return null;
  }
  return isSupportedDotnetSdk(result.stdout) ? result.stdout.trim() : null;
}

/** 人读摘要。凭证本身是给机器解析的 JSON,而 CI 日志和终端需要一眼看清
 * "哪些失败、哪些只是 advisory、哪些被跳过、为什么跳过"。 */
export function formatGateSummary(
  failures: readonly GateFailure[],
  advisory: readonly GateFailure[],
  skipped: readonly GateSkip[],
  dotnetSdk: string | null,
  nativeInScope = true,
): string {
  const lines: string[] = [];
  lines.push(
    `WT Protector gate: ${failures.length === 0 ? "PASS" : "FAIL"} (blocking failures: ${failures.length})`,
  );
  if (failures.length > 0) {
    lines.push("Blocking failures:");
    for (const item of failures) {
      lines.push(`  [${item.tier}] ${item.id}: ${item.message}`);
    }
  }
  if (advisory.length > 0) {
    lines.push("Advisory (reported, does not block):");
    for (const item of advisory) {
      lines.push(`  [${item.tier}] ${item.id}: ${item.message}`);
    }
  }
  if (skipped.length > 0) {
    lines.push("Skipped:");
    for (const item of skipped) {
      lines.push(`  [${item.tier}] ${item.id}: ${item.reason}`);
    }
  }
  // --tier 收窄了范围时,native 层根本没被要求运行,并不等于本机缺 SDK。
  // 不加这句区分,"none installed" 会被误读成环境缺陷。
  lines.push(
    !nativeInScope
      ? "dotnet SDK: not probed (native tier not in scope for this run)"
      : dotnetSdk === null
        ? "dotnet SDK: none installed (native tier skipped)"
        : `dotnet SDK: ${dotnetSdk}`,
  );
  return lines.join("\n");
}

/** 选择本次要评估的检查。--tier 让"最便宜、最常跑"的那层可被单独调用:
 * 36 条 static 检查实测 261ms,而全量门禁含四次 .NET 调用约 4 分钟。
 * 没有这个开关时,想只跑静态检查只能自己造脚本——这正是本重构要消除的摩擦。 */
export function selectChecks(checks: readonly Check[], tiers: readonly TierName[]): Check[] {
  if (tiers.length === 0) {
    return [...checks];
  }
  const wanted = new Set(tiers);
  return checks.filter((check) => wanted.has(check.tier));
}

export interface RunVerifyOptions {
  readonly tiers?: readonly TierName[];
}

export async function runVerify(options: RunVerifyOptions = {}): Promise<VerifyReceipt> {
  assertRegistryIntegrity();
  const tiers = options.tiers ?? [];
  const selected = selectChecks(gateChecks, tiers);

  const world = createRealWorld(repoRoot);
  const isCi = process.env.CI === "true";
  const needsNative = selected.some((check) => check.tier === "native");
  // 只在真的要跑 native 时才探测 SDK:static 快车道不该付 `dotnet --version` 的进程代价。
  const dotnetSdk = needsNative ? await detectActiveDotnetSdk() : null;
  const pre: Preconditions = { isCi, dotnetSdk };

  const outcome: GateOutcome = await runGate(selected, world, pre);

  const status = outcome.failures.length === 0 ? "PASS" : "FAIL";
  const receipt = await makeReceipt(
    "root-verify",
    {
      root: repoRoot,
      failures: outcome.failures,
      advisory: outcome.advisory,
      skipped: outcome.skipped,
      dotnetSdk,
      checks: outcome.checks,
      tiersRun: [...new Set(selected.map((check) => check.tier))],
    },
    status,
  );
  return receipt as VerifyReceipt;
}

function parseTiers(raw: string | undefined): TierName[] {
  if (raw === undefined || raw.trim() === "") {
    return [];
  }
  const parts = raw
    .split(",")
    .map((part) => part.trim())
    .filter((part) => part.length > 0);
  for (const part of parts) {
    if (!TIER_ORDER.includes(part as TierName)) {
      throw new Error(`Unknown tier "${part}"; expected one of ${TIER_ORDER.join(", ")}`);
    }
  }
  return parts as TierName[];
}

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: { tier: { type: "string", default: "" } },
    allowPositionals: false,
  });
  const receipt = await runVerify({ tiers: parseTiers(values.tier) });
  console.error(
    formatGateSummary(
      receipt.failures,
      receipt.advisory,
      receipt.skipped,
      receipt.dotnetSdk,
      receipt.tiersRun.includes("native"),
    ),
  );
  // The receipt is the machine-readable payload on stdout; the human summary above
  // goes to stderr. Native-tier steps run child processes with stdio "inherit", so
  // their output lands on this same stdout before the receipt: the BEGIN/END markers
  // are what lets a consumer locate the JSON unambiguously.
  console.log("=== VERIFY RECEIPT JSON BEGIN ===");
  console.log(JSON.stringify(receipt, null, 2));
  console.log("=== VERIFY RECEIPT JSON END ===");
  if (receipt.status === "FAIL") {
    process.exitCode = 1;
  }
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
