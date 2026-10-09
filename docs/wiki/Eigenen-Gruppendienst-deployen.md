# Eigenen Gruppendienst auf Cloudflare deployen

[Zur Dokumentation](../README.md) · [Gruppen verwenden](../groups.md) · [Backend](../../backend/README.md)

Die App verwendet für neue oder bisher leere Einstellungen `https://wardogs-groups.niels-82f.workers.dev/`. Du kannst stattdessen einen eigenen Gruppendienst betreiben. Bereits gespeicherte eigene Adressen werden nicht überschrieben.

## Voraussetzungen

- Cloudflare-Konto im **Workers-Free-Tarif**.
- Node.js 24 und npm; für die Desktop-Client-Prüfung außerdem das .NET-10-SDK.
- Eine lokale Kopie dieses Repositorys.
- Cloudflare-Account-ID und ein API-Token, begrenzt auf das Zielkonto. Der Token benötigt Rechte zum Deployen von Workers sowie zum Lesen der Kontoabonnements. Als Ausgangspunkt eignet sich die Token-Vorlage **Edit Cloudflare Workers**, ergänzt um **Billing Read**. Konto-Leserechte aus der Vorlage beibehalten.

Der Dienst nutzt einen Worker und SQLite-basierte Durable Objects. Die Konfiguration liegt in `backend/wrangler.jsonc`; Bindings und die erste SQLite-Migration sind vorbereitet. Eine eigene Domain, D1, KV oder R2 sind nicht erforderlich.

