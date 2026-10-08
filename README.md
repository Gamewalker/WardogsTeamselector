# WardogsTeamselector

Windows-Werkzeug für die wiederholte Teamauswahl. Zwei portable EXE-Varianten: mit gebündelter Runtime und ohne Runtime. Beim ersten Start ist der Testmodus eingeschaltet.

Beide fertig gebauten EXE-Varianten werden als Assets im [privaten GitHub-Release](https://github.com/Gamewalker/WardogsTeamselector/releases/latest) bereitgestellt. Der Quellcode enthält Buildskript und Prüfungen; SDK, lokale Vorschauartefakte und Buildausgaben sind nicht im Repository enthalten.

## Benutzung

1. EXE starten und Wardogs öffnen. Titel und Prozessname müssen standardmäßig `wardogs` enthalten. Falls das Spiel anders heißt, beide Filter unter **Monitor & Kalibrierung** anpassen.
2. Im Auswahlbildschirm das **Live-Bild** prüfen. Teamrahmen zeigen die Klickflächen, Punkte deren Mitte; gelbe Markierungen zeigen die Messflächen. Die Diagnose sollte „Dialog: erkannt“ melden.
3. Bei abweichender Oberfläche den Spielbereich, die Dialogkalibrierung und gegebenenfalls die drei Teamflächen anpassen. Teamflächen als Prozentwerte eingeben und **Teamfläche übernehmen**, oder das gewünschte Team unter Kalibrierung auswählen und im Live-Bild ein Rechteck ziehen. Änderungen speichern.
4. A und B sind minimale/maximale zufällige Wartezeit in Millisekunden. Es gilt **50 ≤ A ≤ B ≤ 60000**. Standard: 100–200 ms. Die interne Untergrenze 50 ms kann auch über die Konfigurationsdatei nicht unterschritten werden.
5. Zunächst im Testmodus kontrollieren, anschließend für echte Eingaben **Testmodus** abwählen. **F6** aktiviert Blau, **F7** Rot, **F8** Grün. Alternativ den Teambutton anklicken. Hotkeys sind auf unterschiedliche F1–F24 anpassbar.
6. Die Aktivierung darf schon vor der Auswahl erfolgen. Der Lauf wartet, bis der Dialog mehrfach stabil erkannt wurde und das Spiel im Vordergrund liegt. Auch volle/verblasste Teams werden angeklickt. **ESC** beendet Warten und Klicken global; ESC wird weiterhin an das Spiel gegeben.

Nach dem ersten Klick klickt der Lauf dasselbe Team weiter an, auch wenn der Auswahldialog verschwindet oder seine Erkennung flackert. Er endet automatisch erst, wenn die fünf weißen HUD-Balken unten rechts mindestens **0,5 Sekunden** ununterbrochen erkannt werden. Die bisherige Beendigung nach „Dialog muss fehlen für X Sekunden“ entfällt vollständig.

Die HUD-Prüfung ist immer aktiv und berücksichtigt auch Abstände und dünne Balkenform. **Folgescreen prüfen** testet die mitgelieferte Referenz; cyanfarbene Messflächen und die HUD-Diagnose zeigen die Erkennung. Auch bei gleichzeitig positivem Dialogbefund beendet das stabil erkannte HUD den Lauf. Bei abweichendem HUD bleibt der Lauf aktiv, bis **ESC** gedrückt wird oder eine der unten genannten Stoppbedingungen eintritt. Alte Profile werden weiterhin geladen; die früheren Wartezeit- und HUD-Abschaltwerte werden ignoriert.

ESC, Fokusverlust während eines begonnenen Versuchs, Änderungen der Geometrie oder Aufnahmefehler stoppen weiterhin sofort. Nach einem endgültigen Stopp braucht eine neue Auswahl eine neue Aktivierung. Bearbeiten von Einstellungen stoppt einen laufenden Vorgang.

## Bildschirm und Kalibrierung

Automatisch wird der physische Clientbereich des Spielfensters erkannt. Ein fest ausgewählter Monitor bindet die Auswahl an diesen Monitor; die tatsächliche Fenstergröße wird berücksichtigt. Manuelle Grenzen sind physische Desktopkoordinaten `X,Y,Breite,Höhe` und müssen innerhalb des Spielbereichs liegen. Negative Monitorursprünge sind möglich.

Referenz ist das mitgelieferte, in der EXE eingebettete Bild mit 3838 × 2158 Pixeln. Für 16:9 wird proportionale Skalierung verwendet. Andere Seitenverhältnisse benötigen Kalibrierung und das Häkchen **Geometrie für dieses Profil geprüft**, bevor echte Klicks freigegeben werden. Die Erkennung prüft Rahmen und feste neutrale Flächen, keine sprachabhängige Schrift, Teamfarben oder wechselnden Zahlen.

**Dialogverschiebung/Skalierung** verändern die Erkennungsflächen. Die Teamflächen sind separat zu prüfen und anzupassen. HDR, abweichende UI-Skalierung und exklusives Vollbild müssen mit einer tatsächlichen Spielaufnahme geprüft werden. Schwarze oder verdeckte Aufnahmen erlauben keine Klicks.

Die Live-Vorschau wird regelmäßig aktualisiert. Das Appfenster darf den zu erkennenden Dialog nicht verdecken; idealerweise die GUI auf einem zweiten Monitor verwenden oder zum Spiel wechseln.

Nach der Kalibrierung **Live-Bild und Detaildiagnose aktualisieren** ausschalten. Der Vorschau-Timer, die zusätzlichen Aufnahmen, PNG-Konvertierung und laufende Messwerttabellen entfallen; das Bild wird freigegeben. Die nötige Erkennung für Teamklicks und Beitrittsbestätigung läuft weiter. Statuswechsel bleiben sichtbar, Klickzähler und Countdown aktualisieren sich bei ausgeschalteter Anzeige höchstens einmal pro Sekunde. Umschalten stoppt keinen Versuch. Mit **Einstellungen speichern** bleibt die Wahl beim nächsten Start erhalten. **Live-Bild** aktiviert die Vorschau wieder; Referenzprüfungen bleiben als manuelle Einzelprüfungen verfügbar.

Die **Erkennungsschwelle** ist als Prozentwert unter Monitor & Kalibrierung einstellbar; Standard ist **90 %**. Gespeicherte eigene Werte bleiben erhalten. Hover-Aufhellung einer Teamkarte und eine kleine lokale Cursorüberdeckung werden toleriert. Die schwächste Flächenprobe wird nicht gewichtet und in der Diagnose markiert; alle vier äußeren Rahmenkanten bleiben erforderlich. Der Score ist keine Prozentzahl aller Bildschirm-Pixel. Bei weiterhin fehlender Erkennung zeigt die Diagnose die abweichenden Messflächen; eine Aufnahme mit Maus auf dem Dialog hilft bei der weiteren Kalibrierung.

## Diagnose und Dateien

- **Referenz prüfen** testet die Erkennung am eingebetteten Bild und sendet keine Eingaben.
- **Bild speichern** exportiert den zuletzt gezeigten Frame als PNG.
- **Diagnose** zeigt Einzelwerte, RGB-Soll/Ist, Status, Klickzähler und Stoppgrund.
- **Diagnose exportieren** speichert das Protokoll einschließlich Geometrie und aktueller Messwerte.
- Einstellungen: `%LOCALAPPDATA%\WardogsTeamselector\settings.json`. Ungültige gespeicherte Profile sperren den Start, bis gültige Einstellungen gespeichert wurden.
- Beim ersten Start ohne neues Profil werden gültige Einstellungen aus `%LOCALAPPDATA%\WardogsClicker\settings.json` übernommen. Die alte Datei bleibt erhalten, damit ältere Versionen weiter funktionieren.
- **Profil zurücksetzen** lädt die Standardwerte; anschließend speichern, um sie dauerhaft zu behalten.

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
