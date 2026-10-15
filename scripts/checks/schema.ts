import { type Check, type Ctx, defineCheck } from "../kernel/check.js";

function requireArray(value: unknown, context: string): unknown[] {
  if (!Array.isArray(value)) {
    throw new Error(context + " must be an array");
  }
  return value;
}

export const githubSnapshotShapeCheck: Check = defineCheck({
  id: "github-snapshot-shape",
  tier: "static",
  run: (ctx: Ctx) => {
    ctx.requireRepoPath("scripts/update-github-similar-projects-snapshot.ts", "GitHub snapshot refresh script");
    const filePath = ctx.requireRepoPath("docs/reference/github-similar-projects-snapshot.json", "GitHub similar-project snapshot");
    const snapshot = ctx.readJson<Record<string, unknown>>(filePath);
    ctx.assertOnlyJsonKeys(
      snapshot,
      ["schemaVersion", "capturedAtUtc", "evidenceCommand", "refreshCommand", "queries", "scope", "summary", "repositories"],
      "github-similar-projects-snapshot.json",
    );
    if (Number(snapshot.schemaVersion) !== 3) {
      throw new Error("Unsupported GitHub similar-project snapshot schemaVersion: " + snapshot.schemaVersion);
    }
    if (typeof snapshot.capturedAtUtc !== "string" || snapshot.capturedAtUtc.trim() === "") {
      throw new Error("GitHub similar-project snapshot capturedAtUtc is required");
    }
    const capturedAt = new Date(snapshot.capturedAtUtc);
    if (Number.isNaN(capturedAt.getTime())) {
      throw new Error("GitHub similar-project snapshot capturedAtUtc is not parseable: " + snapshot.capturedAtUtc);
    }
    if (capturedAt.getTime() > Date.now()) {
      throw new Error("GitHub similar-project snapshot capturedAtUtc is in the future: " + snapshot.capturedAtUtc);
    }
    if (typeof snapshot.evidenceCommand !== "string" || !snapshot.evidenceCommand.includes("gh repo view <owner>/<repo>")) {
      throw new Error("GitHub similar-project snapshot must record the gh repo view evidence command");
    }
    if (typeof snapshot.refreshCommand !== "string" || !snapshot.refreshCommand.includes("npm run update:github-snapshot")) {
      throw new Error("GitHub similar-project snapshot must record its repo-local refresh command");
    }
    const summary = snapshot.summary;
    ctx.assertOnlyJsonKeys(
      summary,
      ["repositories", "directNarrowWeaker", "adjacentAxisStronger", "broadMarketStronger", "flowStars", "woxStars"],
      "GitHub similar-project snapshot summary",
    );
    const queries = requireArray(snapshot.queries, "GitHub similar-project snapshot queries");
    for (const query of ["Windows Terminal workspace launcher", "Windows Terminal session manager", "Claude Code launcher"]) {
      if (!queries.includes(query)) {
        throw new Error("GitHub similar-project snapshot missing query: " + query);
      }
    }
    const repos = requireArray(snapshot.repositories, "GitHub similar-project snapshot repositories");
    if (repos.length < 12) {
      throw new Error("GitHub similar-project snapshot should track at least 12 representative repositories; found " + repos.length);
    }
    const repoByName = new Map<string, Record<string, unknown>>();
    const allowedVerdicts = new Set(["direct-narrow-weaker", "adjacent-axis-stronger", "broad-market-stronger"]);
    for (const repo of repos) {
      if (typeof repo !== "object" || repo === null || Array.isArray(repo)) {
        throw new Error("GitHub similar-project snapshot repository must be an object");
      }
      ctx.assertOnlyJsonKeys(
        repo,
        ["fullName", "url", "description", "language", "stars", "forks", "pushedAtUtc", "updatedAtUtc", "archived", "classification", "yardstickVerdict", "strongerAxes", "wtLauncherAxes"],
        "GitHub similar-project snapshot repository",
      );
      const fullName = String(repo.fullName ?? "");
      if (fullName.trim() === "") {
        throw new Error("GitHub similar-project snapshot repository fullName is required");
      }
      if (repoByName.has(fullName)) {
        throw new Error("Duplicate GitHub similar-project snapshot repository: " + fullName);
      }
      if (typeof repo.url !== "string" || !repo.url.startsWith("https://github.com/")) {
        throw new Error("GitHub similar-project snapshot repository url is invalid: " + fullName);
      }
      if (Number(repo.stars) < 0) {
        throw new Error("GitHub similar-project snapshot repository stars must be non-negative: " + fullName);
      }
      if (Number(repo.forks) < 0) {
        throw new Error("GitHub similar-project snapshot repository forks must be non-negative: " + fullName);
      }
      if (typeof repo.archived !== "boolean") {
        throw new Error("GitHub similar-project snapshot repository archived must be boolean: " + fullName);
      }
      if (String(repo.classification ?? "").trim() === "") {
        throw new Error("GitHub similar-project snapshot repository classification is required: " + fullName);
      }
      if (!allowedVerdicts.has(String(repo.yardstickVerdict))) {
        throw new Error("GitHub similar-project snapshot repository yardstickVerdict is unsupported for " + fullName + ": " + repo.yardstickVerdict);
      }
      const strongerAxes = ctx.asArray(repo.strongerAxes);
      const wtLauncherAxes = ctx.asArray(repo.wtLauncherAxes);
      if (strongerAxes.length < 1) {
        throw new Error("GitHub similar-project snapshot repository strongerAxes is required: " + fullName);
      }
      if (wtLauncherAxes.length < 1) {
        throw new Error("GitHub similar-project snapshot repository wtLauncherAxes is required: " + fullName);
      }
      repoByName.set(fullName, repo);
    }
    const requiredRepos = [
      "SyDroX/agentic-cli-toolkit",
      "augustdragon/terminal-manager",
      "noambrand/Launchpad-CLI",
      "kevinkicho/win-screen-setup",
      "ewdieckman/DevWorkspace",
      "jsoulet42/claude-launcher-app",
      "A-ema755/claude-dashboard",
      "adityacodepublic/wtsm-cli",
      "pramin/AgentSpace",
      "joyahmed/devgo-winforms",
      "Flow-Launcher/Flow.Launcher",
      "Wox-launcher/Wox",
    ];
    for (const requiredRepo of requiredRepos) {
      if (!repoByName.has(requiredRepo)) {
        throw new Error("GitHub similar-project snapshot missing representative repo: " + requiredRepo);
      }
    }
    const flowRepo = repoByName.get("Flow-Launcher/Flow.Launcher")!;
    const woxRepo = repoByName.get("Wox-launcher/Wox")!;
    const flowStars = Number(flowRepo.stars);
    const woxStars = Number(woxRepo.stars);
    if (flowStars < 10_000) {
      throw new Error("Flow Launcher market-proof stars unexpectedly low in snapshot: " + flowStars);
    }
    if (woxStars < 10_000) {
      throw new Error("Wox market-proof stars unexpectedly low in snapshot: " + woxStars);
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
      throw new Error("GitHub similar-project snapshot broad-market-stronger count must be 2; found " + broadMarketStrongerCount);
    }
    if (Number(summary.repositories) !== repos.length) {
      throw new Error("GitHub similar-project snapshot summary repositories mismatch: " + summary.repositories + " vs " + repos.length);
    }
    if (Number(summary.directNarrowWeaker) !== directNarrowWeakerCount) {
      throw new Error("GitHub similar-project snapshot summary directNarrowWeaker mismatch: " + summary.directNarrowWeaker + " vs " + directNarrowWeakerCount);
    }
    if (Number(summary.adjacentAxisStronger) !== adjacentAxisStrongerCount) {
      throw new Error("GitHub similar-project snapshot summary adjacentAxisStronger mismatch: " + summary.adjacentAxisStronger + " vs " + adjacentAxisStrongerCount);
    }
    if (Number(summary.broadMarketStronger) !== broadMarketStrongerCount) {
      throw new Error("GitHub similar-project snapshot summary broadMarketStronger mismatch: " + summary.broadMarketStronger + " vs " + broadMarketStrongerCount);
    }
    if (Number(summary.flowStars) !== flowStars) {
      throw new Error("GitHub similar-project snapshot summary flowStars mismatch: " + summary.flowStars + " vs " + flowStars);
    }
    if (Number(summary.woxStars) !== woxStars) {
      throw new Error("GitHub similar-project snapshot summary woxStars mismatch: " + summary.woxStars + " vs " + woxStars);
    }
    return {
      path: "docs\\reference\\github-similar-projects-snapshot.json",
      refreshScript: "scripts\\update-github-similar-projects-snapshot.ts",
      capturedAtUtc: snapshot.capturedAtUtc,
      repositories: summary.repositories,
      directNarrowWeaker: summary.directNarrowWeaker,
      adjacentAxisStronger: summary.adjacentAxisStronger,
      broadMarketStronger: summary.broadMarketStronger,
      flowStars,
      woxStars,
    };
  },
});

