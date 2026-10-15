import path from "node:path";
import { type Evidence, failFrom, pass, type TierName, type Verdict } from "./verdict.js";
import { type ListOptions, type RunOptions, type RunResult, type World } from "./world.js";

/** 检查的上下文:世界 + 一组判定助手。
 *
 * 助手沿用既有的名字(readText/assertMatcherDetects/...),但它们不再自行读盘——
 * 一律经 ctx 转发到 World。于是"这条检查看到了什么"完全由传入的 World 决定:
 * 测试注入 fake world 即可构造违规样本,负例不再是只能写在文档里的承诺。
 * 助手在违规时抛错;runner 把异常收成 {status:"fail"}。检查因此可以保持
 * "断言式"的简短写法,而 receipt 边界上拿到的始终是结构化判词。 */
export interface Ctx {
  readonly repoRoot: string;
  path(...segments: string[]): string;
  // World 直通
  exists(file: string): boolean;
  readText(file: string): string;
  readJson<T = unknown>(file: string): T;
  listFiles(dir: string, options?: ListOptions): readonly string[];
  shortHash(file: string): string;
  bytes(file: string): number;
  run(command: string, args: readonly string[], options?: RunOptions): Promise<RunResult>;
  git(args: readonly string[]): Promise<readonly string[]>;
  // 判定助手(违规即抛)
  repoPath(relativePath: string): string;
  requireRepoPath(relativePath: string, context?: string): string;
  fileExists(relativePath: string): boolean;
  walkFiles(directory: string): string[];
  walkFilesExcluding(directory: string, excludedSegments: readonly string[]): string[];
  countMatches(text: string, pattern: RegExp): number;
  containsRequiredMarkers(text: string, required: readonly string[], context: string): void;
  assertMatcherDetects(pattern: RegExp, violationSample: string, label: string): void;
  assertMatcherAllows(pattern: RegExp, allowedSample: string, label: string): void;
  assertOnlyJsonKeys(
    object: unknown,
    allowedKeys: readonly string[],
    context: string,
  ): asserts object is Record<string, unknown>;
  assertPositiveNumber(value: unknown, context: string): number;
  isRecord(value: unknown): value is Record<string, unknown>;
  asArray<T>(value: T | readonly T[] | undefined | null): T[];
  readConfigItems(relativePath: string): Record<string, unknown>[];
  firstNonDirectorySegment(filePath: string): string;
  execChecked(step: string, command: string, args: string[], options?: RunOptions): Promise<void>;
}

export interface Check {
  readonly id: string;
  readonly tier: TierName;
  /** 本检查依赖的其他检查 id。runner 据此拓扑排序,取代此前靠数组顺序 + 闭包变量传递。
   * 依赖的检查若未通过,本检查显式 skip 而不是拿到 undefined 再抛"order broken"。 */
  readonly needs?: readonly string[];
  readonly run: (ctx: Ctx) => Promise<Evidence | void> | Evidence | void;
}

/** 定义一条检查。存在的意义是把"id 与 tier 必须与实现同处一地"变成结构事实:
 * 注册表直接列出 Check 值,不再有第二份 id 清单需要断言同步。 */
export function defineCheck(check: Check): Check {
  return check;
}

