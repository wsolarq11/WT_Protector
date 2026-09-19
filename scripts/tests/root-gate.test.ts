import { describe, expect, it } from "vitest";
import { assertOnlyJsonKeys, assertPositiveNumber, checks, resetChecks } from "../checks/common.js";
import { checkAotToolchainOwnedBySdk, checkNoImperativeViewUi } from "../checks/static.js";

describe("root gate JSON helpers", () => {
  it("accepts only known keys", () => {
    const value = { a: 1, b: "x" };
    expect(() => assertOnlyJsonKeys(value, ["a", "b"], "sample")).not.toThrow();
  });

  it("rejects unknown keys", () => {
    expect(() => assertOnlyJsonKeys({ a: 1, c: true }, ["a", "b"], "sample")).toThrow("sample has unknown field: c");
  });

  it("requires positive finite numbers", () => {
    expect(assertPositiveNumber(1200, "budget")).toBe(1200);
    expect(() => assertPositiveNumber(0, "budget")).toThrow("budget must be positive");
    expect(() => assertPositiveNumber(null, "budget")).toThrow("budget is required");
  });
});

describe("Native AOT recurrence gate", () => {
  it("requires the .NET SDK to own ILCompiler selection", () => {
    expect(() => checkAotToolchainOwnedBySdk()).not.toThrow();
  });
});

describe("Imperative view UI gate", () => {
  it("scans code-behind and keeps only the deliberate dynamic view whitelisted", () => {
    resetChecks();
    expect(() => checkNoImperativeViewUi()).not.toThrow();
    const record = checks.find((check) => check.name === "no-imperative-view-ui");
    expect(record?.detail.viewsScanned).toBeGreaterThan(10);
    expect(record?.detail.allowedExceptions).toContain("DiscoverAgentsWindow.axaml.cs");
    expect(record?.detail.allowedExceptions).not.toContain("FastWrapPanel.cs");
  });
});
