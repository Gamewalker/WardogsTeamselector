import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { assertFreeAccount } from "./free-account.mjs";

const token = process.env.CLOUDFLARE_API_TOKEN;
const accountId = process.env.CLOUDFLARE_ACCOUNT_ID;
await assertFreeAccount(fetch, accountId, token);
console.log("Workers Free bestätigt. Bereitstellung ausschließlich mit SQLite-Durable-Objects.");
const child = spawnSync(process.execPath, [fileURLToPath(new URL("../node_modules/wrangler/bin/wrangler.js", import.meta.url)), "deploy"], { stdio: "inherit", shell: false });
process.exit(child.status ?? 1);
