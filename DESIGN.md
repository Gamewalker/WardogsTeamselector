---
name: WardogsTeamselector
description: Kompakte native Windows-Werkzeugoberfläche neben dem Spiel.
colors:
  background: "#131a1e"
  text: "#f5f5f5"
  muted: "#9fb1b9"
  border: "#414f55"
  row: "#1c262b"
  row-alternate: "#232d32"
  table-header: "#303e45"
  team-blue: "#1670a3"
  team-red: "#a43835"
  team-green: "#227c4d"
  team-button-text: "#ffffff"
  detected: "#78dca0"
  error: "#ffa189"
  probe: "#ffd700"
  preview-background: "#000000"
typography:
  title:
    fontFamily: "Segoe UI"
    fontSize: "26px"
    fontWeight: 600
  status:
    fontFamily: "Segoe UI"
    fontSize: "18px"
    fontWeight: 600
  heading:
    fontFamily: "Segoe UI"
    fontSize: "17px"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI"
    fontSize: "14px"
    fontWeight: 400
---

# Design System: WardogsTeamselector

## Overview

Bestätigte Richtung: dunkle, kompakte Windows-Werkzeugoberfläche zur Nutzung neben Wardogs. Die deutsche GUI verbindet direkte Teamsteuerung mit Live-Vorschau, Kalibrierung und Diagnose. Grundlage ist die implementierte WPF-Oberfläche in `src/WardogsTeamselector/MainWindow.cs`; native Kontrollzustände stammen aus dem Windows-/WPF-Theme.

## Colors

Anthrazit trägt die Oberfläche, helle Beschriftung die Hauptinformationen und gedämpftes Blaugrau die Hilfstexte. Tabellen und Protokoll verwenden dunkle Flächen mit tonaler Abstufung und sichtbaren Trennlinien.

Blau, Rot und Grün ordnen Aktivierungstasten, Bildrahmen und Klickpunkte den Teams zu. Weiße Tastenbeschriftungen ergänzen ausgeschriebene Teamnamen. Die korrigierte blaue Teamfarbe `team-blue` erreicht mit Weiß 5,41:1 Kontrast. `detected` hebt eine erkannte Auswahl hervor, `error` Fehlermeldungen und `probe` die Messflächen im Bild. Laufzustände werden zusätzlich als Text beschrieben.

## Typography

Segoe UI ist die durchgängige Schrift. Der Titel innerhalb der Oberfläche verwendet 26 DIP und Semibold, der Laufstatus 18 DIP und Semibold, Abschnittsüberschriften 17 DIP und Semibold. Beschriftungen, Eingaben und Hilfstexte übernehmen 14 DIP. Die `px`-Werte im Frontmatter entsprechen WPF-Geräteeinheiten (DIP), nicht physischen Bildschirm-Pixeln. Hilfstexte, Feldbeschriftungen, Geometrie, Fehler und Zähler umbrechen nach verfügbarem Platz.

## Layout

Das Fenster startet mit 1180 × 820 DIP und lässt sich bis 920 × 660 DIP verkleinern. Außenabstand: 24 DIP; Registerinhalt: 16 DIP. Titel, Kurzanleitung, Laufstatus und Fehler stehen oberhalb der Register. Die dauerhaft erreichbare untere Aktionsleiste enthält Speichern, Profil zurücksetzen und Diagnoseexport.

Drei Register: „Steuerung & Live-Bild“, „Monitor & Kalibrierung“ und „Diagnose“. Die Steuerung hat eine feste linke Spalte von 300 DIP mit 24 DIP Abstand zur flexiblen Bildspalte. Die linke Spalte scrollt bei Platzmangel; Bildinformationen und Bildaktionen stehen oberhalb der verbleibenden Vorschaufläche. Bildaktionen können umbrechen. Kalibrierung verwendet zwei gleich breite Spalten und einen gemeinsamen vertikalen Scrollbereich. Diagnose teilt den verbleibenden Platz im Verhältnis 2:1 zwischen Messwerttabelle und Protokoll; beide besitzen eigene Scrollbereiche. Die Register behalten auch im kleinen Fenster ihre Anordnung.

