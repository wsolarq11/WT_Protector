import { describe, expect, it } from "vitest";
import { aotToolchainOwnedBySdkCheck, noImperativeViewUiCheck } from "../checks/static.js";
import { createCtx } from "../kernel/check.js";
import { createRealWorld } from "../kernel/world.js";
import { repoRoot } from "../config-lib.js";

const ctx = createCtx(createRealWorld(repoRoot));

describe("root gate JSON helpers", () => {
  it("accepts only known keys", () => {
    expect(() => ctx.assertOnlyJsonKeys({ a: 1, b: "x" }, ["a", "b"], "sample")).not.toThrow();
  });

  it("rejects unknown keys", () => {
    expect(() => ctx.assertOnlyJsonKeys({ a: 1, c: true }, ["a", "b"], "sample")).toThrow(
      "sample has unknown field: c",
    );
  });

  it("requires positive finite numbers", () => {
    expect(ctx.assertPositiveNumber(1200, "budget")).toBe(1200);
    expect(() => ctx.assertPositiveNumber(0, "budget")).toThrow("budget must be positive");
    expect(() => ctx.assertPositiveNumber(null, "budget")).toThrow("budget is required");
  });
});

describe("Native AOT recurrence gate", () => {
  it("requires the .NET SDK to own ILCompiler selection", async () => {
    await expect(Promise.resolve(aotToolchainOwnedBySdkCheck.run(ctx))).resolves.toBeDefined();
  });
});

describe("Imperative view UI gate", () => {
  it("scans code-behind and keeps only the deliberate dynamic view whitelisted", async () => {
    const evidence = (await noImperativeViewUiCheck.run(ctx)) as Record<string, unknown>;
    expect(Number(evidence.viewsScanned)).toBeGreaterThan(10);
    expect(evidence.allowedExceptions).toContain("DiscoverAgentsWindow.axaml.cs");
    expect(evidence.allowedExceptions).not.toContain("FastWrapPanel.cs");
  });
});