/// <summary>对标快照的外部时效性。它与代码正确性没有因果关系:过期只说明人工采集的
/// 市场对标数据需要刷新。门禁将它列为 advisory,避免一个外部数据文件瘫痪全部代码验证。</summary>
export const githubSnapshotFreshnessCheck: Check = defineCheck({
  id: "github-snapshot-freshness",
  tier: "snapshot",
  run: (ctx: Ctx) => {
    const filePath = ctx.requireRepoPath(
      "docs/reference/github-similar-projects-snapshot.json",
      "GitHub similar-project snapshot",
    );
    const snapshot = ctx.readJson<Record<string, unknown>>(filePath);
    const capturedAt = new Date(String(snapshot.capturedAtUtc ?? ""));
    if (Number.isNaN(capturedAt.getTime())) {
      throw new Error("GitHub similar-project snapshot capturedAtUtc is missing or not parseable");
    }
    const maxAgeDays = 30;
    const ageDays = Math.floor((Date.now() - capturedAt.getTime()) / 86_400_000);
    if (ageDays > maxAgeDays) {
      throw new Error(
        `GitHub similar-project snapshot is stale: captured ${snapshot.capturedAtUtc}, ${ageDays} days old (limit ${maxAgeDays}). Refresh with: npm run update:github-snapshot`,
      );
    }
    return { capturedAtUtc: snapshot.capturedAtUtc, ageDays, maxAgeDays };
  },
});

