import { existsSync } from "node:fs";
import path from "node:path";
import { avaloniaRoot, repoRoot, solutionPath, testsProjectPath, appProjectPath } from "../config-lib.js";
import {
  addCheck,
  containsRequiredMarkers,
  countMatches,
  fileExists,
  git,
  readConfigItems,
  readText,
  repoPath,
  shortHashFile,
  walkFiles,
} from "./common.js";

function relDisplay(filePath: string): string {
  return filePath.replaceAll(repoRoot, "").replace(/^[\\/]+/, "");
}

function forEachLine(filePath: string, visitor: (line: string, lineNumber: number) => void): void {
  const lines = readText(filePath).split(/\r?\n/);
  lines.forEach((line, index) => visitor(line, index + 1));
}

export function checkLineEndingPolicy(): void {
  const text = readText(".gitattributes");
  containsRequiredMarkers(text, ["* text=auto eol=lf"], ".gitattributes");
  addCheck("line-ending-policy", { path: ".gitattributes", text: "lf" });
}

export async function checkSnowIgnoredLocalState(): Promise<void> {
  const gitignore = readText(".gitignore");
  if (!new RegExp("^\\.snow/$", "m").test(gitignore)) {
    throw new Error(".gitignore must ignore .snow/ local state");
  }
  const tracked = await git(["ls-files", "--", ".snow"]);
  if (tracked.length > 0) {
    throw new Error(`.snow/ must remain ignored local state, but tracked files remain: ${tracked.join(", ")}`);
  }
  addCheck("snow-ignored-local-state", { path: ".snow/", trackedFiles: tracked.length, ignored: true });
}

export function checkRetiredPathsRemoved(): void {
  const forbidden = ["spikes", "src/winforms-launcher", "WT Launcher.exe", "WT Launcher"];
  for (const item of forbidden) {
    if (existsSync(repoPath(item))) {
      throw new Error(`Retired path must not remain: ${repoPath(item)}`);
    }
  }
  addCheck("retired-paths-removed", {
    paths: ["spikes", "src\\winforms-launcher", "WT Launcher.exe", "WT Launcher"],
  });
}