Cloudflare dokumentiert [API-Token-Vorlagen](https://developers.cloudflare.com/fundamentals/api/reference/template/), [Workers-Tarife](https://developers.cloudflare.com/workers/platform/pricing/) und [Durable Objects im Free-Tarif](https://developers.cloudflare.com/durable-objects/platform/pricing/). Free-Kontingente gelten für das ganze Konto; bei Überschreitung können Anfragen oder Speicherung ausfallen.

## 1. Abhängigkeiten installieren

Im Terminal vom Repository-Stamm aus:

```powershell
cd backend
npm ci
```

Wenn npm blockierte Installationsskripte meldet, prüfe die Liste und gib gezielt die benötigten Pakete frei:

```powershell
npm install-scripts ls
npm install-scripts approve esbuild workerd
npm rebuild esbuild workerd
```

npm speichert die Freigaben für die installierten Versionen im Feld `allowScripts` der `package.json`. Nach Updates können neue Versionen eine erneute Freigabe benötigen. Die Freigabebefehle gelten für npm-Versionen mit `install-scripts`; ältere npm-Versionen benötigen sie nicht.

## 2. Lokal prüfen

```powershell
npm test
npm run test:client
npm run check
```

Die Client-Prüfung dauert etwa eine Minute und benötigt .NET 10. `check` ist ein Deployment-Dry-Run und veröffentlicht nichts. Alle Prüfungen müssen erfolgreich sein, bevor du deployest.

## 3. Zugangsdaten setzen

Die Account-ID findest du im Cloudflare-Dashboard. Erstelle den Token in der API-Token-Verwaltung und setze beide Werte im selben PowerShell-Terminal:

```powershell
$env:CLOUDFLARE_ACCOUNT_ID = Read-Host "Cloudflare Account-ID"
$env:CLOUDFLARE_API_TOKEN = [System.Net.NetworkCredential]::new("", (Read-Host "Cloudflare API-Token" -AsSecureString)).Password
```

Der Token wird bei der Eingabe verborgen. Die Variablen gelten nur für dieses Terminal und dessen gestartete Prozesse. Zugangsdaten gehören nicht in das Repository, Screenshots oder Diagnoseexporte. Ein `wrangler login` allein genügt dem projektspezifischen Guard nicht.

## 4. Kostenlos deployen

```powershell
npm run deploy:free
```

Der Guard liest die vollständige Konto-Abonnementliste. Ohne aktives kostenpflichtiges Workers-Abonnement gilt der standardmäßige Free-Tarif. Kostenpflichtige oder unklare Workers-Abonnements, unvollständige Antworten und fehlende Leserechte verhindern das Deployment. Das Nutzungsmodell `standard` ist allein kein Nachweis für einen kostenpflichtigen Tarif. Der Guard bucht oder ändert keinen Tarif.

Wrangler veröffentlicht den Worker und legt beim ersten Deployment den Durable-Object-Namensraum aus der SQLite-Migration an. Falls das Konto noch keine `workers.dev`-Subdomain hat, richte diese in Cloudflare **Workers & Pages** ein. Die Ausgabe nennt die HTTPS-Adresse deines Workers, beispielsweise `https://wardogs-groups.<deine-subdomain>.workers.dev`.

## 5. Die App verbinden und testen

1. In der App **Gruppenverwaltung → Gruppendienst einrichten** öffnen.
2. Die ausgegebene HTTPS-Adresse eintragen und **Dienstadresse speichern** wählen.
3. Mit zwei Clients im Testmodus eine Gruppe erstellen, den Einladungslink auf dem zweiten Client verwenden und die Anfrage bestätigen.
4. Als Ersteller unter **Betrieb → Manuell** **Mit Gruppe teilen** einschalten, die eigene Gruppe wählen und einen normalen Teambutton verwenden.
5. Auf dem zweiten Client die geteilte Auswahl, WebSocket-Updates und den Zustandsabgleich prüfen. Danach auch Mitglied entfernen und den entzogenen Zugriff prüfen.

Einladungslinks enthalten die Dienstadresse bereits. Eine neue Standardadresse verschiebt bestehende Gruppen nicht auf einen anderen Dienst; ihre Zugangsdaten bleiben an den ursprünglichen Dienst gebunden.

## Fehler beheben und später aktualisieren

- **Zugangsdaten fehlen:** Variablen im selben Terminal setzen; die Account-ID muss aus 32 Hex-Zeichen bestehen.
- **Tarif nicht prüfbar:** Token-Rechte zum Lesen der Abonnements prüfen. Ein bekannter kostenpflichtiger Workers-Tarif wird absichtlich blockiert.
- **Installationsskripte blockiert:** `npm install-scripts ls` und die gezielten Freigaben aus Schritt 1 verwenden.
- **Client erreicht den Dienst nicht:** Die ausgegebene Worker-Adresse prüfen und in der App speichern. Öffentliche Adressen benötigen HTTPS; lokales HTTP ist nur für Loopback erlaubt.

Für spätere lokale Updates erneut prüfen und `npm run deploy:free` im selben Backend-Verzeichnis ausführen. Der Deployment-Name und bestehende Migrationen bleiben erhalten.

## Automatisch bei Push auf main deployen

Im GitHub-Repository unter **Settings → Secrets and variables → Actions → New repository secret** diese beiden Secrets hinterlegen:

- `CLOUDFLARE_ACCOUNT_ID`: die Account-ID des Zielkontos.
- `CLOUDFLARE_API_TOKEN`: der auf dieses Konto begrenzte Token mit den oben genannten Rechten.

Alternativ mit angemeldeter GitHub CLI und den bereits gesetzten PowerShell-Variablen:

```powershell
$env:CLOUDFLARE_ACCOUNT_ID | gh secret set CLOUDFLARE_ACCOUNT_ID
$env:CLOUDFLARE_API_TOKEN | gh secret set CLOUDFLARE_API_TOKEN
```

Die Workflow-Datei `.github/workflows/windows-build.yml` muss auf `main` vorhanden sein. Bei einem Push auf `main` führt sie zunächst Desktop- und Backend-Prüfungen einschließlich `npm run test:client` und `npm run check` aus. Erst nach deren Erfolg läuft **Deploy groups Worker (Workers Free)** mit `npm run deploy:free` und dem Free-Tarif-Guard.

Der Token steht nur im Deployment-Schritt zur Verfügung. Worker-Deployments laufen nacheinander; vor dem Deployment wird geprüft, ob der Commit noch der aktuelle Stand von `main` ist. Ein inzwischen veralteter Lauf überspringt sein Deployment. Pull Requests, Featurebranches und manuelle Workflow-Läufe veröffentlichen den Worker nicht.

Den Status findest du unter **Actions → Windows EXE → Deploy groups Worker (Workers Free)**. Fehlende Secrets, fehlende Cloudflare-Rechte oder ein nicht bestätigter Free-Tarif lassen den Deployment-Job fehlschlagen.
