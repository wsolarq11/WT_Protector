import { existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { execa } from "execa";
import { normalizePath, repoRoot } from "./config-lib.js";

export interface SmokeOptions {
  exe: string;
  configPath?: string;
  workingDirectory?: string;
  /** 上窗超时的重试次数上限(默认 1 = 不重试)。冷启动 AOT exe 在重载 runner 上
   * 可能首次拿不到 MainWindowHandle;重试上限属冒烟契约本身,而不是某个调用方的私事。 */
  maxAttempts?: number;
}

export interface SmokeReceipt {
  smoke: string;
  exe: string;
  config: string;
  attempts: number;
  checks: Array<{ name: string; pass: boolean; detail?: string }>;
}

/** 冒烟专用的 launcher store fixture。这是"冒烟看到的配置长什么样"的唯一权威源:
 * 此前它在本文件与 verify-mainline.ts 里各有一份逐字节相同的副本,schema 一升级就会
 * 出现两份不同步的真相。 */
export function smokeStoreFixture(): Record<string, unknown> {
  return {
    schemaVersion: 2,
    entries: [{ id: "smoke", name: "smoke", directory: ".", command: "echo smoke", selected: false, updatedAtUtc: "", launchedAtUtc: "" }],
    settings: {
      lastFilterText: "",
      sortMode: "custom",
      entryViewMode: "cards",
      requireConfirmationForLaunches: true,
      runInTray: false,
      customEntryOrder: ["smoke"],
      searchHistory: [],
      commandTemplates: {},
      window: { width: 1180, height: 760, windowState: "Normal" },
    },
    presets: [{ id: "smoke-group", name: "Smoke", entryIds: ["smoke"] }],
    defaultPresetId: "smoke-group",
  };
}

export function writeSmokeConfig(storePath: string): void {
  mkdirSync(path.dirname(storePath), { recursive: true });
  writeFileSync(storePath, JSON.stringify(smokeStoreFixture()), "utf8");
}

async function attemptSmoke(options: SmokeOptions): Promise<SmokeReceipt> {
  const exe = normalizePath(options.exe);
  if (!existsSync(exe)) {
    throw new Error(`Published exe missing: ${exe}`);
  }
  if (process.platform !== "win32") {
    return {
      smoke: "critical-journey",
      exe,
      config: options.configPath ?? "",
      attempts: 0,
      checks: [],
    };
  }

  let configPath = options.configPath;
  let tempRoot: string | undefined;
  if (!configPath) {
    tempRoot = mkdtempSync(path.join(os.tmpdir(), "wt-launcher-e2e-"));
    configPath = path.join(tempRoot, "config", "launcher.json");
    writeSmokeConfig(configPath);
  }
  if (!existsSync(configPath)) {
    throw new Error(`Smoke config missing: ${configPath}`);
  }
  const workingDirectory = options.workingDirectory || path.dirname(exe);
  const probeProject = path.join(repoRoot, "tools", "smoke-probe", "SmokeProbe.csproj");
  if (!existsSync(probeProject)) {
    throw new Error(`Critical journey smoke probe missing: ${probeProject}`);
  }

  try {
    const result = await execa(
      "dotnet",
      ["run", "--project", probeProject, "-c", "Release", "--", "--exe", exe, "--config", configPath, "--working-directory", workingDirectory],
      { reject: false },
    );
    if (result.exitCode !== 0) {
      throw new Error(`Critical journey smoke failed: ${result.stderr || result.stdout}`);
    }
    const receipt = JSON.parse(result.stdout) as SmokeReceipt;
    if (!Array.isArray(receipt.checks)) {
      throw new Error("Critical journey smoke produced no checks");
    }
    return receipt;
  } finally {
    if (tempRoot) {
      rmSync(tempRoot, { recursive: true, force: true });
    }
  }
}

/** 跑关键旅程冒烟,带上窗超时的有界重试。
 *
 * 重试放在这里而不是调用方:此前 verify-mainline.ts 自己写了 2 次重试、
 * build-product.ts 裸调一次也不重试,同一件事在门禁里被容忍、在 `npm run make`
 * 里直接失败。冒烟耐不耐抖属于冒烟契约本身,两个调用方该拿到同一张契约。
 * 实测次数写进 receipt.attempts,让抖动可见而不是被抹平。 */
export async function runSmoke(options: SmokeOptions): Promise<SmokeReceipt> {
  const maxAttempts = Math.max(1, options.maxAttempts ?? 1);
  let lastError: unknown;
  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    try {
      const receipt = await attemptSmoke(options);
      return { ...receipt, attempts: attempt };
    } catch (error) {
      lastError = error;
      const message = error instanceof Error ? error.message : String(error);
      process.stderr.write(`critical journey smoke attempt ${attempt}/${maxAttempts} failed: ${message}\n`);
    }
  }
  throw lastError;
}

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      exe: { type: "string" },
      "config-path": { type: "string", default: "" },
      "working-directory": { type: "string", default: "" },
      "max-attempts": { type: "string", default: "1" },
    },
  });
  if (!values.exe) {
    throw new Error("--exe is required");
  }
  const receipt = await runSmoke({
    exe: normalizePath(values.exe),
    configPath: values["config-path"] || undefined,
    workingDirectory: values["working-directory"] || undefined,
    maxAttempts: Number(values["max-attempts"]),
  });
  console.log(JSON.stringify(receipt));
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
