import { describe, expect, it } from "vitest";
import { deriveVersion } from "../build-release.js";

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
