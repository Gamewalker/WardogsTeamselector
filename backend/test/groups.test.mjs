import { test, after } from "node:test";
import assert from "node:assert/strict";
import { randomBytes } from "node:crypto";
import { runInNewContext } from "node:vm";
import { fileURLToPath } from "node:url";
import { Miniflare } from "miniflare";

const mf = new Miniflare({ name: "groups-test", unsafeInspectDurableObjects: true, modules: true, scriptPath: fileURLToPath(new URL("../src/worker.js", import.meta.url)), compatibilityDate: "2026-08-06", durableObjects: { GROUPS: { className: "GroupRoom", useSQLite: true } } });
after(() => mf.dispose());
const id = () => randomBytes(16).toString("hex");
const token = () => randomBytes(32).toString("base64url");
const pageElements = () => Object.fromEntries(['link', 'copy', 'open', 'name', 'heading', 'status', 'launch-note', 'manual', 'copy-feedback'].map(id => [id, { hidden: id === 'open' }]));
test("Invitation page passes the complete invitation to the app only for valid links", async () => {
  const groupId = id();
  const response = await mf.dispatchFetch("https://groups.example/invite/" + groupId);
  const html = await response.text();
  assert.equal(response.headers.get("Referrer-Policy"), "no-referrer");
  const script = html.match(/<script>(.*?)<\/script>/s)[1];
  for (const secret of [token(), "", "invalid", token() + "extra"]) {
    const elements = pageElements();
    const location = { origin: "https://custom.example:8443", hash: secret ? "#" + secret : "" };
    location.href = location.origin + "/invite/" + groupId + location.hash;
    runInNewContext(script, { location, document: { getElementById: id => elements[id] }, fetch: () => Promise.resolve({ ok: true, json: async () => ({ name: "Friends" }) }) });
    if (secret.length === 43) {
      assert.equal(elements.open.hidden, false);
      assert.equal(decodeURIComponent(elements.open.href.split('#')[1]), location.href);
      assert.match(elements.open.href, /^wardogs:\/\/join\/#/);
      elements.open.onclick();
      assert.equal(elements.status.textContent, 'Öffnen in der App angefordert');
      assert.equal(elements.manual.open, true);
      assert.match(elements['launch-note'].textContent, /Browserabfrage/);
      await new Promise(resolve => setImmediate(resolve));
      assert.equal(elements.status.textContent, 'Öffnen in der App angefordert', 'A late preview must preserve launch feedback');
    } else {
      assert.equal(elements.open.hidden, true);
      assert.equal(elements.open.href, undefined);
    }
    assert.equal(elements.link.value, location.href);
  }
  const root = await mf.dispatchFetch("https://groups.example/");
  const rootScript = (await root.text()).match(/<script>(.*?)<\/script>/s)[1];
  const elements = pageElements();
  runInNewContext(rootScript, { location: { href: "https://groups.example/#" + token(), hash: "#" + token() }, document: { getElementById: id => elements[id] } });
  assert.equal(elements.open.hidden, true);
});
test("Invitation page displays group names safely and explains expired links and clipboard fallback", async () => {
  const response = await mf.dispatchFetch("https://groups.example/invite/" + id());
  const html = await response.text();
  assert.match(html, /src="\/logo.png"/);
  assert.match(response.headers.get('Content-Security-Policy'), /img-src 'self'/);
  const script = html.match(/<script>(.*?)<\/script>/s)[1];
  for (const statusCode of [200, 403, 410, 429, 503]) {
    const elements = pageElements();
    const document = { getElementById: id => elements[id] };
    runInNewContext(script, { document, location: { href: 'https://groups.example/invite/test#' + token(), origin: 'https://groups.example', hash: '#' + token() }, navigator: { clipboard: { writeText: async () => { throw new Error('denied'); } } }, fetch: async () => ({ ok: statusCode === 200, status: statusCode, json: async () => ({ name: '<img src=x onerror=alert(1)>', message: 'Einladung ungültig.' }) }) });
    await new Promise(resolve => setImmediate(resolve));
    if (statusCode === 200) assert.equal(elements.heading.textContent, '<img src=x onerror=alert(1)>');
    assert.equal(elements.open.hidden, statusCode === 403 || statusCode === 410);
    let selected = false;
    elements.link.focus = () => {};
    elements.link.select = () => { selected = true; };
    await elements.copy.onclick();
    assert.equal(selected, true);
    assert.match(elements['copy-feedback'].textContent, /Strg\+C/);
  }
});
async function request(group, action, data, secret = group.token) {
  const path = action === "create" ? "/v1/groups" : `/v1/groups/${group.groupId}/${action}`;
  const response = await mf.dispatchFetch("https://groups.example" + path, {
    method: data === undefined ? "GET" : "POST",
    headers: { ...(secret ? { Authorization: "Bearer " + secret } : {}), "Content-Type": "application/json" },
    body: data === undefined ? undefined : JSON.stringify(data)
  });
  return { status: response.status, body: await response.json() };
}
async function create() {
  const owner = { groupId: id(), memberId: id(), token: token(), inviteToken: token(), name: "Zehnergruppe", displayName: "Ersteller" };
  const result = await request(owner, "create", owner);
  assert.equal(result.status, 201);
  return owner;
}
async function join(owner, displayName = "Mitspieler") {
  const member = { groupId: owner.groupId, memberId: id(), token: token(), inviteToken: owner.inviteToken, displayName };
  const response = await request(member, "join", member);
  assert.equal(response.status, 201);
  return member;
}
const action = (group, name, data = {}) => request(group, name, { operationId: id(), ...data });
function messages(ws) {
  const queue = []; const waiters = [];
  ws.addEventListener("message", event => { const value = JSON.parse(event.data); if (waiters.length) waiters.shift()(value); else queue.push(value); });
  return () => queue.length ? Promise.resolve(queue.shift()) : new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error("WebSocket response timed out")), 5000);
    waiters.push(value => { clearTimeout(timeout); resolve(value); });
  });
}
async function connect(member) {
  const response = await mf.dispatchFetch(`https://groups.example/v1/groups/${member.groupId}/socket`, { headers: { Upgrade: "websocket", Authorization: "Bearer " + member.token } });
  assert.equal(response.status, 101);
  const ws = response.webSocket; const next = messages(ws); ws.accept();
  return { ws, next };
}

