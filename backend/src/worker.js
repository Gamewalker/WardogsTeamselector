import { DurableObject } from "cloudflare:workers";

const ID = /^[a-f0-9]{32}$/;
const TOKEN = /^[A-Za-z0-9_-]{43}$/;
const TEAMS = new Set(["Blue", "Red", "Green"]);
const MEMBER_CALLS_PER_MINUTE = 10;
const headers = { "Content-Type": "application/json; charset=utf-8", "Cache-Control": "no-store", "X-Content-Type-Options": "nosniff" };
const json = (value, status = 200) => new Response(JSON.stringify(value), { status, headers });
class ApiError extends Error {
  constructor(status, code, message) { super(message); this.status = status; this.code = code; }
}
const fail = (status, code, message) => { throw new ApiError(status, code, message); };
const text = (value, label) => {
  if (typeof value !== "string" || !value.trim() || value.trim().length > 48 || /[\u0000-\u001f\u007f]/u.test(value)) fail(400, "invalid_name", `${label}: 1 bis 48 Zeichen verwenden.`);
  return value.trim();
};
const id = value => { if (!ID.test(value ?? "")) fail(400, "invalid_id", "Ungültige Kennung."); return value; };
const token = value => { if (!TOKEN.test(value ?? "")) fail(400, "invalid_token", "Ungültiger Zugangscode."); return value; };
async function hash(value) {
  return [...new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value)))].map(x => x.toString(16).padStart(2, "0")).join("");
}
async function body(request) {
  if (Number(request.headers.get("Content-Length") ?? 0) > 8192) fail(413, "too_large", "Anfrage zu groß.");
  const reader = request.body?.getReader();
  if (!reader) fail(400, "invalid_json", "Leere Anfrage.");
  const parts = []; let total = 0;
  for (;;) {
    const { done, value } = await reader.read(); if (done) break;
    total += value.length;
    if (total > 8192) { await reader.cancel(); fail(413, "too_large", "Anfrage zu groß."); }
    parts.push(value);
  }
  const bytes = new Uint8Array(total); let at = 0;
  for (const part of parts) { bytes.set(part, at); at += part.length; }
  try {
    const result = JSON.parse(new TextDecoder().decode(bytes));
    if (!result || typeof result !== "object" || Array.isArray(result)) throw new Error();
    return result;
  } catch { fail(400, "invalid_json", "Ungültige Anfrage."); }
}
const safeError = error => error instanceof ApiError ? json({ code: error.code, message: error.message }, error.status) : json({ code: "server_error", message: "Gruppendienst vorübergehend nicht verfügbar." }, 503);

export default {
  async fetch(request, env) {
    try {
      const url = new URL(request.url);
      if (url.pathname === "/health" && request.method === "GET") return json({ protocolVersion: 1, service: "wardogs-groups" });
      if (url.pathname === "/" || /^\/invite\/[a-f0-9]{32}$/.test(url.pathname)) return invitationPage(url.pathname);
      if (url.pathname === "/v1/groups" && request.method === "POST") {
        if (env.CREATE_LIMIT && !(await env.CREATE_LIMIT.limit({ key: request.headers.get("CF-Connecting-IP") ?? "unknown" })).success) fail(429, "rate_limit", "Zu viele Gruppenanlagen. Bitte später erneut versuchen.");
        const data = await body(request); const groupId = id(data.groupId);
        return env.GROUPS.get(env.GROUPS.idFromName(groupId)).fetch(new Request("https://group/create", { method: "POST", body: JSON.stringify(data) }));
      }
      const route = /^\/v1\/groups\/([a-f0-9]{32})\/(state|preview|join|socket|approve|reject|remove|leave|publish|clear|invite|delete|credential)$/.exec(url.pathname);
      if (!route) fail(404, "not_found", "Seite nicht gefunden.");
      const forwarded = new Request(request);
      // Routing is derived from the URL; clients cannot select an internal operation.
      const target = new URL(`https://group/${route[2]}`);
      return env.GROUPS.get(env.GROUPS.idFromName(route[1])).fetch(new Request(target, forwarded));
    } catch (error) { return safeError(error); }
  }
};

