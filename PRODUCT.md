# WardogsTeamselector

<!-- impeccable:product-schema 1 -->

## Platform

Windows Desktop, native WPF-Anwendung.

## Stack

C#/.NET 10, WPF, Windows x64. Zwei portable Einzeldatei-EXE-Varianten mit eingebetteten Referenzbildern: mit gebündelter Runtime und ohne Runtime. Das Profil gehört zum jeweiligen Windows-Benutzer.

## Users und Zweck

Wardogs-Spieler, die ein Team vorab aktivieren und ein volles oder verblasstes Team wiederholt anklicken möchten, bis der Beitritt erkannt wird. Die deutsche Oberfläche begleitet Einrichtung, alltäglichen Betrieb, Konfiguration und Fehlersuche.

## Bestätigte Anforderungen

Ein einmaliger Teamhotkey oder eine Teamtaste aktiviert Blau, Rot oder Grün. Standard sind F6, F7 und F8; drei unterschiedliche F1–F24 sind wählbar. ESC und die dauerhaft sichtbare Stopptaste beenden den Lauf. Die Stopptaste ist nur während Warten oder Klicken aktiviert; ESC bleibt auch für das Spiel verfügbar. Zufallsintervalle erfüllen 50 ≤ Minimum ≤ Maximum ≤ 60000 ms, Standard 50–70 ms.

Eine Aktivierung wartet zunächst auf Spielfenster, Spielfokus und den über drei aufeinanderfolgende Erkennungen stabilen Auswahldialog. Die Dialogerkennung prüft feste Rahmen und neutrale Flächen unabhängig von Sprache und Teamfarbe. Nach dem ersten Klick wird dasselbe Team weiter versucht, auch wenn der Dialog verschwindet oder seine Erkennung flackert. Erfolg beendet den Lauf erst, wenn die fünf weißen HUD-Balken mindestens 0,5 Sekunden ununterbrochen und in mindestens drei Erkennungen vorliegen. Die HUD-Prüfung ist immer aktiv und beendet auch bei gleichzeitig erkanntem Dialog. Eine Dialog-Abwesenheitsfrist und ein HUD-Abschalter entfallen; entsprechende alte Profilwerte werden ignoriert.

Nach Beginn stoppen außerdem Fokusverlust, Verlust oder Veränderung des Spielbereichs, fehlende Kalibrierung für echte Eingaben sowie Aufnahme-/Steuerungsfehler. Fokus und Geometrie werden unmittelbar vor Eingaben erneut geprüft; zu alte Aufnahmen stoppen einen begonnenen Versuch. Nach einem endgültigen Stopp ist eine neue Aktivierung nötig. Bearbeiten von Steuerungs- und Kalibrierwerten stoppt einen Lauf; reine Navigation und Umschalten der automatischen Vorschau tun dies nicht.

Testmodus ist im Standardprofil eingeschaltet und simuliert Klicks ohne Mauseingaben. Sein Schalter steht ausschließlich in Diagnose; Umschalten beendet einen laufenden Versuch und wird als Profiländerung behandelt. Eingabemodus und Laufzustand bleiben im Kopf sowie in der Betriebsanzeige sichtbar. Automatisch wird der physische Clientbereich des passenden Spielfensters ermittelt. Monitor, manuelle Desktopgrenzen und drei Teamflächen sind anpassbar. Abweichende Seitenverhältnisse erfordern geprüfte Geometrie, bevor echte Klicks freigegeben werden. Teamflächen und Dialogkalibrierung werden separat bearbeitet.

## Bedienablauf und Profil

Vier Bereiche ordnen die Anwendung nach Aufgabe:

- **Einrichtung:** Spiel verbinden, Monitor und Bild prüfen, Teamflächen kalibrieren und mit „Speichern & zum Betrieb“ abschließen. Zeichnen wird ausdrücklich eingeschaltet und ist nur bei gestopptem Lauf möglich; eine begrenzte Auswahlkontur begleitet den Zug. Prozentwerte werden beim Teamwechsel, Prüfen, Speichern oder Aktivieren validiert und übernommen.
- **Betrieb:** Den angezeigten Test-/Echtmodus prüfen, Team einmal aktivieren und zum Spiel wechseln. Oben steht die zentrierte Laufanzeige mit Zustand, Grund, Zähler und Modus; darunter die Teamtasten mit großen zentrierten F-Tasten. Nur das aktive Team trägt einen weißen Rahmen und „Aktiv · wartet“ oder „Aktiv · klickt“. Nach dem Stopp verschwinden die Marker. Kontextaktionen führen zur Einrichtung, Konfiguration oder zu „Diagnose / Testmodus“.
- **Konfiguration:** Intervalle, unterschiedliche F-Tasten, Erkennungsschwelle und Vorschau wählen. Erweiterte Dialogkalibrierung ist aufklappbar; Standardwerte werden als ungespeicherter Entwurf geladen.
- **Diagnose:** Testmodus umschalten, Dialog- und HUD-Referenz prüfen, Messwerte und Protokoll ansehen, Bild oder Diagnose exportieren. Referenzen stoppen einen laufenden Versuch und senden keine Eingaben. Die linke Spalte scrollt zusätzlich als Ganzes, damit auch bei Mindestgröße alle Prüfungen erreichbar bleiben.

