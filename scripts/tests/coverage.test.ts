import { describe, expect, it } from "vitest";
import { parseCoverageClasses } from "../test-coverage.js";

const sample = `<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="0.8" branch-rate="0" version="1.9" timestamp="1" lines-covered="8" lines-valid="10">
  <packages>
    <package name="WTLauncher.Core" line-rate="0.8">
      <classes>
        <class name="WTLauncher.Core.LauncherStore" filename="LauncherStore.cs" line-rate="0.75" branch-rate="0">
          <methods />
          <lines />
        </class>
        <class name="WTLauncher.Core.LauncherJson" filename="LauncherJson.cs" line-rate="0.84" branch-rate="0">
          <methods />
          <lines />
        </class>
      </classes>
    </package>
  </packages>
</coverage>`;

describe("coverage XML parsing", () => {
  it("extracts class line rates", () => {
    const classes = parseCoverageClasses(sample);
    expect(classes).toContainEqual({ className: "WTLauncher.Core.LauncherStore", lineRate: 0.75 });
    expect(classes).toContainEqual({ className: "WTLauncher.Core.LauncherJson", lineRate: 0.84 });
  });

  it("returns empty for malformed coverage", () => {
    expect(parseCoverageClasses("<coverage />")).toEqual([]);
  });
});
