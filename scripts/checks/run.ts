import path from "node:path";
import { repoRoot, solutionPath } from "../config-lib.js";
import { restoreSolutionNativeAot } from "../publish-lib.js";
import { runVerifyMainline } from "../verify-mainline.js";
import { runCoverageGate } from "../test-coverage.js";
import {
  addCheck,
  checks,
  countMatches,
  execChecked,
  readText,
  walkFiles,
} from "./common.js";

export async function checkReleaseLockfileConsistency(): Promise<void> {
  await restoreSolutionNativeAot(true);
  addCheck("release-lockfile-consistency", { lockedMode: true, aotMarker: "EnableNativeAot" });
}

export async function checkAvaloniaMainlineVerify(): Promise<void> {
  const mainline = await runVerifyMainline();
  const requiredFields = ["schemaVersion", "kind", "status", "repoHead", "worktreeDirty", "worktreeStatus", "nativeAot"];
  for (const field of requiredFields) {
    if (!(field in mainline)) {
      throw new Error(`Avalonia mainline receipt missing field: ${field}`);
    }
  }
  if (Number(mainline.schemaVersion) !== 1) {
    throw new Error(`Avalonia mainline receipt schemaVersion mismatch: ${mainline.schemaVersion}`);
  }
  if (mainline.kind !== "avalonia-mainline-verify") {
    throw new Error(`Avalonia mainline receipt kind mismatch: ${mainline.kind}`);
  }
  if (String(mainline.repoHead ?? "").trim() === "") {
    throw new Error("Avalonia mainline receipt repoHead is required");
  }
  if (mainline.status !== "PASS") {
    throw new Error("Avalonia mainline verify did not pass");
  }
  if (mainline.nativeAot !== true) {
    throw new Error("Avalonia mainline verify must publish with Native AOT");
  }
  if (mainline.publishedDll !== null) {
    throw new Error("Avalonia mainline Native AOT receipt must not claim a managed publishedDll");
  }
  addCheck("avalonia-mainline-verify", {
    schemaVersion: mainline.schemaVersion,
    kind: mainline.kind,
    status: mainline.status,
    repoHead: mainline.repoHead,
    worktreeDirty: mainline.worktreeDirty,
    nativeAot: mainline.nativeAot,
    tests: mainline.tests,
    guiSmoke: mainline.guiSmoke,
    publishedExe: mainline.publishedExe,
    publishedDll: mainline.publishedDll,
    sha256: mainline.sha256,
  });
}

export async function checkCoverageGate(): Promise<void> {
  const coverage = await runCoverageGate(true);
  addCheck("coverage-gate", {
    status: coverage.status,
    criticalPaths: coverage.criticalPaths.length,
  });
}

export async function checkArchitectureRuleCheck(): Promise<void> {
  await execChecked("Architecture rule tests", "dotnet", [
    "test",
    solutionPath,
    "-c",
    "Release",
    "--filter",
    "FullyQualifiedName~ArchitectureRules",
    "--no-build",
  ]);
  addCheck("architecture-rule-check", { pass: true });
}

export async function checkAnalyzerWiring(): Promise<void> {
  await execChecked("Analyzer wiring build", "dotnet", [
    "build",
    solutionPath,
    "--no-restore",
    "-warnaserror",
  ]);
  addCheck("analyzer-wiring", { pass: true });
}

interface BaselineRow {
  label: string;
  path: string;
  filter: string;
  count: number;
}

function matchesFilter(filePath: string, filter: string): boolean {
  const name = path.basename(filePath);
  if (filter === "*.cs") {
    return name.endsWith(".cs");
  }
  if (filter === "*.axaml") {
    return name.endsWith(".axaml");
  }
  if (filter === "MainWindowViewModel.*.cs") {
    const prefix = "MainWindowViewModel.";
    return name.startsWith(prefix) && name.slice(prefix.length).includes(".") && name.endsWith(".cs");
  }
  return name.includes(filter.replaceAll("*", ""));
}

export function checkStructureBaselineConsistency(): void {
  const baselineRows: BaselineRow[] = [
    { label: "WTLauncher.Core C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.Core", filter: "*.cs", count: 53 },
    { label: "ViewModels C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\ViewModels", filter: "*.cs", count: 65 },
    { label: "MainWindowViewModel partials", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\ViewModels", filter: "MainWindowViewModel.*.cs", count: 15 },
    { label: "Features C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Features", filter: "*.cs", count: 16 },
    { label: "Views C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Views", filter: "*.cs", count: 24 },
    { label: "Views AXAML files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Views", filter: "*.axaml", count: 20 },
    { label: "WTLauncher.Analyzers C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.Analyzers", filter: "*.cs", count: 7 },
    { label: "Tests C# files", path: "src\\avalonia-launcher\\tests\\WTLauncher.Tests", filter: "*.cs", count: 129 },
  ];
  const baselineTestAttributes = 756;
  const violations: string[] = [];

  for (const row of baselineRows) {
    const directory = path.join(repoRoot, row.path);
    const actual = walkFiles(directory).filter((file) => {
      const normalized = file.replaceAll("\\", "/");
      return (
        matchesFilter(file, row.filter) &&
        !normalized.includes("/obj/") &&
        !normalized.includes("/bin/") &&
        !normalized.includes("/TestResults/")
      );
    }).length;
    if (actual !== row.count) {
      violations.push(`${row.label}: baseline ${row.count}, found ${actual}`);
    }
  }

  const testDir = path.join(repoRoot, "src", "avalonia-launcher", "tests", "WTLauncher.Tests");
  let factCount = 0;
  let theoryCount = 0;
  for (const file of walkFiles(testDir)) {
    const normalized = file.replaceAll("\\", "/");
    if (
      !file.endsWith(".cs") ||
      normalized.includes("/obj/") ||
      normalized.includes("/bin/") ||
      normalized.includes("/TestResults/")
    ) {
      continue;
    }
    const text = readText(file);
    factCount += countMatches(text, /\[Fact/g);
    theoryCount += countMatches(text, /\[Theory/g);
  }
  if (factCount + theoryCount !== baselineTestAttributes) {
    violations.push(
      `test attributes: baseline ${baselineTestAttributes}, found ${factCount + theoryCount} (${factCount} Fact + ${theoryCount} Theory)`,
    );
  }

  const agentsBaselineText = readText("AGENTS.md");
  for (const row of baselineRows) {
    if (!agentsBaselineText.includes(`| ${row.label} | ${row.count} |`)) {
      violations.push(`AGENTS.md structure-baseline missing/outdated row: | ${row.label} | ${row.count} |`);
    }
  }
  if (!agentsBaselineText.includes(`| Test attributes [Fact]+[Theory] | ${baselineTestAttributes} |`)) {
    violations.push(`AGENTS.md structure-baseline missing/outdated row: | Test attributes [Fact]+[Theory] | ${baselineTestAttributes} |`);
  }
  const baselineCheckCount = checks.length + 1;
  if (!agentsBaselineText.includes(`| Gate checks (Add-Check) | ${baselineCheckCount} |`)) {
    violations.push(`AGENTS.md structure-baseline missing/outdated row: | Gate checks (Add-Check) | ${baselineCheckCount} |`);
  }
  if (violations.length > 0) {
    throw new Error(
      `Structure baseline drift: ${violations.join("; ")}. Update scripts/verify.ts baseline constants AND the AGENTS.md structure-baseline table together.`,
    );
  }
  addCheck("structure-baseline-consistency", {
    rows: baselineRows.length,
    testAttributes: baselineTestAttributes,
    checks: baselineCheckCount,
    enforcedSource: "AGENTS.md structure-baseline table",
  });
}
