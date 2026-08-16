import { existsSync } from "node:fs";
import { readJson } from "../config-lib.js";
import {
  addCheck,
  asArray,
  assertOnlyJsonKeys,
  assertPositiveNumber,
  readText,
  repoPath,
  requireRepoPath,
} from "./common.js";

function requireArray(value: unknown, context: string): unknown[] {
  if (!Array.isArray(value)) {
    throw new Error(`${context} must be an array`);
  }
  return value;
}

export function checkGithubSnapshot(): void {
  requireRepoPath("tools/update-github-similar-projects-snapshot.ps1", "GitHub snapshot refresh script");
  const filePath = requireRepoPath("docs/reference/github-similar-projects-snapshot.json", "GitHub similar-project snapshot");
  const snapshot = readJson<Record<string, unknown>>(filePath);
  assertOnlyJsonKeys(
    snapshot,
    ["schemaVersion", "capturedAtUtc", "evidenceCommand", "refreshCommand", "queries", "scope", "summary", "repositories"],
    "github-similar-projects-snapshot.json",
  );
  if (Number(snapshot.schemaVersion) !== 3) {
    throw new Error(`Unsupported GitHub similar-project snapshot schemaVersion: ${snapshot.schemaVersion}`);
  }
  if (typeof snapshot.capturedAtUtc !== "string" || snapshot.capturedAtUtc.trim() === "") {
    throw new Error("GitHub similar-project snapshot capturedAtUtc is required");
  }
  const capturedAt = new Date(snapshot.capturedAtUtc);
  if (Number.isNaN(capturedAt.getTime())) {
    throw new Error(`GitHub similar-project snapshot capturedAtUtc is not parseable: ${snapshot.capturedAtUtc}`);
  }
  const maxAgeDays = 30;
  const ageMs = Date.now() - capturedAt.getTime();
  if (ageMs < 0) {
    throw new Error(`GitHub similar-project snapshot capturedAtUtc is in the future: ${snapshot.capturedAtUtc}`);
  }
  const ageDays = ageMs / 86_400_000;
  if (ageDays > maxAgeDays) {
    throw new Error(
      `GitHub similar-project snapshot is stale: ${snapshot.capturedAtUtc}. Refresh with: powershell -NoProfile -ExecutionPolicy Bypass -File .\\tools\\update-github-similar-projects-snapshot.ps1`,
    );
  }
  if (typeof snapshot.evidenceCommand !== "string" || !snapshot.evidenceCommand.includes("gh repo view <owner>/<repo>")) {
    throw new Error("GitHub similar-project snapshot must record the gh repo view evidence command");
  }
  if (typeof snapshot.refreshCommand !== "string" || !snapshot.refreshCommand.includes("tools\\update-github-similar-projects-snapshot.ps1")) {
    throw new Error("GitHub similar-project snapshot must record its repo-local refresh command");
  }
  const summary = snapshot.summary;
  assertOnlyJsonKeys(
    summary,
    ["repositories", "directNarrowWeaker", "adjacentAxisStronger", "broadMarketStronger", "flowStars", "woxStars"],
    "GitHub similar-project snapshot summary",
  );
  const queries = requireArray(snapshot.queries, "GitHub similar-project snapshot queries");
  for (const query of ["Windows Terminal workspace launcher", "Windows Terminal session manager", "Claude Code launcher"]) {
    if (!queries.includes(query)) {
      throw new Error(`GitHub similar-project snapshot missing query: ${query}`);
    }
  }
  const repos = requireArray(snapshot.repositories, "GitHub similar-project snapshot repositories");
  if (repos.length < 12) {
    throw new Error(`GitHub similar-project snapshot should track at least 12 representative repositories; found ${repos.length}`);
  }
  const repoByName = new Map<string, Record<string, unknown>>();
  const allowedVerdicts = new Set(["direct-narrow-weaker", "adjacent-axis-stronger", "broad-market-stronger"]);
  for (const repo of repos) {
    if (typeof repo !== "object" || repo === null || Array.isArray(repo)) {
      throw new Error("GitHub similar-project snapshot repository must be an object");
    }
    assertOnlyJsonKeys(
      repo,
      ["fullName", "url", "description", "language", "stars", "forks", "pushedAtUtc", "updatedAtUtc", "archived", "classification", "yardstickVerdict", "strongerAxes", "wtLauncherAxes"],
      "GitHub similar-project snapshot repository",
    );
    const fullName = String(repo.fullName ?? "");
    if (fullName.trim() === "") {
      throw new Error("GitHub similar-project snapshot repository fullName is required");
    }
    if (repoByName.has(fullName)) {
      throw new Error(`Duplicate GitHub similar-project snapshot repository: ${fullName}`);
    }
    if (typeof repo.url !== "string" || !repo.url.startsWith("https://github.com/")) {
      throw new Error(`GitHub similar-project snapshot repository url is invalid: ${fullName}`);
    }
    if (Number(repo.stars) < 0) {
      throw new Error(`GitHub similar-project snapshot repository stars must be non-negative: ${fullName}`);
    }
    if (Number(repo.forks) < 0) {
      throw new Error(`GitHub similar-project snapshot repository forks must be non-negative: ${fullName}`);
    }
    if (typeof repo.archived !== "boolean") {
      throw new Error(`GitHub similar-project snapshot repository archived must be boolean: ${fullName}`);
    }
    if (String(repo.classification ?? "").trim() === "") {
      throw new Error(`GitHub similar-project snapshot repository classification is required: ${fullName}`);
    }
    if (!allowedVerdicts.has(String(repo.yardstickVerdict))) {
      throw new Error(`GitHub similar-project snapshot repository yardstickVerdict is unsupported for ${fullName}: ${repo.yardstickVerdict}`);
    }
    const strongerAxes = asArray(repo.strongerAxes);
    const wtLauncherAxes = asArray(repo.wtLauncherAxes);
    if (strongerAxes.length < 1) {
      throw new Error(`GitHub similar-project snapshot repository strongerAxes is required: ${fullName}`);
    }
    if (wtLauncherAxes.length < 1) {
      throw new Error(`GitHub similar-project snapshot repository wtLauncherAxes is required: ${fullName}`);
    }
    repoByName.set(fullName, repo);
  }
  const requiredRepos = [
    "SyDroX/dev-layout",
    "augustdragon/terminal-manager",
    "noambrand/Launchpad-CLI",
    "kevinkicho/win-screen-setup",
    "ewdieckman/DevWorkspace",
    "jsoulet42/claude-launcher-app",
    "A-ema755/claude-dashboard",
    "adityacodepublic/wtsm-cli",
    "pramin/AgentSpace",
    "joyahmed/devgo-app",
    "Flow-Launcher/Flow.Launcher",
    "Wox-launcher/Wox",
  ];
  for (const requiredRepo of requiredRepos) {
    if (!repoByName.has(requiredRepo)) {
      throw new Error(`GitHub similar-project snapshot missing representative repo: ${requiredRepo}`);
    }
  }
  const flowRepo = repoByName.get("Flow-Launcher/Flow.Launcher")!;
  const woxRepo = repoByName.get("Wox-launcher/Wox")!;
  const flowStars = Number(flowRepo.stars);
  const woxStars = Number(woxRepo.stars);
  if (flowStars < 10_000) {
    throw new Error(`Flow Launcher market-proof stars unexpectedly low in snapshot: ${flowStars}`);
  }
  if (woxStars < 10_000) {
    throw new Error(`Wox market-proof stars unexpectedly low in snapshot: ${woxStars}`);
  }
  if (flowRepo.yardstickVerdict !== "broad-market-stronger") {
    throw new Error("Flow Launcher must remain classified as broad-market-stronger");
  }
  if (woxRepo.yardstickVerdict !== "broad-market-stronger") {
    throw new Error("Wox must remain classified as broad-market-stronger");
  }
  const directNarrowWeakerCount = repos.filter((repo) => (repo as Record<string, unknown>).yardstickVerdict === "direct-narrow-weaker").length;
  const adjacentAxisStrongerCount = repos.filter((repo) => (repo as Record<string, unknown>).yardstickVerdict === "adjacent-axis-stronger").length;
  const broadMarketStrongerCount = repos.filter((repo) => (repo as Record<string, unknown>).yardstickVerdict === "broad-market-stronger").length;
  if (broadMarketStrongerCount !== 2) {
    throw new Error(`GitHub similar-project snapshot broad-market-stronger count must be 2; found ${broadMarketStrongerCount}`);
  }
  if (Number(summary.repositories) !== repos.length) {
    throw new Error(`GitHub similar-project snapshot summary repositories mismatch: ${summary.repositories} vs ${repos.length}`);
  }
  if (Number(summary.directNarrowWeaker) !== directNarrowWeakerCount) {
    throw new Error(`GitHub similar-project snapshot summary directNarrowWeaker mismatch: ${summary.directNarrowWeaker} vs ${directNarrowWeakerCount}`);
  }
  if (Number(summary.adjacentAxisStronger) !== adjacentAxisStrongerCount) {
    throw new Error(`GitHub similar-project snapshot summary adjacentAxisStronger mismatch: ${summary.adjacentAxisStronger} vs ${adjacentAxisStrongerCount}`);
  }
  if (Number(summary.broadMarketStronger) !== broadMarketStrongerCount) {
    throw new Error(`GitHub similar-project snapshot summary broadMarketStronger mismatch: ${summary.broadMarketStronger} vs ${broadMarketStrongerCount}`);
  }
  if (Number(summary.flowStars) !== flowStars) {
    throw new Error(`GitHub similar-project snapshot summary flowStars mismatch: ${summary.flowStars} vs ${flowStars}`);
  }
  if (Number(summary.woxStars) !== woxStars) {
    throw new Error(`GitHub similar-project snapshot summary woxStars mismatch: ${summary.woxStars} vs ${woxStars}`);
  }
  addCheck("github-similar-projects-snapshot", {
    path: "docs\\reference\\github-similar-projects-snapshot.json",
    refreshScript: "tools\\update-github-similar-projects-snapshot.ps1",
    capturedAtUtc: snapshot.capturedAtUtc,
    maxAgeDays,
    ageDays: Math.round(ageDays * 100) / 100,
    repositories: summary.repositories,
    directNarrowWeaker: summary.directNarrowWeaker,
    adjacentAxisStronger: summary.adjacentAxisStronger,
    broadMarketStronger: summary.broadMarketStronger,
    flowStars,
    woxStars,
  });
}

