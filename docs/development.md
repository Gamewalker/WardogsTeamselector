# Entwicklung und Veröffentlichung

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

## Lokal bauen

Benötigt werden Windows x64 und das .NET-10-SDK mit WPF-Unterstützung. Das Buildskript nutzt das lokale SDK unter `.tools/dotnet`, falls vorhanden, ansonsten ein regulär installiertes `dotnet`.

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

Für verständliche Release-Beschreibungen neue Änderungen in einer eigenen Markdown-Datei unter `docs/releases/` erläutern. Das Release übernimmt nur neue oder geänderte Beschreibungen seit dem letzten veröffentlichten Release sowie die Commit-Titel. Beide EXEs sind dauerhaft unter **Releases** verfügbar, unabhängig von der 30-Tage-Aufbewahrung der Actions-Artefakte.

Weitere Details: [Prüfungen und Ergebnisse](TESTING.md), [Produktbeschreibung](PRODUCT.md), [Gestaltung](DESIGN.md), [Umsetzungsplan](PLAN.md), [Dialogerkennung](detection.md) und [Steuerung und Hotkeys](automation.md).

