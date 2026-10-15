import {
  analyzerWiringCheck,
  avaloniaMainlineVerifyCheck,
  coverageGateCheck,
  releaseLockfileConsistencyCheck,
} from "./checks/run.js";
import {
  configExampleSchemaCheck,
  configRequiredFieldsCheck,
  githubSnapshotFreshnessCheck,
  githubSnapshotShapeCheck,
  launcherStoreCheck,
  localEntryDirectoriesCheck,
  shortcutBackupRetirementCheck,
} from "./checks/schema.js";
import {
  aotToolchainOwnedBySdkCheck,
  appRawHexZeroCheck,
  ciFastLaneNoPublishDuplicationCheck,
  commandTableConsistencyCheck,
  dialogServiceNoFuncActionCheck,
  htaRetiredConfigCheck,
  lineEndingPolicyCheck,
  mainVmLineBudgetCheck,
  mainlineLayoutCheck,
  noActivePowerShellScriptsCheck,
  noBareCatchExceptionCheck,
  noImperativeViewUiCheck,
  noThinWrapperDelegationCheck,
  portableInstallerContractCheck,
  readmeCheck,
  retiredPathsRemovedCheck,
  securityAuditF1Check,
  securityAuditF2Check,
  securityAuditF3Check,
  settingsSingleSourceCheck,
  snowIgnoredLocalStateCheck,
  structureBaselineUpToDateCheck,
  testStructureLayeringCheck,
  trinityArchitectureIntegrityCheck,
  updateManifestGenerationCheck,
  viewsNoDirectCoreBusinessCallsCheck,
  vtkNoAppstoreResidueCheck,
  wingetConfigSchemaCheck,
  winformsRetiredDesignCheck,
  wiringMapConsistencyCheck,
  workflowNoCallbackBagCheck,
} from "./checks/static.js";
import type { Check } from "./kernel/check.js";

/** 门禁注册表:42 条检查的唯一权威源。
 *
 * 此前清单分成两份——gate-steps.ts 的 id 数组与 verify.ts 的实现数组——
 * 再用一条 assertManifestMatchesImplementation 断言"两份还一致"。有断言恰恰说明
 * 有两个真相源。现在注册表本身就是清单:结构基线读它的长度,runner 读它的 id,
 * 没有任何需要同步的第二份。
 *
 * 顺序即执行顺序(static 先跑:最便宜、最可能抓错),依赖与阻断在 kernel/policy.ts。
 * 末尾固定为 structure-baseline-up-to-date,让"派生数据已过期"最后才报——
 * 它描述的是前面所有检查看过的仓库状态,不是某一处代码。 */
export const gateChecks: readonly Check[] = [
  // static:纯文件/文本,无外部依赖
  lineEndingPolicyCheck,
  snowIgnoredLocalStateCheck,
  retiredPathsRemovedCheck,
  noBareCatchExceptionCheck,
  appRawHexZeroCheck,
  mainlineLayoutCheck,
  readmeCheck,
  githubSnapshotShapeCheck,
  launcherStoreCheck,
  configExampleSchemaCheck,
  shortcutBackupRetirementCheck,
  configRequiredFieldsCheck,
  winformsRetiredDesignCheck,
  htaRetiredConfigCheck,
  portableInstallerContractCheck,
  updateManifestGenerationCheck,
  testStructureLayeringCheck,
  ciFastLaneNoPublishDuplicationCheck,
  aotToolchainOwnedBySdkCheck,
  commandTableConsistencyCheck,
  settingsSingleSourceCheck,
  wingetConfigSchemaCheck,
  noImperativeViewUiCheck,
  dialogServiceNoFuncActionCheck,
  viewsNoDirectCoreBusinessCallsCheck,
  securityAuditF1Check,
  securityAuditF2Check,
  securityAuditF3Check,
  noThinWrapperDelegationCheck,
  trinityArchitectureIntegrityCheck,
  vtkNoAppstoreResidueCheck,
  wiringMapConsistencyCheck,
  mainVmLineBudgetCheck,
  workflowNoCallbackBagCheck,
  noActivePowerShellScriptsCheck,
  // local:本机环境事实
  localEntryDirectoriesCheck,
  // snapshot:外部人工数据时效性(仅 advisory)
  githubSnapshotFreshnessCheck,
  // native:需要 .NET 10 SDK
  releaseLockfileConsistencyCheck,
  avaloniaMainlineVerifyCheck,
  coverageGateCheck,
  analyzerWiringCheck,
  // 派生数据核对放最后
  structureBaselineUpToDateCheck,
];

/** 注册表自检:不得有重复 id,且 id 形状统一。
 * 这是唯一还需要"校验"的地方——而它校验的是注册表自身的完整性,不是两份清单的一致性。 */
export function assertRegistryIntegrity(checks: readonly Check[] = gateChecks): void {
  const seen = new Set<string>();
  for (const check of checks) {
    if (seen.has(check.id)) {
      throw new Error(`Gate registry has duplicate check id: ${check.id}`);
    }
    if (!/^[a-z0-9]+(-[a-z0-9]+)*$/.test(check.id)) {
      throw new Error(`Gate registry id must be kebab-case: ${check.id}`);
    }
    seen.add(check.id);
  }
}
