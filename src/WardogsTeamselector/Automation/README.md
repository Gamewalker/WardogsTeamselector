# Steuerung und Hotkeys

`AutomationController(IScreenService, IDialogDetector, IClickSink, IJoinedScreenDetector? = null)` bietet
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
Fokusverlust oder geänderte Geometrie beenden einen begonnenen Lauf. Ein fehlender
Dialog pausiert dagegen Eingaben im Zustand `ConfirmingJoin`. Kehrt der Dialog
innerhalb von `DialogAbsenceTimeoutMs` (Standard 10000) zurück, wird dieselbe
Session mit unverändertem Team und Zähler nach drei stabilen Bildern fortgesetzt.
Kontinuierliche Abwesenheit bis zum Fristablauf beendet die Session dauerhaft.
Ein optionaler HUD-Detektor bestätigt den Folgescreen nach mindestens drei
Treffern und 500 ms ununterbrochener Erkennung, ausschließlich ohne erkannten
Auswahldialog. Fokus, Geometrie und Aktualität der Aufnahme werden vor dieser
Bestätigung erneut geprüft. `DetectJoinedHud` kann diesen Frühstopp abschalten.
Beim anfänglichen Warten startet die Abwesenheitsfrist noch nicht; ein bereits
sichtbarer Folgescreen kann den Lauf dennoch über die HUD-Prüfung beenden.
`AbsenceRemainingMs` und `JoinedDetection` liefern GUI-Countdown und Diagnose.

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
