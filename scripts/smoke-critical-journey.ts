import { existsSync } from "node:fs";
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

export async function runSmoke(options: SmokeOptions): Promise<SmokeReceipt> {
  if (!existsSync(options.exe)) {
    throw new Error(`Published exe missing: ${options.exe}`);
  }
  if (process.platform !== "win32") {
    return {
      smoke: "critical-journey",
      exe: options.exe,
      config: options.configPath ?? "",
      checks: [],
    };
  }

  const psScript = path.join(repoRoot, "tools", "smoke-critical-journey.ps1");
  if (!existsSync(psScript)) {
    throw new Error(`Critical journey smoke script missing: ${psScript}`);
  }

  const args = [
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    psScript,
    "-Exe",
    options.exe,
  ];
  if (options.configPath) {
    args.push("-ConfigPath", options.configPath);
  }
  if (options.workingDirectory) {
    args.push("-WorkingDirectory", options.workingDirectory);
  }

  const result = await execa("powershell", args, { reject: false });
  if (result.exitCode !== 0) {
    throw new Error(`Critical journey smoke failed: ${result.stderr || result.stdout}`);
  }
  const receipt = JSON.parse(result.stdout) as SmokeReceipt;
  if (!Array.isArray(receipt.checks)) {
    throw new Error("Critical journey smoke produced no checks");
  }
  return receipt;
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
