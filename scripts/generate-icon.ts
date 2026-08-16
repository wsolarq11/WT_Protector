import { writeFileSync, mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";
import { Resvg } from "@resvg/resvg-js";

const sizes = [16, 24, 32, 48, 64, 128, 256];
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none">
  <rect x="0.5" y="0.5" width="23" height="23" rx="5.28" fill="#2563EB"/>
  <path d="M4 17l6-6-6-6M12 19h8" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
</svg>`;

async function main(): Promise<void> {
  const { values } = parseArgs({
    options: {
      out: { type: "string", default: "" },
    },
  });
  const pngs = sizes.map((size) => {
    const renderer = new Resvg(svg, { fitTo: { mode: "width", value: size }, background: "#2563EB" });
    return renderer.render().asPng();
  });
  const { default: pngToIco } = (await import("png-to-ico")) as { default: (input: Buffer[]) => Promise<Buffer> };
  const ico = await pngToIco(pngs);
  const outPath = values.out
    ? path.resolve(values.out)
    : path.join(
        process.cwd(),
        "src",
        "avalonia-launcher",
        "src",
        "WTLauncher.App",
        "Assets",
        "app.ico",
      );
  mkdirSync(path.dirname(outPath), { recursive: true });
  writeFileSync(outPath, ico);
  console.log(`Wrote ${outPath} (${sizes.join(", ")})`);
}

const isMain = Boolean(process.argv[1]) && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (isMain) {
  await main();
}
