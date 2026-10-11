import { existsSync } from "node:fs";
import path from "node:path";
import { avaloniaRoot, repoRoot, solutionPath, testsProjectPath, appProjectPath } from "../config-lib.js";
import {
  addCheck,
  assertMatcherAllows,
  assertMatcherDetects,
  containsRequiredMarkers,
  countMatches,
  git,
  readConfigItems,
  readText,
  repoPath,
  shortHashFile,
  walkFiles,
  walkFilesExcluding,
} from "./common.js";
import {
  generateStructureBaseline,
  structureBaselineEndMarker,
  structureBaselineStartMarker,
} from "../generate-structure-baseline.js";

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
  const bareCatch = /catch\s*\(\s*Exception\b/;
  const isBareCatch = (line: string): boolean => bareCatch.test(line) && !/\bwhen\b/.test(line);
  assertMatcherDetects(bareCatch, "catch (Exception ex)", "no-bare-catch-exception");
  if (isBareCatch("catch (Exception ex) when (IsRecoverable(ex))")) {
    throw new Error(
      "no-bare-catch-exception: allowed fixture (catch with when-filter) was judged a violation",
    );
  }
  const violations: string[] = [];
  let scannedFiles = 0;
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
    scannedFiles += 1;
    forEachLine(filePath, (line, lineNumber) => {
      if (isBareCatch(line)) {
        violations.push(`${relDisplay(filePath)}:${lineNumber}`);
      }
    });
  }
  if (scannedFiles === 0) {
    throw new Error("no-bare-catch-exception scanned 0 C# files: scan target or file filter is broken");
  }
  if (violations.length > 0) {
    throw new Error(`Bare catch(Exception) found (use catch (Exception ex) when (...)): ${violations.join("; ")}`);
  }
  addCheck("no-bare-catch-exception", {
    path: "src/avalonia-launcher/src",
    scannedFiles,
    violations: 0,
    invariant: "docs/spec/guides/index.md §5",
  });
}

