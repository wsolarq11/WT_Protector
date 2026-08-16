import { describe, expect, it } from "vitest";
import { deriveVersion } from "../build-release.js";
import { buildNativeAotPublishArgs, NativeAotMarker } from "../publish-lib.js";

describe("build-release version derivation", () => {
  it("returns dev fallback for empty describe", () => {
    expect(deriveVersion(null)).toBe("0.1.0-dev");
    expect(deriveVersion("")).toBe("0.1.0-dev");
  });

  it("uses semver tag and maps dirty suffix to dev", () => {
    expect(deriveVersion("v1.2.3")).toBe("1.2.3");
    expect(deriveVersion("1.2.3-dirty")).toBe("1.2.3.dev");
  });

  it("prefixes non-semver describe output", () => {
    expect(deriveVersion("abcdef")).toBe("0.1.0-abcdef");
    expect(deriveVersion("abcdef-dirty")).toBe("0.1.0-abcdef");
  });
});

describe("shared Native AOT publish contract", () => {
  it("keeps AOT marker in every publish invocation", () => {
    expect(NativeAotMarker).toBe("EnableNativeAot");
    const args = buildNativeAotPublishArgs({ outputDir: "out" });
    expect(args).toContain("-p:EnableNativeAot=true");
    expect(args).toContain("--self-contained");
    expect(args).toContain("true");
    expect(args).toContain("--no-restore");
  });

  it("injects version metadata only when provided", () => {
    const base = buildNativeAotPublishArgs({ outputDir: "out" });
    const versioned = buildNativeAotPublishArgs({ outputDir: "out", version: "1.2.3", informationalVersion: "1.2.3" });
    expect(base.some((arg) => arg.startsWith("-p:Version="))).toBe(false);
    expect(versioned).toContain("-p:Version=1.2.3");
    expect(versioned).toContain("-p:InformationalVersion=1.2.3");
  });
});
