# Mehrsprachige Oberfläche

Die Sprachauswahl steht direkt neben **Stopp · ESC** und wechselt die Oberfläche sofort. Beim ersten Start wird Englisch verwendet, unabhängig von der Windows-Sprache. Eine gewählte Sprache wird separat in `%LOCALAPPDATA%/WardogsTeamselector/language.json` gespeichert und beim nächsten Start geladen. Fehlende, beschädigte oder unbekannte Sprachpräferenzen führen zu Englisch.

Verfügbar sind Englisch, Deutsch, Spanisch, Italienisch, Portugiesisch, Polnisch, Niederländisch, Französisch, Türkisch, Russisch, Ukrainisch, Arabisch, Hindi, Bengalisch, Indonesisch, Vietnamesisch, Thai, vereinfachtes Chinesisch, Japanisch und Koreanisch. Die Auswahl zeigt die Namen in der jeweiligen Sprache. Arabische Texte verwenden Rechts-nach-links-Leserichtung.

Die lokalen Sprachdateien sind in beiden EXE-Varianten eingebettet; die App benötigt zum Übersetzen keine Internetverbindung. Oberfläche, Status, Validierung, Diagnose, Updatebereich und Herkunftsfenster verwenden dieselben Kataloge. Die deutschen Quellmeldungen der Steuerung bleiben unabhängig von der angezeigten Sprache. Sprachwechsel erhalten laufende Versuche und ungespeicherte Eingaben.

Die Sprachdateien wurden mit maschinellen Übersetzungen erstellt und die englischen Bedienbegriffe anschließend im Anwendungskontext überarbeitet. Weitere sprachliche Verbesserungen können direkt in `src/WardogsTeamselector/Localization/*.json` vorgenommen werden. Platzhalter wie `{0}` müssen erhalten bleiben; `LocalizationChecks` prüft Vollständigkeit und Platzhalter aller 20 Kataloge. Windows-eigene Dateidialoge und die Beschriftungen nativer MessageBox-Schaltflächen folgen weiterhin der Windows-Anzeigesprache.