export function checkNoBareCatchException(): void {
  const srcRoot = path.join(avaloniaRoot, "src");
  const violations: string[] = [];
  for (const filePath of walkFiles(srcRoot)) {
    const normalized = filePath.replaceAll("\\", "/");
    if (
      !filePath.endsWith(".cs") ||
      normalized.includes("/obj/") ||
      normalized.includes("/bin/") ||
      normalized.includes("/WTLauncher.Analyzers/")
    ) {
      continue;
    }
    forEachLine(filePath, (line, lineNumber) => {
      if (/catch\s*\(\s*Exception\b/.test(line) && !/\bwhen\b/.test(line)) {
        violations.push(`${relDisplay(filePath)}:${lineNumber}`);
      }
    });
  }
  if (violations.length > 0) {
    throw new Error(`Bare catch(Exception) found (use catch (Exception ex) when (...)): ${violations.join("; ")}`);
  }
  addCheck("no-bare-catch-exception", {
    path: "src/avalonia-launcher/src",
    violations: 0,
    invariant: ".trellis/spec/guides/index.md §5",
  });
}

export function checkAppRawHexZero(): void {
  const rawHexPattern = /(?<![0-9A-Fa-f])#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})(?![0-9A-Fa-f])/;
  const appRoot = path.join(avaloniaRoot, "src", "WTLauncher.App");
  const violations: string[] = [];
  for (const filePath of walkFiles(appRoot)) {
    const normalized = filePath.replaceAll("\\", "/");
    if (normalized.includes("/obj/") || normalized.includes("/bin/")) {
      continue;
    }
    if (!filePath.endsWith(".cs") && !filePath.endsWith(".axaml")) {
      continue;
    }
    forEachLine(filePath, (line, lineNumber) => {
      if (rawHexPattern.test(line)) {
        violations.push(`${relDisplay(filePath)}:${lineNumber}`);
      }
    });
  }
  const fixtureRejected = rawHexPattern.test('<Border Background="#123456"/>');
  if (violations.length > 0) {
    throw new Error(`Raw hex color found in WTLauncher.App (use LauncherDesignTokens/resource): ${violations.join("; ")}`);
  }
  if (!fixtureRejected) {
    throw new Error("Raw-hex gate self-check failed to reject fixture");
  }
  addCheck("app-raw-hex-zero", {
    path: "src/avalonia-launcher/src/WTLauncher.App",
    violations: 0,
    fixtureRejected: true,
  });
}

export function checkMainlineLayout(): void {
  for (const [name, filePath] of [
    ["sourceRoot", avaloniaRoot],
    ["solution", solutionPath],
    ["app", appProjectPath],
    ["tests", testsProjectPath],
  ] as const) {
    if (!existsSync(filePath)) {
      throw new Error(`Missing Avalonia mainline surface ${name}: ${filePath}`);
    }
  }
  addCheck("mainline-layout", {
    sourceRoot: "src\\avalonia-launcher",
    solution: "src\\avalonia-launcher\\WTLauncher.Avalonia.sln",
    app: "src\\avalonia-launcher\\src\\WTLauncher.App\\WTLauncher.App.csproj",
    tests: "src\\avalonia-launcher\\tests\\WTLauncher.Tests\\WTLauncher.Tests.csproj",
  });
}

export function checkReadme(): void {
  const text = readText("README.md");
  containsRequiredMarkers(
    text,
    ["Configuration schema", "Build and verification", "Run the mainline app", "WinForms retirement", "Safety notes", "Current implementation status"],
    "README.md",
  );
  containsRequiredMarkers(
    text,
    [
      "all 22 active feature mappings as narrow-domain north-star",
      "portable Windows Terminal workspace-launcher scope only",
      "full absolute objective remains not complete as written",
      "unimprovable-product, stronger-than-every-GitHub-similar, and absolute zero-side-effect claims",
    ],
    "README.md current objective boundary",
  );
  addCheck("readme", { sections: 6, fullObjectiveBoundary: "not-complete-as-written" });
}

export function checkFeatureObjectiveBoundary(): void {
  const text = readText("docs/feature-reality-north-star-audit.md");
  containsRequiredMarkers(
    text,
    [
      "Full objective result: not-complete-as-written",
      "Feature north-star coverage: current-active-features=22,narrow-north-star=22,non-north-star=0",
      "Blocked absolute requirements: unimprovable-product,stronger-than-every-github-similar,absolute-confidence-zero-side-effects",
      "The full objective remains not complete as written.",
      "All 22 current feature mappings classify as narrow-domain north-star",
      "It is not a broad-domain north star for every launcher or automation product",
    ],
    "Feature reality/north-star audit objective boundary",
  );
  const featureHeadingCount = countMatches(text, new RegExp("^### \\d+\\. ", "gm"));
  const narrowNorthStarVerdictCount = countMatches(text, new RegExp("^- Verdict level: narrow-north-star$", "gm"));
  if (featureHeadingCount !== 22) {
    throw new Error(`Feature reality/north-star audit must cover 22 active feature mappings; found ${featureHeadingCount}`);
  }
  if (narrowNorthStarVerdictCount !== featureHeadingCount) {
    throw new Error(
      `Feature reality/north-star audit has non-north-star feature verdicts: narrow=${narrowNorthStarVerdictCount} total=${featureHeadingCount}`,
    );
  }
  addCheck("feature-objective-boundary", {
    path: "docs\\feature-reality-north-star-audit.md",
    activeFeatureMappings: featureHeadingCount,
    narrowNorthStarVerdicts: narrowNorthStarVerdictCount,
    fullObjectiveResult: "not-complete-as-written",
    blockedAbsoluteRequirements: [
      "unimprovable-product",
      "stronger-than-every-github-similar",
      "absolute-confidence-zero-side-effects",
    ],
    finalAnswer: "not-complete-as-written",
  });
}

export function checkClaimBoundaryLedger(): void {
  const text = readText("docs/feature-reality-north-star-audit.md");
  const ledgerStart = text.indexOf("## Claim boundary ledger");
  const finalAnswerStart = text.indexOf("## Final answer");
  if (ledgerStart < 0) {
    throw new Error("Feature reality/north-star audit missing claim boundary ledger");
  }
  if (finalAnswerStart <= ledgerStart) {
    throw new Error("Feature reality/north-star audit claim boundary ledger must appear before final answer");
  }
  const ledger = text.slice(ledgerStart, finalAnswerStart);
  const expected: Record<string, string> = {
    "narrow-domain-north-star": "allowed",
    "narrow-slice-stronger": "allowed",
    "small-beautiful-preserved": "allowed",
    "pre-handoff-zero-side-effect-boundary": "allowed",
    "broad-launcher-platform-north-star": "forbidden",
    "unimprovable-product": "forbidden",
    "stronger-than-every-github-similar": "forbidden",
    "absolute-confidence-zero-side-effects": "forbidden",
    "post-wt-terminal-health": "forbidden",
  };
  for (const [claimId, status] of Object.entries(expected)) {
    const escaped = claimId.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const pattern = new RegExp(`^- Claim id: ${escaped}\\s*\\r?\\n\\s+- Claim status: ${status}\\b`, "ms");
    if (!pattern.test(ledger)) {
      throw new Error(`Feature reality/north-star audit claim boundary mismatch: ${claimId} must be ${status}`);
    }
  }
  const allowedClaimCount = countMatches(ledger, new RegExp("^\\s+- Claim status: allowed$", "gm"));
  const forbiddenClaimCount = countMatches(ledger, new RegExp("^\\s+- Claim status: forbidden$", "gm"));
  if (allowedClaimCount !== 4) {
    throw new Error(`Feature reality/north-star audit allowed claim count must be 4; found ${allowedClaimCount}`);
  }
  if (forbiddenClaimCount !== 5) {
    throw new Error(`Feature reality/north-star audit forbidden claim count must be 5; found ${forbiddenClaimCount}`);
  }
  addCheck("claim-boundary-ledger", {
    path: "docs\\feature-reality-north-star-audit.md",
    allowedClaims: allowedClaimCount,
    forbiddenClaims: forbiddenClaimCount,
    forbiddenAbsoluteClaims: [
      "unimprovable-product",
      "stronger-than-every-github-similar",
      "absolute-confidence-zero-side-effects",
      "post-wt-terminal-health",
    ],
  });
}

export function checkNoStaleThreeFileRefs(): void {
  const activeDocs = [
    "README.md",
    "AGENTS.md",
    "DESIGN.md",
    "docs/feature-reality-north-star-audit.md",
    "docs/feature-first-class-upgrade-audit.md",
    "docs/giants-shoulders/launcher-first-class-patterns.md",
  ];
  const violations: string[] = [];
  for (const doc of activeDocs) {
    if (!fileExists(doc)) {
      continue;
    }
    const text = readText(doc);
    const matches = text.matchAll(/(?<!retired[/\\])config[/\\](?:config|presets|settings)\.json/g);
    const examples = [...matches].map((match) => match[0]);
    if (examples.length > 0) {
      violations.push(`${doc}: ${examples.slice(0, 3).join(", ")}`);
    }
  }
  if (violations.length > 0) {
    throw new Error(`Stale three-file reference in ${violations.join("; ")} (use config/launcher.json)`);
  }
  addCheck("no-stale-three-file-refs", { docs: activeDocs.join(", ") });
}

export function checkNoImperativeViewUi(): void {
  const viewsDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "Views");
  const whitelisted = new Set(["FastWrapPanel.cs", "LauncherDialogText.cs", "MainWindowEntryCardResolver.cs", "MainWindow.Drag.cs"]);
  const pattern = /\bnew\s+(TextBlock|Grid|StackPanel|Border|Button|TextBox|CheckBox|ComboBox|ScrollViewer)\b/;
  const violations: string[] = [];
  for (const filePath of walkFiles(viewsDir)) {
    const name = path.basename(filePath);
    if (!filePath.endsWith(".cs") || name.endsWith(".axaml.cs") || whitelisted.has(name)) {
      continue;
    }
    if (pattern.test(readText(filePath))) {
      violations.push(name);
    }
  }
  if (violations.length > 0) {
    throw new Error(`Imperative UI control construction found in: ${violations.join(", ")}`);
  }
  addCheck("no-imperative-view-ui", {
    viewsScanned: true,
    violations: violations.length,
    allowedExceptions: [...whitelisted],
  });
}

