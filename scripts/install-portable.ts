import { cpSync, existsSync, mkdirSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { execa } from "execa";
import { publishDir, readJson, repoRoot, writeJsonNoBom } from "./config-lib.js";

const defaultInstallDir = path.join(
  process.env.LOCALAPPDATA ?? path.join(os.homedir(), "AppData", "Local"),
  "Programs",
  "WTLauncher",
);
const uninstallerProject = path.join(repoRoot, "tools", "uninstaller", "Uninstaller.csproj");
const uninstallerOutDir = path.join(repoRoot, "src", "avalonia-launcher", "artifacts", "uninstaller", "win-x64");

async function runChecked(step: string, command: string, args: string[]): Promise<void> {
  const result = await execa(command, args, { reject: false });
  if (result.exitCode !== 0) {
    throw new Error(`${step} failed with exit code ${result.exitCode}: ${result.stderr || result.stdout}`);
  }
}

async function buildUninstaller(): Promise<string> {
  rmSync(uninstallerOutDir, { recursive: true, force: true });
  mkdirSync(uninstallerOutDir, { recursive: true });
  await runChecked("build self-contained uninstaller", "dotnet", [
    "publish",
    uninstallerProject,
    "-c",
    "Release",
    "-r",
    "win-x64",
    "--self-contained",
    "true",
    "-p:PublishSingleFile=true",
    "-o",
    uninstallerOutDir,
  ]);
  const uninstaller = path.join(uninstallerOutDir, "Uninstaller.exe");
  if (!existsSync(uninstaller)) {
    throw new Error(`Uninstaller build did not produce ${uninstaller}`);
  }
  return uninstaller;
}

async function createShortcut(target: string, workingDirectory: string, location: "startmenu" | "desktop"): Promise<void> {
  await runChecked("create shortcut", "dotnet", [
    "run",
    "--project",
    uninstallerProject,
    "-c",
    "Release",
    "--",
    "--create-shortcut",
    target,
    workingDirectory,
    location,
  ]);
}

export async function runInstallPortable(options: {
  publishDir?: string;
  installDir?: string;
  desktop?: boolean;
  uninstall?: boolean;
  skipShortcuts?: boolean;
}): Promise<Record<string, unknown>> {
  const sourceDir = path.resolve(options.publishDir ?? publishDir);
  const installDir = path.resolve(options.installDir ?? defaultInstallDir);

  if (options.uninstall) {
    const uninstaller = path.join(installDir, "uninstaller.exe");
    if (!existsSync(uninstaller)) {
      throw new Error(`Uninstaller missing: ${uninstaller}`);
    }
    await runChecked("run uninstaller", uninstaller, []);
    return { status: "UNINSTALLED", installDir };
  }

  const exe = path.join(sourceDir, "WTLauncher.App.exe");
  const manifestPath = path.join(sourceDir, "update-manifest.json");
  for (const required of [sourceDir, exe, manifestPath]) {
    if (!existsSync(required)) {
      throw new Error(`Missing install input: ${required}`);
    }
  }
  const manifest = readJson<{ version?: string }>(manifestPath);
  if (!manifest.version || String(manifest.version).trim() === "") {
    throw new Error("Update manifest has no version");
  }

  rmSync(installDir, { recursive: true, force: true });
  mkdirSync(installDir, { recursive: true });
  for (const entry of readdirSync(sourceDir)) {
    if (entry === "receipt.json" || entry === "update-manifest.json" || entry === "install-manifest.json") {
      continue;
    }
    cpSync(path.join(sourceDir, entry), path.join(installDir, entry), { recursive: true });
  }
  const targetExe = path.join(installDir, "WTLauncher.App.exe");

  const uninstaller = await buildUninstaller();
  cpSync(uninstaller, path.join(installDir, "uninstaller.exe"));
  if (!options.skipShortcuts) {
    await createShortcut(targetExe, installDir, "startmenu");
    if (options.desktop) {
      await createShortcut(targetExe, installDir, "desktop");
    }
  }

  writeJsonNoBom(path.join(installDir, "install-manifest.json"), {
    schemaVersion: 1,
    kind: "portable-install",
    version: manifest.version,
    installDir,
    installDateUtc: new Date().toISOString(),
    configPreserved: true,
    uninstaller: "uninstaller.exe",
  });

  return {
    status: "INSTALLED",
    installDir,
    exe: targetExe,
    version: manifest.version,
    uninstaller: path.join(installDir, "uninstaller.exe"),
  };
}

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      "publish-dir": { type: "string", default: "" },
      "install-dir": { type: "string", default: "" },
      desktop: { type: "boolean", default: false },
      uninstall: { type: "boolean", default: false },
      "skip-shortcuts": { type: "boolean", default: false },
    },
  });
  const result = await runInstallPortable({
    publishDir: values["publish-dir"] || undefined,
    installDir: values["install-dir"] || undefined,
    desktop: Boolean(values.desktop),
    uninstall: Boolean(values.uninstall),
    skipShortcuts: Boolean(values["skip-shortcuts"]),
  });
  console.log(JSON.stringify(result));
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
