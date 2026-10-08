# WardogsTeamselector

Windows-Werkzeug für die wiederholte Teamauswahl. Zwei portable EXE-Varianten: mit gebündelter Runtime und ohne Runtime. Beim ersten Start ist der Testmodus eingeschaltet.

Beide fertig gebauten EXE-Varianten werden als Assets im [privaten GitHub-Release](https://github.com/Gamewalker/WardogsTeamselector/releases/latest) bereitgestellt. Der Quellcode enthält Buildskript und Prüfungen; SDK, lokale Vorschauartefakte und Buildausgaben sind nicht im Repository enthalten.

## Benutzung

Die Oberfläche hat vier Bereiche. Neue Profile starten in **Einrichtung**, vorhandene gültige Profile direkt in **Betrieb**. Ein ungültiges gespeichertes Profil öffnet die Einrichtung mit einem Fehlerhinweis.

1. **Einrichtung:** Wardogs öffnen. Titel und Prozessname müssen standardmäßig `wardogs` enthalten; die Filter und den Monitor bei Bedarf anpassen. **Spiel suchen / Bild laden** übernimmt die Eingaben für die Vorschau. Im Auswahlbildschirm die Teamrahmen und ihre Klickpunkte prüfen. Zum Ändern das gewünschte Team wählen und **Teamfläche im Bild zeichnen** einschalten, oder **Teamfläche als Prozentwerte** aufklappen. Änderungen werden auch beim Teamwechsel und beim Speichern übernommen. Mit **Speichern & zum Betrieb** die Einrichtung abschließen.
2. **Betrieb:** Zunächst **Testmodus verwenden** eingeschaltet lassen. Ein Team per Taste oder Button aktivieren, dann zum Spiel wechseln. **F6** aktiviert Blau, **F7** Rot, **F8** Grün. Für echte Mauseingaben den Testmodus ausschalten; der Modus bleibt auch im globalen Status sichtbar. Der Lauf wartet auf den stabil erkannten Dialog und Spielfokus. **Stopp · ESC** ist in allen Bereichen erreichbar; ESC funktioniert global und wird weiterhin an das Spiel gegeben.
3. **Konfiguration:** Klickintervalle, F-Tasten, Erkennungsschwelle und automatische Vorschau einstellen. Das Klickintervall ist zufällig zwischen Minimum und Maximum; es gilt **50 ≤ Minimum ≤ Maximum ≤ 60000**, Standard **50–70 ms**. Erweiterte Dialogverschiebung und Skalierung bei Bedarf aufklappen, anschließend **Übernehmen & in Einrichtung prüfen** wählen. Die interne 50-ms-Untergrenze kann nicht unterschritten werden.
4. **Diagnose:** Dialog- oder HUD-Referenz prüfen, Messwerte und Protokoll ansehen sowie Bild oder Diagnose exportieren. Referenzen sind klar als statische Testbilder gekennzeichnet; sie senden keine Eingaben und stoppen einen aktiven Lauf. **Live-Bild laden** kehrt zum Spielbild zurück.

**Ungespeicherte Änderungen** stehen in der Fußleiste. **Einstellungen speichern** sichert sie dauerhaft; **Änderungen verwerfen** lädt das gespeicherte Profil. Beim Schließen mit Änderungen wird nach Speichern gefragt. Eine Teamaktivierung verwendet gültige aktuelle Eingaben, speichert diese aber nicht automatisch. Reine Navigation und der Vorschau-Schalter stoppen keinen Lauf; das Bearbeiten der Steuerungs- und Kalibrierwerte stoppt ihn.

Die Aktivierung darf schon vor der Auswahl erfolgen. Auch volle/verblasste Teams werden angeklickt. Globale Teamhotkeys sind auf unterschiedliche F1–F24 anpassbar.

Nach dem ersten Klick klickt der Lauf dasselbe Team weiter an, auch wenn der Auswahldialog verschwindet oder seine Erkennung flackert. Er endet automatisch erst, wenn die fünf weißen HUD-Balken unten rechts mindestens **0,5 Sekunden** ununterbrochen erkannt werden. Die bisherige Beendigung nach „Dialog muss fehlen für X Sekunden“ entfällt vollständig.

Die HUD-Prüfung ist immer aktiv und berücksichtigt auch Abstände und dünne Balkenform. **HUD-Referenz prüfen** unter Diagnose testet die mitgelieferte Referenz; cyanfarbene Messflächen und die HUD-Diagnose zeigen die Erkennung. Auch bei gleichzeitig positivem Dialogbefund beendet das stabil erkannte HUD den Lauf. Bei abweichendem HUD bleibt der Lauf aktiv, bis **ESC** gedrückt wird oder eine der unten genannten Stoppbedingungen eintritt. Alte Profile werden weiterhin geladen; die früheren Wartezeit- und HUD-Abschaltwerte werden ignoriert.

ESC, Fokusverlust während eines begonnenen Versuchs, Änderungen der Geometrie oder Aufnahmefehler stoppen weiterhin sofort. Nach einem endgültigen Stopp braucht eine neue Auswahl eine neue Aktivierung. Bearbeiten von Einstellungen stoppt einen laufenden Vorgang.

## Bildschirm und Kalibrierung

Automatisch wird der physische Clientbereich des Spielfensters erkannt. Ein fest ausgewählter Monitor bindet die Auswahl an diesen Monitor; die tatsächliche Fenstergröße wird berücksichtigt. Manuelle Grenzen sind physische Desktopkoordinaten `X,Y,Breite,Höhe` und müssen innerhalb des Spielbereichs liegen. Negative Monitorursprünge sind möglich.

Referenz ist das mitgelieferte, in der EXE eingebettete Bild mit 3838 × 2158 Pixeln. Für 16:9 wird proportionale Skalierung verwendet. Andere Seitenverhältnisse benötigen Kalibrierung und das Häkchen **Abweichende Geometrie geprüft** unter Einrichtung, bevor echte Klicks freigegeben werden. Die Erkennung prüft Rahmen und feste neutrale Flächen, keine sprachabhängige Schrift, Teamfarben oder wechselnden Zahlen.

**Dialogverschiebung/Skalierung** verändern die Erkennungsflächen. Die Teamflächen sind separat zu prüfen und anzupassen. HDR, abweichende UI-Skalierung und exklusives Vollbild müssen mit einer tatsächlichen Spielaufnahme geprüft werden. Schwarze oder verdeckte Aufnahmen erlauben keine Klicks.

Die Live-Vorschau wird regelmäßig aktualisiert. Das Appfenster darf den zu erkennenden Dialog nicht verdecken; idealerweise die GUI auf einem zweiten Monitor verwenden oder zum Spiel wechseln.

Die automatische Live-Vorschau lässt sich unter Konfiguration ausschalten. Zusätzliche Vorschauaufnahmen und PNG-Konvertierung finden ohnehin nur in Einrichtung und Diagnose statt. Im Betrieb bleibt die nötige Erkennung für Teamklicks und Beitrittsbestätigung aktiv. Statuswechsel bleiben sichtbar; bei ausgeschalteter Vorschau aktualisiert sich der Klickzähler höchstens einmal pro Sekunde. Umschalten stoppt keinen Versuch. Mit **Einstellungen speichern** bleibt die Wahl beim nächsten Start erhalten. **Live-Bild laden** erstellt bei ausgeschalteter Automatik eine Einzelaufnahme; Referenzprüfungen bleiben manuell verfügbar.

Die **Erkennungsschwelle** ist als Prozentwert unter Konfiguration einstellbar; Standard ist **90 %**. Gespeicherte eigene Werte bleiben erhalten. Hover-Aufhellung einer Teamkarte und eine kleine lokale Cursorüberdeckung werden toleriert. Die schwächste Flächenprobe wird nicht gewichtet und in der Diagnose markiert; alle vier äußeren Rahmenkanten bleiben erforderlich. Der Score ist keine Prozentzahl aller Bildschirm-Pixel. Bei weiterhin fehlender Erkennung zeigt die Diagnose die abweichenden Messflächen; eine Aufnahme mit Maus auf dem Dialog hilft bei der weiteren Kalibrierung.

## Diagnose und Dateien

- **Dialogreferenz prüfen** unter Diagnose testet die Erkennung am eingebetteten Bild und sendet keine Eingaben.
- **Bild speichern** exportiert den zuletzt gezeigten Frame als PNG.
- **Diagnose** zeigt Einzelwerte, RGB-Soll/Ist, Status, Klickzähler und Stoppgrund.
- **Diagnose exportieren** speichert das Protokoll einschließlich Geometrie und aktueller Messwerte.
- Einstellungen: `%LOCALAPPDATA%\WardogsTeamselector\settings.json`. Ungültige gespeicherte Profile sperren den Start, bis gültige Einstellungen gespeichert wurden.
- Beim ersten Start ohne neues Profil werden gültige Einstellungen aus `%LOCALAPPDATA%\WardogsClicker\settings.json` übernommen. Die alte Datei bleibt erhalten, damit ältere Versionen weiter funktionieren.
- **Standardwerte laden …** unter Konfiguration lädt das Standardprofil; anschließend speichern, um es dauerhaft zu behalten.

## Entwicklung und Veröffentlichung

.NET-10-SDK benötigt, WPF/Windows x64. Das SDK wurde für diesen Arbeitsbereich lokal unter `.tools/dotnet` installiert. Alternativ ein regulär installiertes SDK nutzen.

```powershell
.\build.ps1 -Tests
```

Ergebnis sind zwei Einzeldatei-EXEs mit eingebetteten Referenzbildern im Verzeichnis `dist/`:

- `WardogsTeamselector-win-x64-with-runtime.exe`: mit gebündelter Runtime; keine separate .NET-Installation erforderlich. Native Bibliotheken können beim Start intern extrahiert werden.
- `WardogsTeamselector-win-x64-without-runtime.exe`: ohne gebündelte Runtime; benötigt die installierte **.NET 10 Desktop Runtime (x64)**. Beide Varianten verwenden dieselben Einstellungen.

Die Bildprüfungen decken Referenz, proportionale Skalierung, veränderte Texte/Teamfarben sowie negative Fälle ab. Die Steuerungsprüfungen verwenden simulierte Bildschirme und Eingaben, insbesondere für die 50-ms-Untergrenze, Wartezustand, Stopp, Fokus-/Geometriewechsel und konkurrierende Starts. Diese Tests ersetzen keine Prüfung von Aufnahme und SendInput im echten Wardogs-Spiel.

## GitHub Actions

`.github/workflows/windows-build.yml` läuft bei Push, Pull Request und manuell über **Actions → Windows EXE → Run workflow**. Auf einem Windows-Runner mit .NET 10 führt sie die Dialog-, HUD-, Steuerungs- und Profilprüfungen aus, kompiliert die Plattformprüfungen und baut anschließend beide portablen Windows-x64-EXE-Varianten. Nach erfolgreichem Lauf steht **WardogsTeamselector-win-x64** unter **Artifacts** für 30 Tage zum Download bereit; das ZIP enthält beide EXE-Varianten mit den Namenszusätzen `with-runtime` und `without-runtime`.

Die Plattformprüfungen werden in CI nur kompiliert: Ihre Ausführung und die GUI-/Spielintegration benötigen einen geeigneten interaktiven Windows-Desktop und müssen dort separat geprüft werden. Nach jedem erfolgreichen Push auf `main` und jedem manuellen Lauf auf `main` veröffentlicht die Pipeline außerdem ein GitHub-Release mit beiden EXE-Varianten und einem Changelog. Releases erhalten eindeutige Tags `build-<Laufnummer>-<Commit>`; Wiederholungen desselben Laufs aktualisieren beide EXE-Assets. Pull Requests und andere Branches erzeugen keine Releases.

Für verständliche Release-Beschreibungen neue Änderungen in einer eigenen Markdown-Datei unter `.github/release-notes/` erläutern. Das Release übernimmt nur neue oder geänderte Beschreibungen seit dem letzten veröffentlichten Release sowie die Commit-Titel. Beide EXEs sind dauerhaft unter **Releases** verfügbar, unabhängig von der 30-Tage-Aufbewahrung der Actions-Artefakte.
