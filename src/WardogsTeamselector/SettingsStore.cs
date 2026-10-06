using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WardogsTeamselector.Core;

namespace WardogsTeamselector;
public static class SettingsStore
{
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WardogsTeamselector");
    private static string LegacyFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WardogsClicker", "settings.json");
    public static string FilePath => Path.Combine(Folder, "settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static AppSettings Deserialize(string json) { var s = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? throw new InvalidDataException("Leere Einstellungen."); s.Validate(); return s; }
    public static string Serialize(AppSettings settings) { settings.Validate(); return JsonSerializer.Serialize(settings, Options); }
    public static AppSettings Load()
    {
        if (File.Exists(FilePath)) return Deserialize(File.ReadAllText(FilePath));
        if (!File.Exists(LegacyFilePath)) return new();
        var legacy = Deserialize(File.ReadAllText(LegacyFilePath));
        Save(legacy); // Copy a valid old profile; keep it for older running versions.
        return legacy;
    }
    public static void Save(AppSettings settings) { var json = Serialize(settings); Directory.CreateDirectory(Folder); var temp = FilePath + ".tmp"; File.WriteAllText(temp, json); File.Move(temp, FilePath, true); }
}
