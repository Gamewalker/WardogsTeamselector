# Steuerung und Hotkeys

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

`AutomationController(IScreenService, IDialogDetector, IClickSink, IJoinedScreenDetector)` bietet
`Start(Team, AppSettings)`, `Stop(string reason = "ESC")`, `Snapshot`,
`event Action<AutomationSnapshot>? Updated` und `Dispose()`.

Start validiert alle Einstellungen und kopiert sie einschließlich Listen und
Hotkeydictionary. Ein neuer Start ersetzt den vorherigen Lauf. Es gibt genau
einen Hintergrundworker, der ungefähr alle 20 ms aufnimmt und prüft. Drei
aufeinanderfolgende Treffer bei gleichem Spielbereich und Spielfokus geben die
Klicks frei. Echte Klicks benötigen eine kalibrierte Geometrie. Im Testmodus
zählt `ClickCount` simulierte Klicks und `Reason` kennzeichnet sie ausdrücklich.

Zwischen Eingaben liegt mindestens die pro Klick neu gezogene ganzzahlige
Wartezeit A–B einschließlich Grenzen, stets mindestens 50 ms. Die Zeitmessung
beginnt nach Rückkehr der Eingabesenke; auch schnelle Teamwechsel umgehen die
50-ms-Grenze nicht. Es gibt kein Nachholen ausgelassener Klicks. Aufnahmefehler,
Fokusverlust oder geänderte Geometrie beenden einen begonnenen Lauf. Nach dem ersten Klick wird unabhängig vom Dialogbefund weitergeklickt, bis der
verpflichtende HUD-Detektor die fünf weißen Balken mindestens drei Mal und
500 ms ununterbrochen erkennt. Auch ein gleichzeitig erkannter Auswahldialog
verhindert den HUD-Stopp nicht. Fokus, Geometrie und Aktualität der Aufnahme
werden vor der Bestätigung erneut geprüft. Es gibt keine Abwesenheitsfrist
und keinen Schalter zum Abschalten der HUD-Prüfung. Vor dem ersten Klick
bleibt die stabile Dialogerkennung erforderlich; ein bereits sichtbarer
Folgescreen kann den wartenden Lauf über die HUD-Prüfung beenden.
`JoinedDetection` liefert die HUD-Diagnose.

`Stop` synchronisiert sich mit der Eingabesenke. Nach seiner Rückkehr kann der
beendete Lauf keinen weiteren Klick senden. Ein neu gestarteter Lauf ist eine
separate explizite Aktivierung. `Updated` wird unter der Steuerungssperre auf
dem Aufrufer- oder Workerthread ausgelöst. WPF-Abonnenten müssen mit
`Dispatcher.BeginInvoke` asynchron aktualisieren. Keine synchrone
`Dispatcher.Invoke` und kein `Dispose` aus dem Eventhandler verwenden.

`HotkeyService(System.Windows.Window)` bietet `Apply(AppSettings)`,
`event Action<Team>? TeamPressed`, `event Action? EscapePressed` und `Dispose()`.
Alle Aufrufe erfolgen auf dem UI-Thread des Fensters. F1–F24 werden mit
`RegisterHotKey` und `MOD_NOREPEAT` gebunden. Konflikte deaktivieren alle
Teamhotkeys und werfen eine verständliche `Win32Exception`; ESC bleibt aktiv.
Ein Low-Level-Keyboardhook beobachtet ESC einmal pro Tastendruck und reicht
jedes Ereignis unverändert an Windows bzw. das Spiel weiter. Beim Schließen
müssen beide Dienste entsorgt werden.

Die eingabelosen Integrationstests laufen mit:

```powershell
.tools/dotnet/dotnet.exe run --project tests/AutomationChecks/AutomationChecks.csproj
```

Sie prüfen ungültige Intervalle, Wartezustand, Stabilisierung, Fokus/Kalibrierung,
Dialogverlust, Geometriewechsel, kopierte Einstellungen, parallele Teamstarts,
die tatsächliche Zeituntergrenze, Testmodus, Aufnahmefehler und einen bewusst
blockierten Eingabesink gegen synchronen Stopp.
