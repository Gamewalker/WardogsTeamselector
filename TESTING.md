# Prüfung des Release-Builds

## Aktuelle Oberflächenprüfung: vier Arbeitsbereiche

Die GUI-Startprüfung `--ui-smoke <Ausgabeordner>` kontrolliert die automatische Startansicht, alle vier Bereiche in 1180 × 820 und 920 × 660 DIP, Navigation ohne Einstellungsänderungen, ausdrücklich eingeschaltetes Zeichnen, den Abbruch einer Zeichnung beim Bereichswechsel, Übernahme der Prozentwerte beim Teamwechsel, Validierungsfehler und doppelte Hotkeys. Sie prüft außerdem, dass Teamaktivierung ungespeicherte Änderungen nicht als gespeichert meldet, der Betrieb keine zusätzliche Vorschauaufnahme startet, Navigation und Vorschauabschaltung einen wartenden Lauf erhalten und der globale Stopp ihn beendet. Dialog- und HUD-Referenzen sowie das fehlende Spielfenster werden als reale GUI-Zustände erfasst. Dabei werden keine Benutzereinstellungen gespeichert und keine globalen Hotkeys registriert.

Historische Prüfläufe stehen darunter; ihre früheren Beschreibungen der Dialogabwesenheitsfrist gelten nicht mehr für den aktuellen Ablauf.

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
