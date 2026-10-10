# Gruppen und gemeinsame Teamauswahl

[← Dokumentation](README.md)

Die Hauptnavigation zeigt **Einrichtung** und **Betrieb** links, **Konfiguration**, **Diagnose** und **Gruppenverwaltung** rechts. Bei zu wenig Platz erscheinen die beiden Bereiche in getrennten Zeilen. Unter **Betrieb** gibt es die Tabs **Manuell** und **Gruppenmodus**. Bildschirmkalibrierung, Klickintervalle, Testmodus und Spielfokus bleiben auf jedem PC separat eingestellt.

Die neuen Gruppenmeldungen stehen auf Deutsch und Englisch zur Verfügung. In den weiteren vorhandenen Oberflächensprachen verwenden die neuen Gruppenmeldungen zunächst Englisch; die bisherigen Übersetzungen bleiben erhalten. Selbst eingegebene Gruppen- und Spielernamen werden nicht übersetzt.

## Gruppe erstellen und einladen

1. Unter **Gruppenverwaltung → Gruppendienst einrichten** ist für neue oder bisher leere Einstellungen `https://wardogs-groups.niels-82f.workers.dev/` voreingestellt. Für einen eigenen Dienst die HTTPS-Adresse ändern und speichern; vorhandene eigene Adressen bleiben erhalten. Die [Deployment-Anleitung im Repository-Wiki](wiki/Eigenen-Gruppendienst-deployen.md) erklärt die Bereitstellung.
2. Unter **Gruppenverwaltung** **Gruppe erstellen** wählen und Gruppenname sowie deinen Namen eingeben.
3. **Einladungslink kopieren** und an deine Mitspieler senden.
4. Mitspieler öffnen den Link im Browser und wählen **In App öffnen**. In der App geben sie ihren Namen ein und bestätigen die Anfrage. Alternativ bleibt das Einfügen unter **Gruppenverwaltung → Gruppe beitreten** möglich. Der Link allein bestätigt niemanden.
5. Als Ersteller die Gruppe aktualisieren. Unter **Gruppe verwalten → Offene Beitrittsanfragen** eine Anfrage auswählen und **Bestätigen** oder **Ablehnen** wählen.

Die Bestätigung wird beim nächsten Aktualisieren auf dem eingeladenen PC sichtbar. Gleichnamige Personen werden durch eine kurze Mitgliedskennung unterschieden. Namen bestätigen keine Identität.

## Ein Team teilen

Es können mehrere Gruppen gespeichert werden. Die Gruppenauswahl unter **Manuell**, **Gruppenmodus** und **Gruppenverwaltung** ist dieselbe aktive Gruppe. Ein Wechsel beendet den bisherigen Gruppenlauf und entwertet seine verspäteten Antworten. Die Auswahl und der Auto-Schalter bleiben nach einem Neustart erhalten.

Unter **Manuell** über den drei Teambuttons **Mit Gruppe teilen** einschalten und eine eigene Gruppe wählen. Anschließend den normalen blauen, roten oder grünen Teambutton oder die zugehörige F-Taste verwenden. Nach erfolgreicher Veröffentlichung beginnt der normale lokale Beitritt. Ohne Häkchen bleiben die Gruppenauswahl deaktiviert und die Teamaktionen rein lokal. Das Häkchen bleibt nach einem Neustart erhalten. Scheitert die Veröffentlichung, wird ein lokaler Beitritt separat angeboten. Der Server kann eine Veröffentlichung bereits erhalten haben, obwohl die Bestätigung auf deinem PC nicht ankommt; der Hinweis lautet deshalb „nicht bestätigt“.

Erneutes Teilen derselben Farbe ist eine neue Veröffentlichung. **Auswahl zurücknehmen** entfernt das Gruppenziel. Ein lokaler Stopp nimmt eine bereits geteilte Auswahl nicht zurück.

## Einer Gruppe folgen

