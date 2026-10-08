# Prüfung des Release-Builds

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
