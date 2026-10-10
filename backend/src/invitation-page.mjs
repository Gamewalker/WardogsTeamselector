export function invitationPage(path) {
  const groupId = path.startsWith("/invite/") ? path.slice(8) : "";
  const html = `<!doctype html>
<html lang="de">
<head>
  <meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
  <meta name="color-scheme" content="dark"><meta name="referrer" content="no-referrer">
  <title>Wardogs – ${groupId ? "Gruppeneinladung" : "Teamselector"}</title>
  <link rel="icon" href="/logo.png" type="image/png">
  <style>
    :root{color-scheme:dark;--bg:#1c1e22;--surface:#25282d;--text:#f5f5f5;--muted:#b2bdc8;--line:#3e434a;--accent:#91c8f6;--ink:#102434;font-family:"Segoe UI",system-ui,sans-serif}
    *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);min-height:100svh;display:flex;flex-direction:column;font-size:16px;line-height:1.6}
    ::selection{background:var(--accent);color:var(--ink)}a{color:var(--accent);text-underline-offset:4px}button,input{font:inherit}a,button,input,summary{-webkit-tap-highlight-color:transparent}a:focus-visible,button:focus-visible,input:focus-visible,summary:focus-visible{outline:3px solid var(--accent);outline-offset:5px}
    [hidden]{display:none!important}.shell{width:min(1080px,100%);margin-inline:auto;padding-inline:40px}
    header{display:flex;align-items:center;justify-content:space-between;gap:24px;padding-block:28px;border-bottom:1px solid var(--line)}.wordmark{font-weight:600;letter-spacing:.01em;color:var(--text);text-decoration:none}.wordmark span{color:var(--muted);font-weight:400;margin-left:6px}.project{font-size:14px;color:var(--muted);text-decoration:none}.project:hover{color:var(--text)}
    main{flex:1;display:grid;grid-template-columns:360px minmax(0,1fr);align-items:center;gap:72px;padding-block:72px}.identity{text-align:center}.logo{display:block;width:280px;height:280px;max-width:100%;margin:auto;object-fit:contain}.identity p{max-width:260px;margin:28px auto 0;color:var(--muted);font-size:17px;text-wrap:balance}
    .invite{min-width:0}h1{font-size:clamp(32px,4vw,48px);font-weight:650;line-height:1.14;letter-spacing:-.025em;margin:0 0 24px;text-wrap:balance;overflow-wrap:anywhere}.intro{font-size:18px;color:var(--muted);margin:0 0 28px;max-width:48ch}.status{display:flex;align-items:center;gap:10px;font-size:14px;color:var(--muted);margin-bottom:20px}.status::before{content:"";width:8px;height:8px;border-radius:50%;background:var(--accent);flex:none}.status.error{color:#ffa189}.status.error::before{background:currentColor}
    .button{display:inline-flex;align-items:center;justify-content:center;gap:12px;border-radius:8px;min-height:52px;padding:12px 22px;font-weight:600;text-decoration:none;border:1px solid transparent;cursor:pointer;transition:background .16s ease,color .16s ease}.button svg{width:20px;height:20px;flex:none}.primary{background:var(--accent);color:var(--ink)}.primary:hover{background:#b8ddfc}.secondary{background:#303338;border-color:#50545a;color:var(--text)}.secondary:hover{background:#3c424a}button:disabled{cursor:wait;opacity:.6}.launch-note{color:var(--muted);font-size:14px;max-width:46ch;margin:14px 0 32px}
    .download{padding-top:24px;border-top:1px solid var(--line)}.download h2{font-size:17px;margin:0 0 6px;font-weight:600}.download p{margin:0 0 14px;color:var(--muted);font-size:14px}.download a{display:inline-flex;align-items:center;gap:8px;font-weight:600;font-size:15px}.download svg{width:18px;height:18px}
    details{margin-top:28px;color:var(--muted);font-size:14px}summary{cursor:pointer;width:fit-content;min-height:44px;display:list-item;padding-block:10px;color:var(--text)}details p{margin:8px 0 12px}label{display:block;margin:16px 0 8px}input{width:100%;min-width:0;padding:12px;border:1px solid #50545a;border-radius:6px;background:#1d2024;color:var(--muted);font-size:13px;caret-color:var(--accent)}.copy-row{display:flex;align-items:center;gap:16px;margin-top:12px}.copy-row .button{min-height:44px;font-size:14px;padding:8px 14px}.copy-feedback{font-size:13px}
    footer{display:flex;justify-content:space-between;gap:16px;border-top:1px solid var(--line);padding-block:22px;color:var(--muted);font-size:13px}footer a{color:var(--muted)}
    @media(max-width:800px){.shell{padding-inline:28px}main{grid-template-columns:220px minmax(0,1fr);gap:36px;padding-block:52px}.logo{width:220px;height:220px}.identity p{font-size:15px}.intro{font-size:17px}}
    @media(max-width:600px){.shell{padding-inline:24px}header{padding-block:20px}.wordmark span{display:block;margin:0;font-size:13px}.project{font-size:13px}main{grid-template-columns:1fr;gap:28px;padding-block:32px}.logo{width:144px;height:144px}.identity p{display:none}h1{font-size:34px;margin-bottom:18px}.intro{margin-bottom:22px}.primary{width:100%}.launch-note{margin-bottom:28px}footer{flex-direction:column;gap:4px}.copy-row{flex-wrap:wrap}}
    @media(prefers-reduced-motion:reduce){.button{transition:none}}
  </style>
</head>
<body>
  <div class="shell"><header><a class="wordmark" href="/">Wardogs <span>Teamselector</span></a><a class="project" href="https://github.com/Gamewalker/WardogsTeamselector">Zum Projekt ↗</a></header></div>
  <main class="shell">
    <div class="identity"><img class="logo" src="/logo.png" width="384" height="384" alt="Wardogs Teamselector – Hundeschild mit drei Teamfarben"><p>Ein Team. Deine Leute.<br>Gemeinsam ins Spiel.</p></div>
    <section class="invite" aria-labelledby="heading">
      <div id="status" class="status" role="status">${groupId ? "Gruppeneinladung" : "Für Windows · portable App"}</div>
      <h1 id="heading">${groupId ? "Zusammen ins gleiche Team." : "Dein Team. Gemeinsam ausgewählt."}</h1>
      <p id="name" class="intro">${groupId ? "Öffne die Einladung in der App, gib deinen Namen ein und sende deine Anfrage. Der Ersteller gibt dich anschließend frei." : "Wähle dein Wardogs-Team und teile die Auswahl mit deiner Gruppe. Deine Mitspieler können ihr einmal beitreten oder automatisch folgen."}</p>
      <a id="open" class="button primary" hidden><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M13 4h7v16h-7M3 12h12m-4-4 4 4-4 4"/></svg>In App öffnen</a>
      <p id="launch-note" class="launch-note" ${groupId ? "" : "hidden"}>Die App einmal starten, damit dein Browser sie öffnen kann. Dein Name und die Bestätigung folgen in der App.</p>
      <div class="download"><h2>${groupId ? "App noch nicht auf deinem PC?" : "Bereit für deine Gruppe?"}</h2><p>Herunterladen, starten und loslegen. Keine Installation nötig.</p><a href="https://github.com/Gamewalker/WardogsTeamselector/releases/latest"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3v12m-4-4 4 4 4-4M4 16v5h16v-5"/></svg>WardogsTeamselector herunterladen</a></div>
      <details id="manual" ${groupId ? "" : "hidden"}><summary>App öffnet sich nicht?</summary><p>Kopiere die Einladung. Wähle in der App <strong>Gruppenverwaltung → Gruppe beitreten</strong> und füge den Link dort ein.</p><label for="link">Vollständiger Einladungslink</label><input id="link" readonly><div class="copy-row"><button id="copy" class="button secondary" type="button"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="8" y="8" width="12" height="13" rx="2"/><path d="M16 8V3H3v13h5"/></svg>Link kopieren</button><span id="copy-feedback" class="copy-feedback" role="status"></span></div></details>
    </section>
  </main>
  <div class="shell"><footer><span>WardogsTeamselector · Gemeinsam spielen</span><a href="https://github.com/Gamewalker/WardogsTeamselector/blob/main/docs/groups.md">Hilfe zum Gruppenmodus</a></footer></div>
  <script>
    const group=${JSON.stringify(groupId)};
    const byId=id=>document.getElementById(id);
    const link=byId('link'),open=byId('open'),status=byId('status');
    link.value=location.href;
    byId('copy').onclick=async()=>{try{await navigator.clipboard.writeText(link.value);byId('copy-feedback').textContent='Link kopiert.'}catch{link.focus();link.select();byId('copy-feedback').textContent='Markierten Link mit Strg+C kopieren.'}};
    const valid=group&&/^[A-Za-z0-9_-]{43}$/.test(location.hash.slice(1));
    if(valid){
      open.href='wardogs://join/#'+encodeURIComponent(location.origin+'/invite/'+group+location.hash);open.hidden=false;status.textContent='Einladung wird geprüft …';
      let launching=false;
      open.onclick=()=>{launching=true;status.textContent='Öffnen in der App angefordert';byId('launch-note').textContent='Bestätige die Browserabfrage zum Öffnen von Wardogs. Gib anschließend in der App deinen Namen ein und sende die Anfrage. Falls nichts passiert: Starte die aktuelle App einmal und klicke erneut auf „In App öffnen“ oder nutze den Link unten.';byId('manual').open=true};
      fetch('/v1/groups/'+group+'/preview',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({inviteToken:location.hash.slice(1)})}).then(async r=>{
        const d=await r.json();
        if(r.ok){document.title='Einladung: '+d.name;byId('heading').textContent=d.name;if(!launching)status.textContent='Du bist eingeladen'}
        else{status.className='status error';status.textContent=r.status===429?'Bitte kurz warten':'Einladung nicht verfügbar';byId('name').textContent=d.message;if(r.status===403||r.status===410){open.hidden=true;byId('launch-note').hidden=true;byId('heading').textContent='Diese Einladung ist nicht mehr gültig.';byId('manual').hidden=true}}
      }).catch(()=>{status.textContent='Vorschau nicht erreichbar';byId('name').textContent='Du kannst die Einladung trotzdem in der App öffnen. Prüfe dort deine Verbindung zum Gruppendienst.'});
    }else if(group){status.className='status error';status.textContent='Einladungslink unvollständig';byId('name').textContent='Bitte deinen Ersteller um den vollständigen Einladungslink mit Zugangscode.';byId('launch-note').hidden=true;byId('manual').hidden=true}
  </script>
</body>
</html>`;
  return new Response(html, { headers: { "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store", "Referrer-Policy": "no-referrer", "X-Content-Type-Options": "nosniff", "Content-Security-Policy": "default-src 'none'; img-src 'self'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'" } });
}