Im **Gruppenmodus** eine bestätigte Gruppe auswählen. **Einmal beitreten** versucht die aktuelle Auswahl einmal. **Auto folgen** bleibt nach dem HUD-Erfolg aktiv und prüft weiter auf den nächsten Teamauswahlbildschirm. Beim Wiedererkennen des Dialogs wird das Spiel erneut in den Vordergrund geholt, wenn „Spiel nach Teamaktivierung in den Vordergrund holen“ aktiviert ist. Pro Dialoganzeige erfolgt ein Fokusversuch; Klicks bleiben bis zu einer neuen Aufnahme mit bestätigtem Spielfokus gesperrt. Sobald der Dialog wieder stabil erkannt wird, fragt das Tool vor dem Klickstart die aktuelle Auswahl des Gruppenleiters erneut beim Gruppendienst ab. Erst nach erfolgreichem Abgleich und erneuter Dialog-/Fokusprüfung beginnt der Beitritt zum bestätigten Team. Ohne veröffentlichte Auswahl oder bei fehlgeschlagenem Abgleich wartet Auto weiter; fehlgeschlagene Abfragen werden frühestens nach 15 Sekunden erneut versucht.

Änderungen kommen sofort per WebSocket; alle 15 Sekunden wird zusätzlich der autorisierte Zustand geprüft. Pro Gruppenmitglied sind zehn Aufrufe pro Minute möglich; Statusabfragen, Schreibaktionen, WebSocket-Verbindungsaufbau und Sync-Nachrichten teilen sich dieses Limit. Bei einer Änderung während eines Versuchs stoppt der alte Klicklauf. Das neue Team wird erst nach erneuter Dialogerkennung versucht. Eine Änderung verlässt kein laufendes Spiel. Automatisches Fokussieren beim nächsten Dialog richtet sich nach der Vordergrund-Einstellung.

**ESC**, **Stopp**, das Ausschalten von Auto, Gruppenwechsel und manuelle Teamtasten beenden das aktive Folgen. Spätere Online-Updates starten es nicht neu. Nach App-Neustart wird eingeschaltetes Auto für die gespeicherte, bestätigte Gruppe wieder gestartet, sofern die Einstellungen gültig sind. Vor Klicks wird der aktuelle Gruppenstatus erneut abgeglichen. Auto bleibt bei Fokusverlust, Aufnahmefehlern und veränderter Geometrie eingeschaltet. Nur der betroffene Klickversuch endet; das Tool kehrt zur Dialogbeobachtung zurück. Bei wiederholten Aufnahmefehlern wird die Beobachtung höchstens einmal pro Sekunde neu gestartet. Erst nach erneut erkanntem Dialog, bestätigtem Spielfokus und frischem Teamabgleich beginnt der nächste Versuch.

Auch ein Verbindungsfehler direkt beim Einschalten beendet Auto nicht. Bei Verbindungsabbruch pausieren die Klicks; Auto und die Dialogbeobachtung bleiben aktiv. Bei wiederhergestellter Verbindung wird ein neuer Zustand geladen; Auto kann dann wieder auf den nächsten Dialog warten. Ausbleibende Zustandsantworten beenden die Freigabe, statt unbegrenzt eine alte Auswahl zu verwenden. Diese zeitliche Freigabe wird direkt vor Eingaben im Klickworker geprüft, auch wenn die Oberfläche beschäftigt ist.

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

## Einladungen direkt in der App öffnen

Auf der Einladungsseite öffnet **In App öffnen** die lokale WardogsTeamselector-App. Die Einladung wird automatisch übernommen; anschließend nur den eigenen Namen eingeben und bestätigen. Eine bereits laufende App erhält die Einladung im bestehenden Fenster. Bereits gespeicherte Gruppen werden ausgewählt und aktualisiert.

Die neue App-Version muss einmal gestartet worden sein. Dabei registriert die portable EXE `wardogs://` für den aktuellen Windows-Benutzer, ohne Administratorrechte. Nach Verschieben der EXE die App am neuen Ort einmal starten. Der Browser kann vor dem Öffnen nach einer Bestätigung fragen. Ohne registrierte App bleibt „Link kopieren“ als Alternative verfügbar.

Technisch übergibt die Webseite `wardogs://join/#<URL-kodierter vollständiger Einladungslink>`. Das Einladungsgeheimnis bleibt im Fragment; die App validiert Dienstadresse, Gruppenkennung und Token vor der Übernahme. Erst die Bestätigung im Namensdialog sendet eine neue Beitrittsanfrage. Die Freigabe durch den Ersteller bleibt erforderlich.

Für die Nutzung müssen sowohl die neue App als auch die aktualisierte Worker-Einladungsseite veröffentlicht werden.
