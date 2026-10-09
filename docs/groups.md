# Gruppen und gemeinsame Teamauswahl

[← Dokumentation](README.md)

Die Hauptnavigation zeigt **Einrichtung** und **Betrieb** links, **Konfiguration**, **Diagnose** und **Gruppenverwaltung** rechts. Bei zu wenig Platz erscheinen die beiden Bereiche in getrennten Zeilen. Unter **Betrieb** gibt es die Tabs **Manuell** und **Gruppenmodus**. Bildschirmkalibrierung, Klickintervalle, Testmodus und Spielfokus bleiben auf jedem PC separat eingestellt.

Die neuen Gruppenmeldungen stehen auf Deutsch und Englisch zur Verfügung. In den weiteren vorhandenen Oberflächensprachen verwenden die neuen Gruppenmeldungen zunächst Englisch; die bisherigen Übersetzungen bleiben erhalten. Selbst eingegebene Gruppen- und Spielernamen werden nicht übersetzt.

## Gruppe erstellen und einladen

1. Unter **Gruppenverwaltung → Gruppendienst einrichten** ist für neue oder bisher leere Einstellungen `https://wardogs-groups.niels-82f.workers.dev/` voreingestellt. Für einen eigenen Dienst die HTTPS-Adresse ändern und speichern; vorhandene eigene Adressen bleiben erhalten. Die [Deployment-Anleitung im Repository-Wiki](wiki/Eigenen-Gruppendienst-deployen.md) erklärt die Bereitstellung.
2. Unter **Gruppenverwaltung** **Gruppe erstellen** wählen und Gruppenname sowie deinen Namen eingeben.
3. **Einladungslink kopieren** und an deine Mitspieler senden.
4. Mitspieler wählen unter **Gruppenverwaltung** **Gruppe beitreten**, fügen den vollständigen Link ein und geben ihren Namen ein. Der Link allein bestätigt niemanden.
5. Als Ersteller die Gruppe aktualisieren. Unter **Gruppe verwalten → Offene Beitrittsanfragen** eine Anfrage auswählen und **Bestätigen** oder **Ablehnen** wählen.

Die Bestätigung wird beim nächsten Aktualisieren auf dem eingeladenen PC sichtbar. Gleichnamige Personen werden durch eine kurze Mitgliedskennung unterschieden. Namen bestätigen keine Identität.

## Ein Team teilen

Es können mehrere Gruppen gespeichert werden. Die Gruppenauswahl unter **Manuell**, **Gruppenmodus** und **Gruppenverwaltung** ist dieselbe aktive Gruppe. Ein Wechsel beendet den bisherigen Gruppenlauf und entwertet seine verspäteten Antworten. Die Auswahl bleibt nach einem Neustart erhalten; Auto bleibt ausgeschaltet.

Unter **Manuell** über den drei Teambuttons **Mit Gruppe teilen** einschalten und eine eigene Gruppe wählen. Anschließend den normalen blauen, roten oder grünen Teambutton oder die zugehörige F-Taste verwenden. Nach erfolgreicher Veröffentlichung beginnt der normale lokale Beitritt. Ohne Häkchen bleiben die Gruppenauswahl deaktiviert und die Teamaktionen rein lokal. Das Häkchen ist nach einem Neustart aus. Scheitert die Veröffentlichung, wird ein lokaler Beitritt separat angeboten. Der Server kann eine Veröffentlichung bereits erhalten haben, obwohl die Bestätigung auf deinem PC nicht ankommt; der Hinweis lautet deshalb „nicht bestätigt“.

Erneutes Teilen derselben Farbe ist eine neue Veröffentlichung. **Auswahl zurücknehmen** entfernt das Gruppenziel. Ein lokaler Stopp nimmt eine bereits geteilte Auswahl nicht zurück.

## Einer Gruppe folgen

Im **Gruppenmodus** eine bestätigte Gruppe auswählen. **Einmal beitreten** versucht die aktuelle Auswahl einmal. **Auto folgen** bleibt nach dem HUD-Erfolg bereit und folgt beim nächsten stabil erkannten Teamauswahlbildschirm wieder der aktuellen Auswahl. Ohne veröffentlichte Auswahl wartet Auto.

