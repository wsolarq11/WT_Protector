import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { execa } from "execa";
import { solutionPath } from "./config-lib.js";

const filter = "Category=CoreUnit|Category=PropertyTest|Category=Analyzer";

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      logger: { type: "string", default: "" },
    },
  });
  const args = ["test", solutionPath, "-c", "Release", "--no-restore", "--filter", filter];
  if (values.logger) {
    args.push("--logger", values.logger);
  }
  const result = await execa("dotnet", args, { stdio: "inherit", reject: false });
  if (result.exitCode !== 0) {
    process.exitCode = result.exitCode ?? 1;
    return;
  }
  console.log(`Fast lane PASS: ${filter}`);
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
