import { describe, expect, it } from "vitest";
import { validateLauncherStore } from "../checks/schema.js";

function validStore(): Record<string, unknown> {
  return {
    schemaVersion: 2,
    entries: [{ id: "one", name: "One", directory: "C:\\tmp", command: "pwsh" }],
    presets: [{ id: "default", name: "Default", entryIds: ["one"], locked: false }],
    defaultPresetId: "default",
    settings: {
      window: { width: 800, height: 600 },
      customEntryOrder: ["one"],
      commandTemplates: {},
    },
  };
}

describe("validateLauncherStore invalid values", () => {
  it("rejects duplicate config ids", () => {
    const store = validStore();
    store.entries = [
      { id: "one", name: "One", command: "pwsh" },
      { id: "one", name: "One copy", command: "pwsh" },
    ];
    expect(() => validateLauncherStore(store, "test")).toThrow("Duplicate config id: one");
  });

  it("rejects preset references to missing entries", () => {
    const store = validStore();
    store.presets = [{ id: "default", name: "Default", entryIds: ["ghost"], locked: false }];
    expect(() => validateLauncherStore(store, "test")).toThrow(
      "references missing config entry id: ghost",
    );
  });

  it("rejects commands referencing undefined template keys", () => {
    const store = validStore();
    store.entries = [{ id: "one", name: "One", command: "$missing" }];
    expect(() => validateLauncherStore(store, "test")).toThrow(
      "command references undefined template key: missing",
    );
  });

  it("rejects non-positive window dimensions", () => {
    const store = validStore();
    store.settings = {
      window: { width: 0, height: 600 },
      customEntryOrder: ["one"],
      commandTemplates: {},
    };
    expect(() => validateLauncherStore(store, "test")).toThrow(
      "test settings window width must be positive",
    );
  });
});
