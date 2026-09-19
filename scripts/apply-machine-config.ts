import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { execa } from "execa";
import { repoRoot } from "./config-lib.js";

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      "dry-run": { type: "boolean", default: false },
    },
  });
  const yaml = path.join(repoRoot, ".wt-protector.dsc.yaml");
  const args = values["dry-run"]
    ? ["configure", "validate", "-f", yaml]
    : ["configure", "-f", yaml, "--accept-configuration-agreements", "--disable-interactivity"];
  const result = await execa("winget", args, { stdio: "inherit", reject: false });
  process.exitCode = result.exitCode ?? 1;
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
