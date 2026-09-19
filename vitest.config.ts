import { configDefaults, defineConfig } from "vitest/config";

// .snow/ is git-ignored local state (backups, scratch probes, indexes). Vitest's
// default exclude does not know about it, so a stray *.test.ts written into that
// directory would silently join the suite and fail against the real repo layout.
export default defineConfig({
  test: {
    exclude: [...configDefaults.exclude, "**/.snow/**"],
  },
});
