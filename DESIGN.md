---
name: WardogsTeamselector
description: Dunkle native Windows-Werkzeugoberfläche neben dem Spiel.
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
  warning: "#f5cc7b"
  probe: "#ffd700"
  hud-probe: "#00ffff"
  preview-background: "#000000"
  input-background: "#ffffff"
  input-text: "#000000"
  tab-background: "#e6eaec"
typography:
  title:
    fontFamily: "Segoe UI"
    fontSize: "24px"
    fontWeight: 600
  page-title:
    fontFamily: "Segoe UI"
    fontSize: "21px"
    fontWeight: 600
  team-button:
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
spacing:
  field-padding: "6px"
  action-gap: "8px"
  team-gap: "12px"
  content-padding: "16px"
  outer-margin: "20px"
components:
  button:
    textColor: "{colors.input-text}"
    padding: "8px 12px"
  button-team-blue:
    backgroundColor: "{colors.team-blue}"
    textColor: "{colors.team-button-text}"
    typography: "{typography.team-button}"
    padding: "8px 12px"
  button-team-red:
    backgroundColor: "{colors.team-red}"
    textColor: "{colors.team-button-text}"
    typography: "{typography.team-button}"
    padding: "8px 12px"
  button-team-green:
    backgroundColor: "{colors.team-green}"
    textColor: "{colors.team-button-text}"
    typography: "{typography.team-button}"
    padding: "8px 12px"
  text-field:
    backgroundColor: "{colors.input-background}"
    textColor: "{colors.input-text}"
    padding: "6px"
---

# Design System: WardogsTeamselector

## Overview

**Creative North Star: "Windows-Werkzeug neben dem Spiel"**

Die bestätigte Identität bleibt eine dunkle, kompakte native Windows-Werkzeugoberfläche. Segoe UI, ausgeschriebene deutsche Beschriftungen, Teamfarben und das vorhandene Anwendungssymbol verbinden das Werkzeug mit Wardogs. Helle native Bedienelemente stehen auf dunklem Grund; ihre Windows-/WPF-Zustände gehören zur Oberfläche.

Die gebaute Anordnung trennt Einrichtung, Betrieb, Konfiguration und Diagnose. Sichtbare Rückmeldungen erklären Lauf, Testmodus, Fehler und Speicherzustand. Die visuelle Quelle sind `src/WardogsTeamselector/MainWindow.Layout.cs` und `MainWindow.cs`; fertige Ansichten liegen unter `artifacts/usability-final-with-runtime/` und `artifacts/usability-final-without-runtime/`. Die bestehende Palette und die Bildassets bleiben erhalten.

**Key Characteristics:**

- Dunkle tonale Flächen mit hellen nativen Windows-Kontrollen.
- Teamfarben mit Teamnamen und registrierten F-Tasten.
- Dauerhaft erreichbarer Stopp, globaler Laufstatus und Profilfußleiste.
- Vorschau und Messwerte bei Einrichtung und Diagnose.

## Colors

Anthrazit trägt die Oberfläche, gedämpftes Blaugrau erklärt sie und die drei Teamfarben kennzeichnen die Teamwahl. Die Frontmatter-Werte sind die Farbquelle.

### Primary

**Teamblau**, **Teamrot** und **Teamgrün** tragen Aktivierungstasten, Teamrahmen und Mittelpunkte im Bild. Weiße Tastenbeschriftungen ergänzen die ausgeschriebenen Namen. Teamrot trägt außerdem die beschriftete globale Stopptaste.

### Secondary

**Erkennungsgrün** hebt einen positiven Befund mit Text und Score hervor. **Fehlerkoralle** markiert Fehlertext und betroffene Eingaben; **Warnsand** ungespeicherte Änderungen und echte Klicks. **Messgelb** und **HUD-Cyan** unterscheiden die Dialog- und HUD-Konturen.

### Neutral

Fenstergrund, dunkle Flächen, Wechselzeilen und Tabellenkopf verwenden die vorhandenen Anthrazitabstufungen. Helltext trägt Hauptinformationen, Hinweisblaugrau Erklärungen und Trennblaugrau Rahmen und Linien. Schwarz umgibt die proportionale Bildvorschau. Textfelder sind weiß mit schwarzem Text; Tabs besitzen eine helle Basis. Windows/WPF liefert die tatsächliche Auswahl-, Hover- und Fokusdarstellung nativer Kontrollen.

**The Teamzuordnung Rule.** Teamfarben bleiben zwischen Aktivierung und Bildmarkierung gleich; Teamname und Zustand bleiben zusätzlich als Text lesbar.

## Typography

