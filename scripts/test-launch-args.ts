import path from "node:path";
import { fileURLToPath } from "node:url";
import { execa } from "execa";
import { solutionPath, testsProjectPath } from "./config-lib.js";
import { restoreSolutionNativeAot } from "./publish-lib.js";

async function run(step: string, command: string, args: string[]): Promise<void> {
  const result = await execa(command, args, { stdio: "inherit", reject: false });
  if (result.exitCode !== 0) {
    throw new Error(`${step} failed with exit code ${result.exitCode}`);
  }
}

async function main(): Promise<void> {
  await restoreSolutionNativeAot(true);
  await run(
    "dotnet test LaunchArgumentBuilder",
    "dotnet",
    ["test", testsProjectPath, "-c", "Release", "--no-restore", "--filter", "FullyQualifiedName~LaunchArgumentBuilder", "--verbosity", "minimal"],
  );
  process.stdout.write(
    `${JSON.stringify({ status: "PASS", tests: "FullyQualifiedName~LaunchArgumentBuilder", project: testsProjectPath })}\n`,
  );
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