export function createCtx(world: World): Ctx {
  /** 检查会直接用绝对路径(如 path.join(avaloniaRoot, "src")),也会用相对仓库根的路径。
   * 这里统一收口:绝对路径原样放行,相对路径挂到 repoRoot 下。少了这一步,绝对路径会被
   * 再拼一次 repoRoot 而指向不存在的目录。 */
  const abs = (file: string): string => (path.isAbsolute(file) ? file : world.path(file));

  const requireRepoPath = (relativePath: string, context = relativePath): string => {
    const fullPath = abs(relativePath);
    if (!world.exists(fullPath)) {
      throw new Error(`Missing ${context}: ${fullPath}`);
    }
    return fullPath;
  };

  const readConfigItems = (relativePath: string): Record<string, unknown>[] => {
    const raw = world.readJson<unknown>(requireRepoPath(relativePath));
    const source = Array.isArray(raw) ? raw : (raw as Record<string, unknown>).entries;
    const items = Array.isArray(source) ? source : source === undefined || source === null ? [] : [source];
    return items.map((item, index) => {
      if (typeof item !== "object" || item === null || Array.isArray(item)) {
        throw new Error(`Config item ${index} must be an object`);
      }
      const record = item as Record<string, unknown>;
      for (const field of ["id", "name", "directory", "command"]) {
        if (record[field] === undefined || record[field] === null || String(record[field]).trim() === "") {
          throw new Error(`Config item ${index} is missing required field: ${field}`);
        }
      }
      return record;
    });
  };

  return {
    repoRoot: world.repoRoot,
    path: (...segments) => abs(path.join(...segments)),
    exists: (file) => world.exists(abs(file)),
    readText: (file) => world.readText(abs(file)),
    readJson: <T>(file: string) => world.readJson<T>(abs(file)),
    listFiles: (dir, options) => world.listFiles(abs(dir), options),
    shortHash: (file) => world.shortHash(abs(file)),
    bytes: (file) => world.bytes(abs(file)),
    run: (command, args, options) => world.run(command, args, options),
    git: (args) => world.git(args),
    repoPath: (relativePath) => abs(relativePath),
    requireRepoPath,
    fileExists: (relativePath) => world.exists(abs(relativePath)),
    walkFiles: (directory) => [...world.listFiles(abs(directory))],
    walkFilesExcluding: (directory, excludedSegments) => [
      ...world.listFiles(abs(directory), { exclude: excludedSegments }),
    ],
    countMatches: (text, pattern) => [...text.matchAll(pattern)].length,
    containsRequiredMarkers: (text, required, context) => {
      for (const marker of required) {
        if (!text.includes(marker)) {
          throw new Error(`${context} missing required marker: ${marker}`);
        }
      }
    },
    // 自检(正向):匹配器必须先能命中一个已知违规样本,否则"没匹配到"只是假绿。
    assertMatcherDetects: (pattern, violationSample, label) => {
      if (!pattern.test(violationSample)) {
        throw new Error(`${label} matcher failed to detect its own violation fixture: ${violationSample}`);
      }
    },
    // 自检(反向):匹配器不得命中合规样本,防过宽匹配把合法代码判成违规。
    assertMatcherAllows: (pattern, allowedSample, label) => {
      if (pattern.test(allowedSample)) {
        throw new Error(`${label} matcher matched its own allowed fixture: ${allowedSample}`);
      }
    },
    assertOnlyJsonKeys: (object, allowedKeys, context) => {
      if (typeof object !== "object" || object === null || Array.isArray(object)) {
        throw new Error(`${context} must be a JSON object`);
      }
      const allowed = new Set(allowedKeys);
      for (const key of Object.keys(object)) {
        if (!allowed.has(key)) {
          throw new Error(`${context} has unknown field: ${key}`);
        }
      }
    },
    assertPositiveNumber: (value, context) => {
      if (value === undefined || value === null) {
        throw new Error(`${context} is required`);
      }
      const number = Number(value);
      if (!Number.isFinite(number) || number <= 0) {
        throw new Error(`${context} must be positive`);
      }
      return number;
    },
    isRecord: (value): value is Record<string, unknown> =>
      typeof value === "object" && value !== null && !Array.isArray(value),
    asArray: <T>(value: T | readonly T[] | undefined | null): T[] => {
      if (value === undefined || value === null) {
        return [];
      }
      return Array.isArray(value) ? [...(value as readonly T[])] : [value as T];
    },
    readConfigItems,
    firstNonDirectorySegment: (filePath) => {
      const segments = filePath.replaceAll("\\", "/").split("/").filter((segment) => segment.length > 0);
      return segments[segments.length - 1] ?? filePath;
    },
    execChecked: async (step, command, args, options) => {
      const result = await world.run(command, args, options ?? { stdio: "inherit" });
      if (result.exitCode !== 0) {
        throw new Error(`${step} failed with exit code ${result.exitCode}`);
      }
    },
  };
}

/** 把"抛错的检查体"收成一个判词。检查可以继续用断言式写法,但对外只说一种语言。 */
export async function evaluate(check: Check, ctx: Ctx): Promise<Verdict> {
  try {
    const evidence = await check.run(ctx);
    return pass(evidence ?? {});
  } catch (error) {
    return failFrom(error);
  }
}
