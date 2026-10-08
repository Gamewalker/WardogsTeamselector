# Automatische Updates

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

## So funktionieren Updates

Sobald eine neue Version erkannt wird, erscheint oben neben **Über die App** ein hervorgehobener **Update**-Button. Während des automatischen Downloads ist er deaktiviert; der Tooltip zeigt die Buildnummer und den Downloadstatus. Nach dem geprüften Download installiert ein Klick das Update und startet die App neu. Ist der Download fehlgeschlagen, lädt ein erneuter Klick das Update herunter, prüft es und startet anschließend die Installation mit Neustart. Ungespeicherte Einstellungen werden dabei wie beim normalen Beenden abgefragt.

Release-EXEs prüfen beim Start und alle sechs Stunden die neueste stabile GitHub-Veröffentlichung und laden eine neuere Version automatisch herunter. Die Runtime-Variante bleibt erhalten. Downloads werden anhand von Größe, EXE-Kennung und GitHubs SHA-256-Prüfsumme geprüft. Erst beim regulären Beenden ersetzt ein unsichtbarer Helfer die EXE; beim nächsten Start läuft die neue Version. Sobald ein Update heruntergeladen und geprüft ist, kann unter **Konfiguration → Automatische Updates** mit **Update installieren und neu starten** sofort installiert werden. Nach erfolgreicher Installation startet der Helfer die App automatisch wieder. Die aktuelle Release-Buildnummer ist im Fenstertitel sichtbar. Laufende Teamklicks werden durch die Prüfung nicht unterbrochen. Die bisherige EXE bleibt als `<EXE>.previous` im selben Ordner erhalten. Benutzereinstellungen bleiben erhalten.

Unter **Konfiguration → Automatische Updates** lässt sich die Automatik deaktivieren oder eine Prüfung manuell starten. Update-Einstellungen werden separat unter `%LOCALAPPDATA%\WardogsTeamselector\updates.json` gespeichert. Es wird kein GitHub-Token benötigt. Bei fehlender Verbindung, unvollständigen Releases, gesperrten Dateien oder fehlenden Schreibrechten bleibt die bisherige EXE erhalten; das Installationsergebnis steht beim nächsten Start im Updatebereich. Der EXE-Ordner muss beschreibbar sein. Zum Wiederherstellen kann bei geschlossener Anwendung die `.previous`-Datei zurückkopiert werden.

Lokale Entwicklungsbuilds und GUI-Prüfläufe aktualisieren sich nicht automatisch. Bereits vorhandene EXEs ohne Updatefunktion müssen einmal manuell durch ein neues Release ersetzt werden. Die CI bettet die GitHub-Laufnummer und Runtime-Variante in beide EXEs ein; lokale Release-Builds unterstützen `build.ps1 -ReleaseBuild <Laufnummer>`.
