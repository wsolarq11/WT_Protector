import { describe, expect, it } from "vitest";
import { assertRegistryIntegrity, gateChecks } from "../registry.js";
import { formatGateSummary, isSupportedDotnetSdk, selectChecks, TIER_ORDER } from "../verify.js";
import { TIER_POLICY } from "../kernel/policy.js";
import type { TierName } from "../kernel/verdict.js";

const KNOWN_TIERS: readonly TierName[] = ["static", "local", "native", "snapshot"];

describe("gate registry", () => {
  it("declares no duplicate ids and only kebab-case ids", () => {
    expect(() => assertRegistryIntegrity()).not.toThrow();
  });

  it("assigns every check a known tier", () => {
    for (const check of gateChecks) {
      expect(KNOWN_TIERS).toContain(check.tier);
    }
  });

  it("keeps only external-freshness work in the non-blocking tier", () => {
    // snapshot 层失败只进 advisory:它描述的是外部人工数据的时效,与代码正确性无关。
    const snapshot = gateChecks.filter((check) => check.tier === "snapshot").map((check) => check.id);
    expect(snapshot).toEqual(["github-snapshot-freshness"]);
  });

  it("keeps only machine-path facts in the local tier", () => {
    const local = gateChecks.filter((check) => check.tier === "local").map((check) => check.id);
    expect(local).toEqual(["local-entry-directories"]);
  });

  it("marks every .NET dependent check as native so missing SDKs are explicit", () => {
    const native = gateChecks.filter((check) => check.tier === "native").map((check) => check.id);
    expect(native).toEqual([
      "release-lockfile-consistency",
      "avalonia-mainline-verify",
      "coverage-gate",
      "analyzer-wiring",
    ]);
  });

  it("declares the shortcut dependency instead of relying on array order", () => {
    const shortcut = gateChecks.find((check) => check.id === "shortcut-backup-retirement");
    expect(shortcut?.needs).toEqual(["launcher-store"]);
  });

  it("ends with structure-baseline-up-to-date so stale derived data is reported last", () => {
    expect(gateChecks[gateChecks.length - 1]?.id).toBe("structure-baseline-up-to-date");
  });
});

describe("tier selection (--tier fast lane)", () => {
  it("returns every check when no tier is requested", () => {
    expect(selectChecks(gateChecks, []).length).toBe(gateChecks.length);
  });

  it("narrows to exactly the requested tier", () => {
    const staticOnly = selectChecks(gateChecks, ["static"]);
    expect(staticOnly.length).toBeGreaterThan(30);
    expect(staticOnly.every((check) => check.tier === "static")).toBe(true);
    expect(staticOnly.length).toBeLessThan(gateChecks.length);
  });

  it("supports multiple tiers in one call", () => {
    const both = selectChecks(gateChecks, ["static", "native"]);
    expect(both.every((check) => check.tier === "static" || check.tier === "native")).toBe(true);
    expect(both.some((check) => check.tier === "local")).toBe(false);
  });
});

describe("tier policy", () => {
  it("blocks only static and native", () => {
    expect(TIER_POLICY.static.blocking).toBe(true);
    expect(TIER_POLICY.native.blocking).toBe(true);
    expect(TIER_POLICY.local.blocking).toBe(false);
    expect(TIER_POLICY.snapshot.blocking).toBe(false);
  });

  it("skips the native tier when no supported SDK is present", () => {
    expect(TIER_POLICY.native.skipReason({ isCi: false, dotnetSdk: null })).toContain("requires .NET 10 SDK");
    expect(TIER_POLICY.native.skipReason({ isCi: false, dotnetSdk: "10.0.401" })).toBeNull();
  });

  it("skips the local tier only in CI", () => {
    expect(TIER_POLICY.local.skipReason({ isCi: true, dotnetSdk: null })).toContain("launcher.ci.json");
    expect(TIER_POLICY.local.skipReason({ isCi: false, dotnetSdk: null })).toBeNull();
  });

  it("covers every tier", () => {
    expect([...TIER_ORDER].sort()).toEqual(["local", "native", "snapshot", "static"]);
  });
});

describe("gate summary formatting", () => {
  it("reports PASS and the detected SDK when nothing blocks", () => {
    const text = formatGateSummary([], [], [], "10.0.301");
    expect(text).toContain("PASS");
    expect(text).toContain("blocking failures: 0");
    expect(text).toContain("dotnet SDK: 10.0.301");
  });

  it("lists blocking failures with their tier and id", () => {
    const text = formatGateSummary(
      [{ id: "winforms-retired-design", tier: "static", message: "missing marker" }],
      [],
      [],
      "10.0.301",
    );
    expect(text).toContain("FAIL");
    expect(text).toContain("[static] winforms-retired-design: missing marker");
  });

  it("keeps local-tier path failures advisory instead of blocking", () => {
    const text = formatGateSummary(
      [],
      [{ id: "local-entry-directories", tier: "local", message: "missing C:\\gone" }],
      [],
      "10.0.301",
    );
    expect(text).toContain("PASS");
    expect(text).toContain("Advisory (reported, does not block)");
    expect(text).toContain("[local] local-entry-directories: missing C:\\gone");
  });

  it("keeps advisory separate from blocking failures", () => {
    const text = formatGateSummary(
      [],
      [{ id: "github-snapshot-freshness", tier: "snapshot", message: "stale" }],
      [],
      "10.0.301",
    );
    expect(text).toContain("PASS");
    expect(text).toContain("Advisory (reported, does not block)");
    expect(text).toContain("[snapshot] github-snapshot-freshness: stale");
  });

  it("records why each check was skipped", () => {
    const text = formatGateSummary(
      [],
      [],
      [{ id: "coverage-gate", tier: "native", reason: "requires .NET 10 SDK" }],
      null,
    );
    expect(text).toContain("[native] coverage-gate: requires .NET 10 SDK");
    expect(text).toContain("dotnet SDK: none installed (native tier skipped)");
  });
});

// SDK 溯源:receipt 的 dotnetSdk 必须是 MSBuild 真正会用的那个 SDK。
// 此前取 `--list-sdks` 的第一个 10.x,在装有多个 SDK 的镜像上会报出没参与构建的版本。
describe("dotnet SDK provenance", () => {
  it("accepts the 10.x line that global.json pins", () => {
    expect(isSupportedDotnetSdk("10.0.301")).toBe(true);
    expect(isSupportedDotnetSdk("10.0.401\n")).toBe(true);
  });

  it("rejects a non-10.x resolution so the native tier skips instead of building wrong", () => {
    expect(isSupportedDotnetSdk("8.0.424")).toBe(false);
    expect(isSupportedDotnetSdk("9.0.100")).toBe(false);
    expect(isSupportedDotnetSdk("")).toBe(false);
  });
});