Änderungen kommen sofort per WebSocket; spätestens alle 60 Sekunden wird zusätzlich der autorisierte Zustand geprüft. Bei einer Änderung während eines Versuchs stoppt der alte Klicklauf. Das neue Team wird erst nach erneuter Dialogerkennung versucht. Eine Änderung verlässt kein laufendes Spiel und holt das Spielfenster nicht ungefragt in den Vordergrund.

**ESC**, **Stopp**, das Ausschalten von Auto, Gruppenwechsel und manuelle Teamtasten beenden das aktive Folgen. Spätere Online-Updates starten es nicht neu. Nach App-Neustart ist Auto aus. Bei Fokusverlust, Aufnahmefehler oder veränderter Geometrie muss Auto erneut aktiviert werden.

Bei Verbindungsabbruch pausiert der Gruppenlauf. Bei wiederhergestellter Verbindung wird ein neuer Zustand geladen; Auto kann dann wieder auf den nächsten Dialog warten. Ausbleibende Zustandsantworten beenden die Freigabe, statt unbegrenzt eine alte Auswahl zu verwenden. Diese zeitliche Freigabe wird direkt vor Eingaben im Klickworker geprüft, auch wenn die Oberfläche beschäftigt ist.

## Administrative Übertragung einer einzelnen Gruppe

Unter **Gruppenverwaltung → Gruppe übertragen** kann der Ersteller den **Adminzugang kopieren**. Das administrative Token wird nicht in der Oberfläche angezeigt; nur der private Übertragungscode wird gezielt in die Zwischenablage kopiert. Er enthält die Rechte und den Einladungslink genau der ausgewählten Gruppe. Der Importdialog verdeckt den Code.

- **Adminzugang importieren** übernimmt die Gruppe in eine weitere Instanz. Beide Instanzen teilen dieselbe administrative Identität und können die Gruppe verwalten.
- **Adminzugang exklusiv übernehmen** überträgt die Administration an die empfangende Instanz oder Person. Nach Online-Prüfung und Bestätigung wird das Token gewechselt: Bisherige Admininstanzen und alte Übertragungscodes verlieren ihren Zugang. Bestehende Mitglieder und die Gruppe bleiben erhalten. Die ursprüngliche Erstellerkennung und der Anzeigename bleiben bestehen.

Der Empfänger muss den Gruppendienst erreichen können. Nach einem Import ist Auto aus. Bei unterbrochenem Tokenwechsel sind die neuen Zugangsdaten vorab lokal gesichert; **Aktualisieren** setzt den Wechsel fort. Den Code nur privat weitergeben und die Zwischenablage anschließend leeren. Eine Einladung für normale Mitglieder verleiht keine Adminrechte.

## Mitglieder und Wiederherstellung

Der Ersteller kann bestätigte Mitglieder **entfernen**, den Einladungslink ersetzen und die Gruppe **löschen**. Entfernen beendet den Online-Zugriff und die bestehende Verbindung. Ein völlig offline befindlicher Client erkennt den Entzug spätestens beim Auslaufen seiner Online-Freigabe. Mitglieder können die Gruppe selbst verlassen. Der Ersteller muss die Gruppe löschen, wenn er sie beenden möchte.

Mitgliedschaften und Erstellerrechte werden mit Windows-DPAPI geschützt in `%LOCALAPPDATA%/WardogsTeamselector/groups.dat` gespeichert. Für einen PC-Wechsel unter **PC-Wechsel und Wiederherstellung** einen Wiederherstellungscode exportieren und privat sichern. Der Code enthält deine Rechte; nicht öffentlich teilen. Nach **Eigenen Zugangscode ersetzen** einen neuen Code sichern – alte Codes für diese Mitgliedschaft gelten dann nicht mehr.

**Aus Liste entfernen** löscht nur die lokalen Zugangsdaten. Es verlässt oder löscht die Online-Gruppe nicht. Ohne gesicherten Wiederherstellungscode können dabei Erstellerrechte verloren gehen.

Bei einer beschädigten Gruppendatei überschreibt das Tool sie nicht automatisch. Datei sichern und einen gültigen Wiederherstellungscode importieren. Eine abgebrochene Gruppenanlage oder Anfrage lässt sich über **Aktualisieren** fortsetzen, weil die Zugangsdaten schon vor der Netzwerkanfrage gesichert werden.