**Body Font:** Segoe UI. Dieselbe native Schrift trägt Titel, Kontrollen und Diagnose; es gibt keine separate dekorative Display- oder Monospace-Schrift. Die Frontmatter-Werte in `px` stehen für WPF-Geräteeinheiten (DIP), nicht für physische Bildschirm-Pixel.

### Hierarchy

- **Title:** Anwendungstitel und großer Laufzustand im Betrieb.
- **Page title:** Einstieg und Aufgabenbeschreibung jedes Bereichs.
- **Team button:** Teamname und F-Taste auf den drei Aktivierungstasten.
- **Heading:** wiederkehrende Gruppen wie Teamflächen, Tastenkürzel und Protokoll.
- **Body:** Felder, Tabs, globaler Status, Hinweise und Diagnose. Hinweise haben 20 DIP Zeilenhöhe; Zähler im Betrieb sind etwas größer (16 DIP).

**The Lesbarer Zustand Rule.** Lauf, Eingabemodus, Speicherzustand und Erkennung werden ausgeschrieben. Hinweise, Fehler, Labels und Zähler umbrechen innerhalb ihrer Fläche.

## Layout

Das Fenster startet mit 1180 × 820 DIP und hat eine Mindestgröße von 920 × 660 DIP. Außenabstand: 20 DIP; Registerinhalt: 16 DIP. Oberhalb der Tabs stehen Anwendungssymbol, Titel, globaler Laufstatus mit Test-/Echtmodus und rechts die Stopptaste. Ein sichtbarer Fehler fügt sich darunter ein. Die feste Fußleiste enthält links den Profilzustand und rechts Verwerfen sowie Speichern.

Vier nummerierte native Tabs bilden die Aufgabenfolge: **1. Einrichtung**, **2. Betrieb**, **3. Konfiguration**, **4. Diagnose**. Einrichtung hat links eine 320-DIP-Formularspalte mit 20 DIP Abstand zur flexiblen Vorschau; das Formular scrollt unabhängig vom Bild. Betrieb verwendet drei gleich breite Teamtasten und einen vertikalen Scrollbereich. Konfiguration hat zwei gleich breite Spalten mit 28 DIP Zwischenraum in einem gemeinsamen Scrollbereich. Diagnose verwendet links 420 DIP für Referenzaktionen, Tabelle und Protokoll und rechts die flexible Vorschau; Tabelle und Protokoll scrollen separat.

Bei Mindestgröße bleibt die Anordnung erhalten: Aktionen und Texte umbrechen, umfangreiche Formulare scrollen. Prozentwerte, manuelle Spielgrenzen und Dialogkalibrierung liegen in zunächst geschlossenen Expandern. Die gemeinsame Vorschau erscheint ausschließlich in Einrichtung und Diagnose. Neue oder ungültige Profile öffnen Einrichtung; ein gespeichertes gültiges Profil öffnet Betrieb.

**The Dauerhaft erreichbar Rule.** Navigation und Scrollen lassen globalen Status, Stopp und Profilaktionen an ihren festen Positionen.

## Elevation & Depth

Die eigene Oberfläche verwendet keine Schatten oder dekorativen Animationen. Tonale Flächen, horizontale Trennlinien und Rahmen vermitteln Struktur. Buttons, Tabs, Eingaben, Auswahlfelder, Checkboxen, Expander und Scrollleisten behalten native Windows-/WPF-Interaktionen einschließlich Fokus, Hover und Auswahl.

## Shapes

Rechteckige Kontroll- und Bildflächen bestimmen die Form; eigene abgerundete Karten oder Pillen sind nicht implementiert. Die schwarze Vorschaufläche beschneidet ihr Overlay und erhält das Bildseitenverhältnis. Dialog- und HUD-Messflächen verwenden 1 DIP Kontur; Teamflächen 2 DIP Kontur mit einem 8-DIP-Mittelpunkt. Die aktuelle Zeichenauswahl verwendet ein weiß gestricheltes Rechteck mit 2 DIP Kontur und schwacher transparenter Füllung.

## Components

### Buttons

Deutsche Verben benennen die Aktion. Die Basis hat mindestens 36 DIP Höhe, 12 DIP horizontalen und 8 DIP vertikalen Innenabstand. Normale Aktionen verwenden native helle Buttons. Teamtasten sind mindestens 78 DIP hoch, gleich breit und mit 12 DIP Abstand angeordnet; sie zeigen Teamname und registrierte F-Taste oder „Hotkey inaktiv“. Die rote Stopptaste bleibt im Kopf erreichbar.

### Inputs / Fields

