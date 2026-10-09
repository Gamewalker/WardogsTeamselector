---
name: WardogsTeamselector
description: Dunkles natives Windows-Werkzeug mit einer einheitlichen Oberfläche im Windows-11-Stil.
colors:
  background: "#1c1e22"
  content: "#25282d"
  surface: "#202328"
  control: "#303338"
  input: "#1d2024"
  text: "#f5f5f5"
  muted: "#9fb1b9"
  border: "#3e434a"
  control-border: "#50545a"
  accent: "#91c8f6"
  accent-text: "#102434"
  disabled-text: "#aeb4be"
  team-blue: "#1670a3"
  team-red: "#a43835"
  team-green: "#227c4d"
  error: "#ffa189"
  warning: "#f5cc7b"
---

# Design System: WardogsTeamselector

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

## Visuelle Richtung

Die App bleibt ein dunkles natives Windows-Werkzeug neben dem Spiel. Die gewünschte Windows-11-Anmutung entsteht durch einheitliche tonale Flächen, dezente Rundungen, klare Gruppen, Segoe UI und konsistente Linien-Icons. Anwendungssymbol, Teamfarben, deutsche Beschriftungen und der Ablauf mit Einrichtung, Betrieb, Konfiguration und Diagnose bleiben erhalten.

Die gemeinsame Kontrollgestaltung liegt in `src/WardogsTeamselector/Theme.xaml`; Vektor-Icons und gemeinsame Flächen in `MainWindow.Theme.cs`. `MainWindow.Layout.cs` ordnet die Bereiche. Das Theme gilt auch im Fenster „Über die App“.

## Typografie und Icons

Segoe UI trägt die gesamte Oberfläche. Titel: 24 DIP, Seitentitel: 21 DIP, Gruppen: 17 DIP, laufender Zustand: 32 DIP, Teamnamen: 18 DIP, Teamhotkeys: 26 DIP, Fließtext: 14 DIP. Titel und Gruppen sind Semibold. Hinweise umbrechen mit 20 DIP Zeilenhöhe.

Die eigenen Vektor-Icons verwenden ein gemeinsames 24er-Koordinatensystem, 1,7 DIP Strichstärke und abgerundete Linienenden. Aktionen und Navigation zeigen 18 DIP große Icons; Teamaktivierung verwendet 20 DIP. Icons ergänzen sichtbare Beschriftungen. Buttons erhalten weiterhin ausdrücklich zugängliche Aktionsnamen; die Teamnamen enthalten auch Hotkey und Aktivierungszustand.

## Flächen und Abstände

Das Fenster startet mit 1180 × 820 DIP; Mindestgröße ist 920 × 660 DIP. Außen stehen 20 DIP, Registerinhalte haben 20 DIP Innenabstand. Der Kopf hält Anwendungstitel, Info, Sprachauswahl und bei verfügbarer neuer Version die Update-Aktion. Laufstatus und Abbruch sind im Betrieb zusammengefasst. Die Profilfußleiste mit Speichern und Verwerfen erscheint nur in Einrichtung, Konfiguration und Diagnose; sie hat 16 × 12 DIP Innenabstand.

Einrichtung verwendet eine 320-DIP-Spalte, Diagnose eine 420-DIP-Spalte. Ein eigener 24-DIP-Zwischenraum mit mittiger 1-DIP-Linie trennt Formulare und Bildvorschau. Konfiguration verwendet zwei gleich breite, gerahmte Flächen mit 18 DIP Innenabstand und 20 DIP Abstand zueinander. Gruppenüberschriften haben 24 DIP Abstand davor und 12 DIP danach.

Scrollbereiche reservieren zusätzlich 16 DIP rechts und 8 DIP unten für Abstand zwischen Inhalt und Scrollleiste. Die dunklen Scrollleisten sind 14 DIP breit und unterstützen Ziehen, Seitenklicks, Mausrad und Tastatur. Formulare scrollen unabhängig von der Bildvorschau. Der Betrieb bietet Stopp direkt auf dem aktiven Teambutton; Profilaktionen stehen in den Bereichen mit Einstellungen.

## Kontrollen und Zustände

Buttons haben mindestens 40 DIP Höhe, 14 × 9 DIP Innenabstand, 6 DIP Rundung und einen feinen Rahmen. Sekundäre Aktionen sind dunkel mit heller Schrift. Profil speichern und Einrichtung abschließen tragen die hellblaue Akzentfarbe mit dunkler Schrift. Hover hellt die Fläche leicht auf, Drücken dunkelt sie ab. Tastaturfokus zeigt einen 2-DIP-Akzentrahmen. Deaktivierte Buttons verwenden eine gedämpfte dunkle Fläche und lesbare graue Schrift.

