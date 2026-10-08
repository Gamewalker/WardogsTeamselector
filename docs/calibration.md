# Bildschirm und Kalibrierung

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

## Spielbild und Teamflächen prüfen

Automatisch wird der physische Clientbereich des Spielfensters erkannt. Ein fest ausgewählter Monitor bindet die Auswahl an diesen Monitor; die tatsächliche Fenstergröße wird berücksichtigt. Manuelle Grenzen sind physische Desktopkoordinaten `X,Y,Breite,Höhe` und müssen innerhalb des Spielbereichs liegen. Negative Monitorursprünge sind möglich.

Referenz ist das mitgelieferte, in der EXE eingebettete Bild mit 3838 × 2158 Pixeln. Für 16:9 wird proportionale Skalierung verwendet. Andere Seitenverhältnisse benötigen Kalibrierung und das Häkchen **Abweichende Geometrie geprüft** unter Einrichtung, bevor echte Klicks freigegeben werden. Die Erkennung prüft Rahmen und feste neutrale Flächen, keine sprachabhängige Schrift, Teamfarben oder wechselnden Zahlen.

**Dialogverschiebung/Skalierung** verändern die Erkennungsflächen. Die Teamflächen sind separat zu prüfen und anzupassen. HDR, abweichende UI-Skalierung und exklusives Vollbild müssen mit einer tatsächlichen Spielaufnahme geprüft werden. Schwarze oder verdeckte Aufnahmen erlauben keine Klicks.

Die Live-Vorschau wird regelmäßig aktualisiert. Das Appfenster darf den zu erkennenden Dialog nicht verdecken; idealerweise die GUI auf einem zweiten Monitor verwenden oder zum Spiel wechseln.

Die automatische Live-Vorschau lässt sich unter Konfiguration ausschalten. Zusätzliche Vorschauaufnahmen und PNG-Konvertierung finden ohnehin nur in Einrichtung und Diagnose statt. Im Betrieb bleibt die nötige Erkennung für Teamklicks und Beitrittsbestätigung aktiv. Statuswechsel bleiben sichtbar; bei ausgeschalteter Vorschau aktualisiert sich der Klickzähler höchstens einmal pro Sekunde. Umschalten stoppt keinen Versuch. Mit **Einstellungen speichern** bleibt die Wahl beim nächsten Start erhalten. **Live-Bild laden** erstellt bei ausgeschalteter Automatik eine Einzelaufnahme; Referenzprüfungen bleiben manuell verfügbar.

Die **Erkennungsschwelle** ist als Prozentwert unter Konfiguration einstellbar; Standard ist **90 %**. Gespeicherte eigene Werte bleiben erhalten. Hover-Aufhellung einer Teamkarte und eine kleine lokale Cursorüberdeckung werden toleriert. Die schwächste Flächenprobe wird nicht gewichtet und in der Diagnose markiert; alle vier äußeren Rahmenkanten bleiben erforderlich. Der Score ist keine Prozentzahl aller Bildschirm-Pixel. Bei weiterhin fehlender Erkennung zeigt die Diagnose die abweichenden Messflächen; eine Aufnahme mit Maus auf dem Dialog hilft bei der weiteren Kalibrierung.
