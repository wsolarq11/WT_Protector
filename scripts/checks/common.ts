import { createHash } from "node:crypto";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";
import { execa, type Options as ExecaOptions } from "execa";
import { readJson, repoRoot } from "../config-lib.js";

export interface GateCheck {
  name: string;
  detail: Record<string, unknown>;
}

export const checks: GateCheck[] = [];

/// <summary>检查累加器。门禁是单次进程执行,但测试需要在同一进程内多次运行,
/// 因此必须显式重置,否则 checks 会跨运行累积并污染数量基线。</summary>
export function resetChecks(): void {
  checks.length = 0;
}

export function addCheck(name: string, detail: Record<string, unknown>): void {
  checks.push({ name, detail });
}

/// <summary>记录一条被跳过的检查。跳过必须留痕:静默跳过会让"本地红/CI 绿"
/// 这类裂脑无法在报告里被发现。</summary>
export function addSkippedCheck(name: string, reason: string): void {
  checks.push({ name, detail: { status: "skipped", reason } });
}

/// <summary>记录一条抛错的检查。失败同样留痕,这样累加器长度与执行清单保持对应,
/// 数量基线不会因为"失败时不记录"而漂移。</summary>
export function addFailedCheck(name: string, error: unknown): void {
  checks.push({
    name,
    detail: {
      status: "failed",
      message: error instanceof Error ? error.message : String(error),
    },
  });
}

export function repoPath(relativePath: string): string {
  return path.join(repoRoot, relativePath);
}

export function requireRepoPath(relativePath: string, context = relativePath): string {
  const fullPath = repoPath(relativePath);
  if (!existsSync(fullPath)) {
    throw new Error(`Missing ${context}: ${fullPath}`);
  }
  return fullPath;
}

export function fileExists(relativePath: string): boolean {
  return existsSync(repoPath(relativePath));
}

export function readText(relativeOrAbsolutePath: string): string {
  const filePath = path.isAbsolute(relativeOrAbsolutePath)
    ? relativeOrAbsolutePath
    : repoPath(relativeOrAbsolutePath);
  return readFileSync(filePath, "utf8");
}

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function asArray<T>(value: T | T[] | undefined | null): T[] {
  if (value === undefined || value === null) {
    return [];
  }
  return Array.isArray(value) ? value : [value];
}

export function assertOnlyJsonKeys(
  object: unknown,
  allowedKeys: string[],
  context: string,
): asserts object is Record<string, unknown> {
  if (!isRecord(object)) {
    throw new Error(`${context} must be a JSON object`);
  }
  const allowed = new Set(allowedKeys);
  for (const key of Object.keys(object)) {
    if (!allowed.has(key)) {
      throw new Error(`${context} has unknown field: ${key}`);
    }
  }
}

export function assertPositiveNumber(value: unknown, context: string): number {
  if (value === undefined || value === null) {
    throw new Error(`${context} is required`);
  }
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) {
    throw new Error(`${context} must be positive`);
  }
  return number;
}

export function countMatches(text: string, pattern: RegExp): number {
  return [...text.matchAll(pattern)].length;
}

export function containsRequiredMarkers(text: string, required: string[], context: string): void {
  for (const marker of required) {
    if (!text.includes(marker)) {
      throw new Error(`${context} missing required marker: ${marker}`);
    }
  }
}

/// <summary>自检(正向):断言"违规不存在"的检查,其匹配器必须先证明能命中一个已知违规样本。
/// 匹配器写错或写得过严时,检查会永久放行——PASS 只说明"没有匹配到",不说明"没有违规"。
/// 把这条自检内建进检查,"匹配器已失效"就变成门禁自身的失败,而不是静默假绿。</summary>
export function assertMatcherDetects(pattern: RegExp, violationSample: string, label: string): void {
  if (!pattern.test(violationSample)) {
    throw new Error(`${label} matcher failed to detect its own violation fixture: ${violationSample}`);
  }
}

/// <summary>自检(反向):匹配器不得命中一个合规样本。防过宽匹配——过宽会把合法代码判成违规,
/// 进而诱导人去削弱或删除这条检查。</summary>
export function assertMatcherAllows(pattern: RegExp, allowedSample: string, label: string): void {
  if (pattern.test(allowedSample)) {
    throw new Error(`${label} matcher matched its own allowed fixture: ${allowedSample}`);
  }
}

export function walkFiles(directory: string): string[] {
  const files: string[] = [];
  const walk = (current: string): void => {
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const fullPath = path.join(current, entry.name);
      if (entry.isDirectory()) {
        walk(fullPath);
      } else if (entry.isFile()) {
        files.push(fullPath);
      }
    }
  };
  walk(directory);
  return files;
}

export function walkFilesExcluding(directory: string, excludedSegments: string[]): string[] {
  const files: string[] = [];
  const excluded = new Set(excludedSegments);
  const walk = (current: string): void => {
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const fullPath = path.join(current, entry.name);
      if (entry.isDirectory()) {
        if (!excluded.has(entry.name)) {
          walk(fullPath);
        }
      } else if (entry.isFile()) {
        files.push(fullPath);
      }
    }
  };
  walk(directory);
  return files;
}

export function shortHashFile(filePath: string): string {
  return createHash("sha256").update(readFileSync(filePath)).digest("hex").toUpperCase().slice(0, 16);
}

export function fileBytes(filePath: string): number {
  return statSync(filePath).size;
}

export function readConfigItems(relativePath: string): Record<string, unknown>[] {
  const filePath = requireRepoPath(relativePath);
  const json = readJson<unknown>(filePath);
  const rawItems = Array.isArray(json) ? json : (json as Record<string, unknown>).entries;
  const items = asArray(rawItems);
  const result: Record<string, unknown>[] = [];
  items.forEach((item, index) => {
    if (!isRecord(item)) {
      throw new Error(`Config item ${index} must be an object`);
    }
    for (const field of ["id", "name", "directory", "command"]) {
      if (item[field] === undefined || item[field] === null || String(item[field]).trim() === "") {
        throw new Error(`Config item ${index} is missing required field: ${field}`);
      }
    }
    result.push(item);
  });
  return result;
}

export async function execChecked(
  step: string,
  command: string,
  args: string[],
  options: ExecaOptions = { stdio: "inherit" },
): Promise<void> {
  const result = await execa(command, args, { reject: false, ...options });
  if (result.exitCode !== 0) {
    throw new Error(`${step} failed with exit code ${result.exitCode}`);
  }
}

export async function git(args: string[]): Promise<string[]> {
  const result = await execa("git", ["-C", repoRoot, ...args], { reject: false });
  if (result.exitCode !== 0) {
    throw new Error(`git ${args.join(" ")} failed`);
  }
  return result.stdout
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0);
}

export function firstNonDirectorySegment(filePath: string): string {
  const normalized = filePath.replaceAll("\\", "/");
  const segments = normalized.split("/").filter((segment) => segment.length > 0);
  return segments[segments.length - 1] ?? filePath;
}
