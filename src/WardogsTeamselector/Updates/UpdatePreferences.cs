using System;
using System.IO;
using System.Text.Json;

namespace WardogsTeamselector.Updates;

public sealed class UpdatePreferences
{
    public bool Enabled { get; set; } = true;
    public static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WardogsTeamselector");
    private static string FilePath => Path.Combine(DirectoryPath, "updates.json");
    public static UpdatePreferences Load() => File.Exists(FilePath) ? JsonSerializer.Deserialize<UpdatePreferences>(File.ReadAllText(FilePath)) ?? new() : new();
    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, FilePath, true);
    }
}
