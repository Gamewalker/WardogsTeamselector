import { test } from "node:test";
import assert from "node:assert/strict";
import { assertFreeAccount } from "../scripts/free-account.mjs";

const account = "a".repeat(32);
const mock = (enabled, subscriptions = [], options = {}) => async url => new Response(JSON.stringify({ success: options.success ?? true, result: url.endsWith("/workers/standard") ? { enabled } : subscriptions, result_info: { total_pages: options.pages ?? 1 } }), { status: options.status ?? 200 });
test("Free deployment accepts only an explicitly confirmed free account", async () => {
  await assertFreeAccount(mock(false), account, "test-token");
  await assert.rejects(() => assertFreeAccount(mock(true), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(undefined), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(false, [], { status: 403 }), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(false, [], { success: false }), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(false, [], { pages: 2 }), account, "test-token"));
  await assert.rejects(() => assertFreeAccount(mock(false), account, ""));
});
test("legacy paid subscriptions are rejected even when Standard is disabled", async () => {
  await assert.rejects(() => assertFreeAccount(mock(false, [{ rate_plan: { id: "workers_bundled", price: 5 } }]), account, "test-token"));
  await assertFreeAccount(mock(false, [{ rate_plan: { id: "workers_free", price: 0 } }]), account, "test-token");
});