export class GroupRoom extends DurableObject {
  constructor(ctx, env) {
    super(ctx, env); this.ctx = ctx; this.state = null; this.buckets = new Map();
    this.ready = ctx.blockConcurrencyWhile(async () => { this.state = await ctx.storage.get("state") ?? null; });
  }
  async fetch(request) {
    await this.ready;
    try {
      const action = new URL(request.url).pathname.slice(1);
      if (action === "socket") {
        if (request.method !== "GET" || request.headers.get("Upgrade")?.toLowerCase() !== "websocket") fail(400, "websocket_required", "WebSocket erforderlich.");
        const member = await this.authorize(request);
        this.rate(member.id, MEMBER_CALLS_PER_MINUTE);
        const pair = new WebSocketPair();
        pair[1].serializeAttachment({ memberId: member.id });
        this.ctx.acceptWebSocket(pair[1]);
        pair[1].send(JSON.stringify(this.snapshot(member)));
        return new Response(null, { status: 101, webSocket: pair[0] });
      }
      if (action === "state" && request.method === "GET") {
        const member = await this.authorize(request);
        this.rate(member.id, MEMBER_CALLS_PER_MINUTE);
        return json(this.snapshot(member));
      }
      if (request.method !== "POST") fail(405, "method", "Methode nicht erlaubt.");
      const data = await body(request);
      // Storage commit and publication run serially, including across awaits.
      return await this.ctx.blockConcurrencyWhile(async () => {
        const previous = this.state;
        this.state = previous ? structuredClone(previous) : null;
        try {
        if (action === "create") return await this.create(data);
        if (!this.state || this.state.deleted) fail(410, "group_deleted", "Die Gruppe existiert nicht mehr.");
        if (action === "preview") {
          this.rate("invitation", 120);
          await this.checkInvite(data.inviteToken);
          return json({ name: this.state.name, protocolVersion: 1 });
        }
        if (action === "join") return await this.join(data);
        const member = await this.authorize(request);
        this.rate(member.id, MEMBER_CALLS_PER_MINUTE);
        const operationId = id(data.operationId);
        const fingerprint = await hash(JSON.stringify({ action, data }));
        const duplicate = this.state.operations.find(x => x.memberId === member.id && x.id === operationId);
        if (duplicate) {
          if (duplicate.fingerprint !== fingerprint) fail(409, "operation_conflict", "Anfragekennung wurde bereits verwendet.");
          return json(this.snapshot(member));
        }
        if (member.status !== "Approved") fail(403, "not_approved", "Die Mitgliedschaft ist nicht bestätigt.");
        if (!["leave", "credential"].includes(action) && member.role !== "Owner") fail(403, "owner_required", "Nur der Ersteller darf diese Aktion ausführen.");
        if (["approve", "reject", "remove"].includes(action)) {
          const target = this.state.members.find(x => x.id === id(data.memberId));
          if (!target || target.role === "Owner") fail(400, "invalid_member", "Mitglied nicht verfügbar.");
          if (action !== "remove" && target.status !== "Pending") fail(409, "invalid_status", "Keine offene Anfrage.");
          if (action === "approve" && Date.now() - target.requestedAt > 7 * 86400000) fail(410, "request_expired", "Diese Anfrage ist abgelaufen.");
          if (action === "approve" && this.state.members.filter(x => x.status === "Approved").length >= 50) fail(409, "group_full", "Maximal 50 Mitglieder.");
          target.status = action === "approve" ? "Approved" : action === "reject" ? "Rejected" : "Removed";
        } else if (action === "leave") {
          if (member.role === "Owner") fail(409, "owner_cannot_leave", "Der Ersteller kann die Gruppe löschen.");
          member.status = "Removed";
        } else if (action === "publish" || action === "clear") {
          if (action === "publish" && !TEAMS.has(data.team)) fail(400, "invalid_team", "Ungültiges Team.");
          this.state.team = action === "clear" ? null : data.team; this.state.selectionVersion++;
        } else if (action === "invite") {
          this.state.inviteHash = await hash(token(data.inviteToken));
        } else if (action === "credential") {
          member.tokenHash = await hash(token(data.newToken));
          this.closeMember(member.id, 4003, "Zugangscode ersetzt");
        } else if (action === "delete") {
          this.state.deleted = true; this.state.team = null; this.state.selectionVersion++;
        } else fail(404, "not_found", "Aktion nicht gefunden.");
        this.state.operations.push({ memberId: member.id, id: operationId, fingerprint });
        this.state.operations = this.state.operations.slice(-200);
        this.state.revision++;
        await this.save(); this.broadcast();
        return json(this.snapshot(member));
        } catch (error) {
          this.state = previous;
          // Expected validation errors must not escape blockConcurrencyWhile,
          // which would reset the Durable Object and produce a non-JSON error.
          return safeError(error);
        }
      });
    } catch (error) { return safeError(error); }
  }
  async create(data) {
    const groupId = id(data.groupId), memberId = id(data.memberId);
    const tokenHash = await hash(token(data.token));
    if (this.state) {
      if (this.state.deleted || this.state.groupId !== groupId || this.state.members[0].id !== memberId || this.state.members[0].tokenHash !== tokenHash) fail(409, "group_exists", "Gruppe bereits vorhanden.");
      return json(this.snapshot(this.state.members[0]));
    }
    this.state = { groupId, name: text(data.name, "Gruppenname"), inviteHash: await hash(token(data.inviteToken)), revision: 1, selectionVersion: 0, team: null, deleted: false, operations: [], members: [{ id: memberId, displayName: text(data.displayName, "Name"), role: "Owner", status: "Approved", tokenHash, requestedAt: Date.now() }] };
    await this.save(); return json(this.snapshot(this.state.members[0]), 201);
  }
  async join(data) {
    this.rate("invitation", 120);
    await this.checkInvite(data.inviteToken);
    const memberId = id(data.memberId), tokenHash = await hash(token(data.token));
    const existing = this.state.members.find(x => x.id === memberId);
    if (existing) {
      if (existing.tokenHash !== tokenHash) fail(409, "member_exists", "Mitgliedskennung bereits vorhanden.");
      return json(this.snapshot(existing));
    }
    // Retain a bounded number of tombstones so removed tokens cannot authenticate.
    this.state.members = this.state.members.filter(x => x.status === "Approved" || (x.status === "Pending" && Date.now() - x.requestedAt < 7 * 86400000) || Date.now() - x.requestedAt < 86400000);
    if (this.state.members.filter(x => x.status === "Pending").length >= 50 || this.state.members.length >= 200) fail(429, "too_many_requests", "Zu viele offene Anfragen.");
    const member = { id: memberId, displayName: text(data.displayName, "Name"), role: "Member", status: "Pending", tokenHash, requestedAt: Date.now() };
    this.state.members.push(member); this.state.revision++; await this.save(); this.broadcast(); return json(this.snapshot(member), 201);
  }
  async checkInvite(value) {
    if (await hash(token(value)) !== this.state.inviteHash) fail(403, "invalid_invitation", "Einladung ungültig oder widerrufen.");
  }
  async authorize(request) {
    if (!this.state || this.state.deleted) fail(410, "group_deleted", "Die Gruppe existiert nicht mehr.");
    const value = request.headers.get("Authorization")?.replace(/^Bearer /, "");
    if (!value || !TOKEN.test(value)) fail(401, "unauthorized", "Zugangscode fehlt.");
    const tokenHash = await hash(value);
    const member = this.state.members.find(x => x.tokenHash === tokenHash);
    if (!member) fail(401, "unauthorized", "Zugangscode ungültig.");
    if (["Removed", "Rejected"].includes(member.status)) fail(403, "membership_revoked", "Mitgliedschaft entfernt oder abgelehnt.");
    if (member.status === "Pending" && Date.now() - member.requestedAt > 7 * 86400000) fail(410, "request_expired", "Beitrittsanfrage abgelaufen.");
    return member;
  }
  snapshot(member) {
    const approved = member.status === "Approved" && !this.state.deleted;
    return { protocolVersion: 1, groupId: this.state.groupId, name: this.state.name, revision: this.state.revision, selectionVersion: approved ? this.state.selectionVersion : 0, team: approved ? this.state.team : null, yourStatus: this.state.deleted ? "Removed" : member.status, yourRole: member.role, memberId: member.id, members: approved && member.role === "Owner" ? this.state.members.map(({ id, displayName, status, role, requestedAt }) => ({ id, displayName, status, role, requestedAt })) : [] };
  }
  save() { return this.ctx.storage.put("state", this.state); }
  rate(key, max) {
    const now = Date.now(); const entry = this.buckets.get(key);
    if (!entry || now - entry.start >= 60000) this.buckets.set(key, { start: now, count: 1 });
    else if (++entry.count > max) fail(429, "rate_limit", "Zu viele Anfragen. Bitte später erneut versuchen.");
    if (this.buckets.size > 250) for (const [name, value] of this.buckets) if (now - value.start >= 60000) this.buckets.delete(name);
  }
  closeMember(memberId, code, reason) {
    for (const ws of this.ctx.getWebSockets()) if (ws.deserializeAttachment()?.memberId === memberId) ws.close(code, reason);
  }
  broadcast() {
    for (const ws of this.ctx.getWebSockets()) {
      const member = this.state.members.find(x => x.id === ws.deserializeAttachment()?.memberId);
      try {
        if (!member || this.state.deleted || ["Removed", "Rejected"].includes(member.status)) ws.close(4003, "Mitgliedschaft beendet");
        else ws.send(JSON.stringify(this.snapshot(member)));
      } catch { ws.close(1011, "Verbindung unterbrochen"); }
    }
  }
  async webSocketMessage(ws, message) {
    await this.ready;
    try {
      const member = this.state?.members.find(x => x.id === ws.deserializeAttachment()?.memberId);
      if (!member || this.state.deleted || ["Removed", "Rejected"].includes(member.status)) { ws.close(4003, "Mitgliedschaft beendet"); return; }
      if (member.status === "Pending" && Date.now() - member.requestedAt > 7 * 86400000) { ws.close(4003, "Anfrage abgelaufen"); return; }
      this.rate(member.id, MEMBER_CALLS_PER_MINUTE);
      if (typeof message !== "string" || message.length > 256 || JSON.parse(message).type !== "sync") { ws.close(1008, "Ungültige Nachricht"); return; }
      // Always answer with an authorized snapshot; transport pings are not a sync.
      ws.send(JSON.stringify(this.snapshot(member)));
    } catch { ws.close(1008, "Anfrage nicht möglich"); }
  }
  webSocketClose(ws, code, reason) { ws.close(code, reason); }
  webSocketError(ws) { ws.close(1011, "Verbindung unterbrochen"); }
}

