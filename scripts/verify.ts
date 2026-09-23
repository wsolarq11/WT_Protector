import path from "node:path";
import { fileURLToPath } from "node:url";
import { execa } from "execa";
import { makeReceipt, repoRoot, type ToolReceipt } from "./config-lib.js";
import {
  addCheck,
  addFailedCheck,
  addSkippedCheck,
  checks,
  resetChecks,
  type GateCheck,
} from "./checks/common.js";
import { gateStepIds } from "./gate-steps.js";
import {
  checkAotToolchainOwnedBySdk,
  checkAppRawHexZero,
  checkCiFastLaneNoPublishDuplication,
  checkCommandTableConsistency,
  checkDialogServiceNoFuncAction,
  checkHtaRetiredConfig,
  checkLineEndingPolicy,
  checkMainVmLineBudget,
  checkMainlineLayout,
  checkNoActivePowerShellScripts,
  checkNoBareCatchException,
  checkNoHardcodedGateCounts,
  checkNoImperativeViewUi,
  checkNoStaleThreeFileRefs,
  checkNoThinWrapperDelegation,
  checkPortableInstallerContract,
  checkReadme,
  checkRetiredPathsRemoved,
  checkSecurityAuditF1,
  checkSecurityAuditF2,
  checkSecurityAuditF3,
  checkSettingsSingleSourceBuildStoreFile,
  checkSnowIgnoredLocalState,
  checkStructureBaselineUpToDate,
  checkTaskLedgerCitationsResolve,
  checkTestStructureLayering,
  checkTrinityArchitectureIntegrity,
  checkUpdateManifestGeneration,
  checkViewsNoDirectCoreBusinessCalls,
  checkVtkNoAppstoreResidue,
  checkWingetConfigSchemaValid,
  checkWinformsRetiredDesign,
  checkWiringMapConsistency,
  checkWorkflowNoCallbackBag,
} from "./checks/static.js";
import {
  checkConfigExampleSchema,
  checkConfigRequiredFields,
  checkGithubSnapshot,
  checkGithubSnapshotFreshness,
  checkLauncherStore,
  checkLocalEntryDirectories,
  checkShortcutBackupRetirement,
} from "./checks/schema.js";
import {
  checkAnalyzerWiring,
  checkArchitectureRuleCheck,
  checkAvaloniaMainlineVerify,
  checkCoverageGate,
  checkReleaseLockfileConsistency,
} from "./checks/run.js";

/** 门禁分层。分层决定失败是否阻断,以及缺少依赖时如何留痕。 */
export type GateTier =
  | "static" // 纯文件/文本检查:无外部依赖,失败即阻断
  | "local" // 本机环境事实:CI 用 config/launcher.ci.json 覆盖,故 CI 上显式跳过
  | "native" // 需要 .NET 10 SDK:缺 SDK 的机器显式跳过,不伪装成通过
  | "snapshot"; // 外部人工数据时效性:仅 advisory,不阻断代码验证

export interface GateStep {
  id: string;
  tier: GateTier;
  run: () => void | Promise<void>;
}

export interface GateFailure {
  id: string;
  tier: GateTier;
  message: string;
}

export interface GateSkip {
  id: string;
  tier: GateTier;
  reason: string;
}

/** 门禁凭证的完整形状。字段与 runVerify 实际写入的内容一一对应。 */
export interface VerifyReceipt extends ToolReceipt {
  failures: GateFailure[];
  advisory: GateFailure[];
  skipped: GateSkip[];
  dotnetSdk: string | null;
  checks: GateCheck[];
}

/** 人读摘要。凭证本身是给机器解析的数百行 JSON,而 CI 日志和终端需要
 * 一眼看清"哪些失败、哪些只是 advisory、哪些被跳过、为什么跳过"。 */
export function formatGateSummary(
  failures: GateFailure[],
  advisory: GateFailure[],
  skipped: GateSkip[],
  dotnetSdk: string | null,
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
  lines.push(
    dotnetSdk === null
      ? "dotnet SDK: none installed (native tier skipped)"
      : `dotnet SDK: ${dotnetSdk}`,
  );
  return lines.join("\n");
}

/** 探测本机是否具备 .NET 10 SDK。门禁的 native 层需要它,而 global.json 钉的
 * 就是 10.x,因此这里只认 10.x 前缀,避免用旧 SDK 产生误导性的 NETSDK1045。 */
async function detectDotnet10Sdk(): Promise<string | null> {
  const result = await execa("dotnet", ["--list-sdks"], { reject: false });
  if (result.exitCode !== 0) {
    return null;
  }
  const versions = result.stdout
    .split(/\r?\n/)
    .map((line) => line.trim().split(/\s+/)[0] ?? "")
    .filter((version) => version.length > 0);
  return versions.find((version) => version.startsWith("10.")) ?? null;
}

