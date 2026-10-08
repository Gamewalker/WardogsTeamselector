using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using WardogsTeamselector.Updates;

// The installer launches this executable as a harmless restart fixture.
if (Environment.GetEnvironmentVariable("WARDOGS_UPDATE_RESTART_MARKER") is string marker)
{
    File.WriteAllText(marker, Environment.CurrentDirectory);
    return;
}

int checks = 0;
void Check(bool result, string description) { if (!result) throw new Exception(description); checks++; }
string Release(long build = 8, bool draft = false, bool prerelease = false, string? digest = null) => JsonSerializer.Serialize(new {
    tag_name = $"build-{build}-abcdef1", draft, prerelease,
    assets = new[] { new { name = "WardogsTeamselector-win-x64-with-runtime.exe", id = 123, size = 4, digest = digest ?? "sha256:" + new string('a', 64) } }
});
Check(ReleaseUpdate.SelectAsset(Release(), 7, "with-runtime")?.Build == 8, "New build selected");
Check(ReleaseUpdate.SelectAsset(Release(), 7, "with-runtime")?.DownloadUrl == "https://github.com/Gamewalker/WardogsTeamselector/releases/download/build-8-abcdef1/WardogsTeamselector-win-x64-with-runtime.exe", "Download is pinned to the selected release on the public CDN");
foreach (bool legacy in new[] { false, true })
{
    using var handler = new FakeRelease(Release(), legacy);
    using var client = new HttpClient(handler);
    Check((await ReleaseUpdate.CheckAsync(client, 7, "with-runtime", CancellationToken.None))?.Build == 8, "Manifest and legacy release checks select the update");
    Check(handler.Requests == (legacy ? 2 : 1), "Manifest check avoids the rate-limited API; older releases use fallback");
}
Check(ReleaseUpdate.SelectAsset(Release(), 8, "with-runtime") == null, "Equal build ignored");
Check(ReleaseUpdate.SelectAsset(Release(), 9, "with-runtime") == null, "Downgrade ignored");
Check(ReleaseUpdate.SelectAsset(Release(draft: true), 7, "with-runtime") == null, "Draft ignored");
Check(ReleaseUpdate.SelectAsset(Release(prerelease: true), 7, "with-runtime") == null, "Prerelease ignored");
Check(ReleaseUpdate.SelectAsset(Release(), 7, "unknown") == null, "Unknown variant ignored");
try { ReleaseUpdate.SelectAsset(Release(), 7, "without-runtime"); throw new Exception("Variant silently changed"); } catch (InvalidDataException) { checks++; }
try { ReleaseUpdate.SelectAsset(Release(digest: "invalid"), 7, "with-runtime"); throw new Exception("Invalid digest accepted"); } catch (InvalidDataException) { checks++; }
string root = Path.Combine(Path.GetTempPath(), "wardogs-update-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    byte[] bytes = [(byte)'M', (byte)'Z', 1, 2];
    string hash = Convert.ToHexString(SHA256.HashData(bytes));
    var asset = new UpdateAsset(8, 123, bytes.Length, hash);
    string download = Path.Combine(root, "download.exe");
    using (var client = new HttpClient(new FakeDownload(bytes, cdn: true)))
        await ReleaseUpdate.DownloadAsync(client, asset with { DownloadUrl = "https://github.com/Gamewalker/WardogsTeamselector/releases/download/build-8-abcdef1/WardogsTeamselector-win-x64-with-runtime.exe" }, download, CancellationToken.None);
    Check(File.ReadAllBytes(download).SequenceEqual(bytes), "CDN download verified without API access");
    File.Delete(download);
    using (var client = new HttpClient(new FakeDownload(bytes)))
        await ReleaseUpdate.DownloadAsync(client, asset, download, CancellationToken.None);
    Check(File.ReadAllBytes(download).SequenceEqual(bytes), "Download verified");
    File.Delete(download);
    using (var client = new HttpClient(new FakeDownload(bytes)))
    {
        try { await ReleaseUpdate.DownloadAsync(client, asset with { Sha256 = new string('0', 64) }, download, CancellationToken.None); throw new Exception("Corrupt hash accepted"); }
        catch (InvalidDataException) { Check(!File.Exists(download), "Bad download removed"); }
        try { await ReleaseUpdate.DownloadAsync(client, asset with { Size = 3 }, download, CancellationToken.None); throw new Exception("Oversize accepted"); }
        catch (InvalidDataException) { Check(!File.Exists(download), "Oversize removed"); }
    }
    using (var client = new HttpClient(new FakeDownload([(byte)'{', (byte)'}', 1, 2])))
    {
        try { await ReleaseUpdate.DownloadAsync(client, asset, download, CancellationToken.None); throw new Exception("Non-EXE accepted"); }
        catch (InvalidDataException) { Check(!File.Exists(download), "Non-EXE removed"); }
    }
    string installer = Path.GetFullPath("src/WardogsTeamselector/Updates/InstallUpdate.ps1");
    if (args.Contains("--live"))
    {
        using var liveClient = ReleaseUpdate.CreateClient();
        var liveAsset = await ReleaseUpdate.CheckAsync(liveClient, 0, "without-runtime", CancellationToken.None);
        Check(liveAsset != null, "Public release is accessible without authentication");
        await ReleaseUpdate.DownloadAsync(liveClient, liveAsset!, download, CancellationToken.None);
        Check(File.Exists(download), "Real public release downloaded and SHA-256 verified");
        File.Delete(download);
    }
    foreach (var scenario in new[] { "success", "corrupt", "changed", "locked", "restart", "restart-corrupt", "restart-failed" })
    {
        string directory = Path.Combine(root, scenario + " apostrophe' space ä 漢字");
        Directory.CreateDirectory(directory);
        string source = Path.Combine(directory, "new.exe"), target = Path.Combine(directory, "app.exe");
        byte[] original = [(byte)'M', (byte)'Z', 9, 9];
        bool restart = scenario.StartsWith("restart");
        bool success = scenario is "success" or "restart" or "restart-failed";
        byte[] updateBytes = restart && scenario != "restart-failed" ? File.ReadAllBytes(Environment.ProcessPath!) : bytes;
        if (scenario == "restart-corrupt") original = updateBytes;
        if (restart)
        {
            foreach (string dependency in Directory.GetFiles(AppContext.BaseDirectory, "UpdateChecks.*").Where(path => !path.EndsWith(".exe")))
                File.Copy(dependency, Path.Combine(directory, Path.GetFileName(dependency)));
        }
        File.WriteAllBytes(source, updateBytes); File.WriteAllBytes(target, original);
        string script = Path.Combine(directory, "install.ps1"), job = script + ".json", result = Path.Combine(directory, "result.txt");
        File.Copy(installer, script);
        File.WriteAllText(job, JsonSerializer.Serialize(new {
            ProcessId = int.MaxValue, Source = source, Target = target, Backup = target + ".previous", Script = script,
            Sha256 = scenario is "corrupt" or "restart-corrupt" ? new string('0', 64) : Convert.ToHexString(SHA256.HashData(updateBytes)),
            Restart = restart,
            TargetSha256 = scenario == "changed" ? new string('0', 64) : Convert.ToHexString(SHA256.HashData(original)), Result = result
        }));
        var start = UpdateInstaller.CreateStartInfo(script, job);
        string restartMarker = Path.Combine(directory, "restarted.txt");
        if (restart) start.Environment["WARDOGS_UPDATE_RESTART_MARKER"] = restartMarker;
        Check(!start.Environment.ContainsKey("PSModulePath"), "Installer reconstructs Windows PowerShell module paths");
        using var lockedFile = scenario == "locked" ? new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.None) : null;
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync();
        lockedFile?.Dispose();
        string installerResult = File.Exists(result) ? File.ReadAllText(result).Trim() : "Installer produced no result file.";
        Check(process.ExitCode == 0, $"Installer completed: {scenario}. {installerResult}");
        Check(File.ReadAllBytes(target).SequenceEqual(success ? updateBytes : original), $"Safe replacement: {scenario}. {installerResult}");
        Check(installerResult.Contains(success ? "erfolgreich" : "fehlgeschlagen"), $"Result: {scenario}. {installerResult}");
        if (success) Check(File.ReadAllBytes(target + ".previous").SequenceEqual(original), "Original backed up");
        Check(!File.Exists(job) && !File.Exists(script), "Helper cleaned up");
        if (scenario == "restart")
        {
            for (int attempt = 0; attempt < 100 && !File.Exists(restartMarker); attempt++) await Task.Delay(100);
            Check(File.Exists(restartMarker), "Installed app restarted");
            Check(File.ReadAllText(restartMarker) == directory, "Restart uses app directory");
        }
        if (scenario == "restart-corrupt")
        {
            await Task.Delay(1000);
            Check(!File.Exists(restartMarker), "Failed update never restarts");
        }
        if (scenario == "restart-failed") Check(installerResult.Contains("Neustart fehlgeschlagen"), "Restart failure preserves successful installation and explains manual recovery");
    }
}
finally { Directory.Delete(root, true); }
Console.WriteLine($"UpdateChecks: {checks} checks passed.");

sealed class FakeRelease(string json, bool legacy) : HttpMessageHandler
{
    public int Requests { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests++;
        string expected = Requests == 1 ? "https://github.com/Gamewalker/WardogsTeamselector/releases/latest/download/update.json" : "https://api.github.com/repos/Gamewalker/WardogsTeamselector/releases/latest";
        if (request.RequestUri?.AbsoluteUri != expected || (!legacy && Requests > 1)) throw new Exception("Unexpected release request");
        return Task.FromResult(new HttpResponseMessage(legacy && Requests == 1 ? HttpStatusCode.NotFound : HttpStatusCode.OK) { Content = new StringContent(json) });
    }
}

sealed class FakeDownload(byte[] bytes, bool cdn = false) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string expected = cdn ? "https://github.com/Gamewalker/WardogsTeamselector/releases/download/build-8-abcdef1/WardogsTeamselector-win-x64-with-runtime.exe" : "https://api.github.com/repos/Gamewalker/WardogsTeamselector/releases/assets/123";
        if (request.RequestUri?.AbsoluteUri != expected) throw new Exception("Unexpected download URL");
        if (request.Headers.Accept.Single().MediaType != "application/octet-stream") throw new Exception("Missing binary download header");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
    }
}
