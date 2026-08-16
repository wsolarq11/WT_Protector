import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import { describe, expect, it } from "vitest";
import { shortSha256, sha256File } from "../config-lib.js";
import { validateManifest, validateReceipt } from "../release-contract.js";

function tempExe(): string {
  const dir = mkdtempSync(path.join(os.tmpdir(), "wt-contract-"));
  const file = path.join(dir, "WTLauncher.App.exe");
  writeFileSync(file, "fake exe", "utf8");
  return file;
}

describe("release contract validators", () => {
  it("accepts matching receipt and manifest", () => {
    const exe = tempExe();
    validateReceipt(
      {
        kind: "release-build",
        status: "PASS",
        publishedExe: exe,
        bytes: 8,
        sha256: shortSha256(exe),
      },
      exe,
    );
    validateManifest(
      {
        schemaVersion: 1,
        version: "0.1.0-dev",
        fileName: "WTLauncher.App.exe",
        sha256: sha256File(exe),
        bytes: 8,
        publishedAtUtc: "2026-08-16T00:00:00.000Z",
      },
      exe,
    );
    rmSync(path.dirname(exe), { recursive: true, force: true });
  });

  it("rejects missing required fields", () => {
    const exe = tempExe();
    expect(() => validateReceipt({ kind: "release-build", status: "PASS" }, exe)).toThrow(
      "receipt.json missing required field: publishedExe",
    );
    expect(() => validateManifest({ schemaVersion: 1 }, exe)).toThrow(
      "update-manifest.json missing required field: version",
    );
    rmSync(path.dirname(exe), { recursive: true, force: true });
  });

  it("rejects hash mismatch", () => {
    const exe = tempExe();
    expect(() =>
      validateReceipt(
        {
          kind: "release-build",
          status: "PASS",
          publishedExe: exe,
          bytes: 8,
          sha256: "0000000000000000",
        },
        exe,
      ),
    ).toThrow("Receipt sha256 mismatch");
    rmSync(path.dirname(exe), { recursive: true, force: true });
  });
});
