import { createHash } from "node:crypto";
import { existsSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import { describe, expect, it } from "vitest";
import {
  assertRequiredField,
  fileBytes,
  makeReceipt,
  pathExists,
  sha256File,
  shortSha256,
} from "../config-lib.js";

function tempFile(content: string): string {
  const dir = mkdtempSync(path.join(os.tmpdir(), "wt-config-lib-"));
  const file = path.join(dir, "sample.bin");
  writeFileSync(file, content, "utf8");
  return file;
}

describe("config-lib", () => {
  it("computes uppercase SHA-256 and short hash", () => {
    const file = tempFile("abc");
    const expected = createHash("sha256").update("abc").digest("hex").toUpperCase();
    expect(sha256File(file)).toBe(expected);
    expect(shortSha256(file)).toBe(expected.slice(0, 16));
    expect(fileBytes(file)).toBe(3);
    rmSync(path.dirname(file), { recursive: true, force: true });
  });

  it("builds a receipt with required built-in fields", async () => {
    const receipt = await makeReceipt("test-kind", { marker: "value" });
    expect(receipt.schemaVersion).toBe(1);
    expect(receipt.kind).toBe("test-kind");
    expect(receipt.status).toBe("PASS");
    expect(receipt.repoHead).toMatch(/^[0-9a-f]+$/);
    expect(Array.isArray(receipt.worktreeStatus)).toBe(true);
    expect(receipt.marker).toBe("value");
  });

  it("asserts required fields", () => {
    expect(() => assertRequiredField({}, "missing", "context")).toThrow("context missing required field: missing");
    expect(() => assertRequiredField({ value: " " }, "value", "context")).toThrow(
      "context missing required field: value",
    );
  });

  it("reports file existence", () => {
    const file = tempFile("x");
    expect(pathExists(file)).toBe(true);
    expect(pathExists(`${file}.nope`)).toBe(false);
    rmSync(path.dirname(file), { recursive: true, force: true });
  });
});