export function checkLauncherStore(): Set<string> {
  const storePath = requireRepoPath("config/launcher.json", "portable launcher store");
  const store = readJson<Record<string, unknown>>(storePath);
  if (Number(store.schemaVersion) !== 2) {
    throw new Error(`Unsupported launcher.json schemaVersion: ${store.schemaVersion}`);
  }
  const entries = asArray(store.entries);
  const seenIds = new Set<string>();
  let selectedFields = 0;
  entries.forEach((entry, index) => {
    if (typeof entry !== "object" || entry === null || Array.isArray(entry)) {
      throw new Error(`Config item ${index} must be an object`);
    }
    const item = entry as Record<string, unknown>;
    const id = String(item.id ?? "");
    if (process.env.CI !== "true" && typeof item.directory === "string" && !existsSync(item.directory)) {
      throw new Error(`Config item ${index} directory does not exist: ${item.directory}`);
    }
    selectedFields += 1;
    if (seenIds.has(id)) {
      throw new Error(`Duplicate config id: ${id}`);
    }
    seenIds.add(id);
  });
  addCheck("json", { path: "config\\launcher.json", items: entries.length });
  addCheck("config-schema", {
    uniqueIds: seenIds.size,
    selectedFields,
    selectedCapable: selectedFields === entries.length,
    existingDirectories: entries.length,
  });

  const defaultPresetId = String(store.defaultPresetId ?? "");
  if (defaultPresetId.trim() === "") {
    throw new Error("launcher.json defaultPresetId is required");
  }
  const presets = asArray(store.presets);
  const presetSeen = new Set<string>();
  const referencedEntryIds = new Set<string>();
  let presetCount = 0;
  let nonEmptyPresetCount = 0;
  for (const presetValue of presets) {
    if (typeof presetValue !== "object" || presetValue === null || Array.isArray(presetValue)) {
      throw new Error(`Preset ${presetCount + 1} must be an object`);
    }
    const preset = presetValue as Record<string, unknown>;
    presetCount += 1;
    const presetId = String(preset.id ?? "");
    if (presetId.trim() === "") {
      throw new Error(`Preset ${presetCount} is missing id`);
    }
    if (presetSeen.has(presetId)) {
      throw new Error(`Duplicate preset id: ${presetId}`);
    }
    presetSeen.add(presetId);
    if (presetId === "blank-manual" && preset.locked !== true) {
      throw new Error("Preset blank-manual must stay locked so manual mode cannot be deleted in the UI");
    }
    const entryIds = asArray(preset.entryIds);
    if (entryIds.length > 0) {
      nonEmptyPresetCount += 1;
    }
    const entrySeen = new Set<string>();
    for (const entryIdValue of entryIds) {
      const entryId = String(entryIdValue);
      if (entryId.trim() === "") {
        throw new Error(`Preset ${presetId} has blank entry id`);
      }
      if (entrySeen.has(entryId)) {
        throw new Error(`Preset ${presetId} has duplicate entry id: ${entryId}`);
      }
      entrySeen.add(entryId);
      if (!seenIds.has(entryId)) {
        throw new Error(`Preset ${presetId} references missing config entry id: ${entryId}`);
      }
      referencedEntryIds.add(entryId);
    }
  }
  if (presetCount < 1) {
    throw new Error("launcher.json must contain at least one preset");
  }
  if (!presetSeen.has(defaultPresetId)) {
    throw new Error(`defaultPresetId does not match any preset: ${defaultPresetId}`);
  }
  if (nonEmptyPresetCount < 1) {
    throw new Error("launcher.json must contain at least one preset with entryIds");
  }
  addCheck("presets-schema", {
    path: "config\\launcher.json",
    presets: presetCount,
    defaultPresetId,
    referencedEntries: referencedEntryIds.size,
  });

  const settings = store.settings;
  if (typeof settings !== "object" || settings === null || Array.isArray(settings)) {
    throw new Error("launcher.json settings is required");
  }
  const settingsObject = settings as Record<string, unknown>;
  const windowSettings = settingsObject.window;
  if (typeof windowSettings !== "object" || windowSettings === null || Array.isArray(windowSettings)) {
    throw new Error("launcher.json settings window is required");
  }
  assertPositiveNumber((windowSettings as Record<string, unknown>).width, "launcher.json settings window width");
  assertPositiveNumber((windowSettings as Record<string, unknown>).height, "launcher.json settings window height");
  const settingsOrder = asArray(settingsObject.customEntryOrder);
  const settingsOrderSeen = new Set<string>();
  for (const idValue of settingsOrder) {
    const id = String(idValue);
    if (!seenIds.has(id)) {
      throw new Error(`launcher.json settings customEntryOrder references missing config entry id: ${id}`);
    }
    if (settingsOrderSeen.has(id)) {
      throw new Error(`launcher.json settings customEntryOrder has duplicate config entry id: ${id}`);
    }
    settingsOrderSeen.add(id);
  }
  if (settingsOrder.length !== seenIds.size) {
    throw new Error("launcher.json settings customEntryOrder should contain every config entry id once");
  }
  const commandTemplates = settingsObject.commandTemplates;
  const templateKeys = new Set<string>();
  if (commandTemplates !== undefined && commandTemplates !== null) {
    if (typeof commandTemplates !== "object" || Array.isArray(commandTemplates)) {
      throw new Error("launcher.json settings commandTemplates must be an object");
    }
    for (const [key, value] of Object.entries(commandTemplates)) {
      if (typeof value !== "string" || value.trim() === "") {
        throw new Error(`launcher.json settings commandTemplates key '${key}' must be a non-empty string`);
      }
      templateKeys.add(key);
    }
  }
  for (const entryValue of entries) {
    const entry = entryValue as Record<string, unknown>;
    const command = String(entry.command ?? "");
    if (command.length > 1 && command.startsWith("$") && command[1] !== "$") {
      const refKey = command.slice(1);
      if (!templateKeys.has(refKey)) {
        throw new Error(`Config item '${entry.id}' command references undefined template key: ${refKey}`);
      }
    }
  }
  addCheck("settings-json", {
    path: "config\\launcher.json",
    sortMode: settingsObject.sortMode,
    entryViewMode: settingsObject.entryViewMode,
    requireConfirmationForLaunches: settingsObject.requireConfirmationForLaunches,
    customEntryOrder: settingsOrder.length,
    hasWindow: settingsObject.window !== undefined && settingsObject.window !== null,
  });
  return seenIds;
}

