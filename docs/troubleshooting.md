# Diagnose und Hilfe

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

## Prüfungen, Exporte und Einstellungen

- **Dialogreferenz prüfen** unter Diagnose testet die Erkennung am eingebetteten Bild und sendet keine Eingaben.
- **Bild speichern** exportiert den zuletzt gezeigten Frame als PNG.
- **Diagnose** zeigt Einzelwerte, RGB-Soll/Ist, Status, Klickzähler und Stoppgrund.
- **Diagnose exportieren** speichert das Protokoll einschließlich Geometrie und aktueller Messwerte.
- Einstellungen: `%LOCALAPPDATA%\WardogsTeamselector\settings.json`. Ungültige gespeicherte Profile sperren den Start, bis gültige Einstellungen gespeichert wurden.
- Beim ersten Start ohne neues Profil werden gültige Einstellungen aus `%LOCALAPPDATA%\WardogsClicker\settings.json` übernommen. Die alte Datei bleibt erhalten, damit ältere Versionen weiter funktionieren.
- **Standardwerte laden …** unter Konfiguration lädt das Standardprofil; anschließend speichern, um es dauerhaft zu behalten.
## Wenn etwas nicht funktioniert

- **Spielfenster fehlt:** Wardogs starten und unter Einrichtung die Filter für Fenstertitel und Prozessname prüfen. Danach **Spiel suchen / Bild laden** wählen.
- **Das Team wartet, ohne zu klicken:** Eingabemodus und Statusgrund ansehen. Das Spielfenster muss verfügbar sein und der Auswahldialog erkannt werden. Bei deaktiviertem Spielfokus selbst zum Spiel wechseln.
- **Nur simulierte Klicks:** Unter **Diagnose → Testmodus verwenden** den Testmodus ausschalten, sobald die Teamflächen geprüft sind.
- **Der Dialog wird nicht erkannt:** Ein aktuelles Spielbild laden und die Messflächen unter Diagnose prüfen. Weitere Hinweise stehen unter [Kalibrierung](calibration.md).
- **Der Lauf endet nicht von selbst:** **ESC** drücken. Der automatische Abschluss benötigt die stabil erkannten fünf weißen HUD-Balken unten rechts.
- **Einstellungen werden nicht übernommen:** Fehlerhinweis korrigieren und **Einstellungen speichern** wählen. Eine Teamaktivierung speichert Änderungen nicht automatisch.

Noch Fragen? [Ein Problem auf GitHub melden](https://github.com/Gamewalker/WardogsTeamselector/issues). Hilfreich sind der angezeigte Statusgrund, die verwendete EXE-Variante und ein Diagnoseexport.

