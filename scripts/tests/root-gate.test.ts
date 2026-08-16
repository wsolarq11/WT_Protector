import { describe, expect, it } from "vitest";
import { assertOnlyJsonKeys, assertPositiveNumber } from "../checks/common.js";

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
