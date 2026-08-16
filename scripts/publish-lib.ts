import { execa } from "execa";
import { appProjectPath, runStep, solutionPath } from "./config-lib.js";

export const NativeAotMarker = "EnableNativeAot";

export async function restoreSolutionNativeAot(locked = true): Promise<void> {
  const args = ["restore", solutionPath, "-r", "win-x64", `-p:${NativeAotMarker}=true`];
  if (locked) {
    args.push("--locked-mode");
  }
  await runStep(locked ? "AOT locked restore" : "AOT restore", async () => {
    const result = await execa("dotnet", args, { stdio: "inherit", reject: false });
    if (result.exitCode !== 0) {
      throw new Error(`exit code ${result.exitCode}`);
    }
  });
}

export interface NativeAotPublishOptions {
  outputDir: string;
  version?: string;
  informationalVersion?: string;
}

export function buildNativeAotPublishArgs(options: NativeAotPublishOptions): string[] {
  const args = [
    "publish",
    appProjectPath,
    "-c",
    "Release",
    "-r",
    "win-x64",
    `-p:${NativeAotMarker}=true`,
    "--self-contained",
    "true",
    "--no-restore",
    "-o",
    options.outputDir,
  ];
  if (options.version) {
    args.push(`-p:Version=${options.version}`);
  }
  if (options.informationalVersion) {
    args.push(`-p:InformationalVersion=${options.informationalVersion}`);
  }
  return args;
}

export async function runNativeAotPublish(options: NativeAotPublishOptions): Promise<void> {
  await runStep("dotnet publish app win-x64 native-aot", async () => {
    const result = await execa("dotnet", buildNativeAotPublishArgs(options), { stdio: "inherit", reject: false });
    if (result.exitCode !== 0) {
      throw new Error(`exit code ${result.exitCode}`);
    }
  });
}