Neue Profile starten in Einrichtung, gespeicherte gültige Profile in Betrieb. Ein ungültiges gespeichertes Profil öffnet Einrichtung mit einem Fehlerhinweis und sperrt die Aktivierung, bis gültige Einstellungen gespeichert wurden. Kopf und Profilfußleiste bleiben in allen Bereichen verfügbar. Die Fußleiste unterscheidet Standardprofil, gespeicherte Einstellungen und ungespeicherte Änderungen und bietet Speichern sowie bei Änderungen Verwerfen. Beim Schließen mit Änderungen wird nach Speichern gefragt; Schließen ohne Speichern oder Abbrechen bleiben möglich.

Eine Teamaktivierung verwendet gültige aktuelle Eingaben, speichert sie aber nicht automatisch. Ungültige Teamflächenentwürfe halten die betreffende Teamauswahl korrigierbar. Feldfehler führen zum zuständigen Bereich, öffnen nötige Expander, markieren das Feld, scrollen es in Sicht und fokussieren es. Hotkeykonflikte nennen Teams und F-Taste und fokussieren das zu ändernde Auswahlfeld mit sichtbarem Fehlerrahmen.

Zusätzliche Vorschauaufnahmen und PNG-Konvertierungen laufen nur in Einrichtung und Diagnose. Automatische Aktualisierung ist abschaltbar; nötige Steuerungs- und HUD-Erkennung bleiben im Betrieb aktiv. Manuelles Laden erstellt eine Einzelaufnahme, Referenzprüfungen bleiben verfügbar. Die Vorschauwahl wird mit dem Profil gespeichert.

## Belege

Implementierung: `src/WardogsTeamselector/MainWindow.Layout.cs`, `MainWindow.cs` und `Automation/AutomationController.cs`. Referenzen: `Assets/reference.png` (3838 × 2158 Pixel) und das eingebettete HUD-Referenzbild im Anwendungsprojekt. `README.md` ist die aktuelle Bedienanleitung; `DESIGN.md` beschreibt die native visuelle Identität.

Die aktuellen GUI-Prüfungen und Ansichten unter `artifacts/operator-status-with-runtime/` und `artifacts/operator-status-without-runtime/` erfassen vier Bereiche bei Standard- und Mindestgröße, Startzuordnung, Zeichenschutz, Teamflächenentwürfe, Validierungsfokus, doppelte Hotkeys, Speicherzustand, Vorschaulebenszyklus, den Testmodus nur in Diagnose, zentrierte Laufanzeige und F-Tasten, exklusive Teammarkierung beim Warten und Wechseln sowie deren Entfernen beim Stopp und die bedingt aktive Stopptaste. Referenzen und der Zustand ohne Spielfenster bleiben geprüft. Dabei werden keine Mauseingaben gesendet und keine Benutzerprofile gespeichert. `clicking-fixture.png` zeigt ausschließlich einen eingespeisten UI-Snapshot bei gestopptem Controller und belegt keinen realen Klicklauf. Die reale Aufnahme-/SendInput-Kompatibilität im Wardogs-Spiel bleibt mit einer tatsächlichen Spielaufnahme und echten Eingaben zu prüfen.

## Darstellung

Die vom Nutzer bestätigte dunkle native Windows-Werkzeugoberfläche bleibt erhalten: Segoe UI, vorhandenes Anwendungssymbol, Teamfarben, klare deutsche Labels und native helle Kontrollen. Die Überarbeitung ordnet die Bedienung nach Aufgabe und erhält Status, Stopp und Profilaktionen unabhängig vom aktiven Bereich. Bestehende Bildassets bleiben unverändert.
