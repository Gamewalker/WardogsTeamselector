### Automatische Updates aus öffentlichen GitHub-Releases

Die Anwendung prüft beim Start und alle sechs Stunden auf neue stabile Versionen. Die passende EXE mit oder ohne Runtime wird automatisch heruntergeladen und anhand der SHA-256-Prüfsumme geprüft. Beim regulären Beenden wird die EXE ausgetauscht; beim nächsten Start ist die neue Version aktiv. Die bisherige EXE bleibt als `.previous` erhalten. Einstellungen und laufende Teamklicks bleiben während der Prüfung erhalten.

Unter Konfiguration lassen sich automatische Updates deaktivieren oder manuell prüfen. Eine GitHub-Anmeldung ist nicht erforderlich. Ältere EXEs ohne Updatefunktion benötigen einmalig den manuellen Download dieser Version.
