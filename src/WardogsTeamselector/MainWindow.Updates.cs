using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WardogsTeamselector.Updates;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    private readonly TextBlock updateStatus = new() { TextWrapping = TextWrapping.Wrap };
    private readonly CheckBox automaticUpdates = new() { Content = "Updates automatisch herunterladen" };
    private readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromHours(6) };
    private readonly CancellationTokenSource updateCancellation = new();
    private UpdatePreferences updatePreferences = new();
    private bool updateBusy;
    private string? stagedUpdate;
    private UpdateAsset? stagedAsset;
    private static string Metadata(string key) => typeof(App).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value ?? "";
    private static long CurrentBuild => long.TryParse(Metadata("ReleaseBuild"), out var number) ? number : 0;

    private UIElement BuildUpdates()
    {
        var panel = new StackPanel();
        panel.Children.Add(Heading("Automatische Updates"));
        panel.Children.Add(Hint("Neue Versionen werden beim Start und alle sechs Stunden geprüft und beim Beenden installiert. Die bisherige EXE bleibt als .previous erhalten."));
        automaticUpdates.Foreground = Foreground;
        panel.Children.Add(automaticUpdates);
        panel.Children.Add(Hint("Updates kommen aus den öffentlichen GitHub-Releases. Eine Anmeldung ist nicht erforderlich."));
        panel.Children.Add(Button("Update-Einstellungen speichern", () =>
        {
            try
            {
                updatePreferences.Enabled = automaticUpdates.IsChecked == true;
                updatePreferences.Save();
                if (!updatePreferences.Enabled) DiscardStagedUpdate();
                updateStatus.Text = "Update-Einstellungen gespeichert.";
                if (updatePreferences.Enabled) _ = CheckForUpdates();
            }
            catch { updateStatus.Text = "Update-Einstellungen konnten nicht gespeichert werden."; }
        }));
        panel.Children.Add(Button("Jetzt auf Updates prüfen", () => _ = CheckForUpdates(manual: true)));
        updateStatus.Foreground = Muted;
        panel.Children.Add(updateStatus);
        return panel;
    }

    private void InitializeUpdates()
    {
        if (smokeMode) { updateStatus.Text = "Updates sind im GUI-Prüflauf deaktiviert."; return; }
        try
        {
            updatePreferences = UpdatePreferences.Load();
            automaticUpdates.IsChecked = updatePreferences.Enabled;
            string result = Path.Combine(UpdatePreferences.DirectoryPath, "update-result.txt");
            if (File.Exists(result)) { updateStatus.Text = File.ReadAllText(result); AddLog(updateStatus.Text.Trim()); File.Delete(result); }
            updateTimer.Tick += async (_, _) => await CheckForUpdates();
            updateTimer.Start();
            _ = CheckForUpdates();
        }
        catch { updateStatus.Text = "Update-Einstellungen nicht lesbar. Update-Einstellungen neu speichern."; }
    }

    private async Task CheckForUpdates(bool manual = false)
    {
        if (smokeMode || closing || updateBusy || stagedUpdate != null || (!manual && !updatePreferences.Enabled)) return;
        string variant = Metadata("UpdateVariant");
        if (CurrentBuild == 0 || string.IsNullOrEmpty(variant)) { updateStatus.Text = "Automatische Updates sind nur in veröffentlichten Release-EXEs verfügbar."; return; }
        updateBusy = true;
        string? download = null;
        try
        {
            updateStatus.Text = "Updates werden geprüft …";
            using var client = ReleaseUpdate.CreateClient();
            var asset = await ReleaseUpdate.CheckAsync(client, CurrentBuild, variant, updateCancellation.Token);
            if (asset == null) { updateStatus.Text = $"Build {CurrentBuild} ist aktuell."; return; }
            updateStatus.Text = $"Build {asset.Build} wird heruntergeladen …";
            Directory.CreateDirectory(UpdatePreferences.DirectoryPath);
            download = Path.Combine(UpdatePreferences.DirectoryPath, "update-" + Guid.NewGuid().ToString("N") + ".exe");
            await ReleaseUpdate.DownloadAsync(client, asset, download, updateCancellation.Token);
            if (closing || (!manual && !updatePreferences.Enabled)) return;
            stagedUpdate = download; stagedAsset = asset; download = null;
            updateStatus.Text = $"Build {asset.Build} ist geprüft und wird beim Beenden installiert. Beim nächsten Start ist die neue Version aktiv.";
        }
        catch (OperationCanceledException) { if (!closing) updateStatus.Text = "Updateprüfung abgebrochen oder Zeitlimit erreicht."; }
        catch (System.Net.Http.HttpRequestException) { updateStatus.Text = "Updateprüfung nicht möglich. Internetverbindung prüfen; später wird erneut geprüft."; }
        catch (InvalidDataException ex) { updateStatus.Text = ex.Message; }
        catch { updateStatus.Text = "Update konnte nicht vorbereitet werden. Die bisherige Version bleibt verfügbar."; }
        finally
        {
            if (download != null && File.Exists(download)) { try { File.Delete(download); } catch { } }
            updateBusy = false;
        }
    }

    private void DiscardStagedUpdate()
    {
        if (stagedUpdate != null) { try { File.Delete(stagedUpdate); } catch { } }
        stagedUpdate = null; stagedAsset = null;
    }

    private void FinishUpdates()
    {
        updateTimer.Stop(); updateCancellation.Cancel();
        if (stagedUpdate == null || stagedAsset == null) return;
        try
        {
            string target = Environment.ProcessPath ?? throw new InvalidOperationException();
            string script = Path.Combine(UpdatePreferences.DirectoryPath, "install-" + Guid.NewGuid().ToString("N") + ".ps1");
            using (var resource = typeof(App).Assembly.GetManifestResourceStream("WardogsTeamselector.InstallUpdate")!)
            using (var output = File.Create(script)) resource.CopyTo(output);
            string job = script + ".json";
            using var targetFile = File.OpenRead(target);
            string targetHash = Convert.ToHexString(SHA256.HashData(targetFile));
            targetFile.Dispose();
            File.WriteAllText(job, JsonSerializer.Serialize(new {
                ProcessId = Environment.ProcessId, Source = stagedUpdate, Target = target,
                Backup = target + ".previous", Sha256 = stagedAsset.Sha256,
                TargetSha256 = targetHash,
                Result = Path.Combine(UpdatePreferences.DirectoryPath, "update-result.txt"), Script = script
            }));
            Process.Start(UpdateInstaller.CreateStartInfo(script, job))?.Dispose();
        }
        catch
        {
            DiscardStagedUpdate();
            LocalizedMessageBox.Show(this, "Das Update konnte nicht gestartet werden. Die bisherige Version bleibt erhalten.", "Update");
        }
    }
}
