# WardogsTeamselector – Umsetzungsplan

Status: Implementiert und als portable EXE veröffentlicht. Bild-, Steuerungs-, Profil- und Plattformprüfungen sowie GUI-Startprüfung erfolgreich; echte Wardogs-Aufnahme und akzeptierte Spieleingaben noch praktisch zu prüfen. Details und Benutzung in README.md.

## Ziel und Technik

Windows-Anwendung mit deutscher WPF-GUI in C#/.NET, Bildschirmaufnahme, globalen Hotkeys und normalen Windows-Mauseingaben über SendInput. Veröffentlichung als portable, eigenständige win-x64-EXE. Einstellungen und Protokolle liegen separat im Benutzerprofil. Keine Änderungen an Spieldateien erforderlich.

## Referenz und Koordinaten

Die gelieferte PNG-Datei hat 3838 × 2158 Pixel, nicht exakt 3840 × 2160. Vorläufige Klickziele in der Mitte der Teamkarten:

| Team | Referenzpunkt, ungefähr | Normalisierte Position |
| --- | --- | --- |
| Blau | (1620, 1123) | (0,4221; 0,5204) |
| Rot | (1920, 1123) | (0,5003; 0,5204) |
| Grün | (2220, 1123) | (0,5784; 0,5204) |

Grundformel: Bildschirm-X = Ursprung-X + round(Referenz-X × Zielbreite / 3838), entsprechend für Y mit 2158. Alle Berechnungen verwenden physische Pixel. Monitorursprung, negative Koordinaten im Mehrmonitorbetrieb und Windows-DPI-Skalierung werden berücksichtigt (PerMonitorV2).

Zielbereich ist bei Vollbild der Spielmonitor, bei Fensterbetrieb der Clientbereich des Spielfensters. Die Monitorauflösung allein reicht bei einem nicht bildschirmfüllenden Fenster nicht aus. Für abweichende Seitenverhältnisse und UI-Skalierungen gibt es ein Kalibrierprofil; ungeprüfte Geometrie erlaubt zunächst nur einen Test ohne Klicks. Eine spätere lokale Suche nach dem Dialog kann Verschiebungen ausgleichen. Ob das Spiel bei anderen Auflösungen proportional skaliert, bleibt praktisch zu prüfen.

## Erkennung und Ablauf

Die Erkennung kombiniert mehrere kleine Pixelbereiche: linke und rechte äußere Dialogkante, die drei Kartenrahmen mit ihren relativen Abständen, gleichmäßige graue Dialogflächen außerhalb der Schrift und dunkle Karteninnenflächen. Die sprachabhängige Überschrift wird weder als Text noch als Pixelmuster geprüft. Hintergrundmuster und wechselnde Spielerzahlen werden nicht als feste Merkmale verwendet. Im Referenzbild sind beispielsweise (1460,900) mit RGB(113,113,113) und (2380,900) mit RGB(154,154,154) Kandidaten für Rahmenmerkmale; die finalen Messflächen werden im Test kalibriert. Kein einzelner Pixel entscheidet allein. Farbabweichungen, Helligkeitskontraste und ein nachvollziehbarer Gesamtscore machen die Prüfung toleranter gegenüber Skalierung und Kantenglättung.

Die Teamverfügbarkeit sperrt niemals Klicks: Gerade volle bzw. verblasste Teams sollen wiederholt angeklickt werden, um bei einem frei werdenden Platz beizutreten. Die Freigabe hängt ausschließlich vom erkannten Auswahldialog und dem gültigen Spielbereich ab. Zusätzliche Screenshots aktiver Teams sind dafür nicht erforderlich.

Ablauf: Teamhotkey aktiviert das Warten; drei stabile Dialogmessungen starten Klicks. Bei Dialogverlust pausieren die Klicks und eine einstellbare Bestätigungsfrist (Standard 10 Sekunden) beginnt. Kehrt die Auswahl zurück, wird dasselbe Team nach erneuter stabiler Erkennung weiter versucht. Endgültiger Stopp bei Fristablauf oder mindestens 0,5 Sekunden stabil erkanntem Gameplay-HUD mit fünf weißen Balken. Die Frist bestätigt nur Dialogabwesenheit. ESC, Fokusverlust während eines begonnenen Versuchs, Aufnahmefehler und Geometriewechsel stoppen sofort. Nach endgültigem Stopp ist eine neue Aktivierung nötig. Es gibt nur eine Klickschleife; keine Klicks auf Zwischenbildern.

Festgelegte Intervallsemantik: Für jedes Intervall wird eine neue gleichverteilte ganzzahlige Wartezeit zwischen A und B Millisekunden einschließlich beider Grenzen gewählt. Ein zusätzlicher Basiswert X entfällt. Es gilt 50 <= A <= B. Die interne, nicht konfigurierbare Untergrenze beträgt 50 ms und wird zentral in der Klicksteuerung durchgesetzt, auch beim Laden von Profilen oder manuell veränderten Konfigurationsdateien. Die GUI lehnt kleinere Werte ab; ungültige geladene Einstellungen sperren den Start mit einer verständlichen Fehlermeldung. A = B ermöglicht ein festes Intervall. Windows-Timing wird mit Soll-/Ist-Werten protokolliert; Millisekunden sind kein Echtzeitversprechen.

## GUI und Debugging

