import { Miniflare } from "miniflare";
import { fileURLToPath } from "node:url";
import { spawn } from "node:child_process";

const mf = new Miniflare({ modules: true, scriptPath: fileURLToPath(new URL("../src/worker.js", import.meta.url)), compatibilityDate: "2026-08-06", durableObjects: { GROUPS: { className: "GroupRoom", useSQLite: true } } });
try {
  const address = (await mf.ready).origin;
  const child = spawn(process.env.DOTNET_COMMAND ?? "dotnet", ["run", "--project", fileURLToPath(new URL("../../tests/GroupChecks/GroupChecks.csproj", import.meta.url)), "-c", "Release", "--", "--live", address], { stdio: "inherit", shell: false });
  const status = await new Promise((resolve, reject) => { child.on("error", reject); child.on("exit", resolve); });
  if (status !== 0) throw new Error("Desktop-Protokollprüfung gegen echten Worker fehlgeschlagen.");
} finally { await mf.dispose(); }
