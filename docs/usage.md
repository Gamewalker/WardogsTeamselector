# Bedienung

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

## Die vier Bereiche

Die Oberfläche hat vier Bereiche. Neue Profile starten in **Einrichtung**, vorhandene gültige Profile direkt in **Betrieb**. Ein ungültiges gespeichertes Profil öffnet die Einrichtung mit einem Fehlerhinweis.

1. **Einrichtung:** Wardogs öffnen. Titel und Prozessname müssen standardmäßig `wardogs` enthalten; die Filter und den Monitor bei Bedarf anpassen. **Spiel suchen / Bild laden** übernimmt die Eingaben für die Vorschau. Im Auswahlbildschirm die Teamrahmen und ihre Klickpunkte prüfen. Zum Ändern das gewünschte Team wählen und **Teamfläche im Bild zeichnen** einschalten, oder **Teamfläche als Prozentwerte** aufklappen. Änderungen werden auch beim Teamwechsel und beim Speichern übernommen. Mit **Speichern & zum Betrieb** die Einrichtung abschließen.
2. **Betrieb:** Den Eingabemodus im Status prüfen; **Testmodus verwenden** lässt sich unter **Diagnose** umschalten. Ein Team per Taste oder Button aktivieren; das passende Spielfenster wird automatisch in den Vordergrund geholt. Unter **Konfiguration → Spielfokus** lässt sich dieses Verhalten abschalten. **F6** aktiviert Blau, **F7** Rot, **F8** Grün. Die Tasten zeigen die F-Taste mittig und markieren ausschließlich das aktive Team mit weißem Rahmen und **Aktiv · wartet** oder **Aktiv · klickt**. Die große mittige Laufanzeige nennt Zustand, Team, Grund und Klickzähler. **Stopp · ESC** bleibt in allen Bereichen sichtbar und ist nur beim Warten oder Klicken bedienbar; ESC funktioniert global und wird weiterhin an das Spiel gegeben.
3. **Konfiguration:** Klickintervalle, F-Tasten, Erkennungsschwelle und automatische Vorschau einstellen. Das Klickintervall ist zufällig zwischen Minimum und Maximum; es gilt **50 ≤ Minimum ≤ Maximum ≤ 60000**, Standard **50–70 ms**. Erweiterte Dialogverschiebung und Skalierung bei Bedarf aufklappen, anschließend **Übernehmen & in Einrichtung prüfen** wählen. Die interne 50-ms-Untergrenze kann nicht unterschritten werden.
4. **Diagnose:** **Testmodus verwenden** zunächst eingeschaltet lassen, um den Ablauf ohne Mauseingaben zu prüfen. Für echte Klicks hier ausschalten; ein Wechsel beendet den aktuellen Lauf, der Modus bleibt im globalen Status sichtbar. Außerdem Dialog- oder HUD-Referenz prüfen, Messwerte und Protokoll ansehen sowie Bild oder Diagnose exportieren. Referenzen sind klar als statische Testbilder gekennzeichnet; sie senden keine Eingaben und stoppen einen aktiven Lauf. **Live-Bild laden** kehrt zum Spielbild zurück.

**Ungespeicherte Änderungen** stehen in der Fußleiste. **Einstellungen speichern** sichert sie dauerhaft; **Änderungen verwerfen** lädt das gespeicherte Profil. Beim Schließen mit Änderungen wird nach Speichern gefragt. Eine Teamaktivierung verwendet gültige aktuelle Eingaben, speichert diese aber nicht automatisch. Reine Navigation und der Vorschau-Schalter stoppen keinen Lauf; das Bearbeiten der Steuerungs- und Kalibrierwerte stoppt ihn.

Die Aktivierung darf schon vor der Auswahl erfolgen. Auch volle/verblasste Teams werden angeklickt. Globale Teamhotkeys sind auf unterschiedliche F1–F24 anpassbar.

Nach dem ersten Klick klickt der Lauf dasselbe Team weiter an, auch wenn der Auswahldialog verschwindet oder seine Erkennung flackert. Er endet automatisch erst, wenn die fünf weißen HUD-Balken unten rechts mindestens **0,5 Sekunden** ununterbrochen erkannt werden. Die bisherige Beendigung nach „Dialog muss fehlen für X Sekunden“ entfällt vollständig.

Die HUD-Prüfung ist immer aktiv und berücksichtigt auch Abstände und dünne Balkenform. **HUD-Referenz prüfen** unter Diagnose testet die mitgelieferte Referenz; cyanfarbene Messflächen und die HUD-Diagnose zeigen die Erkennung. Auch bei gleichzeitig positivem Dialogbefund beendet das stabil erkannte HUD den Lauf. Bei abweichendem HUD bleibt der Lauf aktiv, bis **ESC** gedrückt wird oder eine der unten genannten Stoppbedingungen eintritt. Alte Profile werden weiterhin geladen; die früheren Wartezeit- und HUD-Abschaltwerte werden ignoriert.

ESC, Fokusverlust während eines begonnenen Versuchs, Änderungen der Geometrie oder Aufnahmefehler stoppen weiterhin sofort. Nach einem endgültigen Stopp braucht eine neue Auswahl eine neue Aktivierung. Bearbeiten von Einstellungen stoppt einen laufenden Vorgang.
