import { mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execa } from "execa";
import { XMLParser } from "fast-xml-parser";
import {
  avaloniaRoot,
  fileBytes,
  makeReceipt,
  pathExists,
  runStep,
  sha256File,
  shortSha256,
  testsProjectPath,
  type ToolReceipt,
} from "./config-lib.js";
import { restoreSolutionNativeAot, runNativeAotPublish } from "./publish-lib.js";
import { runSmoke, writeSmokeConfig } from "./smoke-critical-journey.js";

const verifyPublishDir = path.join(avaloniaRoot, "artifacts", "verify-publish", "win-x64");
const testResultsDir = path.join(avaloniaRoot, "artifacts", "test-results");
const testResultsFile = path.join(testResultsDir, "wtlauncher-tests.trx");
const smokeRoot = path.join(avaloniaRoot, "artifacts", "gui-smoke");

export interface TrxCounters {
  total: number;
  passed: number;
  failed: number;
}

export function parseTrxCounters(xml: string): TrxCounters {
  const parser = new XMLParser({ ignoreAttributes: false, attributeNamePrefix: "@_" });
  const parsed = parser.parse(xml) as Record<string, unknown>;
  const testRun = parsed.TestRun as Record<string, unknown> | undefined;
  const resultSummary = testRun?.ResultSummary as Record<string, unknown> | undefined;
  const counters = resultSummary?.Counters as Record<string, unknown> | undefined;
  if (!counters) {
    throw new Error("TRX ResultSummary.Counters missing");
  }
  return {
    total: Number(counters["@_total"] ?? 0),
    passed: Number(counters["@_passed"] ?? 0),
    failed: Number(counters["@_failed"] ?? 0),
  };
}

async function runDotnet(step: string, args: string[]): Promise<void> {
  await runStep(step, async () => {
    const result = await execa("dotnet", args, { stdio: "inherit", reject: false });
    if (result.exitCode !== 0) {
      throw new Error(`exit code ${result.exitCode}`);
    }
  });
}

function contractJsonRequiredCount(coreRoot: string): number {
  const requiredFields = new Set<string>();
  const files = readdirSync(coreRoot, { recursive: true }) as string[];
  const pattern = /\[JsonPropertyName\("(\w+)"\)\]\s*\[JsonRequired\]\s*public \S+ (\w+)/g;
  for (const file of files) {
    const normalized = file.replaceAll("\\", "/");
    if (
      !file.toLowerCase().endsWith(".cs") ||
      file.includes("AssemblyInfo") ||
      normalized.includes("/obj/") ||
      normalized.includes("/bin/")
    ) {
      continue;
    }
    const content = readFileSync(path.join(coreRoot, file), "utf8");
    if (!/public\s+(?:\w+\s+)*record\s+\w+/.test(content)) {
      continue;
    }
    const matches = content.matchAll(pattern);
    for (const match of matches) {
      requiredFields.add(match[1]);
    }
  }
  return requiredFields.size;
}

function contractSaveDiscards(viewModelsRoot: string): string[] {
  const files = readdirSync(viewModelsRoot, { recursive: true }) as string[];
  const violations: string[] = [];
  for (const file of files) {
    if (!file.toLowerCase().endsWith(".cs")) {
      continue;
    }
    const filePath = path.join(viewModelsRoot, file);
    const lines = readFileSync(filePath, "utf8").split(/\r?\n/);
    for (const line of lines) {
      const directDiscard = /^\s+Save\w+\(\)/.test(line);
      const indirectGuard = /^\s+if\s*\([^)]*\.Save\w+\(\)/.test(line);
      const containsSave = /\.Save\w+\(\)/.test(line);
      const lambda = /=>/.test(line);
      if (directDiscard || (containsSave && !indirectGuard && !lambda)) {
        violations.push(`${file}:${line.trim()}`);
      }
    }
  }
  return violations;
}

function contractTimeSpanBare(viewsRoot: string): string[] {
  const files = readdirSync(viewsRoot, { recursive: true }) as string[];
  const violations: string[] = [];
  const pattern = /(MinimumPopulateDelay|Duration)="\d+"/;
  for (const file of files) {
    if (!file.toLowerCase().endsWith(".axaml")) {
      continue;
    }
    const filePath = path.join(viewsRoot, file);
    const lines = readFileSync(filePath, "utf8").split(/\r?\n/);
    for (const line of lines) {
      if (pattern.test(line)) {
        violations.push(`${file}:${line.trim()}`);
      }
    }
  }
  return violations;
}