Die drei Teamtasten sind mindestens 144 DIP hoch und behalten ihre Blau-, Rot- und Grünflächen sowie weiße Schrift. Nur das laufende Team erhält einen weißen 3-DIP-Außenrahmen, den Hauptschriftzug „Stopp“, ein Stoppsymbol und „Aktiv · wartet“ oder „Aktiv · klickt“. Die anderen Teambuttons haben 45 % Deckkraft und bleiben für Teamwechsel verfügbar. Nach erfolgreichem Beitritt oder Abbruch werden alle Teambuttons wieder vollständig sichtbar und zeigen ihre Teamnamen. Schnelle Teamwechsel aktualisieren die Anzeige auch bei abgeschalteter Vorschau.

Eingaben sind dunkel, mindestens 38 DIP hoch und haben 10 × 8 DIP Innenabstand sowie 5 DIP Rundung. Textfelder zeigen bei Fokus eine Akzentlinie; der Fehlerrahmen bleibt erhalten. Auswahlfelder verwenden dunkle Popups, helle Schrift und unterscheidbare Auswahl- und Hoverflächen. Checkboxen haben eine 18-DIP-Kontur mit 4 DIP Rundung und eine hellblaue Markierung. Expander zeigen eine Trennlinie und einen Richtungswinkel.

Die vier Register zeigen jeweils ein Icon und ihre Aufgabenbezeichnung. Die Auswahl erhält eine tonale Fläche und eine kurze hellblaue Linie. Hover und Tastaturfokus bleiben eigenständig sichtbar. Die native Fensterleiste und Systemdialoge bleiben Windows-Kontrollen.

## Betrieb, Vorschau und Diagnose

Der Betrieb beginnt mit einer zentrierten Laufanzeige in einer gerahmten Fläche mit 8 DIP Rundung. Zustand, Grund, Zähler und Test-/Echtmodus bleiben ausgeschrieben. Warten verwendet Warnfarbe, Klicken Erkennungsgrün; der Laufrahmen verwendet die aktive Teamfarbe.

Die Bildvorschau behält ihr Seitenverhältnis auf Schwarz. Quelle, Geometrie, Dialog- und HUD-Befund stehen darüber, die Legende darunter. Teamflächen, Dialogprüfung und HUD-Prüfung bleiben farblich konsistent. Die Vorschau ist im Bereich Einrichtung und Diagnose sichtbar. Referenzbilder sind als statisch gekennzeichnet und senden keine Eingaben.

Die Diagnose verwendet eine schreibgeschützte Tabelle mit dunklen Wechselzeilen und horizontalen Linien sowie ein umbrechendes Ereignisprotokoll. Die gesamte linke Spalte scrollt, Tabelle und Protokoll haben zusätzlich eigene Scrollbereiche. Der Testmodus-Schalter bleibt ausschließlich in Diagnose.

## Verifikation

Der hervorgehobene Update-Button neben dem Infofenster erscheint nur bei einer erkannten neuen Version. Während des Downloads bleibt er gesperrt; danach installiert er das geprüfte Update mit Neustart. Bei Downloadfehlern ermöglicht er einen erneuten Versuch. Bei Mindestbreite kürzt die Kopfzeile Titel und Status mit Auslassungspunkten, damit die Aktionen frei bleiben. Der GUI-Prüflauf prüft Sichtbarkeit, Downloadsperre, Wiederholung und das Ausblenden nach dem Verwerfen; die Sprach- und Größenprüfungen erfassen auch den sichtbaren Update-Button.

Die GUI-Prüfung erfasst alle vier Bereiche bei Standard- und Mindestgröße, Kalibrierung, Validierungsfokus, Hotkeykonflikte, Speicherzustand, Vorschau, Referenzbilder, Warten, schnelle Teamwechsel, Stopp und das Infofenster. Sie speichert keine Benutzerprofile und sendet keine Mauseingaben. `clicking-fixture.png` zeigt nur einen eingespeisten UI-Zustand. Neue Ansichten liegen lokal unter `artifacts/windows11-ui-confirm/`; die Veröffentlichung wird zusätzlich für beide portablen EXE-Varianten geprüft.


## Geteilte Tabnavigation und Gruppenverwaltung

Einrichtung und Betrieb stehen links, Konfiguration, Diagnose und Gruppenverwaltung rechts. Alle fünf Bereiche teilen eine Inhaltsfläche; es werden keine zwei parallelen Arbeitsbereiche geöffnet. Reicht die Breite der übersetzten Tabs nicht aus, ordnet die Navigation beide Gruppen in getrennten Zeilen an. Die Gruppenverwaltung enthält Anlage, Einladung, Freigabe, Entfernung und administrative Übertragung. Der Betrieb enthält ausschließlich die Auswahl einer aktiven Gruppe, Teamfreigabe und Beitritt. Alle Gruppenauswahllisten zeigen dieselbe aktive Gruppe. Administrative Tokens sind verborgen und werden nur auf ausdrückliche Aktion als privater Code kopiert; Eingaben erfolgen verdeckt.
