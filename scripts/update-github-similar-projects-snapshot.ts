import { fileURLToPath } from "node:url";
import path from "node:path";
import { execa } from "execa";
import { repoRoot, writeJsonNoBom } from "./config-lib.js";

const snapshotPath = path.join(repoRoot, "docs", "reference", "github-similar-projects-snapshot.json");
const fields = "nameWithOwner,url,description,stargazerCount,forkCount,pushedAt,updatedAt,primaryLanguage,isArchived";
const evidenceCommand = `gh repo view <owner>/<repo> --json ${fields}`;

interface RepoNote {
  fullName: string;
  classification: string;
  yardstickVerdict: string;
  strongerAxes: string[];
  wtLauncherAxes: string[];
}

const repoNotes: RepoNote[] = [
  {
    fullName: "SyDroX/dev-layout",
    classification: "direct WT workspace launcher",
    yardstickVerdict: "direct-narrow-weaker",
    strongerAxes: ["fixed Claude Code multi-repo layout", "session resume", "cd blocking"],
    wtLauncherAxes: ["editable portable UI entries", "named launch groups", "rollback-aware JSON persistence", "native gate"],
  },
  {
    fullName: "augustdragon/terminal-manager",
    classification: "direct WT profile/session manager",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["WT profile editing", "split-pane groups", "import/export", "WT settings sync"],
    wtLauncherAxes: ["smaller local JSON-to-wt contract", "no local web server", "does not mutate Windows Terminal settings"],
  },
  {
    fullName: "noambrand/Launchpad-CLI",
    classification: "Claude Code launcher/installer",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["installer", "folder picker", "right-click launch", "statusline distribution"],
    wtLauncherAxes: ["portable editable WT workspace groups", "rollback-aware config gate", "smaller app scope"],
  },
  {
    fullName: "kevinkicho/win-screen-setup",
    classification: "Windows multi-monitor workspace launcher",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["visual screen/app positioning"],
    wtLauncherAxes: ["narrower side-effect surface", "portable launch groups", "native repo gate"],
  },
  {
    fullName: "ewdieckman/DevWorkspace",
    classification: "PowerShell WT multi-pane workspace module",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["fixed split-pane layouts"],
    wtLauncherAxes: ["editable GUI entries", "rollback-aware persistence", "native publish/verify gate"],
  },
  {
    fullName: "jsoulet42/claude-launcher-app",
    classification: "Claude Code WT TUI workspace launcher",
    yardstickVerdict: "direct-narrow-weaker",
    strongerAxes: ["adjacent TUI flow"],
    wtLauncherAxes: ["portable editable GUI entries", "named groups", "rollback-aware JSON", "native gate"],
  },
  {
    fullName: "A-ema755/claude-dashboard",
    classification: "Claude Code CLI session launcher/preset manager",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["split-pane layouts", "cost tracking"],
    wtLauncherAxes: ["small local WT launcher contract", "portable GUI config", "native gate"],
  },
  {
    fullName: "adityacodepublic/wtsm-cli",
    classification: "Windows Terminal session manager CLI",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["named terminal sessions CLI"],
    wtLauncherAxes: ["GUI editing", "launch-group preview", "repo-native rollback/verify evidence"],
  },
  {
    fullName: "pramin/AgentSpace",
    classification: "Avalonia agent workspace launcher",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["embedded PTY", "SSH file tree", "code preview"],
    wtLauncherAxes: ["smaller WT handoff scope", "portable JSON groups", "native gate"],
  },
  {
    fullName: "joyahmed/devgo-app",
    classification: "WSL-first developer launcher",
    yardstickVerdict: "adjacent-axis-stronger",
    strongerAxes: ["WSL", "VS Code", "tmux workflows", "installer ambition"],
    wtLauncherAxes: ["verified portable WT launch groups", "rollback-aware JSON", "narrower scope"],
  },
  {
    fullName: "Flow-Launcher/Flow.Launcher",
    classification: "broad Windows launcher",
    yardstickVerdict: "broad-market-stronger",
    strongerAxes: ["market proof", "plugins", "search", "packaging", "community"],
    wtLauncherAxes: ["narrow portable WT workspace-launcher slice only"],
  },
  {
    fullName: "Wox-launcher/Wox",
    classification: "broad cross-platform launcher",
    yardstickVerdict: "broad-market-stronger",
    strongerAxes: ["market proof", "cross-platform packaging", "plugin/theme stores", "AI/MCP direction"],
    wtLauncherAxes: ["narrow portable WT workspace-launcher slice only"],
  },
];

async function main(): Promise<void> {
  const repositories = [];
  for (const note of repoNotes) {
    const result = await execa("gh", ["repo", "view", note.fullName, "--json", fields], { reject: false });
    if (result.exitCode !== 0) {
      throw new Error(`gh repo view failed for ${note.fullName} with exit code ${result.exitCode}: ${result.stderr || result.stdout}`);
    }
    const repo = JSON.parse(result.stdout) as Record<string, unknown>;
    const primaryLanguage = repo.primaryLanguage as Record<string, unknown> | null | undefined;
    repositories.push({
      fullName: repo.nameWithOwner,
      url: repo.url,
      description: repo.description,
      language: primaryLanguage?.name ?? null,
      stars: repo.stargazerCount,
      forks: repo.forkCount,
      pushedAtUtc: repo.pushedAt,
      updatedAtUtc: repo.updatedAt,
      archived: repo.isArchived,
      classification: note.classification,
      yardstickVerdict: note.yardstickVerdict,
      strongerAxes: note.strongerAxes,
      wtLauncherAxes: note.wtLauncherAxes,
    });
  }

  const flowStars = repositories.find((repo) => repo.fullName === "Flow-Launcher/Flow.Launcher")?.stars ?? 0;
  const woxStars = repositories.find((repo) => repo.fullName === "Wox-launcher/Wox")?.stars ?? 0;
  const summary = {
    repositories: repositories.length,
    directNarrowWeaker: repositories.filter((repo) => repo.yardstickVerdict === "direct-narrow-weaker").length,
    adjacentAxisStronger: repositories.filter((repo) => repo.yardstickVerdict === "adjacent-axis-stronger").length,
    broadMarketStronger: repositories.filter((repo) => repo.yardstickVerdict === "broad-market-stronger").length,
    flowStars,
    woxStars,
  };
  const capturedAtUtc = new Date().toISOString().replace(/\.\d{3}Z$/, "Z");
  const snapshot = {
    schemaVersion: 3,
    capturedAtUtc,
    evidenceCommand,
    refreshCommand: "npm run update:github-snapshot",
    queries: ["Windows Terminal workspace launcher", "Windows Terminal session manager", "Claude Code launcher"],
    scope: "Representative similar GitHub projects for WT Launcher's market/north-star audit; not a universal ranking of every launcher repository.",
    summary,
    repositories,
  };
  writeJsonNoBom(snapshotPath, snapshot);
  console.log(JSON.stringify({ status: "PASS", path: snapshotPath, ...summary, capturedAtUtc }));
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