/// <summary>条目目录存在性是本机环境事实,不是配置契约:CI 用 config/launcher.ci.json
/// 覆盖真实配置,因此 CI 上这条检查没有意义。门禁用 local 层标记它并在 CI 上显式留痕跳过,
/// 不再用环境变量在检查内部静默分支。</summary>
export const localEntryDirectoriesCheck: Check = defineCheck({
  id: "local-entry-directories",
  tier: "local",
  run: (ctx: Ctx) => {
    const storePath = ctx.requireRepoPath("config/launcher.json", "local launcher store");
    const store = ctx.readJson<Record<string, unknown>>(storePath);
    const entries = ctx.asArray(store.entries);
    const missing: string[] = [];
    entries.forEach((entry, index) => {
      if (typeof entry !== "object" || entry === null || Array.isArray(entry)) {
        return;
      }
      const item = entry as Record<string, unknown>;
      const directory = item.directory;
      if (typeof directory !== "string" || directory.trim() === "") {
        return;
      }
      if (!ctx.exists(directory)) {
        missing.push(`item ${index} (${String(item.name ?? "")}): ${directory}`);
      }
    });
    if (missing.length > 0) {
      throw new Error("Launcher entry directories missing on this machine: " + missing.join("; "));
    }
    return { entries: entries.length, missing: 0 };
  },
});

