import path from "node:path";
import { gitStatus, normalizePath, publishDir, repoRoot } from "./config-lib.js";
import { runBuildRelease } from "./build-release.js";
import { packFriend } from "./package-friend.js";
import { runSmoke } from "./smoke-critical-journey.js";

async function main(): Promise<void> {
  const dirty = await gitStatus(repoRoot, true);
  if (dirty.length > 0) {
    console.log(`[advisory] worktree has uncommitted changes: ${dirty.join("; ")}`);
  }

  const buildReceipt = await runBuildRelease({ locked: true });
  console.log(JSON.stringify(buildReceipt));

  const exe = normalizePath(path.join(publishDir, "WTLauncher.App.exe"));
  const smokeReceipt = await runSmoke({ exe });
  console.log(JSON.stringify(smokeReceipt));

  const packReceipt = await packFriend();
  console.log(JSON.stringify(packReceipt));
}

await main();
