import { existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execa } from "execa";
import { appProjectPath, repoRoot } from "./config-lib.js";

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

async function main(): Promise<void> {
  const root = locateRepoRoot();
  const store = path.join(root, "config", "launcher.json");
  if (!existsSync(store)) {
    throw new Error(`Missing store: ${store}`);
  }
  const result = await execa("dotnet", ["run", "--project", appProjectPath, "--configuration", "Release", "--", "--config", store], {
    stdio: "inherit",
    reject: false,
  });
  process.exitCode = result.exitCode ?? 1;
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