export interface LauncherStoreValidation {
  entries: unknown[];
  seenIds: Set<string>;
  defaultPresetId: string;
  presetCount: number;
  referencedEntryIds: Set<string>;
  settingsObject: Record<string, unknown>;
  settingsOrder: unknown[];
}

/// Pure shape validation shared by the repo gate and invalid-value unit tests.
export function validateLauncherStore(
  store: Record<string, unknown>,
  label = "launcher.json",
): LauncherStoreValidation {
  if (Number(store.schemaVersion) !== 2) {
    throw new Error("Unsupported " + label + " schemaVersion: " + store.schemaVersion);
  }
  const entries = asArray(store.entries);
  const seenIds = new Set<string>();
  entries.forEach((entry, index) => {
    if (typeof entry !== "object" || entry === null || Array.isArray(entry)) {
      throw new Error("Config item " + index + " must be an object");
    }
    const item = entry as Record<string, unknown>;
    const id = String(item.id ?? "");
    if (seenIds.has(id)) {
      throw new Error("Duplicate config id: " + id);
    }
    seenIds.add(id);
  });

  const defaultPresetId = String(store.defaultPresetId ?? "");
  if (defaultPresetId.trim() === "") {
    throw new Error(label + " defaultPresetId is required");
  }
  const presets = asArray(store.presets);
  const presetSeen = new Set<string>();
  const referencedEntryIds = new Set<string>();
  let presetCount = 0;
  let nonEmptyPresetCount = 0;
  for (const presetValue of presets) {
    if (typeof presetValue !== "object" || presetValue === null || Array.isArray(presetValue)) {
      throw new Error("Preset " + (presetCount + 1) + " must be an object");
    }
    const preset = presetValue as Record<string, unknown>;
    presetCount += 1;
    const presetId = String(preset.id ?? "");
    if (presetId.trim() === "") {
      throw new Error("Preset " + presetCount + " is missing id");
    }
    if (presetSeen.has(presetId)) {
      throw new Error("Duplicate preset id: " + presetId);
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
        throw new Error("Preset " + presetId + " has blank entry id");
      }
      if (entrySeen.has(entryId)) {
        throw new Error("Preset " + presetId + " has duplicate entry id: " + entryId);
      }
      entrySeen.add(entryId);
      if (!seenIds.has(entryId)) {
        throw new Error("Preset " + presetId + " references missing config entry id: " + entryId);
      }
      referencedEntryIds.add(entryId);
    }
  }
  if (presetCount < 1) {
    throw new Error(label + " must contain at least one preset");
  }
  if (!presetSeen.has(defaultPresetId)) {
    throw new Error("defaultPresetId does not match any preset: " + defaultPresetId);
  }
  if (nonEmptyPresetCount < 1) {
    throw new Error(label + " must contain at least one preset with entryIds");
  }

  const settings = store.settings;
  if (typeof settings !== "object" || settings === null || Array.isArray(settings)) {
    throw new Error(label + " settings is required");
  }
  const settingsObject = settings as Record<string, unknown>;
  const windowSettings = settingsObject.window;
  if (typeof windowSettings !== "object" || windowSettings === null || Array.isArray(windowSettings)) {
    throw new Error(label + " settings window is required");
  }
  assertPositiveNumber((windowSettings as Record<string, unknown>).width, label + " settings window width");
  assertPositiveNumber((windowSettings as Record<string, unknown>).height, label + " settings window height");
  const settingsOrder = asArray(settingsObject.customEntryOrder);
  const settingsOrderSeen = new Set<string>();
  for (const idValue of settingsOrder) {
    const id = String(idValue);
    if (!seenIds.has(id)) {
      throw new Error(label + " settings customEntryOrder references missing config entry id: " + id);
    }
    if (settingsOrderSeen.has(id)) {
      throw new Error(label + " settings customEntryOrder has duplicate config entry id: " + id);
    }
    settingsOrderSeen.add(id);
  }
  if (settingsOrder.length !== seenIds.size) {
    throw new Error(label + " settings customEntryOrder should contain every config entry id once");
  }
  const commandTemplates = settingsObject.commandTemplates;
  const templateKeys = new Set<string>();
  if (commandTemplates !== undefined && commandTemplates !== null) {
    if (typeof commandTemplates !== "object" || Array.isArray(commandTemplates)) {
      throw new Error(label + " settings commandTemplates must be an object");
    }
    for (const [key, value] of Object.entries(commandTemplates)) {
      if (typeof value !== "string" || value.trim() === "") {
        throw new Error(label + " settings commandTemplates key '" + key + "' must be a non-empty string");
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
        throw new Error("Config item '" + entry.id + "' command references undefined template key: " + refKey);
      }
    }
  }

  return { entries, seenIds, defaultPresetId, presetCount, referencedEntryIds, settingsObject, settingsOrder };
}

