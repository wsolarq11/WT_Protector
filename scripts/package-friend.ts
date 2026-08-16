import { cpSync, createWriteStream, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { publishDir, readJson, repoRoot } from "./config-lib.js";

const require = createRequire(import.meta.url);
const archiverModule = require("archiver") as typeof import("archiver");

async function pack(): Promise<Record<string, unknown>> {
  const exe = path.join(publishDir, "WTLauncher.App.exe");
  const manifestPath = path.join(publishDir, "update-manifest.json");
  const quickStartFiles = readdirSync(path.join(repoRoot, "docs")).filter((file) => file.endsWith(".txt"));
  if (quickStartFiles.length !== 1) {
    throw new Error(`Expected exactly one quick-start .txt in docs/, found ${quickStartFiles.length}`);
  }
  const quickStart = path.join(repoRoot, "docs", quickStartFiles[0]);
  const exampleConfig = path.join(repoRoot, "config", "launcher.example.json");
  for (const required of [exe, manifestPath, quickStart, exampleConfig]) {
    if (!existsSync(required)) {
      throw new Error(`Missing package input: ${required}`);
    }
  }
  const manifest = readJson<{ version?: string }>(manifestPath);
  const version = (manifest.version || "0.1.0").replace(/[^\w.-]/g, "_");

  const friendRoot = path.join(repoRoot, "src", "avalonia-launcher", "artifacts", "friend");
  const staging = path.join(friendRoot, "staging");
  const bundle = path.join(staging, "WTLauncher");
  mkdirSync(path.join(bundle, "config"), { recursive: true });

  for (const entry of readdirSync(publishDir)) {
    if (entry === "receipt.json" || entry === "install-manifest.json") {
      continue;
    }
    cpSync(path.join(publishDir, entry), path.join(bundle, entry), { recursive: true });
  }
  writeFileSync(path.join(bundle, path.basename(quickStart)), readFileSync(quickStart));
  writeFileSync(path.join(bundle, "config", "launcher.example.json"), readFileSync(exampleConfig));

  const zipPath = path.join(friendRoot, `WTLauncher-${version}.zip`);
  rmSync(zipPath, { force: true });
  await new Promise<void>((resolve, reject) => {
    const output = createWriteStream(zipPath);
    const archive = new archiverModule.ZipArchive({ zlib: { level: 9 } });
    output.on("close", () => resolve());
    output.on("error", reject);
    archive.on("error", reject);
    archive.pipe(output);
    archive.directory(bundle, false);
    void archive.finalize();
  });

  rmSync(staging, { recursive: true, force: true });
  return {
    status: "PASS",
    zip: zipPath,
    bytes: statSync(zipPath).size,
    version,
  };
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  const receipt = await pack();
  console.log(JSON.stringify(receipt));
}
