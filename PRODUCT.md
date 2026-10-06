# WardogsTeamselector

<!-- impeccable:product-schema 1 -->

## Platform
Windows Desktop, native WPF-Anwendung.

## Stack
C#/.NET, WPF, Veröffentlichung als eigenständige EXE entsprechend dem freigegebenen Umsetzungsplan.

## Users und Zweck
Wardogs-Spieler, die die Teamauswahl vorab aktivieren und ein volles Team wiederholt anklicken möchten, bis die Auswahl gelingt.

## Bestätigte Anforderungen
Zufallsintervalle A–B mit interner Untergrenze 50 ms. Einmaliger Teamhotkey aktiviert; ESC bricht ab. Automatische, anpassbare Bildschirmgeometrie. Sprachunabhängige Erkennung des Auswahlrahmens; kein Klick ohne Dialog. Verschwundener Dialog pausiert Klicks; kehrt er innerhalb der standardmäßig 10 Sekunden langen Bestätigungsfrist zurück, wird dasselbe Team weiter versucht. Endgültiger Stopp bei Ablauf der Frist oder stabil erkanntem Folgescreen. Drei Teams mit überschreibbaren Flächen. Deutsche GUI mit Live-Diagnose.

## Belege
Assets/reference.png im Anwendungsprojekt, Referenz 3838 × 2158. Reale Aufnahme-/Eingabekompatibilität im Spiel ist noch zu prüfen.

## Darstellung
Dunkle Darstellung passend zur Nutzung neben dem Spiel wurde vom Nutzer bestätigt. Native, kompakte Werkzeugoberfläche mit Live-Vorschau, Einstellungen und Diagnose.