function asArray<T>(value: T | T[] | undefined | null): T[] {
  if (value === undefined || value === null) {
    return [];
  }
  return Array.isArray(value) ? value : [value];
}

function assertPositiveNumber(value: unknown, context: string): number {
  if (value === undefined || value === null) {
    throw new Error(`${context} is required`);
  }
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0) {
    throw new Error(`${context} must be positive`);
  }
  return number;
}

/** 活动条目标识的计算。此前由 checkLauncherStore 的返回值经 verify.ts 的闭包变量
 * 传给退场检查;改成普通函数后两条检查各自读一次本地 store,数据上互不依赖,
 * 闭包耦合消失(store 是本地小文件,重读代价可忽略)。 */
function activeEntryIds(ctx: Ctx): Set<string> {
  const storePath = ctx.requireRepoPath("config/launcher.json", "portable launcher store");
  const store = ctx.readJson<Record<string, unknown>>(storePath);
  return validateLauncherStore(store).seenIds;
}

export const launcherStoreCheck: Check = defineCheck({
  id: "launcher-store",
  tier: "static",
  run: (ctx: Ctx) => {
    const storePath = ctx.requireRepoPath("config/launcher.json", "portable launcher store");
    const store = ctx.readJson<Record<string, unknown>>(storePath);
    const validation = validateLauncherStore(store);
    // keep all four records as the evidence payload:
    return {
      records: [
        { name: "json", detail: { path: "config\\launcher.json", items: validation.entries.length } },
        { name: "config-schema", detail: { uniqueIds: validation.seenIds.size, selectedFields: validation.entries.length, selectedCapable: true } },
        { name: "presets-schema", detail: { path: "config\\launcher.json", presets: validation.presetCount, defaultPresetId: validation.defaultPresetId, referencedEntries: validation.referencedEntryIds.size } },
        { name: "settings-json", detail: { path: "config\\launcher.json", sortMode: validation.settingsObject.sortMode, entryViewMode: validation.settingsObject.entryViewMode, requireConfirmationForLaunches: validation.settingsObject.requireConfirmationForLaunches, customEntryOrder: validation.settingsOrder.length, hasWindow: validation.settingsObject.window !== undefined && validation.settingsObject.window !== null } },
      ],
      activeIds: [...validation.seenIds],
    };
  },
});

