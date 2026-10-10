using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WardogsTeamselector;
using WardogsTeamselector.Core;
using WardogsTeamselector.Groups;

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
                var languageSelector = (ComboBox)typeof(MainWindow).GetField("languageSelector", Private)!.GetValue(window)!;
                languageSelector.SelectedItem = WardogsTeamselector.Localization.Languages.Single(language => language.Code == "en");
                Set("checkboxPreferences", new System.Collections.Generic.Dictionary<string, bool>());
                Call("ApplyCheckboxPreferences");
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
                // Demo membership exists only in memory; smoke mode prevents online actions.
                var owner = new GroupMembership { ServiceUrl = "https://groups.example", GroupId = GroupMembership.NewId(), MemberId = GroupMembership.NewId(), Token = GroupMembership.NewToken(), InviteToken = GroupMembership.NewToken(), Name = "Wardogs friends", DisplayName = "Alex", Role = "Owner", Status = "Approved" };
                var profile = new GroupProfile();
                profile.Groups.Add(owner);
                profile.ActiveGroupKey = owner.Key;
                Set("groupProfile", profile);
                Call("ReloadGroupPickers");
                var snapshot = new GroupSnapshot(1, owner.GroupId, owner.Name, 3, 1, "Blue", "Approved", "Owner", owner.MemberId, new() { new(owner.MemberId, "Alex", "Approved", "Owner", 0), new(GroupMembership.NewId(), "Sam", "Approved", "Member", 0), new(GroupMembership.NewId(), "Jamie", "Pending", "Member", 0) });
                Set("selectedGroupSnapshot", snapshot);
                var groupStatus = (TextBlock)typeof(MainWindow).GetField("groupStatus", Private)!.GetValue(window)!;
                groupStatus.Text = "Wardogs friends · Shared team: Blue";
                Call("UpdateGroupControls");
                Call("ShowPage", 1);
                var sharing = (CheckBox)typeof(MainWindow).GetField("shareTeamWithGroup", Private)!.GetValue(window)!;
                sharing.IsChecked = true;
                await Shot("share-team");
                var operationTabs = (TabControl)typeof(MainWindow).GetField("operationTabs", Private)!.GetValue(window)!;
                operationTabs.SelectedIndex = 1;
                await Shot("group-mode");
                Call("ShowPage", 4);
                await Shot("group-management");
                var pages = (TabControl)typeof(MainWindow).GetField("pages", Private)!.GetValue(window)!;
                var management = (ScrollViewer)((TabItem)pages.Items[4]).Content;
                var managementBody = (StackPanel)management.Content;
                foreach (var expander in managementBody.Children.OfType<Expander>())
                    if (expander.Header is string heading && (heading == "Gruppe übertragen" || heading == "PC-Wechsel und Wiederherstellung" || heading == "Transfer group" || heading == "Switch PC and recover access"))
                        expander.IsExpanded = true;
                management.ScrollToBottom();
                await Shot("group-recovery");
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
