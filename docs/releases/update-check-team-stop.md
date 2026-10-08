# Updateprüfung und Teamsteuerung

Die Updateprüfung verwendet jetzt eine kleine `update.json` aus dem öffentlichen Release und lädt die EXE direkt aus demselben Release. Damit benötigt sie kein anonymes GitHub-API-Kontingent mehr. Größe, EXE-Kennung und SHA-256 werden weiterhin geprüft. HTTP-Fehler werden mit ihrer tatsächlichen Ursache angezeigt, statt pauschal eine fehlende Internetverbindung zu melden. Ältere Releases ohne Manifest werden über die bisherige API geprüft.

Der separate Stoppbutton und seine Statusleiste entfallen. Während eines Laufs zeigt der aktive Teambutton **Stopp** mit Stoppsymbol; ein Klick beendet den Lauf. Die beiden anderen Teambuttons werden blasser und erlauben weiterhin einen Teamwechsel. Nach erfolgreichem Beitritt oder Abbruch erhalten alle Buttons ihre normale Darstellung zurück. ESC bleibt als Abbruch verfügbar.

Die Profilaktionen zum Speichern und Verwerfen stehen nur noch in Einrichtung, Konfiguration und Diagnose. Im Betrieb entfällt die gesamte Profilfußleiste. **Speichern & zum Betrieb** bleibt direkt am Ende der Einrichtung.
