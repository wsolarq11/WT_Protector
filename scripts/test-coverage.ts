import { mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { execa } from "execa";
import { XMLParser } from "fast-xml-parser";
import { repoRoot, solutionPath } from "./config-lib.js";

const coverageDir = path.join(repoRoot, "src", "avalonia-launcher", "artifacts", "coverage");
const manifestPath = path.join(repoRoot, "tools", "coverage-gate.json");

function asArray<T>(value: T | T[] | undefined): T[] {
  if (value === undefined || value === null) {
    return [];
  }
  return Array.isArray(value) ? value : [value];
}

export interface CoverageClass {
  className: string;
  lineRate: number;
}

export function parseCoverageClasses(xml: string): CoverageClass[] {
  const parser = new XMLParser({ ignoreAttributes: false, attributeNamePrefix: "@_" });
  const parsed = parser.parse(xml) as Record<string, unknown>;
  const coverage = parsed.coverage as Record<string, unknown> | undefined;
  const packages = asArray(
    (coverage?.packages as Record<string, unknown> | undefined)?.package,
  ) as unknown as Array<Record<string, unknown>>;
  const classes: CoverageClass[] = [];
  for (const pkg of packages) {
    const classNodes = asArray(
      (pkg.classes as Record<string, unknown> | undefined)?.class,
    ) as unknown as Array<Record<string, unknown>>;
    for (const classNode of classNodes) {
      const className = classNode["@_name"];
      const lineRate = classNode["@_line-rate"];
      if (typeof className === "string" && lineRate !== undefined && lineRate !== null) {
        classes.push({ className, lineRate: Number(lineRate) });
      }
    }
  }
  return classes;
}

export interface CoverageManifestEntry {
  className: string;
  minLineRate: number;
  rationale?: string;
}

export interface CoverageManifest {
  filter: string;
  criticalPaths: CoverageManifestEntry[];
}

function newestCoverageFile(directory: string): string {
  const entries = readdirSync(directory, { recursive: true }) as string[];
  const xmlFiles = entries
    .filter((entry) => typeof entry === "string" && entry.toLowerCase().endsWith(".xml"))
    .map((entry) => path.join(directory, entry));
  if (xmlFiles.length === 0) {
    throw new Error("Coverage collector produced no coverage XML");
  }
  return xmlFiles.sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs)[0];
}

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      "no-build": { type: "boolean", default: false },
    },
  });

  const manifest = JSON.parse(readFileSync(manifestPath, "utf8")) as CoverageManifest;
  rmSync(coverageDir, { recursive: true, force: true });
  mkdirSync(coverageDir, { recursive: true });

  const args = [
    "test",
    solutionPath,
    "-c",
    "Release",
    "--no-restore",
    "--filter",
    manifest.filter,
    "--collect",
    "XPlat Code Coverage",
    "--results-directory",
    coverageDir,
  ];
  if (values["no-build"]) {
    args.push("--no-build");
  }
  const result = await execa("dotnet", args, { stdio: "inherit", reject: false });
  if (result.exitCode !== 0) {
    process.exitCode = result.exitCode ?? 1;
    return;
  }

  const coverageFile = newestCoverageFile(coverageDir);
  const classes = parseCoverageClasses(readFileSync(coverageFile, "utf8"));
  const reported: CoverageClass[] = [];
  const failures: string[] = [];

  for (const entry of manifest.criticalPaths) {
    const match = classes.find((item) => item.className === entry.className);
    if (!match) {
      failures.push(`${entry.className}: class not found in coverage report`);
      continue;
    }
    reported.push(match);
    if (match.lineRate < entry.minLineRate) {
      failures.push(`${entry.className}: ${match.lineRate.toFixed(3)} below ${entry.minLineRate}`);
    }
  }

  if (failures.length > 0) {
    for (const failure of failures) {
      process.stderr.write(`${failure}\n`);
    }
    process.exitCode = 1;
    return;
  }

  const receipt = {
    status: "PASS",
    report: coverageFile,
    criticalPaths: reported,
  };
  const receiptPath = path.join(coverageDir, "coverage-gate-receipt.json");
  writeFileSync(receiptPath, JSON.stringify(receipt), "utf8");
  process.stdout.write(`${receiptPath}\n`);
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
