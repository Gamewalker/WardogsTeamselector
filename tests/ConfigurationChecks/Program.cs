using System.Drawing;
using WardogsTeamselector;
using WardogsTeamselector.Core;

var defaults = new AppSettings();
var missingIntervals = SettingsStore.Deserialize("{}");
if (!defaults.FocusGameOnTeamActivation || !missingIntervals.FocusGameOnTeamActivation) throw new Exception("Automatischer Spielfokus muss auch in alten Profilen standardmäßig aktiv sein.");
if (defaults.MinIntervalMs != 50 || defaults.MaxIntervalMs != 70 || missingIntervals.MinIntervalMs != 50 || missingIntervals.MaxIntervalMs != 70) throw new Exception("Standard-Klickintervall ist nicht 50–70 ms.");

var profile = new AppSettings { ManualBounds = new Rectangle(-1920, 40, 1280, 720), MonitorId = "DISPLAY2", DetectionOffsetX = .1, GeometryCalibrated = true, DryRun = false, MinIntervalMs = 50, MaxIntervalMs = 50, LivePreviewEnabled = false, FocusGameOnTeamActivation = false };
var json = SettingsStore.Serialize(profile);
var roundtrip = SettingsStore.Deserialize(json);
if (roundtrip.FocusGameOnTeamActivation) throw new Exception("Deaktivierter Spielfokus wurde nicht beibehalten.");
if (roundtrip.ManualBounds != profile.ManualBounds || roundtrip.MonitorId != profile.MonitorId || roundtrip.DryRun || roundtrip.Hotkeys[Team.Blue] != 0x75 || roundtrip.Regions.Count != 3 || roundtrip.DetectionOffsetX != .1 || roundtrip.LivePreviewEnabled || roundtrip.MinIntervalMs != 50 || roundtrip.MaxIntervalMs != 50) throw new Exception("Profil-Roundtrip fehlgeschlagen.");
foreach (var invalid in new[] { json.Replace("\"MinIntervalMs\": 50", "\"MinIntervalMs\": 49"), json.Replace("\"MaxIntervalMs\": 50", "\"MaxIntervalMs\": 20"), "null", "{\"Regions\":null}", "{\"Hotkeys\":null}" })
{
    bool rejected = false; try { SettingsStore.Deserialize(invalid); } catch { rejected = true; } if (!rejected) throw new Exception("Ungültiges Profil akzeptiert.");
}

// Obsolete settings in existing profiles must not disable the mandatory HUD stop.
var legacy = SettingsStore.Deserialize(json.TrimEnd().TrimEnd('}') + ",\"DialogAbsenceTimeoutMs\":0,\"DetectJoinedHud\":false}");
var migrated = SettingsStore.Serialize(legacy);
if (migrated.Contains("DialogAbsenceTimeoutMs") || migrated.Contains("DetectJoinedHud")) throw new Exception("Veraltete Beitrittseinstellungen wurden beibehalten.");

Console.WriteLine("PASS: Profil-Roundtrip, ungültige Profile und Migration veralteter Beitrittseinstellungen. Keine Benutzereinstellungen verändert.");

var checkboxPath = Path.Combine(Path.GetTempPath(), "wardogs-checkboxes-" + Guid.NewGuid().ToString("N") + ".json");
try
{
    if (CheckboxPreferencesStore.Load(checkboxPath).Count != 0) throw new Exception("Fehlende Checkbox-Datei überschreibt Profilwerte.");
    var values = new Dictionary<string, bool> { ["ShareTeamWithGroup"] = true, ["AutoFollow"] = true, ["DryRun"] = false,
        ["FocusGame"] = false, ["GeometryCalibrated"] = true, ["LivePreview"] = false, ["DrawRegion"] = true, ["AutomaticUpdates"] = false };
    CheckboxPreferencesStore.Save(values, checkboxPath);
    var restarted = CheckboxPreferencesStore.Load(checkboxPath);
    if (restarted.Count != values.Count || values.Any(pair => restarted[pair.Key] != pair.Value)) throw new Exception("Checkbox-Zustände wurden nicht vollständig wiederhergestellt.");
    if (!restarted["ShareTeamWithGroup"] || !restarted["AutoFollow"] || restarted["DryRun"] || restarted["FocusGame"]) throw new Exception("Checkboxen gehen nach Neustart verloren.");
    restarted["ShareTeamWithGroup"] = false; restarted["AutoFollow"] = false;
    CheckboxPreferencesStore.Save(restarted, checkboxPath);
    var secondRestart = CheckboxPreferencesStore.Load(checkboxPath);
    if (secondRestart["ShareTeamWithGroup"] || secondRestart["AutoFollow"]) throw new Exception("Ausgeschaltete Checkboxen werden wieder aktiviert.");
    Console.WriteLine("PASS: Checkbox-Zustände nach Neustart einschließlich ausdrücklich ausgeschalteter Gruppenoptionen.");
}
finally { File.Delete(checkboxPath); File.Delete(checkboxPath + ".tmp"); }