export function checkConfigExampleSchema(): void {
  const filePath = requireRepoPath("config/launcher.example.json", "example launcher store");
  const example = readJson<Record<string, unknown>>(filePath);
  if (Number(example.schemaVersion) !== 2) {
    throw new Error(`Example launcher store schemaVersion != 2: ${filePath}`);
  }
  if (asArray(example.entries).length !== 0) {
    throw new Error(`Example launcher store must ship with zero entries: ${filePath}`);
  }
  if (String(example.defaultPresetId ?? "").trim() === "") {
    throw new Error("Example launcher store defaultPresetId is required");
  }
  for (const presetValue of asArray(example.presets)) {
    const preset = presetValue as Record<string, unknown>;
    if (preset.id === "blank-manual" && preset.locked !== true) {
      throw new Error("Example launcher store blank-manual preset must stay locked");
    }
  }
  const exampleText = readText("config/launcher.example.json");
  if (/([A-Za-z]:\\)|(\/Users\/)/.test(exampleText)) {
    throw new Error(`Example launcher store must not contain absolute machine paths: ${filePath}`);
  }
  addCheck("config-example-schema", {
    path: "config\\launcher.example.json",
    schemaVersion: Number(example.schemaVersion),
    entries: asArray(example.entries).length,
    defaultPresetId: String(example.defaultPresetId),
    noAbsolutePaths: true,
  });
}

export function checkShortcutBackupRetirement(activeIds: Set<string>): void {
  const manifest = readJson<Record<string, unknown>>(requireRepoPath("docs/reference/shortcut-backup-manifest.json", "shortcut backup manifest"));
  const shortcuts = asArray(manifest.shortcuts);
  const convertedConfigIds = asArray(manifest.convertedConfigIds).map((id) => String(id));
  if (shortcuts.length < 1) {
    throw new Error("shortcut backup manifest has no shortcuts");
  }
  if (convertedConfigIds.length < 1) {
    throw new Error("shortcut backup manifest did not record any converted config ids");
  }
  for (const id of convertedConfigIds) {
    if (!activeIds.has(id)) {
      throw new Error(`Converted shortcut id missing from active config: ${id}`);
    }
  }
  addCheck("shortcut-backup-retirement", {
    manifest: "docs\\reference\\shortcut-backup-manifest.json",
    shortcuts: shortcuts.length,
    convertedConfigIds: convertedConfigIds.length,
    sourceDirectoryRemoved: !existsSync(repoPath("WT_Shortcuts_Backup")),
  });
}
