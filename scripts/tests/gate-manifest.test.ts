import { describe, expect, it } from "vitest";
import { gateStepIds } from "../gate-steps.js";
import { formatGateSummary, gateSteps, type GateTier } from "../verify.js";

const KNOWN_TIERS: readonly GateTier[] = ["static", "local", "native", "snapshot"];

describe("gate manifest", () => {
  it("declares no duplicate step ids", () => {
    expect(new Set(gateStepIds).size).toBe(gateStepIds.length);
  });

  it("implements exactly the declared manifest, in order", () => {
    expect(gateSteps.map((step) => step.id)).toEqual([...gateStepIds]);
  });

  it("assigns every step a known tier", () => {
    for (const step of gateSteps) {
      expect(KNOWN_TIERS).toContain(step.tier);
    }
  });

  it("keeps only external-freshness work in the non-blocking tier", () => {
    // snapshot 层失败只进 advisory:它描述的是外部人工数据的时效,
    // 与代码正确性无关,因此这里锁死哪些步骤允许出现在该层。
    const snapshotSteps = gateSteps.filter((step) => step.tier === "snapshot").map((step) => step.id);
    expect(snapshotSteps).toEqual(["github-snapshot-freshness"]);
  });

  it("marks every .NET dependent step as native so missing SDKs are explicit", () => {
    // native 层在缺 .NET 10 SDK 的机器上会被显式跳过并留痕,
    // 而不是以 NETSDK1045 的形式伪装成代码失败。
    const nativeSteps = gateSteps.filter((step) => step.tier === "native").map((step) => step.id);
    expect(nativeSteps).toEqual([
      "release-lockfile-consistency",
      "avalonia-mainline-verify",
      "coverage-gate",
      "analyzer-wiring",
    ]);
  });

  it("marks local-entry-directories as the only local-tier step", () => {
    const localSteps = gateSteps.filter((step) => step.tier === "local").map((step) => step.id);
    expect(localSteps).toEqual(["local-entry-directories"]);
  });

  it("runs launcher-store before its dependent shortcut-backup-retirement", () => {
    const ids = gateSteps.map((step) => step.id);
    expect(ids.indexOf("launcher-store")).toBeLessThan(ids.indexOf("shortcut-backup-retirement"));
  });

  it("ends with structure-baseline-up-to-date so stale derived data is reported last", () => {
    expect(gateSteps[gateSteps.length - 1]?.id).toBe("structure-baseline-up-to-date");
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
    // advisory 不改变结论:它只被报告。
    expect(text).toContain("PASS");
    expect(text).toContain("Advisory (reported, does not block)");
    expect(text).toContain("[snapshot] github-snapshot-freshness: stale");
  });

  it("records why each step was skipped", () => {
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