/** launcher-store 的校验结果要喂给 shortcut-backup-retirement,而清单里的步骤
 * 只返回 void。用显式闭包变量传递,并在消费处断言依赖步骤已运行。 */
let launcherActiveIds: Set<string> | undefined;

export const gateSteps: GateStep[] = [
  { id: "line-ending-policy", tier: "static", run: () => checkLineEndingPolicy() },
  { id: "snow-ignored-local-state", tier: "static", run: () => checkSnowIgnoredLocalState() },
  { id: "retired-paths-removed", tier: "static", run: () => checkRetiredPathsRemoved() },
  { id: "no-bare-catch-exception", tier: "static", run: () => checkNoBareCatchException() },
  { id: "app-raw-hex-zero", tier: "static", run: () => checkAppRawHexZero() },
  { id: "mainline-layout", tier: "static", run: () => checkMainlineLayout() },
  { id: "readme", tier: "static", run: () => checkReadme() },
  { id: "github-snapshot-shape", tier: "static", run: () => checkGithubSnapshot() },
  { id: "github-snapshot-freshness", tier: "snapshot", run: () => checkGithubSnapshotFreshness() },
  {
    id: "launcher-store",
    tier: "static",
    run: () => {
      launcherActiveIds = checkLauncherStore();
    },
  },
  { id: "local-entry-directories", tier: "local", run: () => checkLocalEntryDirectories() },
  { id: "config-example-schema", tier: "static", run: () => checkConfigExampleSchema() },
  {
    id: "shortcut-backup-retirement",
    tier: "static",
    run: () => {
      if (launcherActiveIds === undefined) {
        throw new Error("gate step order broken: launcher-store must run before shortcut-backup-retirement");
      }
      checkShortcutBackupRetirement(launcherActiveIds);
    },
  },
  { id: "config-required-fields", tier: "static", run: () => checkConfigRequiredFields() },
  { id: "winforms-retired-design", tier: "static", run: () => checkWinformsRetiredDesign() },
  { id: "hta-retired-config", tier: "static", run: () => checkHtaRetiredConfig() },
  { id: "portable-installer-contract", tier: "static", run: () => checkPortableInstallerContract() },
  { id: "update-manifest-generation", tier: "static", run: () => checkUpdateManifestGeneration() },
  { id: "test-structure-layering", tier: "static", run: () => checkTestStructureLayering() },
  {
    id: "ci-fast-lane-no-publish-duplication",
    tier: "static",
    run: () => checkCiFastLaneNoPublishDuplication(),
  },
  { id: "aot-toolchain-owned-by-sdk", tier: "static", run: () => checkAotToolchainOwnedBySdk() },
  { id: "release-lockfile-consistency", tier: "native", run: () => checkReleaseLockfileConsistency() },
  { id: "avalonia-mainline-verify", tier: "native", run: () => checkAvaloniaMainlineVerify() },
  { id: "coverage-gate", tier: "native", run: () => checkCoverageGate() },
  { id: "architecture-rule-check", tier: "native", run: () => checkArchitectureRuleCheck() },
  { id: "analyzer-wiring", tier: "native", run: () => checkAnalyzerWiring() },
  { id: "no-stale-three-file-refs", tier: "static", run: () => checkNoStaleThreeFileRefs() },
  { id: "task-ledger-citations-resolve", tier: "static", run: () => checkTaskLedgerCitationsResolve() },
  { id: "no-hardcoded-gate-counts", tier: "static", run: () => checkNoHardcodedGateCounts() },
  { id: "command-table-consistency", tier: "static", run: () => checkCommandTableConsistency() },
  {
    id: "settings-single-source",
    tier: "static",
    run: () => checkSettingsSingleSourceBuildStoreFile(),
  },
  { id: "winget-config-schema", tier: "static", run: () => checkWingetConfigSchemaValid() },
  { id: "no-imperative-view-ui", tier: "static", run: () => checkNoImperativeViewUi() },
  { id: "dialog-service-no-func-action", tier: "static", run: () => checkDialogServiceNoFuncAction() },
  {
    id: "views-no-direct-core-business-calls",
    tier: "static",
    run: () => checkViewsNoDirectCoreBusinessCalls(),
  },
  { id: "security-audit-f1", tier: "static", run: () => checkSecurityAuditF1() },
  { id: "security-audit-f2", tier: "static", run: () => checkSecurityAuditF2() },
  { id: "security-audit-f3", tier: "static", run: () => checkSecurityAuditF3() },
  { id: "no-thin-wrapper-delegation", tier: "static", run: () => checkNoThinWrapperDelegation() },
  {
    id: "trinity-architecture-integrity",
    tier: "static",
    run: () => checkTrinityArchitectureIntegrity(),
  },
  { id: "vtk-no-appstore-residue", tier: "static", run: () => checkVtkNoAppstoreResidue() },
  { id: "wiring-map-consistency", tier: "static", run: () => checkWiringMapConsistency() },
  { id: "main-vm-line-budget", tier: "static", run: () => checkMainVmLineBudget() },
  { id: "workflow-no-callback-bag", tier: "static", run: () => checkWorkflowNoCallbackBag() },
  { id: "no-active-powershell-scripts", tier: "static", run: () => checkNoActivePowerShellScripts() },
  { id: "structure-baseline-up-to-date", tier: "static", run: () => checkStructureBaselineUpToDate() },
];