Native helle Felder stehen auf dunklem Grund. Textfelder haben mindestens 32 DIP Höhe, 6 DIP Innenabstand und 6 DIP unteren Abstand. Labels sind ihren Feldern zugeordnet; Eingaben und Teamhotkeys haben zugängliche Namen. Validierungsfehler wechseln zum zuständigen Bereich, öffnen nötige Expander, markieren die Eingabe, scrollen sie in Sicht und setzen den Fokus. Doppelte F-Tasten nennen Teams und Taste und fokussieren das zu ändernde Auswahlfeld. Ein sonst transparenter 2-DIP-Rahmen um die Hotkeyfelder stellt die Fehlermarkierung unabhängig vom nativen ComboBox-Theme sicher.

### Navigation und Profil

Die vier sichtbaren Tabs behalten native Auswahlzustände. Kontextaktionen im Betrieb führen zur Einrichtung oder Konfiguration. Reine Navigation verändert weder Profil noch Lauf. Die Fußleiste unterscheidet „Standardprofil · noch nicht gespeichert“, „Einstellungen gespeichert“ und „Ungespeicherte Änderungen“; Verwerfen erscheint bei Änderungen. Aktivierung übernimmt gültige aktuelle Eingaben, ohne sie automatisch zu speichern. Beim Schließen mit Änderungen bietet ein natives Dialogfenster Speichern, Schließen ohne Speichern oder Abbrechen.

### Laufstatus

Der Kopf zeigt „Gestoppt“, „Wartet“ oder „Klickt“ mit Team und Eingabemodus. Betrieb ergänzt Grund, Klickanzahl und letztes Intervall. Der Modushinweis ist dunkel und gerahmt; echte Klicks erhalten Warnfarbe und ausdrücklichen Text. Die Betriebsübersicht nennt die Beitrittsbedingung: fünf HUD-Balken für mindestens 0,5 Sekunden.

### Bildprüfung und Kalibrierung

Quelle, Geometrie, Fokus und getrennte Dialog-/HUD-Befunde stehen über dem Bild; die Legende erklärt die Konturen. „Live-Bild laden“ und „Bild speichern“ ergänzen die Vorschau. Leere oder fehlgeschlagene Aufnahmen zeigen einen nächsten Schritt. Statische Referenzen sind beschriftet und senden keine Eingaben.

Zeichnen muss in Einrichtung ausdrücklich eingeschaltet werden und ist nur bei gestopptem Lauf möglich. Kreuzcursor und begrenztes Auswahlrechteck geben Rückmeldung; während des Zuges bleibt das Bild bestehen. Abschluss, Navigation oder Verlust der Mausaufnahme räumen die Zugrückmeldung auf. Prozentwerte bleiben zunächst Entwürfe und werden beim Teamwechsel, Prüfen, Speichern oder Aktivieren validiert und übernommen. Ungültige Werte halten das betreffende Team und Fehlerfeld korrigierbar.

Automatische Vorschauaufnahmen laufen nominell alle 350 ms nur in Einrichtung und Diagnose, sofern aktiviert und kein Referenzbild geöffnet ist. Nötige Steuerungserkennung läuft unabhängig davon; manuelles Laden und Referenzprüfungen bleiben bei ausgeschalteter Vorschau verfügbar.

### Diagnose

Die schreibgeschützte Tabelle zeigt „Messfläche“, „Score“, „Soll“ und „Ist“ auf dunklen alternierenden Zeilen mit horizontalen Trennlinien. Das schreibgeschützte, umbrechende Protokoll trägt Zeitstempel und folgt dem neuesten Eintrag. Referenzprüfungen und Diagnoseexport stehen unmittelbar bei diesen Prüfungen.

## Do's and Don'ts

### Do:

- **Do** deutsche Aktionsbeschriftungen, native Kontrollzustände und die bestehende Segoe-UI-Hierarchie beibehalten.
- **Do** Teamfarben konsistent zwischen Aktivierung und Bildmarkierungen verwenden und mit Text ergänzen.
- **Do** Status, Stopp und Profilaktionen außerhalb der scrollenden Aufgabenflächen erhalten.
- **Do** Fehler mit sichtbarer Erklärung und Tastaturfokus am zuständigen Feld korrigierbar machen.
- **Do** Bildquelle, Referenzmodus und erforderliches Einschalten des Zeichnens erklären.

### Don't:

- **Don't** Farbe als einzige Zustands- oder Teaminformation verwenden.
- **Don't** native helle Eingaben und Tabs als vollständig selbst gestaltetes dunkles Kontrolltheme dokumentieren.
- **Don't** Teamaktivierung als automatisches Speichern darstellen oder Entwürfe als gespeichertes Profil beschriften.
- **Don't** die vorhandene Windows-Schrift oder Anwendungsgrafik durch ein dekoratives neues Thema ersetzen.