export function checkDialogServiceNoFuncAction(): void {
  const dialogsDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "Features", "Dialogs");
  const violations: string[] = [];
  for (const filePath of walkFiles(dialogsDir).filter((file) => file.endsWith(".cs"))) {
    forEachLine(filePath, (line, lineNumber) => {
      if (/\b(?:Func|Action)</.test(line)) {
        violations.push(`${path.basename(filePath)}:${lineNumber}`);
      }
    });
  }
  if (violations.length > 0) {
    throw new Error(`Dialog contract still exposes Func/Action bridges at: ${violations.join(", ")}`);
  }
  addCheck("dialog-service-no-func-action", {
    violations: violations.length,
    invariant: "Dialog contracts use ports, not Func/Action bridges",
  });
}

export function checkViewsNoDirectCoreBusinessCalls(): void {
  const viewsDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "Views");
  const pattern = /(?:LauncherEntryDrafts|LauncherConfigExchange)\.|LauncherJson\.Save\(|LauncherStore\.Load\(|EntryName\.Normalize\(/;
  const violations: string[] = [];
  for (const filePath of walkFiles(viewsDir).filter((file) => file.endsWith(".cs"))) {
    forEachLine(filePath, (line, lineNumber) => {
      if (pattern.test(line)) {
        violations.push(`${path.basename(filePath)}:${lineNumber}`);
      }
    });
  }
  if (violations.length > 0) {
    throw new Error(`Views code-behind still touches business IO/normalization at: ${violations.join(", ")}`);
  }
  addCheck("views-no-direct-core-business-calls", {
    violations: violations.length,
    invariant: "View code-behind delegates business IO and normalization to ports",
  });
}

export function checkSecurityAuditF1(): void {
  const source = readText("src/avalonia-launcher/src/WTLauncher.Core/DirectoryPath.cs");
  if (!source.includes("Path.GetFullPath(")) {
    throw new Error("DirectoryPathJsonConverter must call Path.GetFullPath after ExpandEnvironmentVariables (security audit F1)");
  }
  addCheck("security-audit-f1-path-getfullpath", {
    path: "src\\avalonia-launcher\\src\\WTLauncher.Core\\DirectoryPath.cs",
    invariant: "DirectoryPathJsonConverter.Read normalizes via Path.GetFullPath",
    pass: true,
  });
}

export function checkSecurityAuditF2(): void {
  const source = readText("src/avalonia-launcher/src/WTLauncher.App/Features/Presets/IPresetService.cs");
  if (!source.includes("knownEntryIds")) {
    throw new Error("IPresetService.AddPreset/UpdatePreset must accept knownEntryIds parameter (security audit F2)");
  }
  addCheck("security-audit-f2-preset-entryid-validate", {
    path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Features\\Presets\\IPresetService.cs",
    invariant: "AddPreset/UpdatePreset validate entryIds against knownEntryIds",
    pass: true,
  });
}

export function checkSecurityAuditF3(): void {
  const source = readText("src/avalonia-launcher/src/WTLauncher.Core/EntryId.cs");
  if (!source.includes("LatinScript")) {
    throw new Error("EntryId must enforce Latin script whitelist (security audit F3)");
  }
  addCheck("security-audit-f3-homoglyph-protection", {
    path: "src\\avalonia-launcher\\src\\WTLauncher.Core\\EntryId.cs",
    invariant: "EntryId.Parse rejects non-Latin scripts to prevent homoglyph attacks",
    pass: true,
  });
}

export function checkNoThinWrapperDelegation(): void {
  const srcRoot = path.join(avaloniaRoot, "src");
  const regex = new RegExp("^\\s*public\\s+\\w+\\s+(?<wrapMethod>\\w+)\\s*\\([^)]*\\)\\s*\\{\\s*(?:return\\s+(?:this\\.)?)?(?<wrapTarget>\\w+)\\s*\\([^)]*\\)\\s*;\\s*\\}", "m");
  const violations: string[] = [];
  for (const filePath of walkFiles(srcRoot).filter((file) => file.endsWith(".cs"))) {
    const source = readText(filePath);
    const match = regex.exec(source);
    if (!match) {
      continue;
    }
    if (match.groups?.wrapMethod === match.groups?.wrapTarget) {
      continue;
    }
    violations.push(path.basename(filePath));
  }
  if (violations.length > 0) {
    throw new Error(`Thin wrapper delegations detected (potential dead code): ${violations.join(", ")}`);
  }
  addCheck("no-thin-wrapper-delegation", {
    violations: violations.length,
    invariant: "Public methods must not be single-delegation wrappers to same-class methods",
  });
}

export function checkVtkArchitectureIntegrity(): void {
  const components = [{ Name: "StoreGrain", Path: "WTLauncher.App\\Infrastructure\\StoreGrain.cs" }];
  const missing = components.filter((component) => !existsSync(repoPath(`src/avalonia-launcher/src/${component.Path}`))).map((c) => c.Name);
  if (missing.length > 0) {
    throw new Error(`VTuber-Killer architecture components missing: ${missing.join(", ")}`);
  }
  addCheck("vtk-architecture-integrity", {
    components: components.map((c) => c.Name).join(", "),
    missing,
    pass: missing.length === 0,
  });
}

export function checkTrinityArchitectureIntegrity(): void {
  const components = [
    { Name: "LauncherJsonSerializer", Path: "WTLauncher.Core\\LauncherJsonSerializer.cs" },
    { Name: "LauncherStoreValidator", Path: "WTLauncher.Core\\LauncherStoreValidator.cs" },
    { Name: "ILauncherJsonIO", Path: "WTLauncher.Core\\ILauncherJsonIO.cs" },
    { Name: "IStorePersistence", Path: "WTLauncher.Core\\IStorePersistence.cs" },
    { Name: "EntryProjection", Path: "WTLauncher.App\\Features\\Entries\\EntryProjection.cs" },
    { Name: "AppStateMachine", Path: "WTLauncher.App\\ViewModels\\AppStateMachine.cs" },
  ];
  const missing = components.filter((component) => !existsSync(repoPath(`src/avalonia-launcher/src/${component.Path}`))).map((c) => c.Name);
  if (missing.length > 0) {
    throw new Error(`Trinity architecture components missing: ${missing.join(", ")}`);
  }
  addCheck("trinity-architecture-integrity", {
    components: components.map((c) => c.Name).join(", "),
    missing,
    pass: missing.length === 0,
  });
}

export function checkVtkNoAppstoreResidue(): void {
  const srcRoot = path.join(avaloniaRoot, "src");
  const violations: string[] = [];
  for (const filePath of walkFiles(srcRoot).filter((file) => file.endsWith(".cs"))) {
    const hasViolation = readText(filePath)
      .split(/\r?\n/)
      .some((line) => /\bAppStore\b/.test(line) && !/^\s*\/\//.test(line));
    if (hasViolation) {
      violations.push(relDisplay(filePath));
    }
  }
  if (violations.length > 0) {
    throw new Error(`AppStore references found in active code: ${violations.join(", ")} (was deleted by VTuber-Killer)`);
  }
  addCheck("vtk-no-appstore-residue", {
    violations: violations.length,
    invariant: "AppStore was deleted by VTuber-Killer migration; zero references allowed in src/",
  });
}

export function checkWiringMapConsistency(): void {
  const wiringMap = readText("docs/wiring-map.md");
  const workflowDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "ViewModels", "Commands");
  const workflowFiles = walkFiles(workflowDir).filter((file) => file.endsWith(".cs"));
  const missingWorkflows = workflowFiles.filter((file) => !wiringMap.includes(path.basename(file, ".cs"))).map((file) => path.basename(file, ".cs"));
  const requiredPorts = [
    "ICommandExecutionContext",
    "IStatusSink",
    "IProjectionSynchronizer",
    "ICommandHost",
    "IEntryCardHost",
    "ICommandEditApplier",
    "IDirectoryEditApplier",
    "ILaunchCoordinatorContext",
  ];
  const missingPorts = requiredPorts.filter((port) => !wiringMap.includes(port));
  if (missingWorkflows.length > 0 || missingPorts.length > 0) {
    throw new Error(`wiring-map-consistency: missing workflows: ${missingWorkflows.join(", ")}; missing ports: ${missingPorts.join(", ")}`);
  }
  addCheck("wiring-map-consistency", {
    path: "docs\\wiring-map.md",
    workflows: workflowFiles.length,
    missingWorkflows: missingWorkflows.join(", "),
    missingPorts: missingPorts.join(", "),
  });
}

export function checkMainVmLineBudget(): void {
  const viewModelsDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "ViewModels");
  const files = walkFiles(viewModelsDir).filter((file) => path.basename(file).startsWith("MainWindowViewModel"));
  const lines = files.reduce((total, file) => total + readText(file).split(/\r?\n/).length, 0);
  if (lines > 1200) {
    throw new Error(`main-vm-line-budget: MainWindowViewModel*.cs total ${lines} exceeds 1200`);
  }
  addCheck("main-vm-line-budget", {
    lines,
    budget: 1200,
    path: "src\\avalonia-launcher\\src\\WTLauncher.App\\ViewModels\\MainWindowViewModel*.cs",
  });
}

export function checkWorkflowNoCallbackBag(): void {
  const workflowDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "ViewModels", "Commands");
  const violations: string[] = [];
  const ctorRegex = /public\s+Launcher\w+Workflow\s*\((?<params>(?:[^()]|\([^()]*\))*)\)/g;
  for (const filePath of walkFiles(workflowDir).filter((file) => file.endsWith(".cs"))) {
    const source = readText(filePath);
    for (const match of source.matchAll(ctorRegex)) {
      const params = match.groups?.params ?? "";
      if (params.includes("MainWindowViewModel")) {
        violations.push(`${path.basename(filePath)}: ctor depends on MainWindowViewModel`);
      }
      if (params.includes("Action<")) {
        violations.push(`${path.basename(filePath)}: ctor uses Action callback`);
      }
      if (params.includes("Func<") && !params.includes("Func<DateTimeOffset")) {
        violations.push(`${path.basename(filePath)}: ctor uses Func callback`);
      }
    }
  }
  if (violations.length > 0) {
    throw new Error(`workflow-no-callback-bag: ${violations.join("; ")}`);
  }
  addCheck("workflow-no-callback-bag", {
    violations: violations.length,
    invariant: "Workflow ctors use narrow ports, not MainWindowViewModel or Func/Action callback bags",
  });
}

