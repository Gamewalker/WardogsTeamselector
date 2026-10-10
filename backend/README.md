# Wardogs-Gruppendienst

Cloudflare Worker mit einem **SQLite-basierten Durable Object pro Gruppe**. Benötigt ausschließlich Workers Free. Es werden keine zusätzlichen D1-, KV-, R2-, Queue- oder kostenpflichtigen Ressourcen angelegt. WebSockets verwenden die Hibernation-API; Zustandsabfragen alle 15 Sekunden kommen vom Client, nicht von Server-Timern.

## Lokal prüfen

Benötigt Node.js 24:

```sh
npm ci
npm test
npm run test:client
npm run check
npm run dev
```

Der lokale Dienst läuft üblicherweise unter `http://localhost:8787`. Diese lokale HTTP-Adresse ist im Windows-Client erlaubt; öffentliche Dienste benötigen HTTPS. Die Tests verwenden echte Worker- und Durable-Object-Instanzen in Miniflare und benötigen keine Cloudflare-Zugangsdaten. `test:client` benötigt zusätzlich das .NET-10-SDK und prüft den tatsächlichen Desktop-HTTP-/WebSocket-Client einschließlich des geplanten 15-Sekunden-Abgleichs; der Lauf dauert etwa 20 Sekunden.

## Ausschließlich kostenlos bereitstellen

Die vollständige [Deployment-Anleitung im Repository-Wiki](../docs/wiki/Eigenen-Gruppendienst-deployen.md) beschreibt Einrichtung, Prüfungen und Bereitstellung. Der Desktop-Client verwendet standardmäßig `https://wardogs-groups.niels-82f.workers.dev/`; eigene Dienstadressen sind weiterhin möglich.

1. Ein Cloudflare-Konto im Workers-Free-Tarif verwenden.
2. Im Plugin oder in der Umgebung `CLOUDFLARE_ACCOUNT_ID` und einen auf dieses Konto begrenzten API-Token hinterlegen. Zugangsdaten gehören nicht in Git oder Diagnoseexporte. Für den CLI-Guard werden neben der Worker-Bereitstellung Leserechte für den Tarif und die Kontoabonnements benötigt. Fehlende Leserechte verhindern die Bereitstellung.
3. `npm run deploy:free` ausführen. Der Guard prüft den Workers-Tarif und vorhandene Abonnements. Bei Paid, unklarer Antwort oder fehlender Berechtigung bricht er ab. Er ändert keinen Tarif. Der anschließende Wrangler-Aufruf verwendet die SQLite-Migration in `wrangler.jsonc`.
4. Die ausgegebene HTTPS-Adresse unter **Gruppenverwaltung → Gruppendienst einrichten** im Tool speichern. Der konfigurierte Service ist auch ohne eine neue EXE verwendbar.
5. Mit zwei Clients im Testmodus Gruppe erstellen, Anfrage bestätigen, Team teilen, WebSocket-Update und Entfernung praktisch überprüfen. Die erste öffentliche Bereitstellung benötigt diesen Live-Test.

Die kostenlose `workers.dev`-Adresse reicht für API und Einladungsseite. Bei einem Push auf `main` deployt GitHub Actions den Worker automatisch nach erfolgreichen Desktop- und Backend-Prüfungen einschließlich Client-Integration und Deployment-Dry-Run. Dafür müssen die Repository-Secrets `CLOUDFLARE_ACCOUNT_ID` und `CLOUDFLARE_API_TOKEN` gesetzt sein. Der Deployment-Job verwendet ebenfalls `npm run deploy:free`, läuft ohne paralleles Worker-Deployment und überspringt inzwischen veraltete Commits. Pull Requests, Featurebranches und manuelle Workflow-Läufe deployen den Worker nicht.

## API

Alle Daten sind JSON, Protokollversion 1. Einladungslinks verwenden `/invite/<groupId>#<inviteToken>`; das Geheimnis bleibt im Fragment und wird nicht in URL-Abfragen oder Referrer-Headern übertragen.

| Route | Zugriff und Verhalten |
| --- | --- |
| `POST /v1/groups` | Zufällige Gruppen-/Mitgliedskennung und Zugangsdaten vom Client; Anlage durch Worker-Ratelimit begrenzt |
| `POST /v1/groups/:id/preview` | Einladungswert erforderlich; zeigt nur Gruppennamen |
| `POST /v1/groups/:id/join` | Gültige Einladung und eigene Zugangsdaten; zunächst nur offene Anfrage |
| `GET /v1/groups/:id/state` | Authorization: Bearer; offener Antrag erhält keine Teamauswahl |
| `GET /v1/groups/:id/socket` | Autorisierter WebSocket; initialer Zustand, Änderungen und Antwort auf `{"type":"sync"}` |
| `POST …/approve`, `reject`, `remove` | Nur Ersteller; Zielmitglied und zufällige `operationId` |
| `POST …/publish`, `clear` | Nur Ersteller; eigene Auswahlversion, auch bei erneutem Veröffentlichen derselben Farbe |
| `POST …/invite`, `delete` | Nur Ersteller; Einladungswert ersetzen oder Gruppe löschen |
| `POST …/leave`, `credential` | Eigene Mitgliedschaft verlassen bzw. eigenen Zugangscode ersetzen |

IDs bestehen aus 32 zufälligen Hex-Zeichen; Tokens aus 32 zufälligen Bytes in Base64url (43 Zeichen). Der Server speichert SHA-256-Hashes der Zugangsdaten. Zustände enthalten keine Tokens oder Token-Hashes. Namen sind reine Anzeigenamen.

Grenzen: 48 Zeichen pro Name, 50 bestätigte Mitglieder, 50 offene Anfragen, sieben Tage Anfragegültigkeit, begrenzte Tombstones und zuletzt 200 Schreiboperationen zur Deduplizierung. API-Anfragen sind auf 8 KiB begrenzt. Pro Mitglied sind höchstens zehn Aufrufe pro Minute erlaubt; HTTP-Zustandsabfragen, Schreibaktionen, WebSocket-Verbindungsaufbau und Sync-Nachrichten teilen sich dieses Budget. Die Gruppenverwaltung sieht offene Anfragen; normale Mitglieder erhalten diese Liste nicht.

## Kosten und Ausfälle

Free-Kontingente gelten kontoweit. Bei Überschreitung können API, Speicherung oder Sync ausfallen. Der Client stoppt die betroffene Gruppensteuerung, prüft nach steigender Wartezeit erneut und lässt manuelle lokale Teamstarts weiter zu. Eine unbegrenzte öffentliche Verfügbarkeit wird nicht zugesagt. Die Cloudflare-Nutzungsmetriken für Requests, Durable-Object-Laufzeit und Speicherzugriffe vor einer breiten Einführung prüfen.
