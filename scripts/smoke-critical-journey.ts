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
}

export interface SmokeReceipt {
  smoke: string;
  exe: string;
  config: string;
  checks: Array<{ name: string; pass: boolean; detail?: string }>;
}

function writeSmokeConfig(storePath: string): void {
  mkdirSync(path.dirname(storePath), { recursive: true });
  const store = {
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
  writeFileSync(storePath, JSON.stringify(store), "utf8");
}

export async function runSmoke(options: SmokeOptions): Promise<SmokeReceipt> {
  const exe = normalizePath(options.exe);
  if (!existsSync(exe)) {
    throw new Error(`Published exe missing: ${exe}`);
  }
  if (process.platform !== "win32") {
    return {
      smoke: "critical-journey",
      exe,
      config: options.configPath ?? "",
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

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      exe: { type: "string" },
      "config-path": { type: "string", default: "" },
      "working-directory": { type: "string", default: "" },
    },
  });
  if (!values.exe) {
    throw new Error("--exe is required");
  }
  const receipt = await runSmoke({
    exe: normalizePath(values.exe),
    configPath: values["config-path"] || undefined,
    workingDirectory: values["working-directory"] || undefined,
  });
  console.log(JSON.stringify(receipt));
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