## Elevation & Depth

Die eigene Oberfläche verwendet keine Schatten oder Animationen. Dunkle Flächen, Tabellenzeilen und Rahmen vermitteln Struktur. Buttons, Tabs, Eingaben, Auswahlfelder, Checkboxen und Scrollleisten behalten ihre nativen WPF-Interaktionen einschließlich Fokus-, Hover- und Auswahlzuständen.

## Shapes

Rechteckige Kontroll- und Bildflächen bestimmen die Form. Es gibt keine eigenen abgerundeten Karten oder Pillen. Die Vorschau liegt auf Schwarz und erhält das Seitenverhältnis des Bildes. Messflächen erscheinen als gelbe Rechtecke mit 1 DIP Kontur; Teamflächen als farbige Rechtecke mit 2 DIP Kontur und einem 8 DIP großen Mittelpunkt.

## Components

- **Aktivierung und Stopp:** Drei breite Teamtasten mit weißem Text und 8 DIP Abstand; darunter „Stopp · ESC“. Gemeinsame Buttonbasis: mindestens 34 DIP Höhe, Innenabstand 12 DIP horizontal und 8 DIP vertikal. Globale Teamhotkeys und ESC ergänzen die sichtbare Bedienung.
- **Status und Zähler:** „Gestoppt“, „Wartet auf die Teamauswahl“ oder „Klickt“ mit Teamname. Klickanzahl, letztes Intervall und Grund stehen in der Steuerung; Fehler bleiben über allen Registern sichtbar.
- **Einstellungen:** Native helle Textfelder und Auswahlfelder auf dunklem Grund. Textfelder haben mindestens 29 DIP Höhe, 5 DIP Innenabstand und 7 DIP unteren Abstand; Monitor- und Hotkeyfelder mindestens 30 DIP Höhe. Checkboxen markieren Testmodus und geprüfte Geometrie.
- **Live-Vorschau:** „Live-Bild“, „Referenz prüfen“ und „Bild speichern“ ergänzen Geometrie, Fokus, Kalibrierungsstatus und Erkennungsscore. Live-Aufnahmen aktualisieren sich nominell alle 350 ms. Bild und Overlay skalieren gemeinsam; die Referenzprüfung hält ein statisches Bild ohne Eingaben fest. Im gestoppten Zustand kann ein gezogenes Rechteck die in der Kalibrierung gewählte Teamfläche ändern.
- **Diagnose:** Schreibgeschützte Tabelle mit „Messfläche“, „Score“, „Soll“ und „Ist“, dunklen alternierenden Zeilen und horizontalen Trennlinien. Darunter ein schreibgeschütztes, umbrechendes Protokoll mit Zeitstempeln und automatischem Scrollen zum neuesten Eintrag.

## Do's and Don'ts

- **Do:** Deutsche Textbeschriftungen, erkennbare native Kontrollzustände und textliche Laufzustände beibehalten.
- **Do:** Teamfarben konsistent zwischen Tasten und Bildmarkierungen verwenden; gelbe Messflächen davon unterscheiden.
- **Do:** Vorschauproportionen, Scrollbereiche sowie den festen Status- und Aktionsbereich beim Verkleinern erhalten.
- **Don't:** Farbe als einzige Zustands- oder Teaminformation verwenden.
- **Don't:** Native helle Eingaben und Tabs als vollständig selbst gestaltetes dunkles Kontrolltheme dokumentieren.

## Beitrittsbestätigung

Teambuttons zeigen die registrierten F-Tasten. Bei Dialogverlust pausieren Eingaben; ein Countdown zeigt die einstellbare Bestätigungsfrist. Ein HUD-Schalter aktiviert den früheren Stopp bei fünf stabil erkannten Balken. Live-Bild und Diagnose zeigen einen separaten Folgescreen-Score, cyanfarbene Messflächen und eine eingebettete Folgescreen-Referenz.