/** 清单自检:实现清单与 gate-steps.ts 的 id 序列必须逐一对应。
 * 这条断言让"加了检查却忘了登记"和"登记了却忘了实现"都在门禁里暴露。 */
function assertManifestMatchesImplementation(): void {
  const implemented = gateSteps.map((step) => step.id);
  if (implemented.length !== gateStepIds.length) {
    throw new Error(
      `Gate manifest drift: gate-steps.ts declares ${gateStepIds.length} steps, verify.ts implements ${implemented.length}`,
    );
  }
  for (let index = 0; index < implemented.length; index += 1) {
    if (implemented[index] !== gateStepIds[index]) {
      throw new Error(
        `Gate manifest drift at position ${index}: gate-steps.ts has "${gateStepIds[index]}", verify.ts has "${implemented[index]}"`,
      );
    }
  }
}

/** 每个步骤都必须留下一条记录。执行器保证 pass/fail/skip 三种路径都会记账,
 * 因此记录数少于步骤数就说明有步骤被跳过却没留痕。 */
function assertEveryStepAccounted(): void {
  if (checks.length < gateSteps.length) {
    throw new Error(
      `Gate accounting drift: ${gateSteps.length} steps ran but only ${checks.length} results were recorded`,
    );
  }
  // 一步一记的步骤,其记录名必须等于步骤 id,receipt 才能按 id 直接反查。
  // launcher-store 是唯一豁免:它有意一步记 4 条 schema(分别留痕),本身没有同名记录。
  const multiRecordSteps = new Set(["launcher-store"]);
  const checkNames = new Set(checks.map((check) => check.name));
  const nameMismatches = gateSteps
    .map((step) => step.id)
    .filter((id) => !multiRecordSteps.has(id) && !checkNames.has(id));
  if (nameMismatches.length > 0) {
    throw new Error(`Gate accounting name drift: steps without a same-named check record: ${nameMismatches.join(", ")}`);
  }
}

export async function runVerify(): Promise<VerifyReceipt> {
  resetChecks();
  assertManifestMatchesImplementation();

  const dotnetSdk = await detectDotnet10Sdk();
  const isCi = process.env.CI === "true";

  const failures: GateFailure[] = [];
  const advisory: GateFailure[] = [];
  const skipped: GateSkip[] = [];

  for (const step of gateSteps) {
    let skipReason: string | null = null;
    if (step.tier === "native" && dotnetSdk === null) {
      skipReason = "requires .NET 10 SDK; none installed on this machine";
    } else if (step.tier === "local" && isCi) {
      skipReason = "CI overlays config/launcher.ci.json, so entry directories are not applicable";
    }

    if (skipReason !== null) {
      skipped.push({ id: step.id, tier: step.tier, reason: skipReason });
      addSkippedCheck(step.id, skipReason);
      continue;
    }

    try {
      await step.run();
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      const record: GateFailure = { id: step.id, tier: step.tier, message };
      if (step.tier === "snapshot" || step.tier === "local") {
        // snapshot 是外部人工数据时效;local 是本机路径存在性。二者都不描述代码正确性,
        // 只作 advisory 报告,避免一台机器缺一个目录就瘫痪整条验证。
        advisory.push(record);
      } else {
        failures.push(record);
      }
      addFailedCheck(step.id, error);
    }
  }

  // 清单与记账自检放在所有步骤之后:它们描述的是本次执行的整体事实。
  try {
    assertEveryStepAccounted();
    addCheck("gate-accounting", {
      steps: gateSteps.length,
      recorded: checks.length,
      failures: failures.length,
      advisory: advisory.length,
      skipped: skipped.length,
    });
  } catch (error) {
    failures.push({
      id: "gate-accounting",
      tier: "static",
      message: error instanceof Error ? error.message : String(error),
    });
  }

  const status = failures.length === 0 ? "PASS" : "FAIL";
  const receipt = await makeReceipt(
    "root-verify",
    {
      root: repoRoot,
      failures,
      advisory,
      skipped,
      dotnetSdk,
      checks,
    },
    status,
  );
  return receipt as VerifyReceipt;
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  const receipt = await runVerify();
  console.error(
    formatGateSummary(receipt.failures, receipt.advisory, receipt.skipped, receipt.dotnetSdk),
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
