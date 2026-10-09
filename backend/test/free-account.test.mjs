import { test } from "node:test";
import assert from "node:assert/strict";
import { assertFreeAccount } from "../scripts/free-account.mjs";

const account = "a".repeat(32);
const mock = (standard, subscriptions = [], options = {}) => async url => new Response(JSON.stringify({ success: options.success ?? true, result: url.endsWith("/workers/standard") ? { standard } : subscriptions, result_info: { total_pages: options.pages ?? 1 } }), { status: options.status ?? 200 });
test("Free deployment accepts only an explicitly confirmed free account", async () => {
  await assertFreeAccount(mock(false), account, "test-token");
  await assertFreeAccount(mock(true), account, "test-token");
  await assert.rejects(() => assertFreeAccount(mock(false, [], { status: 403 }), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(false, [], { success: false }), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(false, [], { pages: 2 }), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(false), account, ""));
});
test("standard usage model is not a paid-plan signal; complete subscriptions determine billing", async () => {
  const requests = [];
  const fetcher = mock(true, [{ state: "Paid", price: 0, rate_plan: { id: "teams_free" }, product: { name: "prod_teams" } }]);
  await assertFreeAccount(async url => { requests.push(url); return fetcher(url); }, account, "test-token");
  assert.deepEqual(requests, [`https://api.cloudflare.com/client/v4/accounts/${account}/subscriptions`]);
  await assert.rejects(() => assertFreeAccount(mock(true, [{ price: 5, rate_plan: { id: "workers_standard" } }]), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(true, [{ price: 0, rate_plan: { id: "workers_standard" } }]), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(true, [{ rate_plan: { id: "workers_free" } }]), account, "test-token"));
  await assertFreeAccount(mock(true, [{ price: 0, rate_plan: { id: "workers_free" } }]), account, "test-token");
  await assertFreeAccount(mock(true, [{ state: "canceled", price: 5, rate_plan: { id: "workers_standard" } }]), account, "test-token");
  await assert.rejects(() => assertFreeAccount(mock(true, null), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(true, [null]), account, "test-token"));
});
test("legacy paid subscriptions are rejected even when Standard is disabled", async () => {
  await assert.rejects(() => assertFreeAccount(mock(false, [{ rate_plan: { id: "workers_bundled", price: 5 } }]), account, "test-token"));
  await assertFreeAccount(mock(false, [{ rate_plan: { id: "workers_free", price: 0 } }]), account, "test-token");
});
