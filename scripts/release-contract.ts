import { statSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  assertRequiredField,
  gitStatus,
  makeReceipt,
  normalizePath,
  pathExists,
  publishDir,
  readJson,
  repoRoot,
  sha256File,
  shortSha256,
  type ToolReceipt,
} from "./config-lib.js";
import { runBuildRelease } from "./build-release.js";

const exeName = "WTLauncher.App.exe";
const receiptName = "receipt.json";
const manifestName = "update-manifest.json";

export function validateReceipt(receipt: Record<string, unknown>, exePath: string): void {
  assertRequiredField(receipt, "kind", "receipt.json");
  assertRequiredField(receipt, "status", "receipt.json");
  assertRequiredField(receipt, "publishedExe", "receipt.json");
  assertRequiredField(receipt, "bytes", "receipt.json");
  assertRequiredField(receipt, "sha256", "receipt.json");

  if (receipt.kind !== "release-build") {
    throw new Error(`Unexpected receipt kind: ${String(receipt.kind)}`);
  }
  if (receipt.status !== "PASS") {
    throw new Error(`Receipt status is not PASS: ${String(receipt.status)}`);
  }
  if (normalizePath(String(receipt.publishedExe)) !== normalizePath(exePath)) {
    throw new Error(`Receipt publishedExe mismatch: ${String(receipt.publishedExe)}`);
  }
  if (Number(receipt.bytes) !== statSync(exePath).size) {
    throw new Error("Receipt bytes mismatch");
  }
  if (String(receipt.sha256) !== shortSha256(exePath)) {
    throw new Error("Receipt sha256 mismatch");
  }
}

export function validateManifest(manifest: Record<string, unknown>, exePath: string): void {
  assertRequiredField(manifest, "schemaVersion", "update-manifest.json");
  assertRequiredField(manifest, "version", "update-manifest.json");
  assertRequiredField(manifest, "fileName", "update-manifest.json");
  assertRequiredField(manifest, "sha256", "update-manifest.json");
  assertRequiredField(manifest, "bytes", "update-manifest.json");
  assertRequiredField(manifest, "publishedAtUtc", "update-manifest.json");

  if (Number(manifest.schemaVersion) !== 1) {
    throw new Error(`Unexpected manifest schemaVersion: ${String(manifest.schemaVersion)}`);
  }
  if (manifest.fileName !== exeName) {
    throw new Error(`Unexpected manifest fileName: ${String(manifest.fileName)}`);
  }
  if (Number(manifest.bytes) !== statSync(exePath).size) {
    throw new Error("Manifest bytes mismatch");
  }
  if (String(manifest.sha256) !== sha256File(exePath)) {
    throw new Error("Manifest sha256 mismatch");
  }
}

export async function runReleaseContract(): Promise<ToolReceipt> {
  const buildReceipt = await runBuildRelease({ locked: true });
  console.log(JSON.stringify(buildReceipt));

  const dirty = await gitStatus(repoRoot, true);
  if (dirty.length > 0) {
    throw new Error(`Release contract left worktree dirty: ${dirty.join("; ")}`);
  }

  const exePath = normalizePath(path.join(publishDir, exeName));
  const receiptPath = path.join(publishDir, receiptName);
  const manifestPath = path.join(publishDir, manifestName);

  if (!pathExists(exePath)) {
    throw new Error(`Published exe missing: ${exePath}`);
  }
  if (!pathExists(receiptPath)) {
    throw new Error(`Missing receipt: ${receiptPath}`);
  }
  if (!pathExists(manifestPath)) {
    throw new Error(`Missing update manifest: ${manifestPath}`);
  }

  const receipt = readJson<Record<string, unknown>>(receiptPath);
  validateReceipt(receipt, exePath);

  const manifest = readJson<Record<string, unknown>>(manifestPath);
  validateManifest(manifest, exePath);

  return makeReceipt("release-contract", {
    publishedExe: exePath,
    receiptPath,
    manifestPath,
    worktreeClean: true,
  });
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  const contractReceipt = await runReleaseContract();
  console.log(JSON.stringify(contractReceipt));
}
