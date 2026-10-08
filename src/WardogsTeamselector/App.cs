using System;
using System.Windows;

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
        var app = new App();
        app.DispatcherUnhandledException += (_, e) => {
            if (smoke) { RecordFailure(e.Exception); app.Shutdown(1); }
            else LocalizedMessageBox.Show(e.Exception.Message, "Wardogs – Fehler");
            e.Handled = true;
        };
        var window = new MainWindow(registerGlobalHotkeys: !(args.Length == 2 && args[0] == "--ui-smoke"));
        if (args.Length == 2 && args[0] == "--ui-smoke") window.Loaded += async (_, _) => { try { await window.CaptureSmokeImages(args[1]); System.IO.File.WriteAllText(System.IO.Path.Combine(args[1], "result.txt"), "GUI smoke passed"); } catch (Exception ex) { System.IO.Directory.CreateDirectory(args[1]); System.IO.File.WriteAllText(System.IO.Path.Combine(args[1], "result.txt"), ex.ToString()); Environment.ExitCode = 1; } finally { window.Close(); } };
        app.Run(window);
        }
        catch (Exception ex) when (smoke) { RecordFailure(ex); }
    }
}