export function checkSettingsSingleSourceBuildStoreFile(): void {
  const source = readText("src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs");
  const signatureCount = countMatches(
    source,
    /Entries = _list\.MutableEntries\.Select\(ToLaunchEntry\)\.ToImmutableList\(\),\s*\r?\n\s+Settings =/g,
  );
  if (signatureCount !== 1) {
    throw new Error(
      `MainWindowViewModel.cs should have exactly 1 LauncherStoreFile hand-assembly site (BuildStoreFile); found ${signatureCount}. SaveSettingsToStore must call BuildStoreFile, not hand-assemble. See task 07-09-settings-single-source R1.`,
    );
  }
  addCheck("settings-single-source-buildstorefile", { signatureCount });
}

export function checkWingetConfigSchemaValid(): void {
  const text = readText(".wt-protector.dsc.yaml");
  containsRequiredMarkers(
    text,
    ["configuration-dsc-schema/0.2", "configurationVersion: 0.2.0", "id: Microsoft.WindowsTerminal", "id: Microsoft.PowerShell"],
    ".wt-protector.dsc.yaml",
  );
  addCheck("winget-config-schema-valid", {
    path: ".wt-protector.dsc.yaml",
    requiredMarkers: ["schema-0.2", "configVersion-0.2.0", "WindowsTerminal", "PowerShell"],
  });
}