export interface SmokeOutcome {
  result: string;
  attempts: number;
}

/** 关键旅程冒烟的实测结果。CI 的 windows runner 有桌面会话,能拿到 MainWindowHandle,
 * 因此这里不按 CI 跳过:跳过会把"没跑"伪装成"通过",而调用点是阻塞的。
 * 配置 fixture 与重试策略都来自 smoke-critical-journey.ts——配置不再在本文件复制一份,
 * 重试也不再由调用方各自决定。 */
async function processSmoke(exePath: string): Promise<SmokeOutcome> {
  if (process.platform !== "win32") {
    return { result: "skipped-non-windows", attempts: 0 };
  }
  rmSync(smokeRoot, { recursive: true, force: true });
  const smokeStore = path.join(smokeRoot, "config", "launcher.json");
  writeSmokeConfig(smokeStore);
  const receipt = await runSmoke({
    exe: exePath,
    configPath: smokeStore,
    workingDirectory: smokeRoot,
    maxAttempts: 2,
  });
  const checks = receipt.checks.map((check) => check.name).join("+");
  if (checks !== "launch+title") {
    throw new Error(`Critical journey smoke reported unexpected checks: ${checks}`);
  }
  return { result: checks, attempts: receipt.attempts };
}

export async function runVerifyMainline(): Promise<ToolReceipt> {
  await restoreSolutionNativeAot(true);

  mkdirSync(testResultsDir, { recursive: true });
  rmSync(testResultsFile, { force: true });

  const testStarted = Date.now();
  await runDotnet("dotnet test", [
    "test",
    testsProjectPath,
    "-c",
    "Release",
    "--no-restore",
    "--verbosity",
    "minimal",
    "--logger",
    "trx;LogFileName=wtlauncher-tests.trx",
    "--results-directory",
    testResultsDir,
  ]);
  const testSeconds = Math.round(((Date.now() - testStarted) / 1000) * 100) / 100;

  const publishStarted = Date.now();
  await runNativeAotPublish({ outputDir: verifyPublishDir });
  const publishSeconds = Math.round(((Date.now() - publishStarted) / 1000) * 100) / 100;

  const exePath = path.join(verifyPublishDir, "WTLauncher.App.exe");
  if (!pathExists(exePath)) {
    throw new Error(`Published exe missing: ${exePath}`);
  }
  if (!pathExists(testResultsFile)) {
    throw new Error(`Test result file missing: ${testResultsFile}`);
  }

  const counters = parseTrxCounters(readFileSync(testResultsFile, "utf8"));
  if (counters.total < 1) {
    throw new Error("No tests were reported in the TRX result");
  }
  if (counters.failed !== 0) {
    throw new Error(`TRX reported failed tests: ${counters.failed}`);
  }

  const jsonRequiredCount = contractJsonRequiredCount(path.join(avaloniaRoot, "src", "WTLauncher.Core"));
  if (jsonRequiredCount === 0) {
    throw new Error("Contract validation: could not extract any [JsonRequired] fields");
  }
  process.stdout.write(`Contract validation: found ${jsonRequiredCount} [JsonRequired] fields across models\n`);

  const saveDiscards = contractSaveDiscards(path.join(avaloniaRoot, "src", "WTLauncher.App", "ViewModels"));
  if (saveDiscards.length > 0) {
    throw new Error(`Contract violation: Save*() return value discarded at: ${saveDiscards.join(", ")}`);
  }
  const timeSpanBare = contractTimeSpanBare(path.join(avaloniaRoot, "src", "WTLauncher.App", "Views"));
  if (timeSpanBare.length > 0) {
    throw new Error(`Contract violation: bare integer in XAML TimeSpan at: ${timeSpanBare.join(", ")}`);
  }

  const smoke = await processSmoke(exePath);

  return makeReceipt("avalonia-mainline-verify", {
    sourceRoot: avaloniaRoot,
    nativeAot: true,
    tests: counters.total,
    guiSmoke: smoke.result,
    // 重试次数是抖动的一手证据。若这里出现 2,说明首次冒烟上窗超时——
    // 状态仍是 PASS,但"这台 runner 不稳"这个事实必须可被审计,不能被重试抹掉。
    guiSmokeAttempts: smoke.attempts,
    publishedExe: exePath,
    publishedDll: null,
    bytes: fileBytes(exePath),
    sha256: shortSha256(exePath),
    perf: { testSeconds, publishSeconds },
  });
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  const receipt = await runVerifyMainline();
  console.log(JSON.stringify(receipt));
}
