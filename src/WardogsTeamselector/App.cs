using System;
using System.Windows;
using WardogsTeamselector.Groups;
using WardogsTeamselector.Platform;

namespace WardogsTeamselector;
public sealed class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        var smoke = args.Length == 2 && args[0] == "--ui-smoke";
        void RecordFailure(Exception exception)
        {
            System.IO.Directory.CreateDirectory(args[1]);
            System.IO.File.WriteAllText(System.IO.Path.Combine(args[1], "result.txt"), exception.ToString());
            Environment.ExitCode = 1;
        }
        try
        {
        var activation = "";
        if (args.Length > 0 && args[0] == "--join")
        {
            try
            {
                if (args.Length != 2) throw new ArgumentException("Ungültiger Einladungslink.");
                GroupInvitationActivation.Parse(args[1]);
                activation = args[1];
            }
            catch (ArgumentException ex) { LocalizedMessageBox.Show(ex.Message, "Wardogs"); return; }
        }
        using var activationHost = smoke ? null : new InvitationActivationHost();
        if (activationHost != null && !activationHost.IsPrimary)
        {
            try { activationHost.ForwardAsync(activation).GetAwaiter().GetResult(); }
            catch (Exception) { LocalizedMessageBox.Show("Die laufende App konnte nicht erreicht werden. App neu starten und Einladung erneut öffnen.", "Wardogs"); }
            return;
        }
        if (!smoke)
        {
            try { InvitationActivationHost.RegisterProtocol(); }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Security.SecurityException)
            { System.Diagnostics.Trace.WriteLine("Invitation protocol registration failed: " + ex.Message); }
        }
        var app = new App();
        app.DispatcherUnhandledException += (_, e) => {
            if (smoke) { RecordFailure(e.Exception); app.Shutdown(1); }
            else LocalizedMessageBox.Show(e.Exception.Message, "Wardogs – Fehler");
            e.Handled = true;
        };
        var window = new MainWindow(registerGlobalHotkeys: !(args.Length == 2 && args[0] == "--ui-smoke"));
        if (activationHost != null)
        {
            _ = activationHost.ListenAsync(value => app.Dispatcher.BeginInvoke(() => window.ReceiveInvitationActivation(value)));
            window.Loaded += (_, _) => { if (activation.Length != 0) window.ReceiveInvitationActivation(activation); };
        }
        if (args.Length == 2 && args[0] == "--ui-smoke") window.Loaded += async (_, _) => { try { await window.CaptureSmokeImages(args[1]); System.IO.File.WriteAllText(System.IO.Path.Combine(args[1], "result.txt"), "GUI smoke passed"); } catch (Exception ex) { System.IO.Directory.CreateDirectory(args[1]); System.IO.File.WriteAllText(System.IO.Path.Combine(args[1], "result.txt"), ex.ToString()); Environment.ExitCode = 1; } finally { window.Close(); } };
        app.Run(window);
        }
        catch (Exception ex) when (smoke) { RecordFailure(ex); }
    }
}
