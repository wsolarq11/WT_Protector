import { describe, expect, it } from "vitest";
import { validateGuiSmokeReceipt } from "../checks/run.js";

// guiSmoke 的诚实规则:CI 的 windows runner 有桌面会话,冒烟真实执行过,
// 因此 receipt 不允许把跑过的冒烟记成"跳过"。这些用例锁死那条底线。
describe("guiSmoke receipt honesty", () => {
  it("accepts a smoke that actually ran", () => {
    expect(() => validateGuiSmokeReceipt("launch+title", 1)).not.toThrow();
    // 首次超时、重试成功:状态仍是合法记录,但 attempts=2 让抖动可审计。
    expect(() => validateGuiSmokeReceipt("launch+title", 2)).not.toThrow();
  });

  it("accepts the non-Windows skip", () => {
    expect(() => validateGuiSmokeReceipt("skipped-non-windows", 0)).not.toThrow();
  });

  it("rejects the CI-headless lie that caused the receipt to misreport a real run", () => {
    expect(() => validateGuiSmokeReceipt("skipped-ci-headless", 0)).toThrow(
      /guiSmoke must be "launch\+title" or "skipped-non-windows"/,
    );
  });

  it("rejects an unknown label", () => {
    expect(() => validateGuiSmokeReceipt("hidden-temp-config-window-title", 1)).toThrow(
      /guiSmoke must be/,
    );
    expect(() => validateGuiSmokeReceipt("", 0)).toThrow(/guiSmoke must be/);
  });

  it("rejects claiming a smoke ran with zero attempts", () => {
    expect(() => validateGuiSmokeReceipt("launch+title", 0)).toThrow(/zero attempts/);
  });
});
