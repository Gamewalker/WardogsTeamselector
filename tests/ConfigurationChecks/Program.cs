using System.Drawing;
using WardogsTeamselector;
using WardogsTeamselector.Core;

var profile = new AppSettings { ManualBounds = new Rectangle(-1920, 40, 1280, 720), MonitorId = "DISPLAY2", DetectionOffsetX = .1, GeometryCalibrated = true, DryRun = false, MinIntervalMs = 50, MaxIntervalMs = 50, DialogAbsenceTimeoutMs = 12000, DetectJoinedHud = false, LivePreviewEnabled = false };
var json = SettingsStore.Serialize(profile);
var roundtrip = SettingsStore.Deserialize(json);
if (roundtrip.ManualBounds != profile.ManualBounds || roundtrip.MonitorId != profile.MonitorId || roundtrip.DryRun || roundtrip.Hotkeys[Team.Blue] != 0x75 || roundtrip.Regions.Count != 3 || roundtrip.DetectionOffsetX != .1 || roundtrip.DialogAbsenceTimeoutMs != 12000 || roundtrip.DetectJoinedHud || roundtrip.LivePreviewEnabled) throw new Exception("Profil-Roundtrip fehlgeschlagen.");
foreach (var invalid in new[] { json.Replace("\"MinIntervalMs\": 50", "\"MinIntervalMs\": 49"), json.Replace("\"MaxIntervalMs\": 50", "\"MaxIntervalMs\": 20"), "{\"DialogAbsenceTimeoutMs\":0}", "null", "{\"Regions\":null}", "{\"Hotkeys\":null}" })
{
    bool rejected = false; try { SettingsStore.Deserialize(invalid); } catch { rejected = true; } if (!rejected) throw new Exception("Ungültiges Profil akzeptiert.");
}
Console.WriteLine("PASS: Profil-Roundtrip mit negativen Monitorursprüngen und Teamdaten; manipulierte Intervalle und leere Profile abgewiesen. Keine Benutzereinstellungen verändert.");