export function checkWinformsRetiredDesign(): void {
  const text = readText("retired/design/winforms-launcher.md");
  containsRequiredMarkers(
    text,
    ["Retired WinForms launcher design", "Portable config discovery", "Launch safety boundary", "Successor directive"],
    "WinForms retirement design",
  );
  addCheck("winforms-retired-design", { path: "retired\\design\\winforms-launcher.md" });
}

export function checkHtaRetiredConfig(): void {
  const items = readConfigItems("retired/config/config.json");
  addCheck("hta-retired-config", {
    path: "retired\\config\\config.json",
    items: items.length,
    hash: shortHashFile(repoPath("retired/config/config.json")),
    activeSource: false,
  });
}

export function checkPortableInstallerContract(): void {
  const text = readText("tools/install-portable.ps1");
  containsRequiredMarkers(text, ["WTLauncher.App.exe", "uninstall.ps1", "config", "Start Menu"], "Portable installer");
  addCheck("portable-installer-contract", {
    path: "tools\\install-portable.ps1",
    preservesPortableConfig: true,
    uninstallScript: "uninstall.ps1",
  });
}

export function checkUpdateManifestGeneration(): void {
  const text = readText("scripts/build-release.ts");
  if (!text.includes("update-manifest.json")) {
    throw new Error("scripts/build-release.ts must emit update-manifest.json");
  }
  addCheck("update-manifest-generation", { path: "scripts\\build-release.ts", manifest: "update-manifest.json" });
}

export function checkTestStructureLayering(): void {
  const testRoot = path.join(avaloniaRoot, "tests", "WTLauncher.Tests");
  const layers = ["Support", "CoreUnit", "Persistence", "ViewModelBehavior", "Analyzer", "Architecture", "UiContract", "PropertyTest"];
  for (const layer of layers) {
    const layerDir = path.join(testRoot, layer);
    if (!existsSync(layerDir)) {
      throw new Error(`Missing test layer directory: ${layer}`);
    }
    const hasTests = walkFiles(layerDir).some((file) => file.endsWith(".cs"));
    if (!hasTests) {
      throw new Error(`Empty test layer directory: ${layer}`);
    }
  }
  addCheck("test-structure-layering", { layers: layers.join(",") });
}

export function checkCiFastLaneNoPublishDuplication(): void {
  const workflow = readText(".github/workflows/verify.yml");
  if (!workflow.includes("npm run test:fast")) {
    throw new Error("CI build-and-test must run npm run test:fast");
  }
  if (workflow.includes("Publish (smoke check)")) {
    throw new Error("CI build-and-test must not duplicate publish; full-gate owns publish");
  }
  addCheck("ci-fast-lane-no-publish-duplication", { pass: true });
}
