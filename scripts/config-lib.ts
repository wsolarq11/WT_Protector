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
  const raw = readFileSync(filePath, "utf8");
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
  status: "PASS";
  generatedAtUtc: string;
  repoHead: string;
  worktreeDirty: boolean;
  worktreeStatus: string[];
  [key: string]: unknown;
}

export async function makeReceipt(kind: string, fields: Record<string, unknown>): Promise<ToolReceipt> {
  const head = await gitHead();
  const status = await gitStatus();
  const receipt: ToolReceipt = {
    schemaVersion: 1,
    kind,
    status: "PASS",
    generatedAtUtc: new Date().toISOString(),
    repoHead: head,
    worktreeDirty: status.length > 0,
    worktreeStatus: status,
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
  const result = await execa(
    "wmic",
    ["process", "where", "name='WTLauncher.App.exe'", "get", "ExecutablePath,ProcessId", "/format:csv"],
    { reject: false },
  );
  if (result.exitCode !== 0) {
    throw new Error(`wmic process query failed: ${result.stderr || result.stdout}`);
  }
  const target = normalizePath(exePath).toLowerCase();
  const pids: number[] = [];
  const lines = result.stdout.split(/\r?\n/).filter((line) => line.trim().length > 0);
  for (const line of lines.slice(1)) {
    const parts = line.split(",");
    if (parts.length < 3) {
      continue;
    }
    const processPath = parts[1]?.trim();
    const pid = Number(parts[2]?.trim());
    if (processPath && Number.isFinite(pid) && normalizePath(processPath).toLowerCase() === target) {
      pids.push(pid);
    }
  }
  return pids;
}
