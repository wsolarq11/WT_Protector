import path from "node:path";
import { normalizePath, publishDir } from "./config-lib.js";
import { runBuildRelease } from "./build-release.js";
import { packFriend } from "./package-friend.js";
import { runSmoke } from "./smoke-critical-journey.js";

async function main(): Promise<void> {
  const buildReceipt = await runBuildRelease({ locked: true });
  console.log(JSON.stringify(buildReceipt));

  const exe = normalizePath(path.join(publishDir, "WTLauncher.App.exe"));
  const smokeReceipt = await runSmoke({ exe });
  console.log(JSON.stringify(smokeReceipt));

  const packReceipt = await packFriend();
  console.log(JSON.stringify(packReceipt));
}

await main();