export const configExampleSchemaCheck: Check = defineCheck({
  id: "config-example-schema",
  tier: "static",
  run: (ctx: Ctx) => {
    const filePath = ctx.requireRepoPath("config/launcher.example.json", "example launcher store");
    const example = ctx.readJson<Record<string, unknown>>(filePath);
    if (Number(example.schemaVersion) !== 2) {
      throw new Error("Example launcher store schemaVersion != 2: " + filePath);
    }
    if (ctx.asArray(example.entries).length !== 0) {
      throw new Error("Example launcher store must ship with zero entries: " + filePath);
    }
    if (String(example.defaultPresetId ?? "").trim() === "") {
      throw new Error("Example launcher store defaultPresetId is required");
    }
    for (const presetValue of ctx.asArray(example.presets)) {
      const preset = presetValue as Record<string, unknown>;
      if (preset.id === "blank-manual" && preset.locked !== true) {
        throw new Error("Example launcher store blank-manual preset must stay locked");
      }
    }
    const exampleText = ctx.readText("config/launcher.example.json");
    if (/([A-Za-z]:\\)|(\/Users\/)/.test(exampleText)) {
      throw new Error("Example launcher store must not contain absolute machine paths: " + filePath);
    }
    return {
      path: "config\\launcher.example.json",
      schemaVersion: Number(example.schemaVersion),
      entries: ctx.asArray(example.entries).length,
      defaultPresetId: String(example.defaultPresetId),
      noAbsolutePaths: true,
    };
  },
});

export const shortcutBackupRetirementCheck: Check = defineCheck({
  id: "shortcut-backup-retirement",
  tier: "static",
  needs: ["launcher-store"],
  run: (ctx: Ctx) => {
    const activeIds = activeEntryIds(ctx);
    const manifest = ctx.readJson<Record<string, unknown>>(ctx.requireRepoPath("docs/reference/shortcut-backup-manifest.json", "shortcut backup manifest"));
    const shortcuts = ctx.asArray(manifest.shortcuts);
    const convertedConfigIds = ctx.asArray(manifest.convertedConfigIds).map((id) => String(id));
    if (shortcuts.length < 1) {
      throw new Error("shortcut backup manifest has no shortcuts");
    }
    if (convertedConfigIds.length < 1) {
      throw new Error("shortcut backup manifest did not record any converted config ids");
    }
    for (const id of convertedConfigIds) {
      if (!activeIds.has(id)) {
        throw new Error("Converted shortcut id missing from active config: " + id);
      }
    }
    return {
      manifest: "docs\\reference\\shortcut-backup-manifest.json",
      shortcuts: shortcuts.length,
      convertedConfigIds: convertedConfigIds.length,
      sourceDirectoryRemoved: !ctx.exists(ctx.repoPath("WT_Shortcuts_Backup")),
    };
  },
});