test("approval gates team access and member administration", async () => {
  const owner = await create(); const member = await join(owner);
  assert.equal((await action(owner, "publish", { team: "Red" })).status, 200);
  const pending = await request(member, "state");
  assert.equal(pending.body.yourStatus, "Pending"); assert.equal(pending.body.team, null); assert.deepEqual(pending.body.members, []);
  assert.equal((await action(member, "publish", { team: "Blue" })).status, 403);
  assert.equal((await action(owner, "approve", { memberId: member.memberId })).status, 200);
  const approved = await request(member, "state"); assert.equal(approved.body.team, "Red"); assert.deepEqual(approved.body.members, []);
  assert.equal((await action(member, "remove", { memberId: owner.memberId })).status, 403);
  assert.equal((await action(member, "publish", { team: "Blue" })).status, 403);
  assert.equal((await request(owner, "state", undefined, token())).status, 401);
});
test("create and join retry with the same private credentials are idempotent", async () => {
  const owner = await create(); assert.equal((await request(owner, "create", owner)).status, 200);
  assert.equal((await request(owner, "create", { ...owner, token: token() })).status, 409);
  const member = await join(owner);
  assert.equal((await request(member, "join", member)).status, 200);
  const state = await request(owner, "state"); assert.equal(state.body.members.length, 2);
  assert.equal((await request(member, "join", { ...member, token: token() })).status, 409);
});
test("publication operations are deduplicated and same-team republish is a new selection", async () => {
  const owner = await create(); const data = { operationId: id(), team: "Blue" };
  const first = await request(owner, "publish", data); const duplicate = await request(owner, "publish", data);
  assert.equal(first.body.selectionVersion, 1); assert.equal(duplicate.body.selectionVersion, 1);
  assert.equal((await request(owner, "publish", { ...data, team: "Green" })).status, 409);
  const repeat = await action(owner, "publish", { team: "Blue" }); assert.equal(repeat.body.selectionVersion, 2);
  const member = await join(owner); const change = await action(owner, "approve", { memberId: member.memberId }); assert.equal(change.body.selectionVersion, 2);
  const clear = await action(owner, "clear"); assert.equal(clear.body.team, null); assert.equal(clear.body.selectionVersion, 3);
});
test("WebSockets send live changes, authorize every sync and close on removal", async () => {
  const owner = await create(); const member = await join(owner);
  const { ws, next } = await connect(member);
  assert.equal((await next()).team, null);
  await action(owner, "approve", { memberId: member.memberId }); assert.equal((await next()).yourStatus, "Approved");
  await action(owner, "publish", { team: "Green" }); const live = await next(); assert.equal(live.team, "Green");
  ws.send(JSON.stringify({ type: "sync" })); const refreshed = await next(); assert.equal(refreshed.revision, live.revision);
  const closed = new Promise(resolve => ws.addEventListener("close", resolve, { once: true }));
  await action(owner, "remove", { memberId: member.memberId });
  assert.equal((await closed).code, 4003); assert.equal((await request(member, "state")).status, 403);
});
test("invitation rotation invalidates old links without removing members", async () => {
  const owner = await create(); const member = await join(owner);
  await action(owner, "approve", { memberId: member.memberId });
  const replacement = token(); await action(owner, "invite", { inviteToken: replacement });
  assert.equal((await request(owner, "preview", { inviteToken: owner.inviteToken }, null)).status, 403);
  assert.equal((await request(owner, "preview", { inviteToken: replacement }, null)).status, 200);
  assert.equal((await request(member, "state")).status, 200);
});
test("deletion, leave, rejection and owner protection", async () => {
  const owner = await create(); const rejected = await join(owner);
  await action(owner, "reject", { memberId: rejected.memberId }); assert.equal((await request(rejected, "state")).status, 403);
  const member = await join(owner); await action(owner, "approve", { memberId: member.memberId });
  assert.equal((await action(owner, "remove", { memberId: owner.memberId })).status, 400);
  assert.equal((await action(owner, "leave")).status, 409);
  await action(member, "leave"); assert.equal((await request(member, "state")).status, 403);
  assert.equal((await action(owner, "delete")).status, 200); assert.equal((await request(owner, "state")).status, 410);
  assert.equal((await request(owner, "create", owner)).status, 409);
});
test("credential rotation revokes old recovery codes", async () => {
  const owner = await create(); const replacement = token();
  assert.equal((await action(owner, "credential", { newToken: replacement })).status, 200);
  assert.equal((await request(owner, "state")).status, 401);
  assert.equal((await request(owner, "state", undefined, replacement)).status, 200);
});
test("validation bounds input and group request count", async () => {
  const owner = await create();
  assert.equal((await action(owner, "publish", { team: "Orange" })).status, 400);
  assert.equal((await request(owner, "join", { memberId: id(), token: token(), inviteToken: owner.inviteToken, displayName: "<script>" + "a".repeat(50) })).status, 400);
  const response = await mf.dispatchFetch(`https://groups.example/v1/groups/${owner.groupId}/join`, { method: "POST", body: "x".repeat(9000) }); assert.equal(response.status, 413);
  for (let i = 0; i < 50; i++) await join(owner, "Mitspieler " + i);
  assert.equal((await request(owner, "join", { memberId: id(), token: token(), inviteToken: owner.inviteToken, displayName: "Zu viel" })).status, 429);
});
test("state survives object eviction and snapshots reveal no credentials", async () => {
  const owner = await create();
  await action(owner, "publish", { team: "Red" });
  await mf.unsafeEvictDurableObject("groups-test", "GroupRoom", { name: owner.groupId });
  const snapshot = await request(owner, "state"); assert.equal(snapshot.body.team, "Red");
  assert.ok(!JSON.stringify(snapshot.body).includes("tokenHash"));
  assert.ok(!JSON.stringify(snapshot.body).includes(owner.token)); assert.ok(!JSON.stringify(snapshot.body).includes(owner.inviteToken));
  const page = await mf.dispatchFetch(`https://groups.example/invite/${owner.groupId}`); assert.equal(page.headers.get("Referrer-Policy"), "no-referrer");
});

test("ten calls per minute share a member budget across reads and writes", async () => {
  const owner = await create(); const member = await join(owner);
  for (let i = 0; i < 4; i++) assert.equal((await request(owner, "state")).status, 200);
  for (let i = 0; i < 6; i++) assert.equal((await action(owner, "publish", { team: "Blue" })).status, 200);
  const limited = await request(owner, "state");
  assert.equal(limited.status, 429); assert.equal(limited.body.code, "rate_limit");
  assert.equal((await action(owner, "clear")).status, 429);
  assert.equal((await request(member, "state")).status, 200, "another member retains their own budget");
});

test("socket connections and sync messages use the same ten-call budget as HTTP", async () => {
  const owner = await create();
  const { ws, next } = await connect(owner); await next();
  for (let i = 0; i < 4; i++) { ws.send(JSON.stringify({ type: "sync" })); await next(); }
  for (let i = 0; i < 5; i++) assert.equal((await request(owner, "state")).status, 200);
  assert.equal((await request(owner, "state")).status, 429);
  const closed = new Promise(resolve => ws.addEventListener("close", resolve, { once: true }));
  ws.send(JSON.stringify({ type: "sync" }));
  assert.equal((await closed).code, 1008);
});
