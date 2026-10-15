import { createHash } from "node:crypto";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import path from "node:path";
import { execa, type Options as ExecaOptions } from "execa";

/** 子进程结果。execa 的返回对象带一堆门禁用不到的字段,这里只保留判定所需的三个。 */
export interface RunResult {
  readonly exitCode: number;
  readonly stdout: string;
  readonly stderr: string;
}

export interface RunOptions {
  /** "inherit" 让子进程直接写终端(长构建的进度需要可见);"pipe" 收回输出供解析。 */
  readonly stdio?: "inherit" | "pipe";
  readonly cwd?: string;
}

export interface ListOptions {
  /** 按目录名排除(如 node_modules/.git/artifacts),用于扫描型检查。 */
  readonly exclude?: readonly string[];
  readonly extensions?: readonly string[];
}

/** 世界:整个 scripts/ 里唯一与操作系统对话的东西。
 *
 * 检查不直接 import node:fs——它们只读这个接口。这样"某条检查看到了什么"变成一个
 * 可替换的输入:测试注入 fake world 就能构造违规样本,负例不再只能停留在
 * "失败分支未注入"的自述里。生产实现见 createRealWorld。 */
export interface World {
  readonly repoRoot: string;
  /** 相对仓库根的路径拼接,保证所有路径口径一致。 */
  path(...segments: string[]): string;
  exists(file: string): boolean;
  readText(file: string): string;
  readJson<T = unknown>(file: string): T;
  listFiles(dir: string, options?: ListOptions): readonly string[];
  shortHash(file: string): string;
  bytes(file: string): number;
  run(command: string, args: readonly string[], options?: RunOptions): Promise<RunResult>;
  git(args: readonly string[]): Promise<readonly string[]>;
}

/** 把相对路径规范化为绝对路径。绝对路径原样返回,便于检查直接接受 world.path(...) 的结果。 */
function resolve(repoRoot: string, file: string): string {
  return path.isAbsolute(file) ? file : path.join(repoRoot, file);
}

function walk(directory: string, excluded: Set<string>, extensions?: readonly string[]): string[] {
  const files: string[] = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      if (!excluded.has(entry.name)) {
        files.push(...walk(fullPath, excluded, extensions));
      }
    } else if (entry.isFile()) {
      if (extensions === undefined || extensions.some((ext) => entry.name.toLowerCase().endsWith(ext))) {
        files.push(fullPath);
      }
    }
  }
  return files;
}

export function createRealWorld(repoRoot: string): World {
  return {
    repoRoot,
    path: (...segments) => path.join(repoRoot, ...segments),
    exists: (file) => existsSync(resolve(repoRoot, file)),
    readText: (file) => readFileSync(resolve(repoRoot, file), "utf8"),
    readJson: <T>(file: string) => JSON.parse(readFileSync(resolve(repoRoot, file), "utf8")) as T,
    listFiles: (dir, options) => {
      const root = resolve(repoRoot, dir);
      if (!existsSync(root)) {
        return [];
      }
      return walk(root, new Set(options?.exclude ?? []), options?.extensions);
    },
    shortHash: (file) =>
      createHash("sha256").update(readFileSync(resolve(repoRoot, file))).digest("hex").toUpperCase().slice(0, 16),
    bytes: (file) => statSync(resolve(repoRoot, file)).size,
    run: async (command, args, options) => {
      const execaOptions: ExecaOptions = {
        stdio: options?.stdio ?? "pipe",
        reject: false,
        ...(options?.cwd === undefined ? {} : { cwd: options.cwd }),
      };
      const result = await execa(command, [...args], execaOptions);
      // execa 在 pipe 模式下把 stdout 标成 string | unknown[];门禁只消费文本,
      // 这里显式收窄成字符串,避免把联合类型泄漏给每条检查。
      const text = (value: string | unknown[] | undefined): string =>
        typeof value === "string" ? value : value === undefined ? "" : value.join("\n");
      return { exitCode: result.exitCode ?? 0, stdout: text(result.stdout), stderr: text(result.stderr) };
    },
    git: async (args) => {
      const result = await execa("git", ["-C", repoRoot, ...args], { reject: false });
      if (result.exitCode !== 0) {
        throw new Error(`git ${args.join(" ")} failed`);
      }
      return result.stdout
        .split(/\r?\n/)
        .map((line) => line.trim())
        .filter((line) => line.length > 0);
    },
  };
}