function invitationPage(path) {
  const groupId = path.startsWith("/invite/") ? path.slice(8) : "";
  const html = `<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Wardogs – Gruppeneinladung</title><style>body{font:18px system-ui;background:#1c1e22;color:#eee;max-width:640px;margin:10vh auto;padding:24px}a{color:#94c5ff}button,input{font:inherit;padding:12px;border-radius:8px}input{width:90%}p{line-height:1.6}</style><h1>Wardogs Gruppeneinladung</h1><p id="name">Öffne im Tool unter Betrieb → Gruppenmodus die Aktion „Gruppe beitreten“, füge diesen Link ein und gib deinen Namen ein. Der Ersteller bestätigt anschließend deine Anfrage.</p><input id="link" readonly aria-label="Einladungslink"><p><button id="copy">Link kopieren</button></p><p><a href="https://github.com/Gamewalker/WardogsTeamselector/releases/latest">WardogsTeamselector herunterladen</a></p><script>const link=document.getElementById('link');link.value=location.href;document.getElementById('copy').onclick=async()=>{try{await navigator.clipboard.writeText(link.value);document.getElementById('copy').textContent='Kopiert'}catch{link.select()}};const group=${JSON.stringify(groupId)};if(group&&location.hash.length>1)fetch('/v1/groups/'+group+'/preview',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({inviteToken:location.hash.slice(1)})}).then(async r=>{const d=await r.json();if(r.ok)document.title='Einladung: '+d.name;else document.getElementById('name').textContent=d.message}).catch(()=>{});</script></html>`;
  return new Response(html, { headers: { "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store", "Referrer-Policy": "no-referrer", "X-Content-Type-Options": "nosniff", "Content-Security-Policy": "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'" } });
}
