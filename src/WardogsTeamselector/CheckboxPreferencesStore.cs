using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WardogsTeamselector;

public static class CheckboxPreferencesStore
{
    private static string FilePath => Path.Combine(SettingsStore.Folder, "checkboxes.json");
    public static Dictionary<string, bool> Load(string? path = null)
    {
        path ??= FilePath;
        return File.Exists(path) ? JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Leere Checkbox-Einstellungen.") : new();
    }
    public static void Save(Dictionary<string, bool> values, string? path = null)
    {
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(values));
        File.Move(temporary, path, true);
    }
}
