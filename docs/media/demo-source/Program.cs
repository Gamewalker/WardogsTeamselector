using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WardogsTeamselector;
using WardogsTeamselector.Core;

// Documentation-only capture harness. Uses the real UI in smoke mode: no global
// hotkeys, profile writes, update downloads or live mouse input. UI fixtures are
// explicitly labeled by the video captions; they are not real controller runs.
internal static class Program
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Pass a screenshot output directory.");
        string output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        var app = new Application();
        var window = new MainWindow(registerGlobalHotkeys: false);
        object? Call(string name, params object?[] values) => typeof(MainWindow).GetMethod(name, Private)!.Invoke(window, values);
        void Set(string name, object? value) => typeof(MainWindow).GetField(name, Private)!.SetValue(window, value);
        async Task Shot(string name, Window? target = null)
        {
            await Task.Delay(200);
            Call("LocalizeInterface", target);
            var view = (FrameworkElement)(target ?? window).Content;
            view.UpdateLayout();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Call("SaveRender", Path.Combine(output, name + ".png"), target);
        }
        window.Loaded += async (_, _) =>
        {
            try
            {
                // Reset only the in-memory fields, never the user's saved profile.
                var defaults = new AppSettings();
                Set("settings", defaults);
                Set("hasSavedProfile", false);
                Set("startupSettingsError", null);
                Call("LoadFields");
                Call("ApplyHotkeys", defaults);
                Call("ClearError");
                Call("ShowPage", 0);
                Call("LoadReference");
                await Shot("setup");
                Call("ShowPage", 1);
                await Shot("operation");
                Call("UpdateRunDisplay", new AutomationSnapshot(RunState.Waiting, Team.Blue, 0, 0, "Warte auf Spielfenster", null, null));
                await Shot("waiting-fixture");
                Call("UpdateRunDisplay", new AutomationSnapshot(RunState.Stopped, null, 0, 0, "Manuell gestoppt", null, null));
                Call("ShowPage", 2);
                await Shot("configuration");
                Call("ShowPage", 3);
                await Shot("diagnostics");
                Call("LoadJoinedReference");
                await Shot("hud-reference");
                Call("ShowPage", 1);
                var about = (Window)Call("BuildAboutWindow")!;
                about.Show();
                await Shot("about", about);
                about.Close();
                File.WriteAllText(Path.Combine(output, "result.txt"), "English documentation captures passed. No real game input was sent.");
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(output, "result.txt"), error.ToString());
                Environment.ExitCode = 1;
            }
            finally { window.Close(); }
        };
        app.Run(window);
    }
}
