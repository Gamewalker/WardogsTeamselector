using System.Text.Json;
using System.Text.RegularExpressions;
using WardogsTeamselector;

int count = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
Check(Localization.CurrentLanguage == "en", "First launch must use English regardless of Windows language.");
var assembly = typeof(Localization).Assembly;
Dictionary<string, string> Read(string code)
{
    using var stream = assembly.GetManifestResourceStream("WardogsTeamselector.Localization." + code + ".json")!;
    return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
}
var source = Read("de");
string Tokens(string value) => string.Join(",", Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).Order());
foreach (var language in Localization.Languages)
{
    var translated = Read(language.Code);
    Check(source.Keys.Order().SequenceEqual(translated.Keys.Order()), "Incomplete catalog: " + language.Code);
    foreach (var (key, value) in translated)
    {
        Check(!string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(key), "Empty translation: " + language.Code + ": " + key);
        Check(Tokens(key) == Tokens(value), "Changed placeholders: " + language.Code + ": " + key);
    }
    Localization.SetLanguage(language.Code);
    Check(Localization.Text("Einstellungen speichern") == translated["Einstellungen speichern"], "Language switch failed.");
    Check(Localization.Text("Dialog stabilisieren (2/3)").Contains("2/3"), "Counter lost.");
    Check(Localization.Text("Dialog: erkannt · Score 90 %").Contains("90 %"), "Score lost.");
    Check(Localization.Text("Build 123 ist aktuell.").Contains("123"), "Build lost.");
    Check(Localization.Text("1234×567 px · eingebettetes Testbild · keine Eingaben").Contains("1234"), "Geometry lost.");
}
Localization.SetLanguage("en");
Check(Localization.Text("Einstellungen speichern") == "Save settings", "Wrong English label.");
Check(Localization.Text("Wartet · Blau") == "Waiting · Blue", "Team name or phase not translated.");
Check(Localization.Text("Gruppenmodus") == "Group mode", "Group mode has an English label.");
Check(Localization.Text("Gruppe: Rot · Bestätigt") == "Group: Rot · Approved", "Group name is user data and must stay literal.");
Check(Localization.Text("Auswahl: Rot") == "Selection: Red", "Shared team uses translated team name.");
Check(Localization.Text("Blau und Rot verwenden F6. Für Rot eine andere F-Taste wählen.").Contains("Blue and Red"), "Dynamic validation did not translate team names.");
Check(Localization.Text("Stopp · ENDE") == "Stop · ENDE", "Custom stop label was not translated.");
Check(Localization.Text("Stopp und Blau verwenden EINFG. Für Stopp eine andere Taste wählen.").Contains("Stop and Blue"), "Stop conflict did not translate the team name.");
Check(Localization.Text("23:01:02.003  Warte auf Spielfenster").StartsWith("23:01:02.003  "), "Log timestamp modified.");
Check(Localization.Text("C:\\Users\\Example\\my-profile.json") == "C:\\Users\\Example\\my-profile.json", "Unknown data changed.");
Check(Localization.Text("Rotterdam") == "Rotterdam", "Word prefix in unknown data changed.");
Check(Localization.Text("Klickintervall: 50–70 ms  ·  Fenster: Spiel\nBeitritt: fünf HUD-Balken für mindestens 0,5 s  ·  Stopp: ESC").Contains("Window: Spiel"), "User-entered game filter was translated.");
Localization.SetLanguage("invalid");
Check(Localization.CurrentLanguage == "en", "Unsupported language must fall back to English.");
Localization.SetLanguage("ar"); Check(Localization.IsRightToLeft, "Arabic must use RTL text.");
Localization.SetLanguage("de"); Check(!Localization.IsRightToLeft, "RTL must reset on switch.");
Check(Localization.Text("Wartet · Blau") == "Wartet · Blau", "Original source lost on switching back.");
string temporary = Path.Combine(Path.GetTempPath(), "wardogs-language-" + Guid.NewGuid().ToString("N"), "language.json");
try
{
    Check(Localization.LoadPreference(temporary) == "en", "Missing preference must default to English.");
    Localization.SavePreference("pl", temporary);
    Check(Localization.LoadPreference(temporary) == "pl", "Language preference did not survive a restart.");
    Localization.SavePreference("ja", temporary);
    Check(Localization.LoadPreference(temporary) == "ja", "Preference replacement failed.");
    File.WriteAllText(temporary, "broken");
    Check(Localization.LoadPreference(temporary) == "en", "Corrupt preference must recover to English.");
}
finally { if (Directory.Exists(Path.GetDirectoryName(temporary))) Directory.Delete(Path.GetDirectoryName(temporary)!, true); }
Console.WriteLine($"PASS: {count} localization checks across {Localization.Languages.Length} languages. No user preferences written.");
