import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execa } from "execa";
import { appProjectPath, solutionPath } from "./config-lib.js";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));

function locateRepoRoot(): string {
  let current = scriptDir;
  while (true) {
    const candidateStore = path.join(current, "config", "launcher.json");
    const candidateReadme = path.join(current, "README.md");
    if (existsSync(candidateStore) && existsSync(candidateReadme)) {
      return current;
    }
    const parent = path.dirname(current);
    if (parent === current) {
      throw new Error(`Could not locate repository root from ${scriptDir}`);
    }
    current = parent;
  }
}

async function ensureRestored(): Promise<void> {
  const assetsPath = path.join(path.dirname(appProjectPath), "obj", "project.assets.json");
  if (existsSync(assetsPath)) {
    return;
  }
  // dotnet run 默认做非锁定 restore，会把 packages.lock.json 的 win-x64 RID
  // 清空，导致后续 make 的锁定 restore 失败。这里统一走 --no-restore，首次
  // 运行用锁定 restore 补齐（保持 RID 与 lock 一致）。
  const result = await execa("dotnet", ["restore", solutionPath, "-r", "win-x64", "--locked-mode"], {
    stdio: "inherit",
    reject: false,
  });
  if (result.exitCode !== 0) {
    throw new Error(`locked restore failed: exit code ${result.exitCode}`);
  }
}

async function main(): Promise<void> {
  const root = locateRepoRoot();
  const store = path.join(root, "config", "launcher.json");
  if (!existsSync(store)) {
    throw new Error(`Missing store: ${store}`);
  }
  await ensureRestored();
  const result = await execa("dotnet", ["run", "--project", appProjectPath, "--configuration", "Release", "--no-restore", "--", "--config", store], {
    stdio: "inherit",
    reject: false,
  });
  process.exitCode = result.exitCode ?? 1;
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