- Teamwahl, Start/Stopp, frei belegbare Hotkeys pro Team und ESC als globaler Abbruch; Konflikte sichtbar melden.
- Minimales Intervall A, maximales Intervall B in Millisekunden und Erkennungsschwelle.
- Automatische Erkennung von Spielmonitor, Auflösung und Spielbereich nach App-Start; manuelle Monitorbindung und Anpassung der erkannten Geometrie mit Gerätekennung, DPI und Ursprung. Falls das Spiel noch nicht läuft, zunächst Monitore erfassen und auf das Spielfenster warten. Änderungen der Auflösung erkennen und die Geometrie neu bestimmen; laufende Klicks dabei stoppen.
- Live-Vorschau mit Teamrechtecken, Klickpunkten und Messflächen. Rechtecke/Punkte per Auswahlwerkzeug oder Zahlenfeld überschreiben; Profile speichern und zurücksetzen.
- Status, gewähltes Team, Erkennungsscore je Messfläche, RGB-Soll/Ist, letzter Aufnahmezeitpunkt, aktuelles Intervall, Klickzähler, Fokus und letzter Stoppgrund.
- Testmodus ohne Eingaben, Referenzaufnahme und exportierbares Diagnoseprotokoll. Vorschau und Overlay dürfen die Messflächen nicht verdecken.

## Parallel umsetzbare Arbeitspakete

0. Gemeinsam vorab: Projektgerüst, Konfigurationsschema und Verträge für CaptureFrame, TargetGeometry, DetectionResult, ClickSettings und AutomationState festlegen. Physische Koordinaten und Thread-Zuständigkeiten eindeutig definieren.
1. Bildschirm/Geometrie: Monitorinventar, Spielbereich, Aufnahme, DPI und Transformation. Liefert Frames und Zielgeometrie.
2. Erkennung/Kalibrierung: Sprachunabhängige Referenzmessflächen, Score, Toleranzen und Profile. Arbeitet anfangs mit der PNG und gespeicherten Frames, unabhängig von echter Aufnahme.
3. Eingaben/Steuerung: Hotkeys, SendInput, Zufallsintervalle, Zustandsautomat und Abbruch. Arbeitet zunächst mit simulierten Erkennungsresultaten und einer Eingabesenke ohne echte Klicks.
4. GUI/Konfiguration: Bedienoberfläche, Debug-Vorschau, Profilverwaltung und Logexport. Arbeitet zunächst mit simulierten Statusdaten.
5. Integration: Module verbinden, Threading und Abbruchpfade prüfen, echte Klickpunkte kontrolliert kalibrieren.
6. Veröffentlichung: reproduzierbarer Release-Build, eigenständige EXE, README und Test auf einem Windows-System ohne installierte .NET-Runtime.

Pakete 1–4 wurden nach Paket 0 parallel bearbeitet: drei Agenten für Bildschirm, Erkennung und Steuerung sowie der Hauptagent für die GUI. Die Module sind integriert. Eine unabhängige Abschlussprüfung fand keinen wesentlichen Logikfehler; eine Kontrastkorrektur am blauen Teambutton wurde umgesetzt.

## Abnahmekriterien

- Referenzbild und Auswahlbildschirm in unterschiedlichen Sprachen erkannt; Spielansichten, Desktop und überdeckter Dialog lösen keine Klicks aus.
- Klickziele bei 1080p, 1440p und 4K sowie unterschiedlichen DPI-Einstellungen überprüft; Ultrawide und Fensterbetrieb nach Kalibrierung.
- Volle/deaktivierte Teams verhindern weder die Dialogerkennung noch Klicks auf das gewählte Team.
- Aktivierung vor Erscheinen des Dialogs sendet keine Eingaben; erst ein stabil erkannter Dialog mit Spiel im Vordergrund startet Klicks. ESC beendet beide aktiven Zustände.
- Kein weiterer Klick nach einer negativen Prüfung; messbare Stoppzeit, Not-Stopp und Fokusverlust getestet.
- Hotkeywechsel startet keine zweite Schleife; Timing bleibt im konfigurierten Bereich. Intervalle unter 50 ms werden auch über manipulierte Profile und direkte Aufrufe der Klicksteuerung abgewiesen.
- Monitorwechsel, negative Monitorursprünge, Aufnahmeausfall und ungültige Einstellungen sind nachvollziehbar behandelt.
- EXE startet ohne separat installierte Runtime. Funktion echter Eingaben und Aufnahme im verwendeten Vollbildmodus muss im Spiel geprüft werden.

## Festgelegte Anforderungen und verbleibende Vorgaben

Die fünf Ablaufentscheidungen sind geklärt: Zufallsintervall A–B ohne X, automatische und anpassbare Anzeigeerkennung, einmalige Aktivierung mit ESC-Abbruch, Aktivierung bereits vor Erscheinen des Dialogs und ausdrückliches Anklicken voller Teams.

Noch nicht festgelegt sind lediglich die anfänglichen Werte für A/B und die drei Teamhotkeys. Als anpassbare Startwerte sind A = 100 ms, B = 200 ms und F6/F7/F8 für Blau/Rot/Grün vorgesehen. ESC ist fest als Abbruch vorgesehen. Diese Startwerte sind Implementierungsvorschläge, keine bereits bestätigten Nutzerpräferenzen. HDR, Ultrawide und abweichende UI-Skalierung bleiben praktische Kalibrier- und Testfälle, keine vorab manuell erforderlichen Auflösungsangaben.

Technische Quellen:
- https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview
- https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
