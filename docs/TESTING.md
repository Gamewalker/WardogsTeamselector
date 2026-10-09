# Prüfung des Release-Builds

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

## Gruppenverwaltung und geteilte Navigation (9. Oktober 2026, Featurebranch)

Der [abschließende CI-Lauf](https://github.com/Gamewalker/WardogsTeamselector/actions/runs/37911155557) besteht mit beiden Windows-EXEs und GUI-Screenshots. Der GUI-Smoke-Lauf prüft fünf Bereiche, links/rechts getrennte und bei geringer Breite umbrechende Tabs, Mindestfenstergröße, Englisch/Arabisch/Chinesisch, Administration ausschließlich im Verwaltungstab, dieselbe aktive Gruppe in allen drei Auswahllisten sowie Stopp und Entwertung alter Antworten beim Gruppenwechsel. Der neue Verwaltungstab zeigt keine Profil-Speichern-Leiste, weil Gruppenänderungen separat gespeichert werden.

46 Gruppenprüfungen und 19.339 Sprachprüfungen bestanden. Ein einzelner Admin-Übertragungscode enthält nur die ausgewählte Gruppenidentität; Mitglieder, widerrufene Zugänge und unterbrochene Tokenwechsel dürfen nicht als aktueller Adminzugang exportiert werden. Die aktive Gruppenauswahl bleibt im gespeicherten Profil erhalten. Der echte .NET-Client gegen Miniflare bestätigt zusätzlich: kopierter Adminzugang funktioniert in einer zweiten Instanz, exklusive Übernahme entzieht dem ursprünglichen Token den Zugriff, und der Empfänger behält Adminrechte. Backend-/Free-Tarif-Prüfungen und Windows-Speicher-/Automationsprüfungen bestehen ebenfalls.

Die GUI-Aufnahmen sind im CI-Artefakt `WardogsTeamselector-group-ui` verfügbar. Keine manuelle Sichtprüfung dieser Aufnahmen und kein Versuch im echten Spiel. Keine Mauseingaben, kein Merge, keine App-Veröffentlichung und keine Cloudflare-Bereitstellung.

## Gruppenmodus (8. Oktober 2026, Featurebranch)

- Backend: elf Tests mit echten Worker-/Durable-Object-Instanzen in Miniflare bestanden. Geprüft sind Freigaberechte, Schreibschutz für Mitglieder, idempotente Anlage und Veröffentlichung, WebSocket-Updates und Zustandsantworten, Entfernung mit Verbindungsschließung, Einladungstausch, Löschung, Austritt, Zugangscodewechsel, Eingabe-/Anfragegrenzen und Wiederherstellung nach Objekt-Eviction. Zwei dieser Tests prüfen zusätzlich, dass der Deployment-Guard Paid-Tarife und unklare Tarifantworten ablehnt.
- Client: 39 plattformunabhängige Prüfungen bestanden, einschließlich Generationen nach ESC/Gruppenwechsel, alter Revisionen, abgelaufener Online-Freigabe, Wiederherstellung und HTTP-Antwortgrenzen.
- Tatsächlicher .NET-HTTP-/WebSocket-Client gegen den lokalen Worker: Beitrittsanfrage, Freigabe, sofortiges Rot-Update, automatischer Zustandsabgleich nach etwa 55 Sekunden und Entfernung mit endgültigem Zugriffsentzug bestanden. Keine Cloudflare-Zugangsdaten und keine Spieleingaben verwendet.
- Sprachprüfungen: 18.379 Prüfungen über die bestehenden 20 Kataloge bestanden. Gruppennamen bleiben wörtlich; geteilte Teamfarben und Statusmeldungen werden übersetzt. Neue Gruppenmeldungen sind Deutsch/Englisch, weitere Kataloge verwenden hierfür Englisch.
- Windows-WPF-Anwendung sowie erweiterte AutomationChecks und GroupStorageChecks von Linux aus für Windows ohne Warnungen oder Fehler kompiliert. Worker-Deployment-Dry-Run mit SQLite-Migration und Free-kompatiblen Bindings bestanden.

Der abschließende [GitHub-Actions-Lauf](https://github.com/Gamewalker/WardogsTeamselector/actions/runs/37861736790) bestand einschließlich Windows-Bild-, Automations-, DPAPI- und Backend-Prüfungen, der Online-Freigabeprüfung direkt vor der Eingabe sowie beider EXE-Artefakte. Der GUI-Smoke-Lauf der EXE mit Runtime bestand ebenfalls: beide Betriebsmodi, getrennte Beitrittsanfragen/Mitglieder, Gruppen-Stopp und ungültige verspätete Antworten nach ESC werden geprüft. Screenshots bei Standard- und Mindestgröße sind als `WardogsTeamselector-group-ui` im CI-Lauf verfügbar. Die automatisierten Windows-Prüfungen liefen auf dem Windows-Runner; eine manuelle Sichtprüfung der neuen Screenshots und ein Versuch im echten Spiel wurden nicht ausgeführt. Keine Benutzerprofile gespeichert und keine Spieleingaben gesendet. Die Cloudflare-Live-Bereitstellung ist auf Wunsch des Nutzers bis zum nächsten verfügbaren Zugang verschoben. Es wurde kein Tarif geändert und keine neue App-Version veröffentlicht.

## Stopp neben dem Laufstatus (8. Oktober 2026)

`build.ps1 -Tests -OutputDirectory dist/stop-placement` besteht und veröffentlicht beide Runtime-Varianten ohne Warnungen oder Fehler. Die Plattformprüfung besteht mit 23 Checks; Windows verweigert den Test-Fokuswechsel, weshalb diese einzelne Assertion übersprungen wird.

Der GUI-Prüflauf beider endgültigen EXEs besteht. Er prüft die feste Laufsteuerung direkt über den Profilaktionen in allen vier Bereichen, bei Standard- und Mindestgröße sowie auf Englisch, Arabisch und Chinesisch. Stopp steht mittig neben dem Laufstatus, bleibt innerhalb des Fensters und ist weiterhin nur beim Warten oder Klicken aktiv. Die Diagnosevorschau passt sich der verfügbaren Höhe an. Aufnahmen unter `artifacts/stop-placement-confirm-with-runtime` und `artifacts/stop-placement-confirm-without-runtime` erfassen außerdem Teamwechsel, manuellen Stopp und Sprachwechsel. Betrieb und Diagnose wurden visuell kontrolliert. Nach dem Merge mit der neuen Update-Aktion in der Kopfzeile bestehen zusätzlich der Anwendungsbuild ohne Warnungen oder Fehler und der GUI-Prüflauf unter `artifacts/stop-placement-merge`, einschließlich sichtbarem Update-Button bei Mindestgröße. Keine Benutzerprofile gespeichert und keine Mauseingaben gesendet.

## Mehrsprachige Oberfläche (8. Oktober 2026)

`LocalizationChecks` besteht mit 13.176 Prüfungen für alle 20 Sprachkataloge: gleiche Schlüssel, nicht leere Übersetzungen, unveränderte Platzhalter und dynamische Werte, Sprachwechsel und Rückwechsel, Englisch als Standard, unbekannte Sprachcodes, arabische Leserichtung, unveränderte Fensterfilter und Pfade sowie Sprachpräferenz-Roundtrip und Wiederherstellung nach beschädigter JSON-Datei. Persistenzprüfungen verwenden ausschließlich temporäre Dateien; Benutzerpräferenzen bleiben unverändert. Die Prüfung ist in `build.ps1 -Tests` und GitHub Actions eingebunden.

Beide Runtime-Varianten wurden unter `dist/multilingual-final` ohne Warnungen und Fehler veröffentlicht. Der GUI-Prüflauf jeder Einzeldatei-EXE besteht mit jeweils 54 Aufnahmen unter `artifacts/languages-release-with-runtime` und `artifacts/languages-release-without-runtime`. Geprüft werden die englische Startsprache, alle 20 auswählbaren Sprachen, der Erhalt ungespeicherter Eingaben und eines wartenden Laufs beim Sprachwechsel sowie arabische Leserichtung. Alle vier Bereiche sind auf Englisch, Arabisch und Chinesisch in 1180 × 820 und 920 × 660 DIP erfasst; die Kopfzeile bleibt bei Mindestgröße innerhalb des Fensters. Die aktuellen Windows-11-Steuerelemente sowie Update-Neustart und Buildtitel aus `main` sind enthalten. Kleine englische, arabische und chinesische Ansichten wurden visuell kontrolliert.

Alle vorhandenen Bild-, Steuerungs- und Profilprüfungen bestehen; nach Übernahme des Update-Neustarts bestehen alle 54 Updateprüfungen. Die Plattformprüfung besteht mit 23 Checks; Windows verweigerte den Test-Fokuswechsel, sodass diese einzelne Assertion übersprungen wurde. Keine Mauseingaben wurden an das Spiel gesendet. Die Sprachkataloge enthalten maschinell erzeugte Übersetzungen mit kontextuell überarbeiteten englischen Bedienbegriffen; die Prüfungen bestätigen technische Vollständigkeit, keine muttersprachliche Prüfung aller Übersetzungen.

## Update-Neustart und sichtbare Buildnummer (8. Oktober 2026)

Alle 54 Updateprüfungen bestanden. Der echte PowerShell-Helfer installiert und startet dabei eine harmlose Test-EXE aus einem Pfad mit Leerzeichen, Apostroph und Unicode. Prüfsummenfehler verhindern den Neustart; ein fehlgeschlagener Prozessstart erhält die erfolgreiche Installation und meldet den nötigen manuellen Start. Die bisherigen Prüfungen für Backup, gesperrte bzw. geänderte EXEs und Helferbereinigung bestehen weiterhin.

Die Einzeldatei-EXE mit Runtime wurde ohne Warnungen oder Fehler gebaut. Der GUI-Prüflauf bestand und kontrolliert die Buildnummer im Fenstertitel sowie den deaktivierten Neustartbutton ohne vorbereitetes Update. Der Updatebereich wurde bei Mindestgröße visuell geprüft; Screenshots liegen unter `artifacts/update-restart-smoke`. Der ungespeicherte-Einstellungen-Dialog wurde in diesem Prüflauf nicht interaktiv bedient.

## Herkunft und Fehlereinreichung (8. Oktober 2026)

`build.ps1 -Tests -OutputDirectory dist/project-info` war erfolgreich und baute beide Runtime-Varianten ohne Warnungen oder Fehler. Alle Bild-, Steuerungs-, Profil- und 33 Updateprüfungen bestanden. Die Plattformprüfung bestand mit 23 Checks; Windows verweigerte im Test den Vordergrundwechsel, weshalb diese einzelne Assertion übersprungen wurde.

Der GUI-Prüflauf der EXE mit Runtime bestand. Die Kopfzeile, der zusätzliche Fehlerbutton unter Diagnose und das Herkunftsfenster wurden visuell geprüft, einschließlich Mindestgröße und gescrolltem Fensterende. Screenshots stehen unter `artifacts/project-info-smoke`. Es wurde kein GitHub-Issue abgeschickt und kein Diagnoseexport übertragen.

## Automatischer Spielfokus (8. Oktober 2026)

Die Einstellung **Spiel nach Teamaktivierung in den Vordergrund holen** ist standardmäßig aktiv, auch beim Laden alter Profile ohne diesen Wert. Die Profilprüfung deckt die dauerhaft gespeicherte Deaktivierung ab. Der GUI-Prüflauf prüft den Standardwert, die Übernahme der deaktivierten Option bei einer Teamaktivierung und das Weiterlaufen eines wartenden Versuchs beim Umschalten. Im GUI-Prüflauf selbst wird kein fremdes Fenster aktiviert.

Der GUI-Prüflauf der gebauten EXE mit Runtime war erfolgreich. Die Einstellung wurde bei Mindestfenstergröße visuell kontrolliert; Screenshots stehen unter `artifacts/game-focus-smoke`.

Der vollständige `build.ps1 -Tests -OutputDirectory dist/game-focus` war erfolgreich und hat beide EXE-Varianten gebaut. Die Plattformprüfung verwendet ein separates Testfenster und prüft fehlendes Spielfenster, Prozessfilter, Ausschluss minimierter Fenster aus der Aufnahme, Wiederherstellen bei Aktivierung und Rückmeldung des tatsächlichen Vordergrundzustands. Im abschließenden Lauf waren alle 24 Prüfungen erfolgreich, einschließlich des echten Vordergrundwechsels aus einem konkurrierenden Testfenster. Kein echter Wardogs-Fokuswechsel und keine Spieleingaben wurden ausgeführt. Windows-Fokusbeschränkungen werden respektiert; bei verweigertem Wechsel wartet die Teamaktivierung weiter auf Spielfokus und schreibt einen Diagnosehinweis.

## Automatische Updates (8. Oktober 2026)

CI-Korrektur nach dem ersten Push: `pwsh → dotnet → powershell.exe` vererbte PowerShell-7-Modulpfade an Windows PowerShell. Der echte Installationshelfertest ließ sich unter lokalem PowerShell 7 mit `Safe replacement: success` und dem Zusatz `Get-FileHash` nicht gefunden reproduzieren. Anwendung und Tests verwenden jetzt denselben Helferstart, der `PSModulePath` ausschließlich aus der Kindprozessumgebung entfernt. Unter PowerShell 7 bestehen anschließend alle 33 Updateprüfungen; vier davon prüfen den bereinigten Modulpfad. Bei einem erneuten Fehlschlag enthalten die Assertions außerdem das tatsächliche Installationsergebnis. Der Anwendungsbuild ist ohne Warnungen und Fehler erfolgreich. Der erneute GitHub-Actions-Lauf steht bis zum nächsten Push aus.

- Vollständiger `build.ps1 -Tests -OutputDirectory dist/automatic-update` erfolgreich, einschließlich aller vorhandenen Bild-, Steuerungs-, Profil- und 18 Plattformprüfungen. Beide Runtime-Varianten gebaut.
- `UpdateChecks`: 29 Prüfungen für neue/gleiche/ältere Builds, Drafts und Prereleases, passende Runtime-Variante, SHA-256, Downloadgröße, EXE-Kennung und sicheren Austausch. Der reale PowerShell-Helfer wurde mit temporären Dateien ausgeführt: erfolgreicher Austausch mit unveränderter Sicherung, beschädigter Download, zwischenzeitlich geänderte und gesperrte Ziel-EXE. Pfade enthalten Leerzeichen, Apostroph und Unicode. Die Benutzer-EXE wurde nicht ersetzt.
- Öffentlichen GitHub-Release ohne Anmeldung abgefragt und dessen `without-runtime`-EXE tatsächlich heruntergeladen und anhand der Release-Prüfsumme validiert (`UpdateChecks --live`, zwei zusätzliche Prüfungen).
- Abschließender Neubau und GUI-Prüflauf der EXE mit Runtime erfolgreich. Der Prüflauf kontrolliert deaktivierte Netzprüfungen im Smoke-Modus und die Update-Einstellungen unter Konfiguration. Screenshots einschließlich des gescrollten Updatebereichs in beiden Fenstergrößen unter `artifacts/automatic-update-smoke` visuell kontrolliert.
- Die CI führt die Updateprüfungen auf Windows aus und bettet Laufnummer sowie Runtime-Variante in die Release-EXEs ein. Lokale Builds ohne `-ReleaseBuild` bleiben von automatischen Updates ausgeschlossen.
- Ein vollständiger Selbstupdate-Lauf einer produktiven Release-EXE beim Beenden wurde nicht ausgeführt; Download und Austausch wurden separat mit realem GitHub-Asset bzw. temporären Dateien geprüft.

## Aktuelle Oberflächenprüfung: vier Arbeitsbereiche

Die GUI-Startprüfung `--ui-smoke <Ausgabeordner>` kontrolliert die automatische Startansicht, alle vier Bereiche in 1180 × 820 und 920 × 660 DIP, Navigation ohne Einstellungsänderungen, ausdrücklich eingeschaltetes Zeichnen, den Abbruch einer Zeichnung beim Bereichswechsel, Übernahme der Prozentwerte beim Teamwechsel, Validierungsfehler und doppelte Hotkeys. Sie prüft außerdem, dass Teamaktivierung ungespeicherte Änderungen nicht als gespeichert meldet, der Betrieb keine zusätzliche Vorschauaufnahme startet, Navigation und Vorschauabschaltung einen wartenden Lauf erhalten und der globale Stopp ihn beendet. Dialog- und HUD-Referenzen sowie das fehlende Spielfenster werden als reale GUI-Zustände erfasst. Dabei werden keine Benutzereinstellungen gespeichert und keine globalen Hotkeys registriert.

Historische Prüfläufe stehen darunter; ihre früheren Beschreibungen der Dialogabwesenheitsfrist gelten nicht mehr für den aktuellen Ablauf.

Ergänzung zur Betriebsanzeige: Der Prüflauf kontrolliert, dass der Testmodus nur unter Diagnose umschaltbar ist, Zustand/Grund/Zähler und Hotkeys zentriert sind und ausschließlich das aktuell wartende oder klickende Team markiert wird. Wechsel zwischen Blau, Rot und Grün sowie Stopp und Testmoduswechsel werden geprüft; Stopp ist vor der Aktivierung und danach deaktiviert. `clicking-fixture.png` prüft die Darstellung der Klickphase mit einem UI-Snapshot bei gestopptem Controller und ist kein Beleg echter Spieleingaben.

Umbenennung 7. Oktober 2026: Projekt, Namespace, Assemblies und portable EXE heißen `WardogsTeamselector`. Vollständiger Neubau mit allen fünf Prüfprojekten erfolgreich: 19 Dialog-, 17 HUD-Bildprüfungen, Steuerungs-/Profiltests und 18 Plattformprüfungen. Gültige alte Einstellungen werden beim ersten Start in `%LOCALAPPDATA%/WardogsTeamselector` kopiert; die alte Datei bleibt unverändert.

Ergänzung 7. Oktober 2026: Live-Vorschau und Detaildiagnose sind abschaltbar. Profil-Roundtrip der gespeicherten Wahl geprüft. Die GUI-Startprüfung kontrolliert, dass beim Abschalten der Vorschau-Timer deaktiviert, Bild und Messwerttabelle freigegeben und eine aktivierte Steuerung weiterhin im Wartezustand bleibt. Neue Veröffentlichung unter `dist/update`, da die vorherige EXE zum Buildzeitpunkt lief. GUI-Prüfungen binden keine globalen Hotkeys, damit sie laufende Instanzen nicht stören.

Ausgeführt am 6. Oktober 2026 auf Windows x64.

- `build.ps1 -Tests`: erfolgreich; eigenständige Veröffentlichung nach `dist/WardogsTeamselector.exe`.
- DetectionChecks: 19 erfolgreich, darunter 1080p/1440p/4K, geänderte Überschrift und Teamfarben, Hover auf allen drei Karten, kleine Cursorüberdeckung, Wirkung der einstellbaren 90-/85-Prozent-Schwelle, zwei verdeckte Messflächen, verdeckter Rand und fehlender Dialog. Hover-/Cursorfälle wurden synthetisch am Referenzbild nachgebildet.
- JoinedScreenChecks: 17 erfolgreich mit dem gelieferten Gameplay-Screenshot, 720p/1080p/1440p/4K, fehlenden einzelnen Balken, falschen Abständen und zu dicken weißen Formen.
- AutomationChecks: erfolgreich; interne 50-ms-Grenze, Wartezustand, Fokus/Kalibrierung, kurze Dialogabwesenheit mit Fortsetzung desselben Teams, mehrfache stabile Rückkehr, Abwesenheitsfrist, HUD-Flackern und stabile Bestätigung, deaktivierter HUD-Frühstopp, Fokusverlust während der Bestätigung, Geometriewechsel, Einstellungs-Snapshot, konkurrierende Aktivierungen, Testmodus, Aufnahmefehler und synchroner Abbruch.
- ConfigurationChecks: erfolgreich; JSON-Roundtrip mit negativen Monitorursprüngen, Teamdaten, manuellem Bereich und abgewiesenen ungültigen Profilen. Keine Benutzereinstellungen verändert.
- PlatformChecks: im aktuellen Lauf 17 erfolgreich (Monitor-/Fenstergeometrie, Prozessfilter, Aufnahmedimensionen und Win32-INPUT-ABI). Die Farbprobe wurde wegen verdecktem Testfenster übersprungen; ein früherer Lauf bestätigte sie mit insgesamt 18 Prüfungen.
- Veröffentlichte EXE: Start und interne Referenzprüfung über `--ui-smoke` erfolgreich. Screenshots der drei Register und der Mindestfenstergröße unter `artifacts/confirmation-smoke`, einschließlich eingebetteter Folgescreen-Referenz mit erkanntem HUD.
- Unabhängige Prüfung: kein wesentlicher Logikfehler gefunden; Kontrast des blauen Buttons korrigiert und mit 5,41:1 bestätigt.

Es wurden keine echten Mauseingaben an Wardogs gesendet. Tatsächliche Spielaufnahme, HDR/exklusives Vollbild, reale UI-Skalierung und Akzeptanz der Eingaben durch das Spiel bleiben unbestätigt. Die EXE wurde auf diesem Rechner getestet, nicht auf einem frisch aufgesetzten Windows ohne .NET; die Veröffentlichung ist self-contained mit gebündelter Runtime.
