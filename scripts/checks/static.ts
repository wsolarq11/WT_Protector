import path from "node:path";
import { avaloniaRoot, repoRoot, solutionPath, testsProjectPath, appProjectPath } from "../config-lib.js";
import { type Check, type Ctx, defineCheck } from "../kernel/check.js";
import {
  generateStructureBaseline,
  structureBaselineEndMarker,
  structureBaselineStartMarker,
} from "../generate-structure-baseline.js";

function relDisplay(ctx: Ctx, filePath: string): string {
  return filePath.replaceAll(ctx.repoRoot, "").replace(/^[\\/]+/, "");
}

function forEachLine(ctx: Ctx, filePath: string, visitor: (line: string, lineNumber: number) => void): void {
  const lines = ctx.readText(filePath).split(/\r?\n/);
  lines.forEach((line, index) => visitor(line, index + 1));
}

export const lineEndingPolicyCheck: Check = defineCheck({
  id: "line-ending-policy",
  tier: "static",
  run: (ctx) => {
    const text = ctx.readText(".gitattributes");
    ctx.containsRequiredMarkers(text, ["* text=auto eol=lf"], ".gitattributes");
    return { path: ".gitattributes", text: "lf" };
  },
});

export const snowIgnoredLocalStateCheck: Check = defineCheck({
  id: "snow-ignored-local-state",
  tier: "static",
  run: async (ctx) => {
    const gitignore = ctx.readText(".gitignore");
    if (!new RegExp("^\\.snow/$", "m").test(gitignore)) {
      throw new Error(".gitignore must ignore .snow/ local state");
    }
    const tracked = await ctx.git(["ls-files", "--", ".snow"]);
    if (tracked.length > 0) {
      throw new Error(`.snow/ must remain ignored local state, but tracked files remain: ${tracked.join(", ")}`);
    }
    return { path: ".snow/", trackedFiles: tracked.length, ignored: true };
  },
});

export const retiredPathsRemovedCheck: Check = defineCheck({
  id: "retired-paths-removed",
  tier: "static",
  run: (ctx) => {
    const forbidden = ["spikes", "src/winforms-launcher", "WT Launcher.exe", "WT Launcher"];
    for (const item of forbidden) {
      if (ctx.exists(item)) {
        throw new Error(`Retired path must not remain: ${ctx.repoPath(item)}`);
      }
    }
    return {
      paths: ["spikes", "src\\winforms-launcher", "WT Launcher.exe", "WT Launcher"],
    };
  },
});

