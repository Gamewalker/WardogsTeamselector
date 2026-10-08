# Dialogerkennung

[← Dokumentation](README.md) · [Zur Projektstartseite](../README.md)

`DialogDetector : IDialogDetector` prüft neun kleine, gleichmäßige Pixelbereiche und vier äußere Rahmenkanten. Alle Positionen basieren auf der gelieferten Referenz (3838 × 2158). Überschrift, Teamembleme und Spielerzahlen liegen außerhalb der Messflächen; gesperrte Teams beeinflussen die Freigabe nicht.

`DialogDetector.GetProbeAreas(bitmapSize, settings)` liefert `DetectionProbeArea(Name, Bounds)` für das Vorschau-Overlay. `Bounds` sind lokale Pixelkoordinaten im aufgenommenen Bitmap, ohne Desktop-/Monitorursprung. Bei der Darstellung mit `Stretch=Uniform` müssen die Bitmapkoordinaten um den Vorschau-Skalierungsfaktor und den Letterbox-Versatz transformiert werden.

Kalibrierung: `x' = ((x / 3838 - 0.5) × DetectionScale + 0.5 + DetectionOffsetX) × Bitmapbreite`, analog für Y. Ein positiver Offset verschiebt Messflächen nach rechts/unten. Klickregionen werden separat über `AppSettings.Regions` kalibriert.

Pro Pixelbereich werden RGB-Mediane, Abweichung vom Referenzgrau und mediane Streuung gemessen. Kleine helle Cursor-Ausreißer dominieren damit nicht die Flächenfarbe. Karteninnenflächen dürfen neutral dunkel zwischen RGB 0 und 64 bleiben, damit Hover-Aufhellung akzeptiert wird. Von neun Flächenproben wird genau die schwächste aus der Bewertung genommen und in der Diagnose entsprechend markiert. Alle acht übrigen Flächen müssen mindestens 0,55 erreichen. Für Rahmen wird an 20 Positionen ein lokales Maximum quer zur Kante gegen die innere Dialogfläche verglichen; jede äußere Kante muss weiterhin mindestens 0,8 erreichen. Der Gesamtscore ist das Mittel der acht gewerteten Flächen und vier Kanten, standardmäßig mindestens 0,90. Die GUI zeigt die einstellbare Schwelle als Prozentwert; dieser ist ein Erkennungsscore, kein Anteil sämtlicher Bildpixel. Ein verdeckter ganzer Rahmen oder zwei vollständig verdeckte Flächen sperren weiterhin die Freigabe. Außerhalb des Bitmaps liegende Messflächen werden abgelehnt.

Prüfung: `.tools\dotnet\dotnet.exe run --project tests/DetectionChecks/DetectionChecks.csproj` vom Repository-Stamm. Die PNG-Tests prüfen Referenz, 1080p, 1440p, 4K, veränderte Überschrift/Embleme/Zahlen, leere Flächen, entfernten bzw. verdeckten Dialog und Scale/Offset-Kalibrierung. Sie bestätigen proportional skalierte Bildgeometrie; tatsächliche UI-Skalierung, HDR, Gamma und Aufnahmemodus im Spiel müssen in der Live-Vorschau überprüft werden.

Die Erkennung ist strukturell, nicht semantisch. Eine andere Anwendung mit exakt nachgebildetem Dialog kann dieselben Pixelwerte liefern. Daher muss die Steuerung zusätzlich Spielfenster, Vordergrund und gültige Zielgeometrie prüfen.

