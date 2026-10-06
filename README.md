# WardogsTeamselector

Windows-Werkzeug für die wiederholte Teamauswahl. Portable EXE: `dist/WardogsTeamselector.exe`. Beim ersten Start ist der Testmodus eingeschaltet.

Die fertig gebaute EXE wird als Asset im [privaten GitHub-Release](https://github.com/Gamewalker/WardogsTeamselector/releases/latest) bereitgestellt. Der Quellcode enthält Buildskript und Prüfungen; SDK, lokale Vorschauartefakte und Buildausgaben sind nicht im Repository enthalten.

## Benutzung

1. EXE starten und Wardogs öffnen. Titel und Prozessname müssen standardmäßig `wardogs` enthalten. Falls das Spiel anders heißt, beide Filter unter **Monitor & Kalibrierung** anpassen.
2. Im Auswahlbildschirm das **Live-Bild** prüfen. Teamrahmen zeigen die Klickflächen, Punkte deren Mitte; gelbe Markierungen zeigen die Messflächen. Die Diagnose sollte „Dialog: erkannt“ melden.
3. Bei abweichender Oberfläche den Spielbereich, die Dialogkalibrierung und gegebenenfalls die drei Teamflächen anpassen. Teamflächen als Prozentwerte eingeben und **Teamfläche übernehmen**, oder das gewünschte Team unter Kalibrierung auswählen und im Live-Bild ein Rechteck ziehen. Änderungen speichern.
4. A und B sind minimale/maximale zufällige Wartezeit in Millisekunden. Es gilt **50 ≤ A ≤ B ≤ 60000**. Standard: 100–200 ms. Die interne Untergrenze 50 ms kann auch über die Konfigurationsdatei nicht unterschritten werden.
5. Zunächst im Testmodus kontrollieren, anschließend für echte Eingaben **Testmodus** abwählen. **F6** aktiviert Blau, **F7** Rot, **F8** Grün. Alternativ den Teambutton anklicken. Hotkeys sind auf unterschiedliche F1–F24 anpassbar.
6. Die Aktivierung darf schon vor der Auswahl erfolgen. Der Lauf wartet, bis der Dialog mehrfach stabil erkannt wurde und das Spiel im Vordergrund liegt. Auch volle/verblasste Teams werden angeklickt. **ESC** beendet Warten und Klicken global; ESC wird weiterhin an das Spiel gegeben.

Wenn der Dialog verschwindet, pausieren die Klicks und die App prüft den Beitritt. Kommt der Dialog innerhalb der Bestätigungswartezeit zurück, setzt sie dasselbe Team nach erneuter stabiler Erkennung fort. Der Standardwert **10 Sekunden** ist unter **Dialog muss fehlen für** einstellbar. Erst nach durchgehender Abwesenheit bis zum Ablauf der Frist endet der Lauf dauerhaft. Ohne erkannten Auswahldialog werden auch während dieser Frist keine Klicks gesendet.

Optional endet der Lauf früher, wenn der Folgescreen anhand seiner fünf weißen HUD-Balken unten rechts mindestens **0,5 Sekunden** stabil erkannt wird. Der Schalter **Beitritt über fünf HUD-Balken erkennen** ist standardmäßig aktiv. Die Prüfung berücksichtigt auch Abstände und dünne Balkenform, nicht nur helle Pixel. **Folgescreen prüfen** testet die mitgelieferte Referenz; cyanfarbene Messflächen und die HUD-Diagnose zeigen die Erkennung. Bei anderem HUD, veränderter Gesundheit oder UI-Skalierung kann die Erkennung fehlen: Dann bleibt die 10-Sekunden-Prüfung wirksam. Die Frist bestätigt Abwesenheit des Dialogs, nicht semantisch einen erfolgreichen Beitritt.

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

Ergebnis ist eine eigenständige Einzeldatei-EXE mit eingebettetem Referenzbild. Eine separat installierte .NET-Runtime ist für die Veröffentlichung nicht erforderlich. Native Bibliotheken können beim Start intern extrahiert werden.

Die Bildprüfungen decken Referenz, proportionale Skalierung, veränderte Texte/Teamfarben sowie negative Fälle ab. Die Steuerungsprüfungen verwenden simulierte Bildschirme und Eingaben, insbesondere für die 50-ms-Untergrenze, Wartezustand, Stopp, Fokus-/Geometriewechsel und konkurrierende Starts. Diese Tests ersetzen keine Prüfung von Aufnahme und SendInput im echten Wardogs-Spiel.
