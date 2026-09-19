import { createHash } from "node:crypto";
import { existsSync, readFileSync, statSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execa } from "execa";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
export const repoRoot = path.resolve(scriptDir, "..");
export const avaloniaRoot = path.join(repoRoot, "src", "avalonia-launcher");
export const solutionPath = path.join(avaloniaRoot, "WTLauncher.Avalonia.sln");
export const appProjectPath = path.join(avaloniaRoot, "src", "WTLauncher.App", "WTLauncher.App.csproj");
export const testsProjectPath = path.join(avaloniaRoot, "tests", "WTLauncher.Tests", "WTLauncher.Tests.csproj");
export const publishDir = path.join(avaloniaRoot, "artifacts", "publish", "win-x64");
export const launcherStorePath = path.join(repoRoot, "config", "launcher.json");

export function pathExists(filePath: string): boolean {
  return existsSync(filePath);
}

export function normalizePath(filePath: string): string {
  return path.resolve(filePath);
}

export function readJson<T>(filePath: string): T {
  const raw = readFileSync(filePath, "utf8").replace(/^\uFEFF/, "");
  const parsed = JSON.parse(raw) as unknown;
  if (parsed === null || parsed === undefined) {
    throw new Error(`JSON parsed to null: ${filePath}`);
  }
  return parsed as T;
}

export function writeJsonNoBom(filePath: string, value: unknown): void {
  writeFileSync(filePath, JSON.stringify(value), "utf8");
}

export function sha256File(filePath: string): string {
  return createHash("sha256").update(readFileSync(filePath)).digest("hex").toUpperCase();
}

export function shortSha256(filePath: string): string {
  return sha256File(filePath).slice(0, 16);
}

export function fileBytes(filePath: string): number {
  return statSync(filePath).size;
}

function pad2(value: number): string {
  return String(value).padStart(2, "0");
}

export function lastWriteTimeString(filePath: string): string {
  const stat = statSync(filePath);
  const value = new Date(stat.mtime);
  return [
    `${value.getFullYear()}-${pad2(value.getMonth() + 1)}-${pad2(value.getDate())}`,
    `${pad2(value.getHours())}:${pad2(value.getMinutes())}:${pad2(value.getSeconds())}`,
  ].join(" ");
}

export async function runStep<T>(name: string, fn: () => Promise<T>): Promise<T> {
  try {
    return await fn();
  } catch (error) {
    const detail = error instanceof Error ? error.message : String(error);
    throw new Error(`${name} failed: ${detail}`);
  }
}

export async function gitHead(repo = repoRoot): Promise<string> {
  const result = await execa("git", ["-C", repo, "rev-parse", "--short", "HEAD"], {
    reject: false,
  });
  if (result.exitCode !== 0) {
    return "";
  }
  return result.stdout.trim();
}

export async function gitStatus(repo = repoRoot, untrackedAll = false): Promise<string[]> {
  const args = ["-C", repo, "status", "--short"];
  if (untrackedAll) {
    args.push("--untracked-files=all");
  }
  const result = await execa("git", args, { reject: false });
  if (result.exitCode !== 0) {
    throw new Error("git status failed");
  }
  return result.stdout
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0);
}

export interface ToolReceipt {
  schemaVersion: 1;
  kind: string;
  status: "PASS" | "FAIL";
  generatedAtUtc: string;
  repoHead: string;
  worktreeDirty: boolean;
  worktreeStatus: string[];
  [key: string]: unknown;
}

/// <summary>凭证结论必须能表达失败。此前 status 硬编码为 "PASS",凭证在失败时
/// 依然宣称通过,下游只能靠进程退出码判断,凭证本身不可信。</summary>
export async function makeReceipt(
  kind: string,
  fields: Record<string, unknown>,
  outcome: "PASS" | "FAIL" = "PASS",
): Promise<ToolReceipt> {
  const head = await gitHead();
  const worktreeStatus = await gitStatus();
  const receipt: ToolReceipt = {
    schemaVersion: 1,
    kind,
    status: outcome,
    generatedAtUtc: new Date().toISOString(),
    repoHead: head,
    worktreeDirty: worktreeStatus.length > 0,
    worktreeStatus,
  };
  for (const [key, value] of Object.entries(fields)) {
    if (key in receipt) {
      throw new Error(`Receipt field conflicts with built-in field: ${key}`);
    }
    receipt[key] = value;
  }
  return receipt;
}

export function assertRequiredField(object: Record<string, unknown>, field: string, context: string): void {
  const value = object[field];
  if (value === undefined || value === null || String(value).trim() === "") {
    throw new Error(`${context} missing required field: ${field}`);
  }
}

export async function runningReleasePids(exePath: string): Promise<number[]> {
  const script = [
    "$processes = @(Get-Process -Name 'WTLauncher.App' -ErrorAction SilentlyContinue | Select-Object Path, Id);",
    "if ($processes.Count -eq 0) { '[]' } else { $processes | ConvertTo-Json -Compress }",
  ].join(" ");
  const result = await execa("pwsh", ["-NoProfile", "-Command", script], { reject: false });
  if (result.exitCode !== 0) {
    throw new Error(`process query failed: ${result.stderr || result.stdout}`);
  }

  let items: unknown;
  try {
    items = JSON.parse(result.stdout.trim() || "[]");
  } catch {
    throw new Error(`process query returned invalid JSON: ${result.stdout}`);
  }

  const target = normalizePath(exePath).toLowerCase();
  const records = Array.isArray(items) ? items : [items];
  const pids: number[] = [];
  for (const record of records) {
    if (!record || typeof record !== "object") {
      continue;
    }
    const processPath = (record as Record<string, unknown>)["Path"];
    const pid = Number((record as Record<string, unknown>)["Id"]);
    if (typeof processPath === "string" && Number.isFinite(pid) && normalizePath(processPath).toLowerCase() === target) {
      pids.push(pid);
    }
  }
  return pids;
}
