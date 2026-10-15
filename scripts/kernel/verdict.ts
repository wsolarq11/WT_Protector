/** 门禁分层。分层决定失败是否阻断,以及缺依赖时如何留痕。 */
export type TierName =
  | "static" // 纯文件/文本检查:无外部依赖,失败即阻断
  | "local" // 本机环境事实:CI 用 config/launcher.ci.json 覆盖,故 CI 上显式跳过
  | "native" // 需要 .NET 10 SDK:缺 SDK 的机器显式跳过,不伪装成通过
  | "snapshot"; // 外部人工数据时效性:仅 advisory,不阻断代码验证

export type Evidence = Record<string, unknown>;

/** 判词:一条检查对世界做的唯一断言。
 *
 * 关键是"跳过"与"通过"是两种不同的值,而不是同一个字符串里的两种拼法。
 * 此前的 guiSmoke 是一个 `let s: string`,冒烟真跑过之后被 `isCi` 分支覆盖成
 * "skipped-ci-headless"——一次真实失败与一次干净通过在 receipt 里写成同一串。
 * 换成判别联合后,runner 只能照抄它收到的判词:没有任何一次赋值能把
 * pass 盖成 skip,这类谎话在类型层面就写不出来。 */
export type Verdict =
  | { readonly status: "pass"; readonly evidence: Evidence }
  | { readonly status: "fail"; readonly reason: string }
  | { readonly status: "skip"; readonly reason: string };

export const pass = (evidence: Evidence = {}): Verdict => ({ status: "pass", evidence });
export const skip = (reason: string): Verdict => ({ status: "skip", reason });
export const fail = (reason: string): Verdict => ({ status: "fail", reason });

export function failFrom(error: unknown): Verdict {
  return fail(error instanceof Error ? error.message : String(error));
}
