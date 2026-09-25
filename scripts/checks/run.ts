import { solutionPath } from "../config-lib.js";
import { restoreSolutionNativeAot } from "../publish-lib.js";
import { runVerifyMainline } from "../verify-mainline.js";
import { runCoverageGate } from "../test-coverage.js";
import { addCheck, execChecked } from "./common.js";

export async function checkReleaseLockfileConsistency(): Promise<void> {
  await restoreSolutionNativeAot(true);
  addCheck("release-lockfile-consistency", { lockedMode: true, aotMarker: "EnableNativeAot" });
}

export async function checkAvaloniaMainlineVerify(): Promise<void> {
  const mainline = await runVerifyMainline();
  const requiredFields = ["schemaVersion", "kind", "status", "repoHead", "worktreeDirty", "worktreeStatus", "nativeAot"];
  for (const field of requiredFields) {
    if (!(field in mainline)) {
      throw new Error(`Avalonia mainline receipt missing field: ${field}`);
    }
  }
  if (Number(mainline.schemaVersion) !== 1) {
    throw new Error(`Avalonia mainline receipt schemaVersion mismatch: ${mainline.schemaVersion}`);
  }
  if (mainline.kind !== "avalonia-mainline-verify") {
    throw new Error(`Avalonia mainline receipt kind mismatch: ${mainline.kind}`);
  }
  if (String(mainline.repoHead ?? "").trim() === "") {
    throw new Error("Avalonia mainline receipt repoHead is required");
  }
  if (mainline.status !== "PASS") {
    throw new Error("Avalonia mainline verify did not pass");
  }
  if (mainline.nativeAot !== true) {
    throw new Error("Avalonia mainline verify must publish with Native AOT");
  }
  if (mainline.publishedDll !== null) {
    throw new Error("Avalonia mainline Native AOT receipt must not claim a managed publishedDll");
  }
  addCheck("avalonia-mainline-verify", {
    schemaVersion: mainline.schemaVersion,
    kind: mainline.kind,
    status: mainline.status,
    repoHead: mainline.repoHead,
    worktreeDirty: mainline.worktreeDirty,
    nativeAot: mainline.nativeAot,
    tests: mainline.tests,
    guiSmoke: mainline.guiSmoke,
    publishedExe: mainline.publishedExe,
    publishedDll: mainline.publishedDll,
    sha256: mainline.sha256,
    bytes: mainline.bytes,
    // 主线 receipt 已实测出 testSeconds/publishSeconds,但此前抄录时被丢弃,
    // 于是"门禁时间花在哪一步"只能靠猜。保留测量值,运行时优化才有依据。
    perf: mainline.perf,
  });
}

export async function checkCoverageGate(): Promise<void> {
  // Known optimization opportunity: runVerifyMainline() already runs dotnet test
  // (with --verbosity minimal --logger trx). Adding --collect "XPlat Code Coverage"
  // to that command and reusing the coverage XML here would save the second
  // dotnet test pass. See: scripts/verify-mainline.ts L167-179.
  // Measured 2026-09-17: full gate WALL ~52s (test pass ~19s, AOT publish ~5s),
  // so this saves roughly one test pass of ~15-20s, under a third of the gate.
  // The gate is not yet time-constrained; keep the separate pass until a real
  // budget forces the merge.
  const coverage = await runCoverageGate(true);
  addCheck("coverage-gate", {
    status: coverage.status,
    criticalPaths: coverage.criticalPaths.length,
  });
}

export async function checkAnalyzerWiring(): Promise<void> {
  await execChecked("Analyzer wiring build", "dotnet", [
    "build",
    solutionPath,
    "--no-restore",
    "-warnaserror",
  ]);
  addCheck("analyzer-wiring", { pass: true });
}
