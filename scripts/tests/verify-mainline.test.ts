import { describe, expect, it } from "vitest";
import { parseTrxCounters } from "../verify-mainline.js";

const sample = `<?xml version="1.0" encoding="utf-8"?>
<TestRun id="x">
  <ResultSummary outcome="Completed">
    <Counters total="832" executed="832" passed="832" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
  </ResultSummary>
</TestRun>`;

describe("TRX counter parsing", () => {
  it("extracts total/passed/failed", () => {
    expect(parseTrxCounters(sample)).toEqual({ total: 832, passed: 832, failed: 0 });
  });

  it("rejects missing counters", () => {
    expect(() => parseTrxCounters("<TestRun />")).toThrow("TRX ResultSummary.Counters missing");
  });
});
