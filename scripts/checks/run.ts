import { solutionPath } from "../config-lib.js";
import { type Check, defineCheck } from "../kernel/check.js";
import { restoreSolutionNativeAot } from "../publish-lib.js";
import { runVerifyMainline } from "../verify-mainline.js";
import { runCoverageGate } from "../test-coverage.js";

/** guiSmoke 账单的诚实规则:在 Windows 上冒烟确实执行过,因此 receipt 只能记
 * "launch+title"(跑了)或 "skipped-non-windows"(平台不支持)。此前 CI 分支把真实结果
 * 覆盖成 "skipped-ci-headless",而 CI 的 windows runner 有桌面会话、冒烟实际跑过——
 * 于是"首次超时重试成功"与"干净通过"在 receipt 里写成同一个字符串。
 * 抽成纯函数,好让这条规则有自己的单测,而不只靠昂贵的全量门禁兜底。 */
export function validateGuiSmokeReceipt(guiSmoke: string, attempts: number): void {
  if (guiSmoke !== "launch+title" && guiSmoke !== "skipped-non-windows") {
    throw new Error(
      `Avalonia mainline receipt guiSmoke must be "launch+title" or "skipped-non-windows"; got "${guiSmoke}"`,
    );
  }
  if (guiSmoke === "launch+title" && attempts < 1) {
    throw new Error("Avalonia mainline receipt ran guiSmoke but reported zero attempts");
  }
}

export const releaseLockfileConsistencyCheck: Check = defineCheck({
  id: "release-lockfile-consistency",
  tier: "native",
  run: async () => {
    await restoreSolutionNativeAot(true);
    return { lockedMode: true, aotMarker: "EnableNativeAot" };
  },
});

export const avaloniaMainlineVerifyCheck: Check = defineCheck({
  id: "avalonia-mainline-verify",
  tier: "native",
  run: async () => {
    const mainline = await runVerifyMainline();
    const requiredFields = [
      "schemaVersion",
      "kind",
      "status",
      "repoHead",
      "worktreeDirty",
      "worktreeStatus",
      "nativeAot",
      "guiSmoke",
      "guiSmokeAttempts",
    ];
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
    validateGuiSmokeReceipt(String(mainline.guiSmoke ?? ""), Number(mainline.guiSmokeAttempts));
    return {
      schemaVersion: mainline.schemaVersion,
      kind: mainline.kind,
      status: mainline.status,
      repoHead: mainline.repoHead,
      worktreeDirty: mainline.worktreeDirty,
      nativeAot: mainline.nativeAot,
      tests: mainline.tests,
      guiSmoke: mainline.guiSmoke,
      guiSmokeAttempts: mainline.guiSmokeAttempts,
      publishedExe: mainline.publishedExe,
      publishedDll: mainline.publishedDll,
      sha256: mainline.sha256,
      bytes: mainline.bytes,
      // 主线 receipt 已实测出 testSeconds/publishSeconds,但此前抄录时被丢弃,
      // 于是"门禁时间花在哪一步"只能靠猜。保留测量值,运行时优化才有依据。
      perf: mainline.perf,
    };
  },
});

export const coverageGateCheck: Check = defineCheck({
  id: "coverage-gate",
  tier: "native",
  run: async () => {
    const coverage = await runCoverageGate(true);
    return { status: coverage.status, criticalPaths: coverage.criticalPaths.length };
  },
});

export const analyzerWiringCheck: Check = defineCheck({
  id: "analyzer-wiring",
  tier: "native",
  run: async (ctx) => {
    await ctx.execChecked("Analyzer wiring build", "dotnet", ["build", solutionPath, "--no-restore", "-warnaserror"]);
    return { pass: true };
  },
});