export const configRequiredFieldsCheck: Check = defineCheck({
  id: "config-required-fields",
  tier: "static",
  run: (ctx: Ctx) => {
    const coreRoot = ctx.repoPath("src/avalonia-launcher/src/WTLauncher.Core");
    const requiredByRecord = new Map<string, Set<string>>();
    const propNamePattern = /\[JsonPropertyName\("(\w+)"\)\]/;
    const requiredPattern = /\[JsonRequired\]/;
    const recordPattern = /public\s+(?:sealed\s+)?record\s+(\w+)/;

    const csFiles = ctx.listFiles(coreRoot);
    for (const filePath of csFiles) {
      const normalized = filePath.replaceAll("\\", "/");
      if (
        !filePath.toLowerCase().endsWith(".cs") ||
        filePath.includes("AssemblyInfo") ||
        normalized.includes("/obj/") ||
        normalized.includes("/bin/")
      ) {
        continue;
      }
      const content = ctx.readText(filePath);
      if (!recordPattern.test(content)) {
        continue;
      }

      const lines = content.split(/\r?\n/);
      let currentRecord = "";
      for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        const recordMatch = line.match(recordPattern);
        if (recordMatch) {
          currentRecord = recordMatch[1];
        }
        if (!requiredPattern.test(line)) {
          continue;
        }
        const nameMatch = lines[i - 1]?.match(propNamePattern);
        if (nameMatch) {
          const fields = requiredByRecord.get(currentRecord) ?? new Set<string>();
          fields.add(nameMatch[1]);
          requiredByRecord.set(currentRecord, fields);
        }
      }
    }

    // 只把真正的 launcher.json 记录纳入校验。LauncherPresetFile 是内存态，
    // LauncherUpdateManifest 是独立发布清单，二者不属于 launcher.json schema。
    const storeRecords = [
      "LauncherStoreFile",
      "LaunchEntry",
      "LauncherPreset",
      "LauncherSettings",
      "LauncherWindowSettings",
      "LauncherRunHistoryEntry",
    ];
    const requiredFields = new Set<string>();
    for (const recordName of storeRecords) {
      for (const field of requiredByRecord.get(recordName) ?? []) {
        requiredFields.add(field);
      }
    }
    if (requiredFields.size === 0) {
      throw new Error("Could not extract any [JsonRequired] fields from launcher-store records");
    }

    const storeRequired = requiredByRecord.get("LauncherStoreFile") ?? new Set<string>();
    const entryRequired = requiredByRecord.get("LaunchEntry") ?? new Set<string>();
    const presetRequired = requiredByRecord.get("LauncherPreset") ?? new Set<string>();
    const settingsRequired = requiredByRecord.get("LauncherSettings") ?? new Set<string>();
    const windowRequired = requiredByRecord.get("LauncherWindowSettings") ?? new Set<string>();
    const historyRequired = requiredByRecord.get("LauncherRunHistoryEntry") ?? new Set<string>();

    const configs = [
      { path: "config/launcher.json", label: "launcher.json" },
      { path: "config/launcher.ci.json", label: "launcher.ci.json" },
      { path: "config/launcher.example.json", label: "launcher.example.json" },
    ];

    const results: Record<string, unknown> = {
      totalRequired: requiredFields.size,
      settingsRequired: settingsRequired.size,
      windowRequired: windowRequired.size,
    };

    const isObject = (value: unknown): value is Record<string, unknown> =>
      typeof value === "object" && value !== null && !Array.isArray(value);

    const collectMissing = (target: Record<string, unknown> | undefined, fields: Set<string>, prefix: string, index?: number): string[] => {
      const missing: string[] = [];
      const label = index === undefined ? prefix : `${prefix}[${index}]`;
      if (target === undefined) {
        missing.push(label);
        return missing;
      }
      for (const field of fields) {
        if (target[field] === undefined) {
          missing.push(`${label}.${field}`);
        }
      }
      return missing;
    };

    for (const config of configs) {
      if (!ctx.exists(ctx.repoPath(config.path))) {
        results[config.label] = "missing-file";
        continue;
      }
      const store = ctx.readJson<Record<string, unknown>>(ctx.repoPath(config.path));
      const missing: string[] = [];

      missing.push(...collectMissing(store, storeRequired, config.label));

      const entries = ctx.asArray(store.entries);
      entries.forEach((entry, index) => {
        if (!isObject(entry)) {
          missing.push(`${config.label}.entries[${index}]`);
          return;
        }
        missing.push(...collectMissing(entry, entryRequired, `${config.label}.entries`, index));
      });

      const presets = ctx.asArray(store.presets);
      presets.forEach((preset, index) => {
        if (!isObject(preset)) {
          missing.push(`${config.label}.presets[${index}]`);
          return;
        }
        missing.push(...collectMissing(preset, presetRequired, `${config.label}.presets`, index));
      });

      const settings = isObject(store.settings) ? store.settings : undefined;
      missing.push(...collectMissing(settings, settingsRequired, `${config.label}.settings`));
      const windowSettings = settings !== undefined && isObject(settings.window) ? settings.window : undefined;
      missing.push(...collectMissing(windowSettings, windowRequired, `${config.label}.settings.window`));

      const runHistory = ctx.asArray(store.runHistory);
      runHistory.forEach((item, index) => {
        if (!isObject(item)) {
          missing.push(`${config.label}.runHistory[${index}]`);
          return;
        }
        missing.push(...collectMissing(item, historyRequired, `${config.label}.runHistory`, index));
      });

      if (missing.length > 0) {
        throw new Error(
          config.label + " is missing required [JsonRequired] fields: " + missing.join(", "),
        );
      }
      results[config.label] = "ok";
    }

    return results;
  },
});
