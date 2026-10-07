using System.Drawing;
using WardogsTeamselector;
using WardogsTeamselector.Core;

var profile = new AppSettings { ManualBounds = new Rectangle(-1920, 40, 1280, 720), MonitorId = "DISPLAY2", DetectionOffsetX = .1, GeometryCalibrated = true, DryRun = false, MinIntervalMs = 50, MaxIntervalMs = 50, LivePreviewEnabled = false };
var json = SettingsStore.Serialize(profile);
var roundtrip = SettingsStore.Deserialize(json);
if (roundtrip.ManualBounds != profile.ManualBounds || roundtrip.MonitorId != profile.MonitorId || roundtrip.DryRun || roundtrip.Hotkeys[Team.Blue] != 0x75 || roundtrip.Regions.Count != 3 || roundtrip.DetectionOffsetX != .1 || roundtrip.LivePreviewEnabled) throw new Exception("Profil-Roundtrip fehlgeschlagen.");
foreach (var invalid in new[] { json.Replace("\"MinIntervalMs\": 50", "\"MinIntervalMs\": 49"), json.Replace("\"MaxIntervalMs\": 50", "\"MaxIntervalMs\": 20"), "null", "{\"Regions\":null}", "{\"Hotkeys\":null}" })
{
    bool rejected = false; try { SettingsStore.Deserialize(invalid); } catch { rejected = true; } if (!rejected) throw new Exception("Ungültiges Profil akzeptiert.");
}

// Obsolete settings in existing profiles must not disable the mandatory HUD stop.
var legacy = SettingsStore.Deserialize(json.TrimEnd().TrimEnd('}') + ",\"DialogAbsenceTimeoutMs\":0,\"DetectJoinedHud\":false}");
var migrated = SettingsStore.Serialize(legacy);
if (migrated.Contains("DialogAbsenceTimeoutMs") || migrated.Contains("DetectJoinedHud")) throw new Exception("Veraltete Beitrittseinstellungen wurden beibehalten.");

Console.WriteLine("PASS: Profil-Roundtrip, ungültige Profile und Migration veralteter Beitrittseinstellungen. Keine Benutzereinstellungen verändert.");