export const noBareCatchExceptionCheck: Check = defineCheck({
  id: "no-bare-catch-exception",
  tier: "static",
  run: (ctx) => {
    const srcRoot = ctx.path(avaloniaRoot, "src");
    const bareCatch = /catch\s*\(\s*Exception\b/;
    const isBareCatch = (line: string): boolean => bareCatch.test(line) && !/\bwhen\b/.test(line);
    ctx.assertMatcherDetects(bareCatch, "catch (Exception ex)", "no-bare-catch-exception");
    if (isBareCatch("catch (Exception ex) when (IsRecoverable(ex))")) {
      throw new Error(
        "no-bare-catch-exception: allowed fixture (catch with when-filter) was judged a violation",
      );
    }
    const violations: string[] = [];
    let scannedFiles = 0;
    for (const filePath of ctx.walkFiles(srcRoot)) {
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
      forEachLine(ctx, filePath, (line, lineNumber) => {
        if (isBareCatch(line)) {
          violations.push(`${relDisplay(ctx, filePath)}:${lineNumber}`);
        }
      });
    }
    if (scannedFiles === 0) {
      throw new Error("no-bare-catch-exception scanned 0 C# files: scan target or file filter is broken");
    }
    if (violations.length > 0) {
      throw new Error(`Bare catch(Exception) found (use catch (Exception ex) when (...)): ${violations.join("; ")}`);
    }
    return {
      path: "src/avalonia-launcher/src",
      scannedFiles,
      violations: 0,
      invariant: "docs/spec/guides/index.md §5",
    };
  },
});

export const appRawHexZeroCheck: Check = defineCheck({
  id: "app-raw-hex-zero",
  tier: "static",
  run: (ctx) => {
    const rawHexPattern = /(?<![0-9A-Fa-f])#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})(?![0-9A-Fa-f])/;
    const appRoot = ctx.path(avaloniaRoot, "src", "WTLauncher.App");
    ctx.assertMatcherDetects(rawHexPattern, '<Border Background="#123456"/>', "app-raw-hex-zero");
    ctx.assertMatcherAllows(
      rawHexPattern,
      '<Setter Property="ToolTip" Value="https://example.com/docs#anchor"/>',
      "app-raw-hex-zero",
    );
    const violations: string[] = [];
    let scannedFiles = 0;
    for (const filePath of ctx.walkFiles(appRoot)) {
      const normalized = filePath.replaceAll("\\", "/");
      if (normalized.includes("/obj/") || normalized.includes("/bin/")) {
        continue;
      }
      if (!filePath.endsWith(".cs") && !filePath.endsWith(".axaml")) {
        continue;
      }
      scannedFiles += 1;
      forEachLine(ctx, filePath, (line, lineNumber) => {
        if (rawHexPattern.test(line)) {
          violations.push(`${relDisplay(ctx, filePath)}:${lineNumber}`);
        }
      });
    }
    if (scannedFiles === 0) {
      throw new Error("app-raw-hex-zero scanned 0 C#/AXAML files: scan target or file filter is broken");
    }
    if (violations.length > 0) {
      throw new Error(`Raw hex color found in WTLauncher.App (use LauncherDesignTokens/resource): ${violations.join("; ")}`);
    }
    return {
      path: "src/avalonia-launcher/src/WTLauncher.App",
      scannedFiles,
      violations: 0,
      fixtureChecked: true,
    };
  },
});

export const mainlineLayoutCheck: Check = defineCheck({
  id: "mainline-layout",
  tier: "static",
  run: (ctx) => {
    for (const [name, filePath] of [
      ["sourceRoot", avaloniaRoot],
      ["solution", solutionPath],
      ["app", appProjectPath],
      ["tests", testsProjectPath],
    ] as const) {
      if (!ctx.exists(filePath)) {
        throw new Error(`Missing Avalonia mainline surface ${name}: ${filePath}`);
      }
    }
    return {
      sourceRoot: "src\\avalonia-launcher",
      solution: "src\\avalonia-launcher\\WTLauncher.Avalonia.sln",
      app: "src\\avalonia-launcher\\src\\WTLauncher.App\\WTLauncher.App.csproj",
      tests: "src\\avalonia-launcher\\tests\\WTLauncher.Tests\\WTLauncher.Tests.csproj",
    };
  },
});

export const readmeCheck: Check = defineCheck({
  id: "readme",
  tier: "static",
  run: (ctx) => {
    const text = ctx.readText("README.md");
    ctx.containsRequiredMarkers(
      text,
      ["Configuration schema", "Build and verification", "Run the mainline app", "WinForms retirement", "Safety notes", "Current implementation status"],
      "README.md",
    );
    // claim-boundary 用语义锚而非逐字锚:锁"边界声明存在 + 指向权威审计 + 否定性语义",
    // 措辞可改,但"不声称不可改进 / 有副作用 / 未完成"的语义不可丢。逐字锁会让 README
    // 一次措辞调整就红门禁,而这里真正要防的是"边界声明被悄悄弱化"。
    ctx.containsRequiredMarkers(
      text,
      [
        "feature-reality-north-star-audit",
        "does not claim",
        "side effect",
        "not complete",
      ],
      "README.md claim boundary",
    );
    return { sections: 6, claimBoundary: "semantic-anchors" };
  },
});

export const commandTableConsistencyCheck: Check = defineCheck({
  id: "command-table-consistency",
  tier: "static",
  run: (ctx) => {
    const agents = ctx.readText("AGENTS.md");
    const toolboxMatch = agents.match(/## 脚本工具箱[\s\S]*?## 结构基线/);
    if (!toolboxMatch) {
      throw new Error("command-table-consistency: AGENTS.md 脚本工具箱 section missing");
    }
    const commandPattern = /npm run ([A-Za-z0-9:_-]+)/g;
    // 自检用非全局副本:test() 会推进全局正则 lastIndex,污染后续 matchAll 的扫描起点。
    ctx.assertMatcherDetects(new RegExp(commandPattern.source), "`npm run verify`", "command-table-consistency");
    ctx.assertMatcherAllows(new RegExp(commandPattern.source), "npm ci", "command-table-consistency");
    const listed = new Set([...toolboxMatch[0].matchAll(commandPattern)].map((match) => match[1]));
    if (listed.size === 0) {
      throw new Error("command-table-consistency scanned 0 npm run commands: AGENTS.md 脚本工具箱 format may be broken");
    }
    const pkg = JSON.parse(ctx.readText("package.json")) as { scripts?: Record<string, string> };
    const scripts = new Set(Object.keys(pkg.scripts ?? {}));
    const tableOnly = [...listed].filter((name) => !scripts.has(name)).sort();
    const packageOnly = [...scripts].filter((name) => !listed.has(name)).sort();
    if (tableOnly.length > 0 || packageOnly.length > 0) {
      throw new Error(
        `command-table-consistency drift: AGENTS.md table-only ${tableOnly.join(", ") || "none"}; package.json-only ${packageOnly.join(", ") || "none"}. 脚本工具箱必须镜像 package.json scripts。`,
      );
    }
    return {
      tableCommands: listed.size,
      packageScripts: scripts.size,
      tableOnly: tableOnly.join(", "),
      packageOnly: packageOnly.join(", "),
    };
  },
});

export const structureBaselineUpToDateCheck: Check = defineCheck({
  id: "structure-baseline-up-to-date",
  tier: "static",
  run: (ctx) => {
    const agents = ctx.readText("AGENTS.md");
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
    return { derivedBlockCurrent: true };
  },
});

export const noImperativeViewUiCheck: Check = defineCheck({
  id: "no-imperative-view-ui",
  tier: "static",
  run: (ctx) => {
    const viewsDir = ctx.path(avaloniaRoot, "src", "WTLauncher.App", "Views");
    // DiscoverAgentsWindow.axaml.cs 是唯一有意用代码动态渲染 UI 的视图:
    // 代理发现结果数量未知,卡片布局只能在运行时构建。其余 View 一律走 XAML。
    const whitelisted = new Set(["DiscoverAgentsWindow.axaml.cs", "LauncherDialogText.cs", "MainWindowEntryCardResolver.cs", "MainWindow.Drag.cs"]);
    const pattern = /\bnew\s+(TextBlock|Grid|StackPanel|Border|Button|TextBox|CheckBox|ComboBox|ScrollViewer)\b/;
    ctx.assertMatcherDetects(pattern, "var label = new TextBlock();", "no-imperative-view-ui");
    ctx.assertMatcherAllows(pattern, "var card = new EntryCardViewModel();", "no-imperative-view-ui");
    const violations: string[] = [];
    let scannedFiles = 0;
    for (const filePath of ctx.walkFiles(viewsDir)) {
      const name = path.basename(filePath);
      if (!filePath.endsWith(".cs") || whitelisted.has(name)) {
        continue;
      }
      scannedFiles += 1;
      if (pattern.test(ctx.readText(filePath))) {
        violations.push(name);
      }
    }
    if (scannedFiles === 0) {
      throw new Error("no-imperative-view-ui scanned 0 View code-behind files: scan target or filter is broken");
    }
    if (violations.length > 0) {
      throw new Error(`Imperative UI control construction found in: ${violations.join(", ")}`);
    }
    return {
      viewsScanned: scannedFiles,
      violations: violations.length,
      allowedExceptions: [...whitelisted],
    };
  },
});

export const dialogServiceNoFuncActionCheck: Check = defineCheck({
  id: "dialog-service-no-func-action",
  tier: "static",
  run: (ctx) => {
    const dialogsDir = ctx.path(avaloniaRoot, "src", "WTLauncher.App", "Features", "Dialogs");
    const bridgePattern = /\b(?:Func|Action)</;
    ctx.assertMatcherDetects(bridgePattern, "public Func<bool> Confirm { get; }", "dialog-service-no-func-action");
    ctx.assertMatcherAllows(bridgePattern, "public bool Confirm { get; }", "dialog-service-no-func-action");
    const dialogFiles = ctx.walkFiles(dialogsDir).filter((file) => file.endsWith(".cs"));
    if (dialogFiles.length === 0) {
      throw new Error("dialog-service-no-func-action scanned 0 dialog files: scan target is broken");
    }
    const violations: string[] = [];
    for (const filePath of dialogFiles) {
      forEachLine(ctx, filePath, (line, lineNumber) => {
        if (bridgePattern.test(line)) {
          violations.push(`${path.basename(filePath)}:${lineNumber}`);
        }
      });
    }
    if (violations.length > 0) {
      throw new Error(`Dialog contract still exposes Func/Action bridges at: ${violations.join(", ")}`);
    }
    return {
      scannedFiles: dialogFiles.length,
      violations: violations.length,
      invariant: "Dialog contracts use ports, not Func/Action bridges",
    };
  },
});

export const viewsNoDirectCoreBusinessCallsCheck: Check = defineCheck({
  id: "views-no-direct-core-business-calls",
  tier: "static",
  run: (ctx) => {
    const viewsDir = ctx.path(avaloniaRoot, "src", "WTLauncher.App", "Views");
    const pattern = /(?:LauncherEntryDrafts|LauncherConfigExchange)\.|LauncherJson\.Save\(|LauncherStore\.Load\(|EntryName\.Normalize\(/;
    ctx.assertMatcherDetects(pattern, "LauncherJson.Save(store);", "views-no-direct-core-business-calls");
    ctx.assertMatcherAllows(
      pattern,
      "// Views delegate EntryName.Normalize to ports",
      "views-no-direct-core-business-calls",
    );
    const viewFiles = ctx.walkFiles(viewsDir).filter((file) => file.endsWith(".cs"));
    if (viewFiles.length === 0) {
      throw new Error("views-no-direct-core-business-calls scanned 0 View files: scan target is broken");
    }
    const violations: string[] = [];
    for (const filePath of viewFiles) {
      forEachLine(ctx, filePath, (line, lineNumber) => {
        if (pattern.test(line)) {
          violations.push(`${path.basename(filePath)}:${lineNumber}`);
        }
      });
    }
    if (violations.length > 0) {
      throw new Error(`Views code-behind still touches business IO/normalization at: ${violations.join(", ")}`);
    }
    return {
      scannedFiles: viewFiles.length,
      violations: violations.length,
      invariant: "View code-behind delegates business IO and normalization to ports",
    };
  },
});

export const securityAuditF1Check: Check = defineCheck({
  id: "security-audit-f1",
  tier: "static",
  run: (ctx) => {
    const source = ctx.readText("src/avalonia-launcher/src/WTLauncher.Core/DirectoryPath.cs");
    if (!source.includes("Path.GetFullPath(")) {
      throw new Error("DirectoryPathJsonConverter must call Path.GetFullPath after ExpandEnvironmentVariables (security audit F1)");
    }
    return {
      path: "src\\avalonia-launcher\\src\\WTLauncher.Core\\DirectoryPath.cs",
      invariant: "DirectoryPathJsonConverter.Read normalizes via Path.GetFullPath",
      pass: true,
    };
  },
});

export const securityAuditF2Check: Check = defineCheck({
  id: "security-audit-f2",
  tier: "static",
  run: (ctx) => {
    const source = ctx.readText("src/avalonia-launcher/src/WTLauncher.App/Features/Presets/IPresetService.cs");
    if (!source.includes("knownEntryIds")) {
      throw new Error("IPresetService.AddPreset/UpdatePreset must accept knownEntryIds parameter (security audit F2)");
    }
    return {
      path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Features\\Presets\\IPresetService.cs",
      invariant: "AddPreset/UpdatePreset validate entryIds against knownEntryIds",
      pass: true,
    };
  },
});

export const securityAuditF3Check: Check = defineCheck({
  id: "security-audit-f3",
  tier: "static",
  run: (ctx) => {
    const source = ctx.readText("src/avalonia-launcher/src/WTLauncher.Core/EntryId.cs");
    if (!source.includes("LatinScript")) {
      throw new Error("EntryId must enforce Latin script whitelist (security audit F3)");
    }
    return {
      path: "src\\avalonia-launcher\\src\\WTLauncher.Core\\EntryId.cs",
      invariant: "EntryId.Parse rejects non-Latin scripts to prevent homoglyph attacks",
      pass: true,
    };
  },
});

export const noThinWrapperDelegationCheck: Check = defineCheck({
  id: "no-thin-wrapper-delegation",
  tier: "static",
  run: (ctx) => {
    const srcRoot = ctx.path(avaloniaRoot, "src");
    const wrapperSource =
      "^\\s*public\\s+\\w+\\s+(?<wrapMethod>\\w+)\\s*\\([^)]*\\)\\s*\\{\\s*(?:return\\s+(?:this\\.)?)?(?<wrapTarget>\\w+)\\s*\\([^)]*\\)\\s*;\\s*\\}";
    // 自检必须用非全局副本:全局正则的 lastIndex 会被 test() 推进,
    // 而 matchAll 从 lastIndex 起扫,按原写法会让每个文件都从错误起点开始。
    ctx.assertMatcherDetects(
      new RegExp(wrapperSource, "m"),
      "public int Wrapper()\n{\n    return Target();\n}",
      "no-thin-wrapper-delegation",
    );
    ctx.assertMatcherAllows(
      new RegExp(wrapperSource, "m"),
      "public int Wrapper()\n{\n    return Target() + 1;\n}",
      "no-thin-wrapper-delegation",
    );
    const wrapperPattern = new RegExp(wrapperSource, "gm");
    const coreFiles = ctx.walkFiles(srcRoot).filter((file) => file.endsWith(".cs"));
    if (coreFiles.length === 0) {
      throw new Error("no-thin-wrapper-delegation scanned 0 C# files: scan target is broken");
    }
    const violations: string[] = [];
    for (const filePath of coreFiles) {
      const source = ctx.readText(filePath);
      // 遍历全部匹配。此前只取 exec() 的首个匹配,首个匹配若是合法的
      // "同名包装"(wrapMethod === wrapTarget)就会 continue 掉整个文件,后续真缺陷永不暴露。
      for (const match of source.matchAll(wrapperPattern)) {
        const wrapMethod = match.groups?.wrapMethod;
        const wrapTarget = match.groups?.wrapTarget;
        if (wrapMethod === wrapTarget) {
          continue;
        }
        const lineNumber = source.slice(0, match.index ?? 0).split(/\r?\n/).length;
        violations.push(`${relDisplay(ctx, filePath)}:${lineNumber} ${wrapMethod} -> ${wrapTarget}`);
      }
    }
    if (violations.length > 0) {
      throw new Error(`Thin wrapper delegations detected (potential dead code): ${violations.join(", ")}`);
    }
    return {
      scannedFiles: coreFiles.length,
      violations: violations.length,
      invariant: "Public methods must not be single-delegation wrappers to same-class methods",
    };
  },
});

export const trinityArchitectureIntegrityCheck: Check = defineCheck({
  id: "trinity-architecture-integrity",
  tier: "static",
  run: (ctx) => {
    const components = [
      { Name: "LauncherJsonSerializer", Path: "WTLauncher.Core\\LauncherJsonSerializer.cs" },
      { Name: "LauncherStoreValidator", Path: "WTLauncher.Core\\LauncherStoreValidator.cs" },
      { Name: "ILauncherJsonIO", Path: "WTLauncher.Core\\ILauncherJsonIO.cs" },
      { Name: "IStorePersistence", Path: "WTLauncher.Core\\IStorePersistence.cs" },
    ];
    const missing = components.filter((component) => !ctx.exists(`src/avalonia-launcher/src/${component.Path}`)).map((c) => c.Name);
    if (missing.length > 0) {
      throw new Error(`Trinity architecture components missing: ${missing.join(", ")}`);
    }
    return {
      components: components.map((c) => c.Name).join(", "),
      missing,
      pass: missing.length === 0,
    };
  },
});

export const vtkNoAppstoreResidueCheck: Check = defineCheck({
  id: "vtk-no-appstore-residue",
  tier: "static",
  run: (ctx) => {
    const srcRoot = ctx.path(avaloniaRoot, "src");
    const appStorePattern = /\bAppStore\b/;
    const isResidue = (line: string): boolean => appStorePattern.test(line) && !/^\s*\/\//.test(line);
    ctx.assertMatcherDetects(appStorePattern, "var store = AppStore.Load();", "vtk-no-appstore-residue");
    if (isResidue("// AppStore was deleted by VTuber-Killer")) {
      throw new Error("vtk-no-appstore-residue: a commented-out mention must not count as residue");
    }
    const coreFiles = ctx.walkFiles(srcRoot).filter((file) => file.endsWith(".cs"));
    if (coreFiles.length === 0) {
      throw new Error("vtk-no-appstore-residue scanned 0 C# files: scan target is broken");
    }
    const violations: string[] = [];
    for (const filePath of coreFiles) {
      const hasViolation = ctx.readText(filePath)
        .split(/\r?\n/)
        .some((line) => isResidue(line));
      if (hasViolation) {
        violations.push(relDisplay(ctx, filePath));
      }
    }
    if (violations.length > 0) {
      throw new Error(`AppStore references found in active code: ${violations.join(", ")} (was deleted by VTuber-Killer)`);
    }
    return {
      scannedFiles: coreFiles.length,
      violations: violations.length,
      invariant: "AppStore was deleted by VTuber-Killer migration; zero references allowed in src/",
    };
  },
});

export const wiringMapConsistencyCheck: Check = defineCheck({
  id: "wiring-map-consistency",
  tier: "static",
  run: (ctx) => {
    const wiringMap = ctx.readText("docs/adr/wiring-map.md");
    const workflowDir = ctx.path(avaloniaRoot, "src", "WTLauncher.App", "ViewModels", "Commands");
    const workflowFiles = ctx.walkFiles(workflowDir).filter((file) => file.endsWith(".cs"));
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
    return {
      path: "docs\\adr\\wiring-map.md",
      workflows: workflowFiles.length,
      missingWorkflows: missingWorkflows.join(", "),
      missingPorts: missingPorts.join(", "),
    };
  },
});

export const mainVmLineBudgetCheck: Check = defineCheck({
  id: "main-vm-line-budget",
  tier: "static",
  run: (ctx) => {
    const viewModelsDir = ctx.path(avaloniaRoot, "src", "WTLauncher.App", "ViewModels");
    const files = ctx.walkFiles(viewModelsDir).filter((file) => path.basename(file).startsWith("MainWindowViewModel"));
    // 扫描集为空时 lines 自然是 0,而 0 永远不超预算——检查会以"零违规"静默通过。
    // 名称过滤条件是本检查仅有的输入,它一旦失效(改名/移目录),这条预算就没人守了。
    if (files.length === 0) {
      throw new Error("main-vm-line-budget scanned 0 MainWindowViewModel partials: scan target or name filter is broken");
    }
    const lines = files.reduce((total, file) => total + ctx.readText(file).split(/\r?\n/).length, 0);
    if (lines > 1200) {
      throw new Error(`main-vm-line-budget: MainWindowViewModel*.cs total ${lines} exceeds 1200`);
    }
    return {
      lines,
      scannedFiles: files.length,
      budget: 1200,
      path: "src\\avalonia-launcher\\src\\WTLauncher.App\\ViewModels\\MainWindowViewModel*.cs",
    };
  },
});

export const workflowNoCallbackBagCheck: Check = defineCheck({
  id: "workflow-no-callback-bag",
  tier: "static",
  run: (ctx) => {
    const workflowDir = ctx.path(avaloniaRoot, "src", "WTLauncher.App", "ViewModels", "Commands");
    const violations: string[] = [];
    const ctorRegex = /public\s+Launcher\w+Workflow\s*\((?<params>(?:[^()]|\([^()]*\))*)\)/g;
    // 自检:构造器匹配器依赖 "Launcher*Workflow" 命名约定。约定漂移会让它从此
    // 一个构造器都匹配不到,而每个文件都"零违规"通过——正是这条检查最该防的死法。
    // 自检用非全局副本:test() 会推进全局正则的 lastIndex,影响后续 matchAll 的起点。
    ctx.assertMatcherDetects(
      new RegExp(ctorRegex.source),
      "public LauncherSaveWorkflow(MainWindowViewModel owner)",
      "workflow-no-callback-bag",
    );
    const workflowFiles = ctx.walkFiles(workflowDir).filter((file) => file.endsWith(".cs"));
    if (workflowFiles.length === 0) {
      throw new Error("workflow-no-callback-bag scanned 0 workflow files: scan target is broken");
    }
    for (const filePath of workflowFiles) {
      const source = ctx.readText(filePath);
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
    return {
      scannedFiles: workflowFiles.length,
      violations: violations.length,
      invariant: "Workflow ctors use narrow ports, not MainWindowViewModel or Func/Action callback bags",
    };
  },
});

export const settingsSingleSourceCheck: Check = defineCheck({
  id: "settings-single-source",
  tier: "static",
  run: (ctx) => {
    const source = ctx.readText("src/avalonia-launcher/src/WTLauncher.App/ViewModels/MainWindowViewModel.cs");
    const signatureCount = ctx.countMatches(
      source,
      /Entries = _list\.MutableEntries\.Select\(ToLaunchEntry\)\.ToImmutableList\(\),\s*\r?\n\s+Settings =/g,
    );
    if (signatureCount !== 1) {
      throw new Error(
        `MainWindowViewModel.cs should have exactly 1 LauncherStoreFile hand-assembly site (BuildStoreFile); found ${signatureCount}. SaveSettingsToStore must call BuildStoreFile, not hand-assemble. See task 07-09-settings-single-source R1.`,
      );
    }
    return { signatureCount };
  },
});

export const wingetConfigSchemaCheck: Check = defineCheck({
  id: "winget-config-schema",
  tier: "static",
  run: (ctx) => {
    const text = ctx.readText(".wt-protector.dsc.yaml");
    ctx.containsRequiredMarkers(
      text,
      ["configuration-dsc-schema/0.2", "configurationVersion: 0.2.0", "id: Microsoft.WindowsTerminal", "id: Microsoft.PowerShell"],
      ".wt-protector.dsc.yaml",
    );
    return {
      path: ".wt-protector.dsc.yaml",
      requiredMarkers: ["schema-0.2", "configVersion-0.2.0", "WindowsTerminal", "PowerShell"],
    };
  },
});

export const winformsRetiredDesignCheck: Check = defineCheck({
  id: "winforms-retired-design",
  tier: "static",
  run: (ctx) => {
    const text = ctx.readText("retired/design/winforms-launcher.md");
    ctx.containsRequiredMarkers(
      text,
      ["Retired WinForms launcher design", "Portable config discovery", "Launch safety boundary", "Successor directive"],
      "WinForms retirement design",
    );
    return { path: "retired\\design\\winforms-launcher.md" };
  },
});

export const htaRetiredConfigCheck: Check = defineCheck({
  id: "hta-retired-config",
  tier: "static",
  run: (ctx) => {
    const items = ctx.readConfigItems("retired/config/config.json");
    return {
      path: "retired\\config\\config.json",
      items: items.length,
      hash: ctx.shortHash(ctx.repoPath("retired/config/config.json")),
      activeSource: false,
    };
  },
});

export const portableInstallerContractCheck: Check = defineCheck({
  id: "portable-installer-contract",
  tier: "static",
  run: (ctx) => {
    const text = ctx.readText("scripts/install-portable.ts");
    ctx.containsRequiredMarkers(
      text,
      ["WTLauncher.App.exe", "uninstaller.exe", "config", "createShortcut"],
      "Portable installer",
    );
    return {
      path: "scripts\\install-portable.ts",
      preservesPortableConfig: true,
      uninstaller: "uninstaller.exe",
    };
  },
});

export const updateManifestGenerationCheck: Check = defineCheck({
  id: "update-manifest-generation",
  tier: "static",
  run: (ctx) => {
    const text = ctx.readText("scripts/build-release.ts");
    if (!text.includes("update-manifest.json")) {
      throw new Error("scripts/build-release.ts must emit update-manifest.json");
    }
    return { path: "scripts\\build-release.ts", manifest: "update-manifest.json" };
  },
});

export const testStructureLayeringCheck: Check = defineCheck({
  id: "test-structure-layering",
  tier: "static",
  run: (ctx) => {
    const testRoot = ctx.path(avaloniaRoot, "tests", "WTLauncher.Tests");
    const layers = ["Support", "CoreUnit", "Persistence", "ViewModelBehavior", "Analyzer", "UiContract", "PropertyTest"];
    for (const layer of layers) {
      const layerDir = ctx.path(testRoot, layer);
      if (!ctx.exists(layerDir)) {
        throw new Error(`Missing test layer directory: ${layer}`);
      }
      const hasTests = ctx.walkFiles(layerDir).some((file) => file.endsWith(".cs"));
      if (!hasTests) {
        throw new Error(`Empty test layer directory: ${layer}`);
      }
    }
    return { layers: layers.join(",") };
  },
});

export const aotToolchainOwnedBySdkCheck: Check = defineCheck({
  id: "aot-toolchain-owned-by-sdk",
  tier: "static",
  run: (ctx) => {
    const csproj = ctx.readText("src/avalonia-launcher/src/WTLauncher.App/WTLauncher.App.csproj");
    const props = ctx.readText("src/avalonia-launcher/Directory.Packages.props");
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
    return {
      csprojExplicit: false,
      propsExplicit: false,
      invariant: ".NET SDK owns Native AOT compiler and linker selection",
    };
  },
});

export const noActivePowerShellScriptsCheck: Check = defineCheck({
  id: "no-active-powershell-scripts",
  tier: "static",
  run: (ctx) => {
    const scanned = ctx.walkFilesExcluding(repoRoot, ["node_modules", ".git", ".snow", "artifacts", "retired", "archive"]);
    // 期望值本身就是 0,因此结果无法区分"仓里真的没有 active .ps1"与"扫描/排除规则失效"。
    // 只有"扫到了若干文件"这个事实才能证明这次扫描确实发生过。
    if (scanned.length === 0) {
      throw new Error("no-active-powershell-scripts scanned 0 repository files: scan target or exclusion list is broken");
    }
    const active = scanned.filter((file) => file.toLowerCase().endsWith(".ps1"));
    if (active.length > 0) {
      throw new Error(`Active PowerShell scripts must be zero; found: ${active.map((file) => relDisplay(ctx, file)).join(", ")}`);
    }
    return {
      scannedFiles: scanned.length,
      activePowerShellScripts: active.length,
    };
  },
});

export const ciFastLaneNoPublishDuplicationCheck: Check = defineCheck({
  id: "ci-fast-lane-no-publish-duplication",
  tier: "static",
  run: (ctx) => {
    const workflow = ctx.readText(".github/workflows/verify.yml");
    const required = ["name: CI", "npm run verify"];
    for (const marker of required) {
      if (!workflow.includes(marker)) {
        throw new Error(`CI workflow must include: ${marker}`);
      }
    }
    // CI 是一个 job，verify 是唯一门禁命令。release:contract 属于发布流程不在 CI 里。
    // 拆成多 job 或用 always() 分支会引入重复 Native AOT。
    if (workflow.includes("needs:")) {
      throw new Error("CI workflow must be a single sequential job with no needs graph");
    }
    if (workflow.includes("if: always()")) {
      throw new Error("CI workflow must not use always() upload branching");
    }
    // 以下四项是 2026-10 加固的 CI 不变量。它们此前只存在于 review 记忆里:
    // 浮动标签、无权限收敛、无并发组、无超时,任何一次编辑都能悄悄退回原状。
    const permissionsPattern = /^permissions:\n(?: {2}\S.*\n)* {2}contents: read$/m;
    const timeoutPattern = /^ {4}timeout-minutes: \d+$/m;
    const tagsPattern = /^ {4}tags: \["v\*"\]$/m;
    const hardened: Array<{ test: boolean; message: string }> = [
      {
        test: permissionsPattern.test(workflow),
        message: "CI workflow must declare least-privilege `permissions: contents: read`",
      },
      {
        test: /^concurrency:\n/m.test(workflow) && workflow.includes("cancel-in-progress:"),
        message: "CI workflow must declare a concurrency group so superseded runs are bounded",
      },
      {
        test: timeoutPattern.test(workflow),
        message: "CI job must declare timeout-minutes so a hung step cannot burn the default budget",
      },
      {
        test: tagsPattern.test(workflow),
        message: 'CI must also run on `tags: ["v*"]` so the tag→artifact binding is machine-verified',
      },
    ];
    // 匹配器自检:这些断言检查的是"某项声明存在",若正则写错就会永久放行。
    // 先证明每条正则可以命中一个最小合规样本,再拿它去判真实 workflow。
    ctx.assertMatcherDetects(permissionsPattern, "permissions:\n  contents: read", "CI permissions");
    ctx.assertMatcherDetects(timeoutPattern, "    timeout-minutes: 30", "CI job timeout");
    ctx.assertMatcherDetects(tagsPattern, '    tags: ["v*"]', "CI tag trigger");
    for (const { test, message } of hardened) {
      if (!test) {
        throw new Error(message);
      }
    }
    // action 必须钉 commit SHA:浮动大版本标签可被上游移动,同一份 workflow 会解析到不同代码。
    // 覆盖整个 .github/workflows 目录,而不只是 verify.yml——dependabot-lockfile.yml 持有
    // contents: write,是全仓权限最大的 workflow,更不能让它引用可被移动的标签。
    const workflowFiles = ctx.walkFiles(ctx.repoPath(".github/workflows")).filter(
      (file) => file.toLowerCase().endsWith(".yml") || file.toLowerCase().endsWith(".yaml"),
    );
    if (workflowFiles.length === 0) {
      throw new Error("CI hygiene: found no workflow files to verify action pinning");
    }
    let usesCount = 0;
    for (const file of workflowFiles) {
      const label = file.replaceAll("\\", "/").split("/.github/")[1] ?? file;
      for (const rawLine of ctx.readText(file).split("\n")) {
        if (!rawLine.trimStart().startsWith("- uses:")) {
          continue;
        }
        usesCount += 1;
        if (!/@[0-9a-f]{40}\b/.test(rawLine)) {
          throw new Error(`Workflow action must be pinned to a 40-char commit SHA (${label}): ${rawLine.trim()}`);
        }
      }
    }
    if (usesCount === 0) {
      throw new Error("CI hygiene: found no `uses:` lines; scan target or matcher is broken");
    }
    return {
      jobs: 1,
      gateCommand: "npm run verify",
      pinnedActions: usesCount,
      workflowFiles: workflowFiles.length,
      hardenedInvariants: hardened.length,
    };
  },
});
