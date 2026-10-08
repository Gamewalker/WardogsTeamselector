using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WardogsTeamselector.Updates;

public sealed record UpdateAsset(long Build, long Id, long Size, string Sha256);

public static class ReleaseUpdate
{
    public const string Repository = "Gamewalker/WardogsTeamselector";
    public static UpdateAsset? SelectAsset(string json, long currentBuild, string variant)
    {
        if (variant != "with-runtime" && variant != "without-runtime") return null;
        using var document = JsonDocument.Parse(json);
        var release = document.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean()) return null;
        var match = Regex.Match(release.GetProperty("tag_name").GetString() ?? "", @"^build-(\d+)-[0-9a-f]{7,40}$");
        if (!match.Success || !long.TryParse(match.Groups[1].Value, out long build) || build <= currentBuild) return null;
        string name = $"WardogsTeamselector-win-x64-{variant}.exe";
        foreach (var asset in release.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() != name) continue;
            var digest = asset.TryGetProperty("digest", out var value) ? value.GetString() : null;
            if (digest == null || !Regex.IsMatch(digest, "^sha256:[0-9a-fA-F]{64}$")) throw new InvalidDataException("Für dieses Release fehlt die SHA-256-Prüfsumme.");
            long size = asset.GetProperty("size").GetInt64(), id = asset.GetProperty("id").GetInt64();
            if (size < 2 || size > 512L * 1024 * 1024 || id <= 0) throw new InvalidDataException("Ungültiges Update-Asset.");
            return new(build, id, size, digest[7..]);
        }
        throw new InvalidDataException("Die passende EXE-Variante fehlt im Release.");
    }

    public static async Task VerifyAsync(string path, UpdateAsset asset, CancellationToken cancellation)
    {
        await using var file = File.OpenRead(path);
        if (file.Length != asset.Size || file.ReadByte() != 'M' || file.ReadByte() != 'Z') throw new InvalidDataException("Der Update-Download ist keine vollständige Windows-EXE.");
        file.Position = 0;
        string hash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellation));
        if (!hash.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Die Update-Prüfsumme stimmt nicht überein.");
    }

    public static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WardogsTeamselector-Updater/1.0");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    public static async Task<UpdateAsset?> CheckAsync(HttpClient client, long build, string variant, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases/latest");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await client.SendAsync(request, cancellation);
        response.EnsureSuccessStatusCode();
        return SelectAsset(await response.Content.ReadAsStringAsync(cancellation), build, variant);
    }

    public static async Task DownloadAsync(HttpClient client, UpdateAsset asset, string path, CancellationToken cancellation)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases/assets/{asset.Id}");
            request.Headers.Accept.ParseAdd("application/octet-stream");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
            response.EnsureSuccessStatusCode();
            await using (var source = await response.Content.ReadAsStreamAsync(cancellation))
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                long total = 0;
                int count;
                while ((count = await source.ReadAsync(buffer, cancellation)) > 0)
                {
                    total += count;
                    if (total > asset.Size) throw new InvalidDataException("Update-Download ist größer als angekündigt.");
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellation);
                }
            }
            await VerifyAsync(path, asset, cancellation);
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }
}
