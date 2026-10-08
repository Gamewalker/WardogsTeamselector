using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using WardogsTeamselector.Updates;

int checks = 0;
void Check(bool result, string description) { if (!result) throw new Exception(description); checks++; }
string Release(long build = 8, bool draft = false, bool prerelease = false, string? digest = null) => JsonSerializer.Serialize(new {
    tag_name = $"build-{build}-abcdef1", draft, prerelease,
    assets = new[] { new { name = "WardogsTeamselector-win-x64-with-runtime.exe", id = 123, size = 4, digest = digest ?? "sha256:" + new string('a', 64) } }
});
Check(ReleaseUpdate.SelectAsset(Release(), 7, "with-runtime")?.Build == 8, "New build selected");
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
    foreach (var scenario in new[] { "success", "corrupt", "changed", "locked" })
    {
        string directory = Path.Combine(root, scenario + " apostrophe' space ä 漢字");
        Directory.CreateDirectory(directory);
        string source = Path.Combine(directory, "new.exe"), target = Path.Combine(directory, "app.exe");
        byte[] original = [(byte)'M', (byte)'Z', 9, 9];
        File.WriteAllBytes(source, bytes); File.WriteAllBytes(target, original);
        string script = Path.Combine(directory, "install.ps1"), job = script + ".json", result = Path.Combine(directory, "result.txt");
        File.Copy(installer, script);
        File.WriteAllText(job, JsonSerializer.Serialize(new {
            ProcessId = int.MaxValue, Source = source, Target = target, Backup = target + ".previous", Script = script,
            Sha256 = scenario == "corrupt" ? new string('0', 64) : hash,
            TargetSha256 = scenario == "changed" ? new string('0', 64) : Convert.ToHexString(SHA256.HashData(original)), Result = result
        }));
        var start = UpdateInstaller.CreateStartInfo(script, job);
        Check(!start.Environment.ContainsKey("PSModulePath"), "Installer reconstructs Windows PowerShell module paths");
        using var lockedFile = scenario == "locked" ? new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.None) : null;
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync();
        lockedFile?.Dispose();
        string installerResult = File.Exists(result) ? File.ReadAllText(result).Trim() : "Installer produced no result file.";
        Check(process.ExitCode == 0, $"Installer completed: {scenario}. {installerResult}");
        Check(File.ReadAllBytes(target).SequenceEqual(scenario == "success" ? bytes : original), $"Safe replacement: {scenario}. {installerResult}");
        Check(installerResult.Contains(scenario == "success" ? "erfolgreich" : "fehlgeschlagen"), $"Result: {scenario}. {installerResult}");
        if (scenario == "success") Check(File.ReadAllBytes(target + ".previous").SequenceEqual(original), "Original backed up");
        Check(!File.Exists(job) && !File.Exists(script), "Helper cleaned up");
    }
}
finally { Directory.Delete(root, true); }
Console.WriteLine($"UpdateChecks: {checks} checks passed.");

sealed class FakeDownload(byte[] bytes) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.AbsoluteUri != "https://api.github.com/repos/Gamewalker/WardogsTeamselector/releases/assets/123") throw new Exception("Unexpected download URL");
        if (request.Headers.Accept.Single().MediaType != "application/octet-stream") throw new Exception("Missing binary download header");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
    }
}
