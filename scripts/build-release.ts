import { existsSync } from "node:fs";
import { mkdir, readdir, rm } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { execa } from "execa";
import {
  appProjectPath,
  avaloniaRoot,
  fileBytes,
  lastWriteTimeString,
  makeReceipt,
  normalizePath,
  pathExists,
  publishDir,
  runningReleasePids,
  sha256File,
  shortSha256,
  solutionPath,
  writeJsonNoBom,
  type ToolReceipt,
} from "./config-lib.js";
import { restoreSolutionNativeAot, runNativeAotPublish } from "./publish-lib.js";

const vsInstaller = "C:\\Program Files (x86)\\Microsoft Visual Studio\\Installer";
const exeName = "WTLauncher.App.exe";
const dllName = "WTLauncher.App.dll";

export function deriveVersion(describe: string | null | undefined): string {
  const source = describe?.trim() ?? "";
  if (source.length === 0) {
    return "0.1.0-dev";
  }
  let version = source.replace(/^v/, "");
  if (/^\d+\.\d+\.\d+/.test(version)) {
    return version.replace(/-dirty$/, ".dev");
  }
  version = version.replace(/-dirty$/, "");
  return `0.1.0-${version}`;
}

export async function describeVersion(): Promise<string> {
  const result = await execa("git", ["-C", avaloniaRoot, "describe", "--tags", "--always", "--dirty"], {
    reject: false,
  });
  if (result.exitCode !== 0) {
    return deriveVersion(null);
  }
  return deriveVersion(result.stdout);
}

async function removePdbFiles(directory: string): Promise<void> {
  const entries = await readdir(directory, { withFileTypes: true });
  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      await removePdbFiles(fullPath);
    } else if (entry.name.toLowerCase().endsWith(".pdb")) {
      await rm(fullPath, { force: true });
    }
  }
}

async function removeStaleConfig(directory: string): Promise<void> {
  await rm(path.join(directory, "config"), { recursive: true, force: true });
}

export interface BuildReleaseOptions {
  locked: boolean;
}

export async function runBuildRelease(options: BuildReleaseOptions): Promise<ToolReceipt> {
  if (existsSync(vsInstaller)) {
    process.env.PATH = `${vsInstaller}${path.delimiter}${process.env.PATH ?? ""}`;
  }

  if (!pathExists(solutionPath)) {
    throw new Error(`Missing solution: ${solutionPath}`);
  }
  if (!pathExists(appProjectPath)) {
    throw new Error(`Missing app project: ${appProjectPath}`);
  }

  const exePath = normalizePath(path.join(publishDir, exeName));
  const dllPath = normalizePath(path.join(publishDir, dllName));
  const runningPids = await runningReleasePids(exePath);
  if (runningPids.length > 0) {
    throw new Error(`Release publish target is running: ${exePath} (pid(s): ${runningPids.join(", ")})`);
  }

  const version = await describeVersion();
  await mkdir(publishDir, { recursive: true });

  await restoreSolutionNativeAot(options.locked);

  await runNativeAotPublish({
    outputDir: publishDir,
    version,
    informationalVersion: version,
  });

  if (!pathExists(exePath)) {
    throw new Error(`Published exe missing: ${exePath}`);
  }

  await removePdbFiles(publishDir);
  await removeStaleConfig(publishDir);

  const receipt = await makeReceipt("release-build", {
    sourceRoot: avaloniaRoot,
    publishedExe: exePath,
    publishedDll: pathExists(dllPath) ? dllPath : null,
    bytes: fileBytes(exePath),
    sha256: shortSha256(exePath),
    builtAt: lastWriteTimeString(exePath),
  });

  writeJsonNoBom(path.join(publishDir, "receipt.json"), receipt);

  const manifest = {
    schemaVersion: 1,
    version,
    fileName: exeName,
    sha256: sha256File(exePath),
    bytes: fileBytes(exePath),
    publishedAtUtc: new Date().toISOString(),
    notes: "Portable win-x64 release build; config stays portable.",
  };
  writeJsonNoBom(path.join(publishDir, "update-manifest.json"), manifest);

  return receipt;
}

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      locked: { type: "boolean", default: false },
    },
  });
  const receipt = await runBuildRelease({ locked: values.locked ?? false });
  console.log(JSON.stringify(receipt));
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