export function checkAppRawHexZero(): void {
  const rawHexPattern = /(?<![0-9A-Fa-f])#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})(?![0-9A-Fa-f])/;
  const appRoot = path.join(avaloniaRoot, "src", "WTLauncher.App");
  assertMatcherDetects(rawHexPattern, '<Border Background="#123456"/>', "app-raw-hex-zero");
  assertMatcherAllows(
    rawHexPattern,
    '<Setter Property="ToolTip" Value="https://example.com/docs#anchor"/>',
    "app-raw-hex-zero",
  );
  const violations: string[] = [];
  let scannedFiles = 0;
  for (const filePath of walkFiles(appRoot)) {
    const normalized = filePath.replaceAll("\\", "/");
    if (normalized.includes("/obj/") || normalized.includes("/bin/")) {
      continue;
    }
    if (!filePath.endsWith(".cs") && !filePath.endsWith(".axaml")) {
      continue;
    }
    scannedFiles += 1;
    forEachLine(filePath, (line, lineNumber) => {
      if (rawHexPattern.test(line)) {
        violations.push(`${relDisplay(filePath)}:${lineNumber}`);
      }
    });
  }
  if (scannedFiles === 0) {
    throw new Error("app-raw-hex-zero scanned 0 C#/AXAML files: scan target or file filter is broken");
  }
  if (violations.length > 0) {
    throw new Error(`Raw hex color found in WTLauncher.App (use LauncherDesignTokens/resource): ${violations.join("; ")}`);
  }
  addCheck("app-raw-hex-zero", {
    path: "src/avalonia-launcher/src/WTLauncher.App",
    scannedFiles,
    violations: 0,
    fixtureChecked: true,
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
  // claim-boundary 用语义锚而非逐字锚:锁"边界声明存在 + 指向权威审计 + 否定性语义",
  // 措辞可改,但"不声称不可改进 / 有副作用 / 未完成"的语义不可丢。逐字锁会让 README
  // 一次措辞调整就红门禁,而这里真正要防的是"边界声明被悄悄弱化"。
  containsRequiredMarkers(
    text,
    [
      "feature-reality-north-star-audit",
      "does not claim",
      "side effect",
      "not complete",
    ],
    "README.md claim boundary",
  );
  addCheck("readme", { sections: 6, claimBoundary: "semantic-anchors" });
}

export function checkCommandTableConsistency(): void {
  const agents = readText("AGENTS.md");
  const toolboxMatch = agents.match(/## 脚本工具箱[\s\S]*?## 结构基线/);
  if (!toolboxMatch) {
    throw new Error("command-table-consistency: AGENTS.md 脚本工具箱 section missing");
  }
  const commandPattern = /npm run ([A-Za-z0-9:_-]+)/g;
  // 自检用非全局副本:test() 会推进全局正则 lastIndex,污染后续 matchAll 的扫描起点。
  assertMatcherDetects(new RegExp(commandPattern.source), "`npm run verify`", "command-table-consistency");
  assertMatcherAllows(new RegExp(commandPattern.source), "npm ci", "command-table-consistency");
  const listed = new Set([...toolboxMatch[0].matchAll(commandPattern)].map((match) => match[1]));
  if (listed.size === 0) {
    throw new Error("command-table-consistency scanned 0 npm run commands: AGENTS.md 脚本工具箱 format may be broken");
  }
  const pkg = JSON.parse(readText("package.json")) as { scripts?: Record<string, string> };
  const scripts = new Set(Object.keys(pkg.scripts ?? {}));
  const tableOnly = [...listed].filter((name) => !scripts.has(name)).sort();
  const packageOnly = [...scripts].filter((name) => !listed.has(name)).sort();
  if (tableOnly.length > 0 || packageOnly.length > 0) {
    throw new Error(
      `command-table-consistency drift: AGENTS.md table-only ${tableOnly.join(", ") || "none"}; package.json-only ${packageOnly.join(", ") || "none"}. 脚本工具箱必须镜像 package.json scripts。`,
    );
  }
  addCheck("command-table-consistency", {
    tableCommands: listed.size,
    packageScripts: scripts.size,
    tableOnly: tableOnly.join(", "),
    packageOnly: packageOnly.join(", "),
  });
}

export function checkStructureBaselineUpToDate(): void {
  const agents = readText("AGENTS.md");
  const startIdx = agents.indexOf(structureBaselineStartMarker);
  const endIdx = agents.indexOf(structureBaselineEndMarker);
  if (startIdx === -1 || endIdx === -1 || endIdx <= startIdx) {
    throw new Error("structure-baseline-up-to-date: AGENTS.md DERIVED markers missing");
  }
  const current = agents.slice(startIdx + structureBaselineStartMarker.length, endIdx).trim();
  const expected = generateStructureBaseline().trim();
  if (current !== expected) {
    throw new Error(
      "structure-baseline-up-to-date: AGENTS.md derived block is stale. Run `npm run gen:structure-baseline`.",
    );
  }
  addCheck("structure-baseline-up-to-date", { derivedBlockCurrent: true });
}

export function checkNoImperativeViewUi(): void {
  const viewsDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "Views");
  // DiscoverAgentsWindow.axaml.cs 是唯一有意用代码动态渲染 UI 的视图:
  // 代理发现结果数量未知,卡片布局只能在运行时构建。其余 View 一律走 XAML。
  const whitelisted = new Set(["DiscoverAgentsWindow.axaml.cs", "LauncherDialogText.cs", "MainWindowEntryCardResolver.cs", "MainWindow.Drag.cs"]);
  const pattern = /\bnew\s+(TextBlock|Grid|StackPanel|Border|Button|TextBox|CheckBox|ComboBox|ScrollViewer)\b/;
  assertMatcherDetects(pattern, "var label = new TextBlock();", "no-imperative-view-ui");
  assertMatcherAllows(pattern, "var card = new EntryCardViewModel();", "no-imperative-view-ui");
  const violations: string[] = [];
  let scannedFiles = 0;
  for (const filePath of walkFiles(viewsDir)) {
    const name = path.basename(filePath);
    if (!filePath.endsWith(".cs") || whitelisted.has(name)) {
      continue;
    }
    scannedFiles += 1;
    if (pattern.test(readText(filePath))) {
      violations.push(name);
    }
  }
  if (scannedFiles === 0) {
    throw new Error("no-imperative-view-ui scanned 0 View code-behind files: scan target or filter is broken");
  }
  if (violations.length > 0) {
    throw new Error(`Imperative UI control construction found in: ${violations.join(", ")}`);
  }
  addCheck("no-imperative-view-ui", {
    viewsScanned: scannedFiles,
    violations: violations.length,
    allowedExceptions: [...whitelisted],
  });
}

export function checkDialogServiceNoFuncAction(): void {
  const dialogsDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "Features", "Dialogs");
  const bridgePattern = /\b(?:Func|Action)</;
  assertMatcherDetects(bridgePattern, "public Func<bool> Confirm { get; }", "dialog-service-no-func-action");
  assertMatcherAllows(bridgePattern, "public bool Confirm { get; }", "dialog-service-no-func-action");
  const dialogFiles = walkFiles(dialogsDir).filter((file) => file.endsWith(".cs"));
  if (dialogFiles.length === 0) {
    throw new Error("dialog-service-no-func-action scanned 0 dialog files: scan target is broken");
  }
  const violations: string[] = [];
  for (const filePath of dialogFiles) {
    forEachLine(filePath, (line, lineNumber) => {
      if (bridgePattern.test(line)) {
        violations.push(`${path.basename(filePath)}:${lineNumber}`);
      }
    });
  }
  if (violations.length > 0) {
    throw new Error(`Dialog contract still exposes Func/Action bridges at: ${violations.join(", ")}`);
  }
  addCheck("dialog-service-no-func-action", {
    scannedFiles: dialogFiles.length,
    violations: violations.length,
    invariant: "Dialog contracts use ports, not Func/Action bridges",
  });
}

export function checkViewsNoDirectCoreBusinessCalls(): void {
  const viewsDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "Views");
  const pattern = /(?:LauncherEntryDrafts|LauncherConfigExchange)\.|LauncherJson\.Save\(|LauncherStore\.Load\(|EntryName\.Normalize\(/;
  assertMatcherDetects(pattern, "LauncherJson.Save(store);", "views-no-direct-core-business-calls");
  assertMatcherAllows(
    pattern,
    "// Views delegate EntryName.Normalize to ports",
    "views-no-direct-core-business-calls",
  );
  const viewFiles = walkFiles(viewsDir).filter((file) => file.endsWith(".cs"));
  if (viewFiles.length === 0) {
    throw new Error("views-no-direct-core-business-calls scanned 0 View files: scan target is broken");
  }
  const violations: string[] = [];
  for (const filePath of viewFiles) {
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
    scannedFiles: viewFiles.length,
    violations: violations.length,
    invariant: "View code-behind delegates business IO and normalization to ports",
  });
}

export function checkSecurityAuditF1(): void {
  const source = readText("src/avalonia-launcher/src/WTLauncher.Core/DirectoryPath.cs");
  if (!source.includes("Path.GetFullPath(")) {
    throw new Error("DirectoryPathJsonConverter must call Path.GetFullPath after ExpandEnvironmentVariables (security audit F1)");
  }
  addCheck("security-audit-f1", {
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
  addCheck("security-audit-f2", {
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
  addCheck("security-audit-f3", {
    path: "src\\avalonia-launcher\\src\\WTLauncher.Core\\EntryId.cs",
    invariant: "EntryId.Parse rejects non-Latin scripts to prevent homoglyph attacks",
    pass: true,
  });
}

export function checkNoThinWrapperDelegation(): void {
  const srcRoot = path.join(avaloniaRoot, "src");
  const wrapperSource =
    "^\\s*public\\s+\\w+\\s+(?<wrapMethod>\\w+)\\s*\\([^)]*\\)\\s*\\{\\s*(?:return\\s+(?:this\\.)?)?(?<wrapTarget>\\w+)\\s*\\([^)]*\\)\\s*;\\s*\\}";
  // 自检必须用非全局副本:全局正则的 lastIndex 会被 test() 推进,
  // 而 matchAll 从 lastIndex 起扫,按原写法会让每个文件都从错误起点开始。
  assertMatcherDetects(
    new RegExp(wrapperSource, "m"),
    "public int Wrapper()\n{\n    return Target();\n}",
    "no-thin-wrapper-delegation",
  );
  assertMatcherAllows(
    new RegExp(wrapperSource, "m"),
    "public int Wrapper()\n{\n    return Target() + 1;\n}",
    "no-thin-wrapper-delegation",
  );
  const wrapperPattern = new RegExp(wrapperSource, "gm");
  const coreFiles = walkFiles(srcRoot).filter((file) => file.endsWith(".cs"));
  if (coreFiles.length === 0) {
    throw new Error("no-thin-wrapper-delegation scanned 0 C# files: scan target is broken");
  }
  const violations: string[] = [];
  for (const filePath of coreFiles) {
    const source = readText(filePath);
    // 遍历全部匹配。此前只取 exec() 的首个匹配,首个匹配若是合法的
    // "同名包装"(wrapMethod === wrapTarget)就会 continue 掉整个文件,后续真缺陷永不暴露。
    for (const match of source.matchAll(wrapperPattern)) {
      const wrapMethod = match.groups?.wrapMethod;
      const wrapTarget = match.groups?.wrapTarget;
      if (wrapMethod === wrapTarget) {
        continue;
      }
      const lineNumber = source.slice(0, match.index ?? 0).split(/\r?\n/).length;
      violations.push(`${relDisplay(filePath)}:${lineNumber} ${wrapMethod} -> ${wrapTarget}`);
    }
  }
  if (violations.length > 0) {
    throw new Error(`Thin wrapper delegations detected (potential dead code): ${violations.join(", ")}`);
  }
  addCheck("no-thin-wrapper-delegation", {
    scannedFiles: coreFiles.length,
    violations: violations.length,
    invariant: "Public methods must not be single-delegation wrappers to same-class methods",
  });
}

export function checkTrinityArchitectureIntegrity(): void {
  const components = [
    { Name: "LauncherJsonSerializer", Path: "WTLauncher.Core\\LauncherJsonSerializer.cs" },
    { Name: "LauncherStoreValidator", Path: "WTLauncher.Core\\LauncherStoreValidator.cs" },
    { Name: "ILauncherJsonIO", Path: "WTLauncher.Core\\ILauncherJsonIO.cs" },
    { Name: "IStorePersistence", Path: "WTLauncher.Core\\IStorePersistence.cs" },
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
  const appStorePattern = /\bAppStore\b/;
  const isResidue = (line: string): boolean => appStorePattern.test(line) && !/^\s*\/\//.test(line);
  assertMatcherDetects(appStorePattern, "var store = AppStore.Load();", "vtk-no-appstore-residue");
  if (isResidue("// AppStore was deleted by VTuber-Killer")) {
    throw new Error("vtk-no-appstore-residue: a commented-out mention must not count as residue");
  }
  const coreFiles = walkFiles(srcRoot).filter((file) => file.endsWith(".cs"));
  if (coreFiles.length === 0) {
    throw new Error("vtk-no-appstore-residue scanned 0 C# files: scan target is broken");
  }
  const violations: string[] = [];
  for (const filePath of coreFiles) {
    const hasViolation = readText(filePath)
      .split(/\r?\n/)
      .some((line) => isResidue(line));
    if (hasViolation) {
      violations.push(relDisplay(filePath));
    }
  }
  if (violations.length > 0) {
    throw new Error(`AppStore references found in active code: ${violations.join(", ")} (was deleted by VTuber-Killer)`);
  }
  addCheck("vtk-no-appstore-residue", {
    scannedFiles: coreFiles.length,
    violations: violations.length,
    invariant: "AppStore was deleted by VTuber-Killer migration; zero references allowed in src/",
  });
}

export function checkWiringMapConsistency(): void {
  const wiringMap = readText("docs/wiring-map.md");
  const workflowDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "ViewModels", "Commands");
  const workflowFiles = walkFiles(workflowDir).filter((file) => file.endsWith(".cs"));
  if (workflowFiles.length === 0) {
    throw new Error("wiring-map-consistency scanned 0 workflow files: scan target is broken");
  }
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
  // 扫描集为空时 lines 自然是 0,而 0 永远不超预算——检查会以"零违规"静默通过。
  // 名称过滤条件是本检查仅有的输入,它一旦失效(改名/移目录),这条预算就没人守了。
  if (files.length === 0) {
    throw new Error("main-vm-line-budget scanned 0 MainWindowViewModel partials: scan target or name filter is broken");
  }
  const lines = files.reduce((total, file) => total + readText(file).split(/\r?\n/).length, 0);
  if (lines > 1200) {
    throw new Error(`main-vm-line-budget: MainWindowViewModel*.cs total ${lines} exceeds 1200`);
  }
  addCheck("main-vm-line-budget", {
    lines,
    scannedFiles: files.length,
    budget: 1200,
    path: "src\\avalonia-launcher\\src\\WTLauncher.App\\ViewModels\\MainWindowViewModel*.cs",
  });
}

export function checkWorkflowNoCallbackBag(): void {
  const workflowDir = path.join(avaloniaRoot, "src", "WTLauncher.App", "ViewModels", "Commands");
  const violations: string[] = [];
  const ctorRegex = /public\s+Launcher\w+Workflow\s*\((?<params>(?:[^()]|\([^()]*\))*)\)/g;
  // 自检:构造器匹配器依赖 "Launcher*Workflow" 命名约定。约定漂移会让它从此
  // 一个构造器都匹配不到,而每个文件都"零违规"通过——正是这条检查最该防的死法。
  // 自检用非全局副本:test() 会推进全局正则的 lastIndex,影响后续 matchAll 的起点。
  assertMatcherDetects(
    new RegExp(ctorRegex.source),
    "public LauncherSaveWorkflow(MainWindowViewModel owner)",
    "workflow-no-callback-bag",
  );
  const workflowFiles = walkFiles(workflowDir).filter((file) => file.endsWith(".cs"));
  if (workflowFiles.length === 0) {
    throw new Error("workflow-no-callback-bag scanned 0 workflow files: scan target is broken");
  }
  for (const filePath of workflowFiles) {
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
    scannedFiles: workflowFiles.length,
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
  addCheck("settings-single-source", { signatureCount });
}

export function checkWingetConfigSchemaValid(): void {
  const text = readText(".wt-protector.dsc.yaml");
  containsRequiredMarkers(
    text,
    ["configuration-dsc-schema/0.2", "configurationVersion: 0.2.0", "id: Microsoft.WindowsTerminal", "id: Microsoft.PowerShell"],
    ".wt-protector.dsc.yaml",
  );
  addCheck("winget-config-schema", {
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
  const text = readText("scripts/install-portable.ts");
  containsRequiredMarkers(
    text,
    ["WTLauncher.App.exe", "uninstaller.exe", "config", "createShortcut"],
    "Portable installer",
  );
  addCheck("portable-installer-contract", {
    path: "scripts\\install-portable.ts",
    preservesPortableConfig: true,
    uninstaller: "uninstaller.exe",
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
  const layers = ["Support", "CoreUnit", "Persistence", "ViewModelBehavior", "Analyzer", "UiContract", "PropertyTest"];
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

export function checkAotToolchainOwnedBySdk(): void {
  const csproj = readText("src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj");
  const props = readText("src/avalonia-launcher/Directory.Packages.props");
  const violations: string[] = [];
  for (const packageName of ["Microsoft.DotNet.ILCompiler", "Microsoft.NET.ILLink.Tasks"]) {
    if (csproj.includes(`Include="${packageName}"`)) {
      violations.push(`WTLauncher.App.csproj must not reference ${packageName} explicitly`);
    }
    if (props.includes(`Include="${packageName}"`)) {
      violations.push(`Directory.Packages.props must not pin ${packageName}`);
    }
  }
  if (violations.length > 0) {
    throw new Error(`Native AOT toolchain must be owned by the .NET SDK: ${violations.join("; ")}`);
  }
  addCheck("aot-toolchain-owned-by-sdk", {
    csprojExplicit: false,
    propsExplicit: false,
    invariant: ".NET SDK owns Native AOT compiler and linker selection",
  });
}

export function checkNoActivePowerShellScripts(): void {
  const scanned = walkFilesExcluding(repoRoot, ["node_modules", ".git", ".snow", "artifacts", "retired", "archive"]);
  // 期望值本身就是 0,因此结果无法区分"仓里真的没有 active .ps1"与"扫描/排除规则失效"。
  // 只有"扫到了若干文件"这个事实才能证明这次扫描确实发生过。
  if (scanned.length === 0) {
    throw new Error("no-active-powershell-scripts scanned 0 repository files: scan target or exclusion list is broken");
  }
  const active = scanned.filter((file) => file.toLowerCase().endsWith(".ps1"));
  if (active.length > 0) {
    throw new Error(`Active PowerShell scripts must be zero; found: ${active.map((file) => relDisplay(file)).join(", ")}`);
  }
  addCheck("no-active-powershell-scripts", {
    scannedFiles: scanned.length,
    activePowerShellScripts: active.length,
  });
}

export function checkCiFastLaneNoPublishDuplication(): void {
  const workflow = readText(".github/workflows/verify.yml");
  const required = ["name: CI", "npm run test:fast", "npm run verify", "npm run release:contract"];
  for (const marker of required) {
    if (!workflow.includes(marker)) {
      throw new Error(`CI workflow must include: ${marker}`);
    }
  }
  if (workflow.includes("needs:")) {
    throw new Error("CI workflow must be a single sequential job with no needs graph");
  }
  if (workflow.includes("if: always()")) {
    throw new Error("CI workflow must not use always() upload branching");
  }
  addCheck("ci-fast-lane-no-publish-duplication", { jobs: 1, publishOwnedBy: "npm run release:contract" });
}
