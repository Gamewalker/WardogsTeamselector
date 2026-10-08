# Gruppen mit freigegebenen Mitgliedschaften und gemeinsamer Teamwahl

Im Betrieb ergänzen die Tabs **Manuell** und **Gruppenmodus** die bisherige Teamsteuerung. Ersteller können Gruppen anlegen, Einladungslinks teilen, Anfragen bestätigen oder ablehnen, Mitglieder entfernen und eine Teamauswahl veröffentlichen. Bestätigte Mitglieder können einmal beitreten oder bei kommenden Teamauswahlbildschirmen automatisch folgen.

Der neue Cloudflare-Worker verwendet ausschließlich SQLite-Durable-Objects und WebSocket-Hibernation. Änderungen kommen sofort; spätestens alle 60 Sekunden prüft der Client den aktuellen autorisierten Zustand. Netzwerkverlust pausiert Gruppenläufe. ESC, Stopp und manuelle Teamwahl beenden Auto und verhindern Starts durch verspätete Antworten.

Gruppenzugangsdaten werden unter Windows geschützt gespeichert. Ein privater Wiederherstellungscode ermöglicht den PC-Wechsel; ein ersetzter Zugangscode entzieht alten Wiederherstellungscodes die betroffene Mitgliedschaft. Gruppennamen und Spielernamen bleiben in der gewählten Sprache unverändert. Neue Gruppenmeldungen sind auf Deutsch und Englisch vorhanden; die weiteren bestehenden Sprachen verwenden für diese neuen Meldungen zunächst Englisch.

Die Cloudflare-Live-Bereitstellung ist separat erforderlich und wurde in der Implementierungssitzung bis zum nächsten verfügbaren Zugang verschoben. Das Deploymentskript akzeptiert ausschließlich einen eindeutig bestätigten Workers-Free-Tarif und ändert keine Abonnements. GitHub Actions stellt den Worker nicht automatisch bereit. Dieser Featurebranch wurde weder gemergt noch als neue App-Version veröffentlicht.
