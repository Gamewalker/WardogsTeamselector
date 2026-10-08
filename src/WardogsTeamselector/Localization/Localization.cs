using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WardogsTeamselector;

// Source messages stay in the controller's original language. Translation happens only
// at the presentation boundary, so switching language cannot alter a running session.
internal static class Localization
{
    internal sealed record Language(string Code, string Name);
    internal static readonly Language[] Languages = {
        new("en", "English"), new("de", "Deutsch"), new("es", "Español"),
        new("it", "Italiano"), new("pt", "Português"), new("pl", "Polski"),
        new("nl", "Nederlands"), new("fr", "Français"), new("tr", "Türkçe"),
        new("ru", "Русский"), new("uk", "Українська"), new("ar", "العربية"),
        new("hi", "हिन्दी"), new("bn", "বাংলা"), new("id", "Bahasa Indonesia"),
        new("vi", "Tiếng Việt"), new("th", "ไทย"), new("zh-CN", "简体中文"),
        new("ja", "日本語"), new("ko", "한국어")
    };
    private static readonly Dictionary<string, Dictionary<string, string>> catalogs = new();
    private static readonly Dictionary<string, string> cache = new();
    private static readonly Regex placeholder = new(@"\{(\d+)\}");
    private static readonly string[] sources = Catalog("de").Keys.OrderByDescending(s => placeholder.Replace(s, "").Length).ToArray();
    private static readonly (string Source, Regex Pattern)[] templates = sources
        .Where(s => placeholder.IsMatch(s))
        .Select(s => (s, new Regex("^" + placeholder.Replace(Regex.Escape(s).Replace(@"\{", "{").Replace(@"\}", "}"), m => "(?<p" + m.Groups[1].Value + ">.+?)") + "$", RegexOptions.Singleline, TimeSpan.FromMilliseconds(100))))
        .ToArray();
    internal static string CurrentLanguage { get; private set; } = "en";
    internal static bool IsRightToLeft => CurrentLanguage == "ar";
    internal static void SetLanguage(string code)
    {
        CurrentLanguage = Languages.Any(l => l.Code == code) ? code : "en";
        cache.Clear();
    }
    private static Dictionary<string, string> Catalog(string code)
    {
        if (catalogs.TryGetValue(code, out var catalog)) return catalog;
        using var stream = typeof(Localization).Assembly.GetManifestResourceStream("WardogsTeamselector.Localization." + code + ".json")
            ?? throw new InvalidDataException("Missing language catalog: " + code);
        catalog = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? throw new InvalidDataException("Empty language catalog: " + code);
        catalogs[code] = catalog;
        return catalog;
    }
    internal static string Text(string source)
    {
        if (CurrentLanguage == "de" || string.IsNullOrEmpty(source)) return source;
        if (cache.TryGetValue(source, out var result)) return result;
        result = Translate(source, 0);
        // Long diagnostic sessions must not retain an unbounded number of counters.
        if (cache.Count > 2048) cache.Clear();
        cache[source] = result;
        return result;
    }
    private static string Translate(string source, int depth)
    {
        if (depth > 8 || source.Length == 0) return source;
        var catalog = Catalog(CurrentLanguage);
        if (catalog.TryGetValue(source, out var exact)) return exact;
        foreach (var (key, pattern) in templates)
        {
            var match = pattern.Match(source);
            if (!match.Success) continue;
            var format = catalog.GetValueOrDefault(key, Catalog("en").GetValueOrDefault(key, key));
            return placeholder.Replace(format, m => Translate(match.Groups["p" + m.Groups[1].Value].Value, depth + 1));
        }
        // Original messages also compose prefixes with exception details, team names,
        // timestamps and paths. Translate known pieces, preserving unknown user data.
        foreach (var key in sources.Where(s => !placeholder.IsMatch(s) && s.Trim().Length >= 3))
        {
            if (source.StartsWith(key, StringComparison.Ordinal))
                return catalog.GetValueOrDefault(key, key) + Translate(source[key.Length..], depth + 1);
            if (source.EndsWith(key, StringComparison.Ordinal))
                return Translate(source[..^key.Length], depth + 1) + catalog.GetValueOrDefault(key, key);
        }
        if (source.Contains('\n')) return string.Join("\n", source.Split('\n').Select(s => Translate(s, depth + 1)));
        var timestamp = Regex.Match(source, @"^\d{2}:\d{2}:\d{2}\.\d{3}  ");
        if (timestamp.Success) return timestamp.Value + Translate(source[timestamp.Length..], depth + 1);
        if (source.Contains(" und ")) return string.Join(catalog[" und "], source.Split(" und ").Select(s => Translate(s, depth + 1)));
        if (source.Contains(" · ")) return string.Join(" · ", source.Split(" · ").Select(s => Translate(s, depth + 1)));
        return source;
    }
    internal static string PreferencePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WardogsTeamselector", "language.json");
    internal static string LoadPreference(string? path = null)
    {
        try { return JsonSerializer.Deserialize<string>(File.ReadAllText(path ?? PreferencePath)) ?? "en"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return "en"; }
    }
    internal static void SavePreference(string code, string? path = null)
    {
        path ??= PreferencePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(code));
        File.Move(path + ".tmp", path, true);
    }
}
