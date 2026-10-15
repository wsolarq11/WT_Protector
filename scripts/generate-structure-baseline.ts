import path from "node:path";
import { writeFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { repoRoot } from "./config-lib.js";
import { type Ctx, createCtx } from "./kernel/check.js";
import { createRealWorld, type World } from "./kernel/world.js";
import { gateChecks } from "./registry.js";

// 结构基线是派生数据：文件数、测试属性数、门禁步数都能从文件系统与注册表
// 算出来。手工维护这些数字再用门禁锁死，是"派生数据当权威源"的反模式。
// 本脚本是唯一权威：AGENTS.md 只保留生成结果，漂移在物理上不可能发生。

interface BaselineRow {
  label: string;
  path: string;
  filter: string;
}

const baselineRows: BaselineRow[] = [
  { label: "WTLauncher.Core C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.Core", filter: "*.cs" },
  { label: "ViewModels C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\ViewModels", filter: "*.cs" },
  { label: "MainWindowViewModel partials", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\ViewModels", filter: "MainWindowViewModel.*.cs" },
  { label: "Features C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Features", filter: "*.cs" },
  { label: "Views C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Views", filter: "*.cs" },
  { label: "Views AXAML files", path: "src\\avalonia-launcher\\src\\WTLauncher.App\\Views", filter: "*.axaml" },
  { label: "WTLauncher.Analyzers C# files", path: "src\\avalonia-launcher\\src\\WTLauncher.Analyzers", filter: "*.cs" },
  { label: "Tests C# files", path: "src\\avalonia-launcher\\tests\\WTLauncher.Tests", filter: "*.cs" },
];

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

function isGeneratedArtifact(file: string): boolean {
  const normalized = file.replaceAll("\\", "/");
  return normalized.includes("/obj/") || normalized.includes("/bin/") || normalized.includes("/TestResults/");
}

function countFiles(ctx: Ctx, row: BaselineRow): number {
  return ctx
    .walkFiles(ctx.path(row.path))
    .filter((file) => matchesFilter(file, row.filter) && !isGeneratedArtifact(file)).length;
}

function countTestAttributes(ctx: Ctx): number {
  const testDir = ctx.path("src", "avalonia-launcher", "tests", "WTLauncher.Tests");
  let factCount = 0;
  let theoryCount = 0;
  for (const file of ctx.walkFiles(testDir)) {
    if (!file.endsWith(".cs") || isGeneratedArtifact(file)) {
      continue;
    }
    const text = ctx.readText(file);
    factCount += ctx.countMatches(text, /\[Fact/g);
    theoryCount += ctx.countMatches(text, /\[Theory/g);
  }
  return factCount + theoryCount;
}

export function generateStructureBaseline(world: World = createRealWorld(repoRoot)): string {
  const ctx = createCtx(world);
  const lines = ["| 基线项 | 数量 |", "|--------|------|"];
  for (const row of baselineRows) {
    lines.push(`| ${row.label} | ${countFiles(ctx, row)} |`);
  }
  lines.push(`| Test attributes [Fact]+[Theory] | ${countTestAttributes(ctx)} |`);
  lines.push(`| Gate checks (registry.ts) | ${gateChecks.length} |`);
  return lines.join("\n");
}

export const structureBaselineStartMarker = "<!-- DERIVED:structure-baseline:START -->";
export const structureBaselineEndMarker = "<!-- DERIVED:structure-baseline:END -->";

export function updateAgentsFile(): void {
  const agentsPath = path.join(repoRoot, "AGENTS.md");
  const content = createCtx(createRealWorld(repoRoot)).readText(agentsPath);
  const startIdx = content.indexOf(structureBaselineStartMarker);
  const endIdx = content.indexOf(structureBaselineEndMarker);
  if (startIdx === -1 || endIdx === -1 || endIdx <= startIdx) {
    throw new Error("AGENTS.md structure-baseline DERIVED markers missing");
  }
  const block = `${structureBaselineStartMarker}\n${generateStructureBaseline()}\n${structureBaselineEndMarker}`;
  const updated = content.slice(0, startIdx) + block + content.slice(endIdx + structureBaselineEndMarker.length);
  writeFileSync(agentsPath, updated, "utf8");
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  updateAgentsFile();
  console.log("Updated AGENTS.md structure-baseline derived block.");
}
