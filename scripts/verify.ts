import path from "node:path";
import { fileURLToPath } from "node:url";
import { makeReceipt, repoRoot } from "./config-lib.js";
import { checks } from "./checks/common.js";
import {
  checkAotToolchainOwnedBySdk,
  checkAppRawHexZero,
  checkCiFastLaneNoPublishDuplication,
  checkDialogServiceNoFuncAction,
  checkHtaRetiredConfig,
  checkLineEndingPolicy,
  checkMainVmLineBudget,
  checkMainlineLayout,
  checkNoActivePowerShellScripts,
  checkNoBareCatchException,
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
  checkTestStructureLayering,
  checkTrinityArchitectureIntegrity,
  checkUpdateManifestGeneration,
  checkViewsNoDirectCoreBusinessCalls,
  checkVtkArchitectureIntegrity,
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
  checkLauncherStore,
  checkShortcutBackupRetirement,
} from "./checks/schema.js";
import {
  checkAnalyzerWiring,
  checkArchitectureRuleCheck,
  checkAvaloniaMainlineVerify,
  checkCoverageGate,
  checkReleaseLockfileConsistency,
  checkStructureBaselineConsistency,
} from "./checks/run.js";
import { checkSnowIgnoredLocalState } from "./checks/static.js";

export async function runVerify(): Promise<unknown> {
  checkLineEndingPolicy();
  await checkSnowIgnoredLocalState();
  checkRetiredPathsRemoved();
  checkNoBareCatchException();
  checkAppRawHexZero();
  checkMainlineLayout();
  checkReadme();
  checkGithubSnapshot();
  const activeIds = checkLauncherStore();
  checkConfigExampleSchema();
  checkShortcutBackupRetirement(activeIds);
  checkConfigRequiredFields();
  checkWinformsRetiredDesign();
  checkHtaRetiredConfig();
  checkPortableInstallerContract();
  checkUpdateManifestGeneration();
  checkTestStructureLayering();
  checkCiFastLaneNoPublishDuplication();
  checkAotToolchainOwnedBySdk();
  await checkReleaseLockfileConsistency();
  await checkAvaloniaMainlineVerify();
  await checkCoverageGate();
  await checkArchitectureRuleCheck();
  await checkAnalyzerWiring();
  checkNoStaleThreeFileRefs();
  checkSettingsSingleSourceBuildStoreFile();
  checkWingetConfigSchemaValid();
  checkNoImperativeViewUi();
  checkDialogServiceNoFuncAction();
  checkViewsNoDirectCoreBusinessCalls();
  checkSecurityAuditF1();
  checkSecurityAuditF2();
  checkSecurityAuditF3();
  checkNoThinWrapperDelegation();
  checkVtkArchitectureIntegrity();
  checkTrinityArchitectureIntegrity();
  checkVtkNoAppstoreResidue();
  checkWiringMapConsistency();
  checkMainVmLineBudget();
  checkWorkflowNoCallbackBag();
  checkNoActivePowerShellScripts();
  checkStructureBaselineConsistency();
  return makeReceipt("root-verify", { root: repoRoot, checks });
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  const receipt = await runVerify();
  console.log(JSON.stringify(receipt));
}
